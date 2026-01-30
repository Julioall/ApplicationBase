# Application Base

Monolithic base ready for production with **ASP.NET Core 8**, **Angular 18**, and **RavenDB**.

Current scope focuses on **authentication** (local + Moodle) and core platform modules (users, settings, notifications, todo, WhatsApp). Any education, courses, students, or performance features were removed.

---

## Stack
- ASP.NET Core 8, Angular 18, RavenDB
- FluentValidation, JWT Bearer, ngx-translate, ngx-spinner, FontAwesome, Bootstrap

---

## Authentication
- JWT (2h) + refresh token (7d) with PBKDF2 hash
- Moodle authentication flow supported via `IMoodleAuthClient`

---

## Configuration

### JWT
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_SIGNING_KEY`

### Moodle (auth)
- `MOODLE_BASE_URL`
- `MOODLE_API_TOKEN` **or** `MOODLE_OAUTH_*`
- `MOODLE_TIMEOUT_SECONDS` (optional)

### RavenDB
- `RAVENDBSETTINGS_URLS`
- `RAVENDBSETTINGS_DATABASE_NAME`
- `RAVENDBSETTINGS_CERTIFICATE_SUBJECT` (secured mode)

---

## Run
1. **Backend**
   ```bash
   dotnet restore Application.sln
   dotnet build Application.sln
   dotnet run --project Application.Web/Application.Api.csproj
   ```
2. **Frontend**
   ```bash
   cd Application.Client
   npm install
   npm start
   ```
