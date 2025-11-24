# Application Base (Angular 18 + ASP.NET Core 8 + RavenDB)

Base monolítica em camadas com autenticação JWT, CRUD de usuários, i18n (pt-BR/pt/en-US/en) e tratamento unificado de erros (ProblemDetails). Este README concentra toda a documentação: arquitetura, padrões, arquivos-chave, fluxos e guia para estender e operar o sistema.

---

## 1. Visão Geral
- **Propósito:** Boilerplate para apps web com autenticação, gestão de usuários e localização.
- **Arquitetura:** Onion/Clean (Domain → Service → Infrastructure → Web/API → Client).
- **Tecnologias:** ASP.NET Core 8, Angular 18, RavenDB, JWT, FluentValidation, ngx-translate, Bootstrap, Toastr.
- **Funcionalidades:** Login/refresh de tokens, CRUD de usuários, validação com mensagens localizadas, erros em RFC 7807.

## 2. Mapa de Arquitetura (alto nível)
- **Camadas:** Client (Angular) → Web/API (Controllers, Middlewares, Filters) → Service (casos de uso) → Infrastructure (Repositórios Raven) → Domain (modelos/validações/exceções/resources).
- **Fluxos:** SPA chama `/api/authentication/*` e `/api/user/*`; API valida JWT/cultura; Services consultam repositórios; middleware salva sessão Raven e formata erros.

## 3. Camadas e Arquivos-Chave
### 3.1 Web/API (`Application.Web`)
- `Program.cs`: pipeline (RequestLocalization pt-BR default; pt/en-US/en) → `ProblemDetailsMiddleware` → `MiddlewareServiceRavenDbStore` → static/CORS/HTTPS/Auth → `MapControllers` → SPA fallback.
- Controllers: `AuthenticationController` (400/401/200 login/refresh), `UserController` (400/404/201/200/204 CRUD) com mensagens via `IStringLocalizer<SharedResource>`.
- Middlewares: `ProblemDetailsMiddleware.cs` (exceções → ProblemDetails i18n + traceId), `MiddlewareServiceRavenDbStore.cs` (abre sessão Raven e salva ao fim).
- Filtro: `ValidationProblemDetailsFilter.cs` (ModelState inválido → 400 ValidationProblemDetails i18n + traceId).

### 3.2 Service (`Application.Service`)
- `UserService.cs`: valida `User` (UserValidator), checa e-mail duplicado (ConflictException), define `DateJoined`, salva/atualiza via repo.
- `TokenService.cs`: autentica via `IUserService`, gera JWT (claims Name/Role, expira 2h) e refresh (GUID Base64, expira 7 dias), atualiza usuário.
- Interfaces: `IUserService`, `ITokenService` para DI/testes.

### 3.3 Domain (`Application.Domain`)
- Modelos/DTOs: `User`, `UserAccount`, `UserProfile`, `LoginDto`, `RefreshRequestDto`, `TokenResponseDto`, `ApplicationConstants` (env JWT/Raven).
- Validação: `Validator/UserValidator.cs` (e-mail obrigatório/válido/único; senha forte; nome; datas; roles permitidas). Mensagens em `Resources/SharedResource.resx/.en.resx` via `SharedResource`.
- Exceções: `DomainException` base + `ConflictException`, `NotFoundException`, `ForbiddenException`, `BusinessException`.

### 3.4 Infrastructure (`Application.Infrastructure`)
- `Repository/UserRepository.cs`: Add/Update/Delete/GetById/GetByEmail/GetByRole/GetByRefreshToken/GetAll usando `IServiceRavenDB` (Session/AsyncSession/Store).
- `ConfigurationDb/DocumentStoreHolderAlternative.cs`: cria `DocumentStore` a partir de env vars.

### 3.5 Client (`Application.Client`)
- Angular SPA com i18n (pt/en), interceptors (`problem.interceptor`, `loading.interceptor`), páginas Auth/Register/Home; consumo de `/api/authentication/*` e `/api/user/*`.

### 3.6 Tests (`Application.Test`)
- Controllers: `AuthenticationControllerTests`, `UserControllerTests`.
- Services: `UserServiceTests` (Raven embutido).
- Validators: `UserValidatorTests`.
- Middlewares: `ProblemDetailsMiddlewareTests`.
- Setup: `Setup/BaseTest.cs` (Raven embutido, DI, cultura pt).

## 4. Padrões de Projeto (implementação)
- **Onion/Clean:** Dependências apontam para o domínio; controllers → services → interfaces de repo; infra implementa as interfaces.
- **Repository:** `IUserRepository` abstrai Raven; `UserRepository` implementa; services ignoram o DB.
- **DI:** Registros em `DependencyInjectionModule*.cs`/`Program.cs`; injeção por construtor; facilita fakes/mocks em testes.
- **ProblemDetails (RFC 7807):** Middleware + filtro geram `application/problem+json` padronizado, com i18n e `traceId`.
- **Validação:** FluentValidation (`UserValidator`) + ModelState; mensagens em resx.
- **i18n:** `SharedResource` + resx pt/en; `Program.cs` configura culturas; controllers/validators/middlewares usam localizer; SPA também usa i18n.
- **JWT:** Config em `Program.cs`; geração em `TokenService`; uso de `[Authorize]` e `Roles`.
- **SOLID:** SRP (responsabilidades separadas), OCP/DIP (interfaces + DI), ISP (interfaces coesas), LSP (exceções de domínio tratadas uniformemente).

## 5. Fluxos Importantes
- Autenticação: `POST /api/authentication/login` (200 tokens | 400 se payload nulo | 401 se credencial inválida); `POST /api/authentication/refresh` (200 novo par | 401 inválido/expirado).
- Usuários: `POST /api/user/add` (201 sucesso | 400 payload | 409 e-mail duplicado via ConflictException → ProblemDetails); `PUT /api/user/update` (400 id vazio | 404 se não existe | 200 sucesso); `GET /api/user/get/{id}`, `GET /api/user/email/{email}`, `GET /api/user/role/{role}`, `GET /api/user/all` (200 ou 404); `DELETE /api/user/delete/{id}` (204 ou 404).
- Erros: ModelState inválido → 400 ValidationProblemDetails (filtro); exceções → ProblemDetails (middleware) com status apropriado e i18n; inclui `traceId`.
- Localização: `RequestLocalization` aplica cultura do `Accept-Language` (default pt-BR) e propaga nos headers/respostas.

## 6. Banco de Dados (RavenDB)
- Documento `User`:  
  - `Account`: Email (único em regra), Password, Role, DateJoined?, LastLogin?, RefreshToken?, RefreshTokenExpiry?  
  - `Profile`: Name, DateOfBirth?
- Consultas: por email/role/refreshToken/id via `AsyncSession.Query<T>()`.
- Persistência: `MiddlewareServiceRavenDbStore` mantém sessão por request e chama `SaveChangesAsync`.

## 7. Configuração e Dependências
- Env obrigatórias:  
  - JWT: `JWT_AUDIENCE`, `JWT_ISSUER`, `JWT_SIGNING_KEY`  
  - RavenDB: `RAVENDBSETTINGS_DATABASE_NAME`, `RAVENDBSETTINGS_URLS`, `RAVENDBSETTINGS_CERTIFICATE_SUBJECT`
- Config internas: Resources i18n; culturas pt-BR/pt/en-US/en; CORS permissivo; HTTPS redirection; SPA fallback; RequestLocalization antes dos middlewares.

## 8. Execução, Build e Testes
- Backend: `dotnet restore Application.sln && dotnet build Application.sln && dotnet run --project Application.Web/Application.Api.csproj`
- Frontend: `cd Application.Client && npm install && npm start` (dev); build `npm run build`
- Testes: `dotnet test Application.sln`

## 9. Rotas Principais
- Auth: `POST /api/authentication/login`, `POST /api/authentication/refresh`
- Usuários: `POST /api/user/add`, `PUT /api/user/update`, `DELETE /api/user/delete/{id}`,
  `GET /api/user/get/{id}`, `GET /api/user/email/{email}`, `GET /api/user/role/{role}`, `GET /api/user/all`

## 10. Como Estender (exemplo Product)
1) Domain: `Product.cs`, `ProductValidator.cs`, mensagens em resx, `IProductRepository`.  
2) Infrastructure: `ProductRepository` (Raven).  
3) Service: `ProductService` com regras; registre em `DependencyInjectionModuleService`.  
4) Web: `ProductController` com rotas/status/mensagens via localizer; use ProblemDetails.  
5) Tests: validator + service (Raven embutido) + controller (fakes).  
6) Client: traduções em `public/i18n`, serviços/componentes Angular se aplicável.

## 11. Dicas de Desenvolvimento (fluxo sugerido)
1. Comece no domínio: modelos, resx, validador.  
2. Implemente caso de uso no service; lance exceções de domínio.  
3. Ajuste repositório/queries na infra se precisar.  
4. Exponha via controller com status/mensagens; deixe middleware/filtro tratarem erros.  
5. Crie testes (validator/service/controller/middleware).  
6. Atualize i18n no back e no front.  
7. Rode `dotnet test` e, se front alterado, `npm test` (se configurado).

## 12. Glossário
- **ProblemDetails:** Erro padronizado RFC 7807 (`application/problem+json`).  
- **ValidationProblemDetails:** Variante com erros de campo ModelState.  
- **DomainException:** Exceção base de negócio com status específico.  
- **Localizer (`IStringLocalizer<SharedResource>`):** Resolve textos i18n (pt/en).  
- **RefreshToken:** Token de renovação com expiração armazenado no usuário.  
- **SharedResource.resx/.en.resx:** Recursos de mensagens de validação/erro/sucesso.  
- **MiddlewareServiceRavenDbStore:** Garante sessão/SaveChanges Raven por request.  
- **UserValidator:** Regras de e-mail, senha, nome, datas e role.  
- **ApplicationConstants:** Lê env vars JWT/Raven.

---

# Tipos de Commit

## feat
Nova funcionalidade.
Exemplo:
`feat(api): adicionar endpoint de autenticação`

## fix
Correção de bug.
Exemplo:
`fix(domain): corrigir cálculo de validação`

## docs
Alterações na documentação.
Exemplo:
`docs: atualizar guia de instalação`

## style
Mudanças que não afetam lógica (espaços, formatação, lint).
Exemplo:
`style: aplicar padrão de formatação no projeto`

## refactor
Refatoração sem mudar comportamento.
Exemplo:
`refactor(service): simplificar método de processamento`

## perf
Melhorias de performance.
Exemplo:
`perf(api): reduzir tempo de resposta`

## test
Adição ou atualização de testes.
Exemplo:
`test(app): incluir testes de integração`

## build
Mudanças em build, dependências ou ferramentas.
Exemplo:
`build: atualizar dependências do Angular`

## ci
Alterações em pipelines de CI/CD.
Exemplo:
`ci: ajustar workflow do GitHub Actions`

## chore
Tarefas internas sem alteração funcional.
Exemplo:
`chore: ajustar scripts de automação`

## revert
Reversão de commit anterior.
Exemplo:
`revert: desfazer commit da feature de login`


MIT — contribuições são bem-vindas!﻿
