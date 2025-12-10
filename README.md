# Application Base
Base monolítica pronta para produção com ASP.NET Core 8, Angular 18 e RavenDB. Inclui autenticação JWT com rotação de refresh tokens, autorização por permissões, gerenciamento completo de usuários, recuperação de senha, configuração de SMTP (com criptografia de segredo), i18n full-stack e tratamento unificado de erros via ProblemDetails.

## Visão Geral
- **Arquitetura:** Clean/Onion (Domain → Service → Infrastructure → Web/API → Client).
- **Stack:** ASP.NET Core 8, Angular 18, RavenDB, FluentValidation, JWT Bearer, ngx-translate, ngx-spinner, FontAwesome, Bootstrap.
- **Principais entregas:** CRUD de usuários, permissões granulares, perfil com avatar (anexo RavenDB), troca de senha e fluxo completo de recuperação, configurações de e-mail persistidas e criptografadas, interceptores de erros e loading, temas claro/escuro, i18n (pt/en) e ProblemDetails padronizado.

## Funcionalidades
- Autenticação JWT (2h) + refresh token rotativo (7d) com hash PBKDF2.
- Cadastro/login, CRUD de usuários, consulta por permissão, e lista de permissões disponíveis.
- Perfil: edição de dados, foto (upload multipart ou data URL), offsets/zoom do avatar, funções/cargo/departamento/organização/localização.
- Segurança da conta: troca de senha, geração/validação/uso de código de recuperação (6 dígitos, TTL 10 min, cooldown 1 min, 5 tentativas).
- Administração: painel Angular para usuários (lista, detalhes, gestão de permissões) e configuração de serviços (SMTP).
- E-mail: envio de reset/recovery, teste de SMTP e persistência de credenciais criptografadas.
- Observabilidade de erros: ProblemDetails com `traceId`, ModelState → ValidationProblemDetails, i18n backend/frontend.
- UX: toasts centralizados, loading global, tema claro/escuro, shell dashboard com navegação protegida por permissões.

## Arquitetura e Camadas (Backend)
- **Domain (`Application.Domain`):**
  - Modelos: `User` (Account+Profile), `ApplicationPermissions` (claim `permissions`, defaults user/admin), `ApplicationConstants` (env keys), `Configurations`/`EmailSettings`.
  - DTOs: `LoginDto`, `CreateUserDto`, `ChangePasswordDto`, `RefreshRequestDto`, `TokenResponseDto`, `UpdateProfileDto`, `Generate/Validate/VerifyRecoveryCodeDto`, `SendResetEmailDto`, `PasswordInput`.
  - Validações: `UserValidator` (e-mail, nome, data, permissões), `PasswordValidator` (força mínima).
  - Exceções: `DomainException` + `Conflict/NotFound/Forbidden/Business/ConfigurationException`.
  - i18n: `SharedResource` (.resx pt/en) e `SharedResourceProvider` para usos estáticos.

- **Service (`Application.Service`):**
  - `UserService`: valida domínio, normaliza permissões, hash de senha (PBKDF2), CRUD, perfil/arquivo de avatar (Raven attachment), troca de senha (revoga refresh), recuperação (gera código 6 dígitos, TTL 10 min, cooldown 1 min, 5 tentativas, envia e-mail opcional).
  - `TokenService`: autentica, gera JWT com permissões, emite/rotaciona refresh tokens (id.secret + hash), expiração 2h/7d.
  - `EmailService`: envio de reset e código de recuperação via SMTP, envio de teste; usa configurações persistidas ou default.
  - `SettingsService`: persiste `Configurations.Email` no RavenDB; de/para criptografia de senha SMTP.
  - Segurança: `SecureHash` (PBKDF2) e `SecretEncryptionService` (AES key derivada do env `APP_SECRET_ENCRYPTION_KEY`).
  - DI: registrado em `DependencyInjectionModuleService` (IUserService, ITokenService, IEmailService, ISettingsService, ISecretEncryptionService).

- **Infrastructure (`Application.Infrastructure`):**
  - DocumentStore RavenDB: `DocumentStoreHolderAlternative` (URLs via `RAVENDBSETTINGS_URLS`, certificado por assunto em store, convenções, criação de DB, índices).
  - Repositórios: `UserRepository` (CRUD, consultas, refresh token, permissões, profile picture como attachment), `SettingsRepository` (Configurations), `ServiceRavenDB` para sessão/async session.
  - Índices: `User_ByEmail`.
  - DI: `DependencyInjectionModuleInfra` registra `IDocumentStore`, `IServiceRavenDB`, repositórios e cria índices.

- **Web/API (`Application.Web`):**
  - Pipeline: localization (pt-BR default; pt/en-US/en) → `ProblemDetailsMiddleware` → `MiddlewareServiceRavenDbStore` (abre/salva/dispose sessão) → static files → CORS liberado → HTTPS → AuthZ → controllers → SPA fallback.
  - Autenticação: JWT Bearer configurado com env (`JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SIGNING_KEY`). Policies por permissão (`ApplicationPermissions.All`).
  - Middlewares/Filters: `ProblemDetailsMiddleware` (FluentValidation → 400 ValidationProblemDetails; DomainException → status específico; 401; 500; todos com traceId), `ValidationProblemDetailsFilter` (ModelState → 400 ValidationProblemDetails).
  - Controllers:
    - `AuthenticationController`: `POST /api/authentication/login`, `POST /api/authentication/refresh`.
    - `UserController`: cadastro público, `GET /api/user/all|get/{id}|email/{email}|permission/{permission}|permissions`, `GET /api/user/me`, `PUT /api/user/update`, `PUT /api/user/{id}/permissions`, `PUT /api/user/profile` (multipart ou JSON/data URL), `PUT /api/user/change-password`, fluxo de recuperação `POST /api/user/recovery/code|validate|verify`, `DELETE /api/user/delete/{id}`.
    - `EmailController`: `POST /api/email/reset` (envia e-mail se usuário existe), `GET/PUT /api/email/settings` (SMTP), `POST /api/email/test` (testa com override opcional).
  - Swagger está comentado/desativado.

## Camada Client (Angular 18 - `Application.Client`)
- **Roteamento:** home (`/home`), perfil (`/profile`), admin (`/admin/*`), auth/login, registro, forgot/reset password. Guards: `AuthGuard` + `PermissionGuard` (JWT decode de claim `permissions`).
- **Componentes/Páginas:**
  - `AppComponent`: shell dashboard com side-nav, topbar, menu de perfil, toasts e spinner.
  - Auth: `AuthComponent` (login), `RegisterComponent` (signup), `ForgotPasswordComponent` e `ResetPasswordComponent` (fluxo de código 6 dígitos e nova senha).
  - Perfil: `ProfileComponent` (edição de dados, avatar com offset/zoom, troca de senha, geração/uso de código de recuperação, seleção de idioma e tema).
  - Admin: `AdminUsersComponent` (lista/paginação), `AdminUserDetailComponent` (detalhe e gestão de permissões), `AdminServicesComponent` (SMTP com teste), `AdminEmailSettingsComponent` (card estático/teaser).
  - Compartilhados: navbar, toast container, loading spinner.
- **Serviços e interceptors:** `AuthService` (login/refresh/signup), `UserService` (CRUD/profile/permissões/recovery), `EmailSettingsService` (SMTP), `ProblemInterceptor` (ProblemDetails → toast), `LoadingInterceptor` (spinner global), `ThemeService` (tema persistido), `NotificationService` (toasts).
- **i18n:** `ngx-translate` com `public/i18n/en.json` e `pt.json`; loader HTTP, fallback en, detecção de idioma do navegador.
- **UI/estilo:** SCSS com variáveis em `src/styles/_variables.scss` + temas em `src/styles/_theme.scss`; FontAwesome; Bootstrap 5.
- **Config:** `environment.ts` aponta `apiUrl: http://localhost:5095/api`; `proxy.conf.js` direciona `/api` para SPA proxy ASP.NET.

## Fluxos & Endpoints
- **Autenticação:** `POST /api/authentication/login` → { token, refreshToken, expiresAt }; `POST /api/authentication/refresh` (refresh rotativo). Claims incluem `permissions`.
- **Usuários:** `POST /api/user/add` (público, cria com permissões default), `PUT /api/user/update`, `PUT /api/user/{id}/permissions`, `DELETE /api/user/delete/{id}`, `GET /api/user/all|get/{id}|email/{email}|permission/{permission}|permissions`, `GET /api/user/me`.
- **Perfil/Senha:** `PUT /api/user/profile` (multipart ou JSON), `PUT /api/user/change-password`, recuperação `POST /api/user/recovery/code|validate|verify`.
- **E-mail/SMTP:** `POST /api/email/reset` (silencioso para e-mail inexistente), `GET/PUT /api/email/settings`, `POST /api/email/test`.
- **Permissões:** claim type `permissions`. Defaults: usuário (`view:home`, `view:profile`), admin adiciona `manage:users`. Policies geradas dinamicamente.

## Banco de Dados (RavenDB)
- Configuração via env: `RAVENDBSETTINGS_URLS` (vírgula separada), `RAVENDBSETTINGS_DATABASE_NAME`, `RAVENDBSETTINGS_CERTIFICATE_SUBJECT` (busca certificado no store do usuário atual, exige chave privada).
- Conexão e criação de DB/índices em `DocumentStoreHolderAlternative`. Convens: `MaxNumberOfRequestsPerSession=30`, optimistic concurrency, `IdentityPartsSeparator='-'`.
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
- **ProblemDetails / ValidationProblemDetails:** respostas RFC 7807 com `traceId` para exceções e ModelState/FluentValidation.
- **DomainException:** exceções de negócio com status code específico.
- **ApplicationPermissions:** claim `permissions` usada em policies. Defaults: user (`view:home`, `view:profile`), admin (`manage:users`).
- **Refresh Token Rotativo:** token composto `id.secret`; `id` armazenado em texto, `secret` em hash; rotacionado a cada refresh.
- **Recovery Code:** código de 6 dígitos, TTL 10 min, cooldown 1 min, 5 tentativas; revoga quando expira ou excede tentativas.
- **SecretEncryptionService:** AES CBC/PKCS7 com chave derivada de `APP_SECRET_ENCRYPTION_KEY` para armazenar senha SMTP de forma reversível.


## Prompt base
Você é o Codex trabalhando no monorepo ApplicationBase (Angular 18 + @ngx-translate no front e API .NET 8 com RavenDB). Siga estas regras em qualquer implementação:

Front-end (Application.Client)
- Sempre internacionalize: use o pipe/serviço `@ngx-translate/core`; todas as strings devem virar chaves em `public/i18n/en.json` e `public/i18n/pt.json` (defaultLanguage = en, fallback configurado). Evite textos literais em templates/TS.
- Notificações: nunca use alert/snackbar genérico. Use `NotificationService` (`showSuccess|showError|showWarning|showInfo`) que renderiza os toasts via `app-toast-container`.
- HTTP: baseie-se em `environment.apiUrl`; use `HttpClient` e deixe `LoadingInterceptor` + `ProblemInterceptor` cuidarem de spinner e erros `application/problem+json`. Não duplique handling de loading/erro.
- Forms: use Reactive Forms (`FormBuilder` + validators). Mensagens de erro e toasts devem ser traduzidas. Mantenha acessibilidade (aria-labels, etc.).
- Permissões/rotas: proteja com `AuthGuard`/`PermissionGuard` usando as claims `permissions` (ex.: `view:home`, `view:profile`, `manage:users`). Respeite tokens armazenados pelo `AuthService`.
- Preferências: idioma em `preferredLanguage` (localStorage) via `TranslateService`; tema com `ThemeService` (`light`/`dark`), sem criar lógica paralela.
- Componentes/shared: reutilize estilos e padrões existentes (navbar, dashboard shell, avatar handling, ngx-spinner). Nada de bibliotecas de UI ou notificações extras sem necessidade.
- Testes: escreva specs Jasmine/Karma quando alterar lógica; mocke `TranslateService`/pipe e `NotificationService` como nos specs atuais.

Back-end (Application.Web/.Domain/.Service/.Infrastructure)
- Globalização: mensagens via `IStringLocalizer<SharedResource>` com chaves nos resx `Application.Domain/Resources/SharedResource.resx` e `SharedResource.en.resx`. Não retornar strings cruas.
- Erros/validação: use FluentValidation para regras de domínio; lance `DomainException` (`BusinessException`, `NotFoundException`, etc.) e deixe o `ProblemDetailsMiddleware`/`ValidationProblemDetailsFilter` gerar `application/problem+json`.
- Autorização/autenticação: JWT com claim `permissions` (constantes em `Application.Domain/Model/ApplicationPermissions.cs`); proteja endpoints com `[Authorize(Policy = ...)]`. Respeite rate limiting via `IRateLimiter` quando aplicável.
- Persistência: RavenDB via `IServiceRavenDB` e repositórios (`IUserRepository`, `ISettingsRepository`, etc.). Para arquivos (ex.: avatar), use attachments. Não abra sessões diretas.
- Serviços: siga o padrão das services (UserService, TokenService, EmailService, SettingsService) e mantenha regras de segurança (hash de senha com `SecureHash`, refresh tokens, códigos de recuperação com TTL).
- Configuração: JWT keys e SECRET_ENCRYPTION_KEY vêm de env vars (`ApplicationConstants`). Mantenha JsonSerializer sem naming policy (camel-case desativado já no Program.cs).
- Testes: use xUnit; para cenários com RavenDB, herde de `BaseTest` (RavenTestDriver, cultura pt) e mocke localizador/serviços conforme os testes existentes.

Saída esperada: código alinhado a essas práticas, com traduções e notificações corretas, seguindo os padrões de arquitetura e testes do repositório.

---
MIT — contribuições são bem-vindas.
