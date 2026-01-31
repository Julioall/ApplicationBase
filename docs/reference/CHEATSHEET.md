# 🚀 CHEAT SHEET - Quick Reference

## ⚡ Um Comando para Começar

```bash
# Setup completo (Docker + Backend + Frontend)
.\run-dev.bat all
```

---

## 📌 Comandos Essenciais

### Setup (PRIMEIRA VEZ)
```bash
# 1. Variáveis de ambiente
copy .env.template .env

# 2. Dependências de backend
dotnet build

# 3. Dependências de frontend
cd Application.Client
npm install
```

### Desenvolvimento

#### Terminal 1: Infraestrutura
```bash
docker-compose up -d
```

#### Terminal 2: Backend
```bash
# Iniciar
dotnet run --project Application.Web

# Ou com auto-reload
dotnet watch run --project Application.Web
```

#### Terminal 3: Frontend
```bash
cd Application.Client
npm start
```

#### Terminal 4: Testes
```bash
# Unit tests
dotnet test

# E2E tests
cd Application.Client/e2e
npm test
```

---

## 🔗 URLs Importantes

| Serviço | URL | Uso |
|---------|-----|-----|
| **Backend API** | https://localhost:5001 | API REST |
| **Frontend SPA** | http://localhost:4200 | Interface web |
| **RavenDB** | http://localhost:8080 | Banco de dados |
| **MailHog** | http://localhost:8025 | Testing emails |
| **Redis** | localhost:6379 | Cache (CLI) |
| **PostgreSQL** | localhost:5432 | Hangfire (CLI) |

---

## 📁 Estrutura de Pastas

```
src/ (código)
├── Application.Domain/       # Entities, Interfaces, DTOs
├── Application.Service/      # Business Logic, Handlers
├── Application.Infrastructure/ # Repositories, External APIs
├── Application.Web/          # API Controllers, Program.cs
├── Application.Client/       # Angular 18 Frontend
└── Application.Test/         # Unit Tests

docs/ (documentação)
├── QUICKSTART.md            # Guia rápido
├── DEVELOPMENT_READY.md     # Status e troubleshooting
├── PROJECT_SUMMARY.md       # Resumo técnico
└── FINAL_COMPLETION.md      # Conclusão

config/ (configuração)
├── .env                     # Variáveis (gerado)
├── .env.template           # Template
├── docker-compose.yml      # Infraestrutura
└── docker-compose.override.yml # Desenvolvimento local
```

---

## ⚙️ Variáveis de Ambiente Críticas

```bash
# Copiar e customizar:
JWT_SIGNING_KEY=your-secret-key-here
JWT_ISSUER=ApplicationBase
JWT_AUDIENCE=ApplicationBaseClient
APP_SECRET_ENCRYPTION_KEY=your-encryption-key
RAVENDBSETTINGS_URLS=http://localhost:8080
REDIS_CONNECTION_STRING=localhost:6379
HANGFIRE_CONNECTION_STRING=Host=localhost;Port=5432;Database=evolution;Username=evolution;Password=evolution
```

---

## 🧪 Testes

### Unit Tests
```bash
# Rodar todos
dotnet test

# Rodar teste específico
dotnet test --filter "MyTest"

# Com coverage
dotnet test /p:CollectCoverage=true
```

### E2E Tests
```bash
cd Application.Client/e2e

# Rodar todos
npm test

# Rodar com UI (vê o browser)
npm test -- --headed

# Rodar teste específico
npm test auth.spec.ts
```

---

## 🐛 Troubleshooting Rápido

### Backend não inicia
```bash
# Port em uso?
docker-compose stop application-web

# Redis não conecta?
docker-compose logs redis

# PostgreSQL erro?
docker-compose restart postgres
```

### Frontend não carrega
```bash
# Cache?
rm -rf node_modules package-lock.json
npm install

# Proxy wrong?
# Editar: Application.Client/src/proxy.conf.js
```

### E2E tests falhando
```bash
# Atualizar Playwright
npx playwright install

# Backend rodando?
curl -k https://localhost:5001/health

# Frontend rodando?
# Verificar http://localhost:4200
```

---

## 📊 Health Check

```bash
# Backend
curl -k https://localhost:5001/health

# Esperado:
# {"status":"Healthy", "checks":{"startup_configuration":{"status":"Healthy"}}}
```

---

## 🔒 Segurança - Antes de Produção

```bash
# Editar .env com valores reais
JWT_SIGNING_KEY=sua-chave-secreta-aqui-minimo-32-caracteres
APP_SECRET_ENCRYPTION_KEY=sua-chave-encriptacao-aqui-32-chars

# Verificar SSL
# Certificate path em Program.cs

# Rate limiting
# Configurado em RateLimitSettings.cs
```

---

## 📦 Deploy

### Build Release
```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### Docker Image
```bash
docker build -t applicationbase:latest -f Application.Web/Dockerfile .
docker run -p 5001:8080 applicationbase:latest
```

### Docker Compose (Stack)
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## 🔍 Logs e Debug

### Logs em Tempo Real
```bash
docker-compose logs -f

# Apenas um serviço
docker-compose logs -f postgres
```

### RavenDB Studio
```
http://localhost:8080
- Ver collections
- Executar queries
- Monitorar performance
```

### Email Testing (MailHog)
```
http://localhost:8025
- Ver emails enviados
- Testar integrações
- Não envia realmente
```

---

## 💾 Backup & Restore

```bash
# Backup PostgreSQL
docker-compose exec postgres pg_dump -U evolution evolution > backup.sql

# Restore
cat backup.sql | docker-compose exec -T postgres psql -U evolution evolution

# Backup RavenDB
# Via UI: http://localhost:8080 → Manage → Backup
```

---

## 🎯 Status Checklist

- [ ] `docker-compose up -d` - Docker rodando
- [ ] `dotnet run --project Application.Web` - Backend ok
- [ ] `npm start` - Frontend ok
- [ ] `curl -k https://localhost:5001/health` - Health check ok
- [ ] `dotnet test` - Testes ok
- [ ] `npm test` - E2E ok

---

## 📚 Documentação Completa

Arquivo | Conteúdo
---------|----------
[QUICKSTART.md](./QUICKSTART.md) | Guia passo-a-passo
[DEVELOPMENT_READY.md](./DEVELOPMENT_READY.md) | Status e troubleshooting
[PROJECT_SUMMARY.md](./PROJECT_SUMMARY.md) | Resumo técnico
[.env.template](./.env.template) | Todas as variáveis

---

## ⏱️ Tempos Aproximados

| Atividade | Tempo |
|-----------|-------|
| Setup inicial | 5 min |
| Docker start | 30 seg |
| Backend start | 1 seg |
| Frontend start | 10 seg |
| Build completo | 3.5 seg |
| Unit tests | 2 min |
| E2E tests | 1 min |

---

## 🎓 Tech Stack

```
Backend: .NET 8 (ASP.NET Core)
Frontend: Angular 18
Database: RavenDB (NoSQL)
Cache: Redis
Jobs: Hangfire (PostgreSQL)
Testing: xUnit + Playwright
Logging: Serilog
Auth: JWT Bearer + DPAPI
```

---

**Última atualização:** 31 de Janeiro de 2026  
**Status:** ✅ Pronto para desenvolvimento
