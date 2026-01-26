# Application Base
Base monolítica pronta para produção com ASP.NET Core 8, Angular 18 e RavenDB. Inclui autenticação JWT com rotação de refresh tokens, autorização por permissões, gerenciamento completo de usuários, recuperação de senha, configuração de SMTP (com criptografia de segredo), i18n full-stack e tratamento unificado de erros via ProblemDetails.

## Visão Geral
- **Arquitetura:** Clean/Onion (Domain -> Service -> Infrastructure -> Web/API -> Client).
- **Stack:** ASP.NET Core 8, Angular 18, RavenDB, FluentValidation, JWT Bearer, ngx-translate, ngx-spinner, FontAwesome, Bootstrap.
- **Principais entregas:** CRUD de usuarios, permissao granular, perfil com avatar (attachment RavenDB), troca de senha e recuperacao completa, SMTP com segredo criptografado, i18n full-stack e ProblemDetails, modulo de estudantes (CRUD + importacao/exportacao XLSX), modulo de educacao (escolas/programas/turmas/UCs, busca e importacao assincrona, alunos por UC).

## Funcionalidades
- Autenticacao JWT (2h) + refresh token rotativo (7d) com hash PBKDF2.
- Cadastro/login, CRUD de usuarios, consulta por permissao e lista de permissoes disponiveis.
- Perfil: edicao de dados, foto (upload multipart ou data URL), offsets/zoom do avatar, funcoes/cargo/departamento/organizacao/localizacao.
- Seguranca da conta: geracao/validacao/uso de codigo de recuperacao (6 digitos, TTL 10 min, cooldown 1 min, 5 tentativas) e troca de senha.
- Administracao: painel Angular para usuarios e gestao de permissoes, configuracao de servicos (SMTP).
- Estudantes: CRUD, importacao XLSX, exportacao XLSX, status ativo/suspenso, filtros por texto e ativo.
- Educacao: catalogo de escolas/programas/turmas/UCs, busca paginada, alunos por UC, importacao assincrona de cursos.
- E-mail: envio de reset/recovery, teste de SMTP e persistencia de credenciais criptografadas.
- Observabilidade de erros: ProblemDetails com `traceId`, ModelState -> ValidationProblemDetails, i18n backend/frontend.
- UX: toasts centralizados, loading global, tema claro/escuro, shell dashboard com navegacao protegida por permissoes.
- Importacoes: cursos (JSON) enfileirados via Hangfire com notificacoes in-app; relatorios XLSX com caches para reduzir round-trips.

## Arquitetura e Camadas (Backend)
- **Domain (`Application.Domain`):**
  - Modelos: `User`, `ApplicationPermissions`, `ApplicationConstants`, `Configurations`/`EmailSettings`, `Student`, `School`, `ProgramDocument`, `ClassDocument`, `UcDocument`, `EducationImport`.
  - DTOs: auth/usuario/perfil, `CreateStudentDto`/`UpdateStudentDto`, `StudentImportResult`, `PaginationQuery`.
  - Validacoes: `UserValidator`, `PasswordValidator`, `StudentValidator`.
  - Excecoes: `DomainException` + `Conflict/NotFound/Forbidden/Business/ConfigurationException`.
  - i18n: `SharedResource` (.resx pt/en) e `SharedResourceProvider` para usos estaticos.

- **Service (`Application.Service`):**
  - `UserService`, `TokenService`, `EmailService`, `SettingsService`.
  - `StudentService`: CRUD, validacao, importacao/exportacao XLSX, regras de status.
  - `EducationService`: escolas/programas/turmas/UCs, busca paginada, alunos por UC, enfileiramento de importacao.
  - Background: `EducationImportHangfireJob` + `EducationImportProcessor` (fallback: `EducationImportBackgroundService`).
  - Seguranca: `SecureHash` (PBKDF2) e `SecretEncryptionService` (AES key derivada do env `APP_SECRET_ENCRYPTION_KEY`).
  - DI: registrado em `DependencyInjectionModuleService`.

- **Infrastructure (`Application.Infrastructure`):**
  - DocumentStore RavenDB: `DocumentStoreHolderAlternative` (URLs via `RAVENDBSETTINGS_URLS`, certificado por assunto, convencoes, criacao de DB, indices).
  - Repositorios: `UserRepository`, `SettingsRepository`, `StudentRepository`, `EducationRepository`, `EducationImportRepository`.
  - Indices: `User_ByEmail`, `UcSearchIndex`, `Classes_BySchoolAndProgram`, `ClassUcMaps_ByClass`, `StudentUcMaps_ByUc`.
  - DI: `DependencyInjectionModuleInfra` registra `IDocumentStore`, `IServiceRavenDB` e repositorios.

- **Web/API (`Application.Web`):**
  - Pipeline: localization (pt-BR default; pt/en-US/en) -> `ProblemDetailsMiddleware` -> `MiddlewareServiceRavenDbStore` -> static files -> CORS -> HTTPS -> AuthZ -> controllers -> SPA fallback.
  - Autenticacao: JWT Bearer com env (`JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SIGNING_KEY`). Policies por permissao (`ApplicationPermissions.All`).
  - Controllers:
    - `AuthenticationController`: `POST /api/authentication/login`, `POST /api/authentication/refresh`.
    - `UserController`: cadastro publico, CRUD, perfil, permissao e recuperacao.
    - `EmailController`: reset, settings SMTP e teste.
    - `StudentsController`: CRUD, importacao/exportacao XLSX.
    - `EducationController`: escolas/programas/turmas/UCs, busca, importacao e alunos por UC.
  - Swagger esta comentado/desativado.

## Camada Client (Angular 18 - `Application.Client`)
- **Roteamento:** home (`/home`), perfil (`/profile`), admin (`/admin/*`), estudantes (`/students`), educacao (`/education`), auth/login, registro, forgot/reset password. Guards: `AuthGuard` + `PermissionGuard` (JWT decode de claim `permissions`).
- **Componentes/Paginas:**
  - `AppComponent`: shell dashboard com side-nav, topbar, menu de perfil, toasts e spinner.
  - Auth: `AuthComponent` (login), `RegisterComponent`, `ForgotPasswordComponent`, `ResetPasswordComponent`.
  - Perfil: `ProfileComponent` (edicao, avatar, troca de senha, recuperacao, idioma/tema).
  - Admin: `AdminUsersComponent`, `AdminUserDetailComponent`, `AdminServicesComponent`, `AdminEmailSettingsComponent`.
  - Estudantes: `StudentsListComponent`, `StudentFormComponent` (novo/edicao), import/export.
  - Educacao: `EducationExplorerComponent`, `EducationClassesComponent`, `EducationClassDetailComponent` (UCs, alunos, importacao).
  - Compartilhados: navbar, toast container, loading spinner.
- **Servicos e interceptors:** `AuthService`, `UserService`, `EmailSettingsService`, `StudentsService`, `EducationService`, `ProblemInterceptor`, `LoadingInterceptor`, `ThemeService`, `NotificationService`.
- **i18n:** `ngx-translate` com `public/i18n/en.json` e `pt.json`; loader HTTP, fallback en, deteccao de idioma do navegador.
- **UI/estilo:** SCSS com variaveis em `src/styles/_variables.scss` + temas em `src/styles/_theme.scss`; FontAwesome; Bootstrap 5.
- **Config:** `environment.ts` aponta `apiUrl: http://localhost:5095/api`; `proxy.conf.js` direciona `/api` para SPA proxy ASP.NET.

## Fluxos & Endpoints
- **Autenticacao:** `POST /api/authentication/login` -> { token, refreshToken, expiresAt }; `POST /api/authentication/refresh` (refresh rotativo). Claims incluem `permissions`.
- **Usuarios:** `POST /api/user/add` (publico), `PUT /api/user/update`, `PUT /api/user/{id}/permissions`, `DELETE /api/user/delete/{id}`, `GET /api/user/all|get/{id}|email/{email}|permission/{permission}|permissions`, `GET /api/user/me`.
- **Perfil/Senha:** `PUT /api/user/profile` (multipart ou JSON), `PUT /api/user/change-password`, recuperacao `POST /api/user/recovery/code|validate|verify`.
- **E-mail/SMTP:** `POST /api/email/reset` (silencioso para e-mail inexistente), `GET/PUT /api/email/settings`, `POST /api/email/test`.
- **Estudantes:** `GET /api/students` (query `PageNumber`, `PageSize`, `Search`, `IsActive`), `GET /api/students/{id}`, `POST /api/students`, `PUT /api/students/{id}`, `DELETE /api/students/{id}`, `POST /api/students/import` (multipart XLSX), `GET /api/students/export`.
- **Educacao:** `GET /api/education/schools`, `GET /api/education/programs?schoolId=...`, `GET /api/education/classes?programId=...`, `GET /api/education/ucs?classId=...`, `GET /api/education/ucs/search?PageNumber=1&PageSize=50&Search=...`, `GET /api/education/ucs/students?eadId=123`, `POST /api/education/import` (multipart).
- **Permissoes:** claim type `permissions`. Defaults: usuario (`view:home`, `view:profile`); adicionais: `manage:users`, `view:students`, `manage:students`, `view:education`, `manage:education`.

## Importacoes e processamento em lote
- **Cursos (JSON):** `POST /api/education/import` armazena o arquivo como attachment, retorna 202 e agenda `EducationImportHangfireJob` (batch 3 cursos) que reaproveita a `AsyncSession` Raven para repositórios e gera notificacao de sucesso/erro (navbar faz polling a cada 20s).
- **Relatorios (XLSX):** `POST /api/education/import-report` processa de forma síncrona em memória usando caches (`EducationReportImportProcessor`). Adequado para lotes menores; para volumes maiores, considere enfileirar como no fluxo de cursos.
- **Visibilidade:** hangfire dashboard em `/hangfire` (com auth) e notificacoes in-app apontam para `/education`.

## Banco de Dados (RavenDB)
### RavenDB em modo seguro (TLS)
- A UI do RavenDB fica em `https://localhost:8081` (certificado autoassinado).
- Gere o certificado de dev (cria `certs/ravendb.pfx` e atualiza `certs/ravendb.cer`) em cada maquina nova:
```powershell
pwsh ./certs/generate-ravendb-cert.ps1
```
  Usa a senha padrao `changeme-ravendb-cert`; se alterar, atualize tambem `RAVEN_Security_Certificate_Password` e `RAVENDBSETTINGS_CERTIFICATE_PASSWORD` no `docker-compose.yml`.
- Para confiar no certificado (Windows, usuario atual):
```powershell
certutil -user -addstore Root certs\ravendb.cer
```
- Para remover a confianca (opcional):
```powershell
certutil -user -delstore Root <thumbprint>
# pegue o thumbprint via: certutil -user -store Root | findstr ravendb
```
- Configuração via env: `RAVENDBSETTINGS_URLS` (vírgula separada), `RAVENDBSETTINGS_DATABASE_NAME`, `RAVENDBSETTINGS_CERTIFICATE_SUBJECT` (busca certificado no store do usuário atual, exige chave privada).
- Conexão e criação de DB/índices em `DocumentStoreHolderAlternative`. Convens: `MaxNumberOfRequestsPerSession=500`, optimistic concurrency, `IdentityPartsSeparator='-'`.
- Anexos: avatar salvo como attachment (`profile-picture`) com content-type preservado.
- Índice: `User_ByEmail` para busca de e-mail com `WaitForNonStaleResults` nos cadastros.

## Configuração e Variáveis de Ambiente
- **JWT:** `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SIGNING_KEY` (obrigatórios; API lança se ausentes).
- **RavenDB:** `RAVENDBSETTINGS_URLS`, `RAVENDBSETTINGS_DATABASE_NAME`, `RAVENDBSETTINGS_CERTIFICATE_SUBJECT`.
- **Segredos:** `APP_SECRET_ENCRYPTION_KEY` (obrigatório para criptografar senha SMTP).
- **Front:** `Application.Client/src/app/environment/environment.ts` (`apiUrl`), `proxy.conf.js` (dev).
- Logging/CORS/HTTPS estão em `Program.cs`; Swagger desativado por padrão.

## Executar
1. **Backend**
   ```bash
   dotnet restore Application.sln
   dotnet build Application.sln
   dotnet run --project Application.Web/Application.Api.csproj
   ```
   Configure as variáveis de ambiente acima antes de subir. Lançamento padrão: http://localhost:5095 e https://localhost:7240.

2. **Frontend**
   ```bash
   cd Application.Client
   npm install
   npm start        # usa proxy/SSL do ASP.NET SPA
   # ou build de produção
   npm run build
   ```

3. **Testes**
   ```bash
   dotnet test Application.sln
   # Angular (Karma)
   cd Application.Client && npm test
   ```
   Os testes .NET usam RavenDB embarcado (`RavenTestDriver`) com cultura pt setada em `BaseTest`.

## Estrutura de Diretórios
```
Application.Domain/               # Modelos, DTOs, validators, exceções, recursos i18n
Application.Infrastructure/       # DocumentStore, repositórios RavenDB, índices, DI infra
Application.Service/              # Serviços de domínio (User/Token/Email/Settings), segurança, DI service
Application.Web/                  # ASP.NET Core API, controllers, middlewares, filters, configs
Application.Client/               # Angular 18 SPA (src/app/...)
  src/app/page/auth|home|profile|admin/... 
  src/app/service/auth|user|email|http|loading|notification|theme
  src/app/shared/...              # toast, navbar, etc.
Application.Test/                 # Testes de controllers, services, validators, middlewares
.github/                          # (pasta de workflows vazia)
```

## Como Estender
1. **Domínio:** crie o modelo/DTOs/validator e adicione mensagens nos `.resx`. Se precisar de permissão nova, inclua em `ApplicationPermissions` (claim + policy automática).
2. **Infra:** acrescente repositório/índices RavenDB e registre em `DependencyInjectionModuleInfra`.
3. **Service:** implemente regras/casos de uso, valide com FluentValidation e exceções de domínio. Hash/cripte segredos via `SecureHash`/`SecretEncryptionService` quando aplicável.
4. **API:** exponha via controller com ProblemDetails/i18n; use policies por permissão. Adicione mapping de DTOs e suporte a multipart se houver upload.
5. **Client:** crie serviços Angular e páginas, proteja rotas com `AuthGuard`/`PermissionGuard`, internacionalize (`public/i18n/*.json`), use interceptors existentes.
6. **Testes:** amplie `Application.Test` (serviço/controller/middleware) e, se front mudar, adicione specs no Angular.

## Padrões e Práticas
- Clean/Onion com dependências direcionadas ao domínio.
- ProblemDetails (RFC 7807) para erros; traceId propagado.
- FluentValidation para domínio; mensagens localizadas.
- JWT + policies por permissão; refresh token rotativo com hash.
- Segredos reversíveis (SMTP) criptografados por AES com chave de ambiente; senhas de usuário/refresh hash PBKDF2.
- RavenDB com sessão por request via middleware; attachments para arquivos binários.
- Front com interceptors de erro/loading, toasts centralizados, tema persistido, i18n, guards de rota.

## Glossário
- **ProblemDetails / ValidationProblemDetails:** respostas RFC 7807 com `traceId` para excecoes e ModelState/FluentValidation.
- **DomainException:** excecoes de negocio com status code especifico.
- **ApplicationPermissions:** claim `permissions` usada em policies. Defaults: user (`view:home`, `view:profile`); demais: `manage:users`, `view:students`, `manage:students`, `view:education`, `manage:education`.
- **Refresh Token Rotativo:** token composto `id.secret`; `id` armazenado em texto, `secret` em hash; rotacionado a cada refresh.
- **Recovery Code:** codigo de 6 digitos, TTL 10 min, cooldown 1 min, 5 tentativas; revoga quando expira ou excede tentativas.
- **SecretEncryptionService:** AES CBC/PKCS7 com chave derivada de `APP_SECRET_ENCRYPTION_KEY` para armazenar senha SMTP de forma reversivel.

## Prompt base
Voce e o Codex trabalhando no monorepo ApplicationBase (Angular 18 + @ngx-translate no front e API .NET 8 com RavenDB). Siga estas regras em qualquer implementacao:

Docker (ambiente padrao)
- Use `docker compose up -d` para subir o stack completo. A API serve o build Angular como arquivos estaticos.
- Portas locais: API `http://localhost:5095` (container `app-api`), RavenDB `https://localhost:8081` (container `raven-db`), Evolution API `http://localhost:8082` (container `evolution-api`).
- Variaveis do container `app-api` estao em `docker-compose.yml` (JWT_*, APP_SECRET_ENCRYPTION_KEY, RAVENDBSETTINGS_*, EVOLUTION_API_*). Ajuste valores ali, nao no `launchSettings.json`.
- RavenDB roda em modo secured e usa certificado montado de `./certs` em `/certs` dentro dos containers. Se precisar acessar fora do Docker, instale o certificado localmente.
- Logs: `docker compose logs -f app-api` para API; `docker compose logs -f raven-db` para o banco.

Front-end (Application.Client)
- Sempre internacionalize: use o pipe/servico `@ngx-translate/core`; todas as strings devem virar chaves em `public/i18n/en.json` e `public/i18n/pt.json` (defaultLanguage = en, fallback configurado). Evite textos literais em templates/TS.
- Notificacoes: nunca use alert/snackbar generico. Use `NotificationService` (`showSuccess|showError|showWarning|showInfo`) que renderiza os toasts via `app-toast-container`.
- HTTP: baseie-se em `environment.apiUrl`; use `HttpClient` e deixe `LoadingInterceptor` + `ProblemInterceptor` cuidarem de spinner e erros `application/problem+json`. Nao duplique handling de loading/erro.
- Forms: use Reactive Forms (`FormBuilder` + validators). Mensagens de erro e toasts devem ser traduzidas. Mantenha acessibilidade (aria-labels, etc.).
- Permissoes/rotas: proteja com `AuthGuard`/`PermissionGuard` usando as claims `permissions`. Permissoes atuais: `view:home`, `view:profile`, `manage:users`, `view:students`, `manage:students`, `view:education`, `manage:education`. Respeite tokens armazenados pelo `AuthService`.
- Preferencias: idioma em `preferredLanguage` (localStorage) via `TranslateService`; tema com `ThemeService` (`light`/`dark`), sem criar logica paralela.
- Componentes/shared: reutilize estilos e padroes existentes (navbar, dashboard shell, avatar handling, ngx-spinner). Nada de bibliotecas de UI ou notificacoes extras sem necessidade.
- Estudantes: use `StudentsService` para CRUD, importacao (`/students/import`, XLSX) e exportacao (`/students/export`) e mantenha toasts i18n.
- Educacao: use `EducationService` para escolas/programas/turmas/UCs, busca paginada, alunos por UC e importacao (`/education/import`) via multipart.
- Testes: escreva specs Jasmine/Karma quando alterar logica; mocke `TranslateService`/pipe e `NotificationService` como nos specs atuais.

Back-end (Application.Web/.Domain/.Service/.Infrastructure)
- Globalizacao: mensagens via `IStringLocalizer<SharedResource>` com chaves nos resx `Application.Domain/Resources/SharedResource.resx` e `SharedResource.en.resx`. Nao retornar strings cruas.
- Erros/validacao: use FluentValidation para regras de dominio; lance `DomainException` (`BusinessException`, `NotFoundException`, etc.) e deixe o `ProblemDetailsMiddleware`/`ValidationProblemDetailsFilter` gerar `application/problem+json`.
- Autorizacao/autenticacao: JWT com claim `permissions` (constantes em `Application.Domain/Model/ApplicationPermissions.cs`); proteja endpoints com `[Authorize(Policy = ...)]`. Respeite rate limiting via `IRateLimiter` quando aplicavel.
- Persistencia: RavenDB via `IServiceRavenDB` e repositorios (`IUserRepository`, `ISettingsRepository`, `IEducationRepository`, `IStudentRepository`, etc.). Para arquivos (ex.: avatar, imports), use attachments. Nao abra sessoes diretas.
- Servicos: siga o padrao das services (UserService, TokenService, EmailService, SettingsService, EducationService, StudentService) e mantenha regras de seguranca (hash de senha com `SecureHash`, refresh tokens, codigos de recuperacao com TTL).
- Educacao: importacao e processamento assincrono com `EducationImportBackgroundService` + `EducationImportProcessor`; endpoints em `EducationController` com policies `view:education`/`manage:education`.
- Estudantes: `StudentsController` com CRUD, importacao XLSX e exportacao; valide com `StudentValidator` e mensagens localizadas.
- Configuracao: JWT keys e SECRET_ENCRYPTION_KEY vem de env vars (`ApplicationConstants`). Mantenha JsonSerializer sem naming policy (camel-case desativado ja no Program.cs).
- Testes: use xUnit; para cenarios com RavenDB, herde de `BaseTest` (RavenTestDriver, cultura pt) e mocke localizador/servicos conforme os testes existentes.

Exemplos de endpoints e payloads (referencia rapida)
```
POST /api/authentication/login
{ "email": "user@example.com", "password": "Secret123!" }

POST /api/user/add
{ "Name": "Ana", "Email": "ana@corp.com", "Password": "Secret123!" }

GET /api/students?PageNumber=1&PageSize=10&Search=ana&IsActive=true

POST /api/students
{ "FirstName": "Ana", "LastName": "Silva", "Email": "ana@corp.com" }

GET /api/education/programs?schoolId=schools/1-A
GET /api/education/classes?programId=programs/1-A
GET /api/education/ucs/search?PageNumber=1&PageSize=50&Search=ux
GET /api/education/ucs/students?eadId=27535

POST /api/education/import (multipart: file)
POST /api/students/import (multipart: file)
```

Saida esperada: codigo alinhado a essas praticas, com traducoes e notificacoes corretas, seguindo os padroes de arquitetura e testes do repositorio.
