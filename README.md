# Guia Tutor (Moodle-first)
Base monolítica pronta para produção com **ASP.NET Core 8**, **Angular 18** e **RavenDB**, agora orientada a **consumir dados do Moodle como fonte de verdade**.  
A aplicação **não mantém mais entidades de “Educação”** (escolas/programas/turmas/UCs) no banco — **apenas cache** (quando necessário) para reduzir carga e latência ao consultar o Moodle.

> **Mudança-chave:** antes o sistema persistia e gerenciava entidades educacionais internamente (CRUD + importações). Agora, o Moodle é o “source of truth” e o backend expõe **somente endpoints de leitura** para os cursos do usuário.

---

## Visão Geral
- **Stack:** ASP.NET Core 8, Angular 18, RavenDB, FluentValidation, JWT Bearer, ngx-translate, ngx-spinner, FontAwesome, Bootstrap.
- **Arquitetura:** Clean/Onion (Domain -> Service -> Infrastructure -> Web/API -> Client).
- **Foco atual:** acesso e listagem de cursos do usuário via **Moodle API**, com **cache opcional** em RavenDB.

---

## O que mudou
### Removido / descontinuado
- CRUD e armazenamento interno de:
  - escolas / programas / turmas / CourseUnits
  - fluxos de importação (JSON/XLSX) e processamento em lote relacionados a Educação
- Endpoints de **criação/atualização/exclusão** do módulo Educação (agora o app não “cria” esses dados).

### Mantido
- Autenticação JWT + refresh token rotativo
- Tratamento unificado de erros via ProblemDetails
- i18n full-stack (backend + frontend)
- RavenDB (agora com ênfase em **cache**)

### Novo fluxo de Educação (Moodle-first)
- A aplicação consulta o Moodle para obter:
  - **Cursos vinculados ao usuário**
  - **Detalhes de um curso por id**
- O RavenDB pode ser usado como cache (TTL) para reduzir chamadas repetidas ao Moodle.

---

## Funcionalidades (escopo atual)
- **Autenticação**
  - JWT (2h) + refresh token rotativo (7d) com hash PBKDF2.
- **Educação via Moodle**
  - Listar cursos do usuário autenticado (Meus cursos)
  - Obter detalhes de um curso por id
- **Cache (RavenDB)**
  - Cache de respostas do Moodle (ex.: lista de cursos e detalhes)
  - Expiração por TTL e revalidação sob demanda
- **Observabilidade de erros**
  - ProblemDetails com `traceId`
  - ValidationProblemDetails para validações

---

## Fluxos & Endpoints (atual)
> Os endpoints abaixo refletem a mudança para **somente leitura** de dados educacionais.

### Autenticação
- `POST /api/authentication/login` → { token, refreshToken, expiresAt }
- `POST /api/authentication/refresh` → refresh rotativo

> Observação: o login pode continuar emitindo JWT internamente; a validação de credenciais deve estar alinhada ao fluxo de autenticação adotado com o Moodle (ex.: token de serviço/OAuth/validação via provedor institucional).

### Cursos (Moodle)
- `GET /api/courses/me` → retorna os cursos vinculados ao usuário autenticado
- `GET /api/courses/{id}` → retorna detalhes de um curso específico

---

## Cache e desempenho (RavenDB)
A aplicação **não persiste** entidades educacionais como fonte de verdade. O RavenDB pode ser utilizado para:
- **Cache de lista de cursos do usuário** (por usuário)
- **Cache de detalhes do curso** (por courseId)
- TTL sugerido: 5–15 minutos (ajustável por configuração)
- Em caso de erro do Moodle, pode retornar:
  - dados do cache (se existirem) + indicação de “desatualizado”
  - ou erro controlado com ProblemDetails (se não houver cache)

---

## Arquitetura e Camadas (Backend)
### Domain (`Application.Domain`)
- Mantém modelos/contratos essenciais (auth, erros, validações, constantes, i18n).
- **Não** contém mais modelos de Educação como “entidades de negócio persistidas” (School/Program/Class/CourseUnit etc.).
- Pode conter **DTOs** para representar dados vindos do Moodle.

### Service (`Application.Service`)
- Casos de uso:
  - autenticação, tokens, regras de acesso
  - “Meus cursos” e “Curso por id” consumindo Moodle
  - política de cache (quando usar, quando invalidar, TTL)

### Infrastructure (`Application.Infrastructure`)
- Integração com Moodle:
  - client HTTP / SDK (conforme o método escolhido)
- Integração com RavenDB:
  - persistência de cache (se habilitado)

### Web/API (`Application.Web`)
- Pipeline com ProblemDetails, localization e auth.
- Controllers expõem apenas leitura para cursos.

---

## Camada Client (Angular 18 - `Application.Client`)
- Telas principais no escopo atual:
  - Login
  - Home com “Meus Cursos”
  - Detalhe do Curso
- Continua usando:
  - `ngx-translate` (pt/en)
  - `ProblemInterceptor` + `LoadingInterceptor`
  - `NotificationService` (toasts)
  - Guards de rota conforme auth

---

## Configuração e Variáveis de Ambiente
### JWT
- `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SIGNING_KEY`

### RavenDB (cache)
- `RAVENDBSETTINGS_URLS`
- `RAVENDBSETTINGS_DATABASE_NAME`
- `RAVENDBSETTINGS_CERTIFICATE_SUBJECT` (se estiver em modo secured)

### Moodle (integração)
Defina variáveis conforme o método de autenticação adotado. Sugestão de nomes (ajuste conforme implementação):
- `MOODLE_BASE_URL`
- `MOODLE_API_TOKEN` **ou** `MOODLE_OAUTH_*` (se OAuth)
- `MOODLE_TIMEOUT_SECONDS` (opcional)

### Cache
- `CACHE_TTL_MINUTES` (opcional)

---

## Executar
1. **Backend**
   ```bash
   dotnet restore Application.sln
   dotnet build Application.sln
   dotnet run --project Application.Web/Application.Api.csproj
