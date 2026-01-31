# 🚀 Quick Start - Development Local

## ✅ Status Atual
- Backend: **Em desenvolvimento**
- Frontend: **Pronto para rodar**
- Testes E2E: **Prontos para executar**
- Infraestrutura: **Docker compose configurado**

---

## 📋 Pré-requisitos

1. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. **Node.js 18+** - [Download](https://nodejs.org/)
3. **Docker + Docker Compose** - [Download](https://www.docker.com/products/docker-desktop)

### Verificar Instalações

```bash
# Verificar .NET
dotnet --version

# Verificar Node.js
node --version
npm --version

# Verificar Docker
docker --version
docker-compose --version
```

---

## 🔧 Configuração Inicial (Uma Única Vez)

### 1. Clonar Repositório
```bash
git clone https://github.com/seu-usuario/ApplicationBase.git
cd ApplicationBase
```

### 2. Configurar Variáveis de Ambiente
```bash
# Copiar template para arquivo local
cp .env.template .env.local

# Editar com seus valores
# Windows: notepad .env.local
# Linux/Mac: nano .env.local

# Copiar para .env (que o programa usa)
cp .env.local .env
```

### 3. Iniciar Infraestrutura (Docker)
```bash
# Subir todos os serviços: Redis, PostgreSQL, RavenDB, MailHog
docker-compose up -d

# Verificar status
docker-compose ps

# Ver logs
docker-compose logs -f
```

**Serviços disponíveis:**
- 🔴 **Redis**: localhost:6379 (cache)
- 🐘 **PostgreSQL**: localhost:5432 (Hangfire)
- 📚 **RavenDB**: localhost:8080 (banco principal) → [UI](http://localhost:8080)
- 📧 **MailHog**: localhost:8025 (email testing) → [UI](http://localhost:8025)

---

## 🎯 Executar em Desenvolvimento

### Opção 1: Backend Local + Frontend Local

#### Terminal 1 - Backend
```bash
cd ApplicationBase
dotnet build
dotnet run --project Application.Web

# Ou com watch mode (auto-restart ao salvar):
dotnet watch run --project Application.Web
```
**Backend rodando em:** `https://localhost:5001`

#### Terminal 2 - Frontend
```bash
cd Application.Client
npm install
npm start

# Ou com npm run serve
npm run serve
```
**Frontend rodando em:** `http://localhost:4200`

---

### Opção 2: Backend Local + Docker Compose para Frontend (Recomendado para CI)

```bash
# Terminal 1: Backend
dotnet run --project Application.Web

# Terminal 2: Frontend no Docker
docker build -t applicationbase-frontend -f Application.Client/Dockerfile .
docker run -p 4200:4200 applicationbase-frontend
```

---

## ✅ Validar Configuração

### Health Check API
```bash
# Testar backend
curl -X GET http://localhost:5001/health

# Esperar resposta:
# {"status":"Healthy", "checks":{...}}
```

### Health Check Frontend
```bash
# Abrir no navegador
http://localhost:4200

# Deve carregar a aplicação Angular sem erros no console
```

---

## 🧪 Executar Testes

### Testes Unitários (Backend)
```bash
dotnet test

# Com coverage:
dotnet test /p:CollectCoverage=true
```

### Testes E2E (Frontend)
```bash
cd Application.Client/e2e
npm test

# Ou com modo headed (ver o browser):
npm test -- --headed
```

**Testes incluem:**
- ✅ 6 testes de autenticação
- ✅ 9 testes de CRUD
- ✅ Fluxos críticos completos

---

## 📊 Monitoramento

### Logs da Aplicação
```bash
# Backend logs em tempo real
dotnet run --project Application.Web

# Ou com Serilog estruturado
# Ver: Application.Web/Program.cs - Serilog configuration
```

### Bases de Dados

**RavenDB Dashboard:**
```
http://localhost:8080
- Studio para gerenciar coleções
- Visualizar documentos
- Rodar queries
```

**PostgreSQL (Hangfire):**
```bash
# Conectar via CLI
psql -h localhost -U evolution -d evolution

# Ou via DBeaver/pgAdmin
```

**Redis:**
```bash
# Via CLI Docker
docker exec applicationbase-redis redis-cli

# Ou via RedisInsight
```

**MailHog (Email):**
```
http://localhost:8025
- Visualizar emails enviados em dev
- Não envia emails reais
```

---

## 🐛 Troubleshooting

### Backend não inicia: "Connection refused"

**Redis não rodando?**
```bash
docker-compose up redis
```

**PostgreSQL/Hangfire não está acessível?**
```bash
docker-compose up postgres
# Verificar .env tem HANGFIRE_CONNECTION_STRING correto
```

**RavenDB não está acessível?**
```bash
docker-compose up ravendb
# Verificar RAVENDBSETTINGS_URLS em .env
```

### Frontend não conecta ao backend

```bash
# Verificar proxy em Application.Client/src/proxy.conf.js
# Deve apontar para http://localhost:5001

# Limpar cache:
rm -rf node_modules package-lock.json
npm install
npm start
```

### E2E tests falham

```bash
# Verificar que backend está rodando em https://localhost:5001
# Verificar que frontend está rodando em http://localhost:4200

# Limpar cache do Playwright
npx playwright install

# Rodar com debug
npm test -- --debug
```

### Variáveis de ambiente não são carregadas

```bash
# Verificar que .env existe no root
ls -la | grep .env

# Recarregar terminais ou reiniciar Docker
docker-compose restart
```

---

## 📚 Estrutura de Diretórios

```
ApplicationBase/
├── .env                          # Variáveis de ambiente (gerado de .env.local)
├── .env.local                    # Desenvolvimento local (NÃO COMMIT)
├── .env.template                 # Template com todas as variáveis
├── docker-compose.yml            # Orquestração de containers
├── docker-compose.override.yml   # Overrides para desenvolvimento local
│
├── Application.Domain/           # Entities, Interfaces, CQRS contracts
├── Application.Service/          # Handlers, Services, Validators
├── Application.Infrastructure/   # Repositories, External Services
├── Application.Web/              # API Controllers, Program.cs
│
├── Application.Client/           # Angular 18 frontend
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   └── pages/
│   │   └── styles/
│   └── e2e/                      # Playwright E2E tests
│
└── Application.Test/             # Unit tests
```

---

## 🔐 Segurança em Desenvolvimento

⚠️ **NUNCA commite** `.env.local` ou `.env`

Variáveis sensíveis:
- `JWT_SIGNING_KEY` - Change em produção!
- `APP_SECRET_ENCRYPTION_KEY` - Change em produção!
- `MOODLE_API_TOKEN` - Nunca compartilhe
- `WHATSAPP_API_TOKEN` - Nunca compartilhe

---

## 🚀 Deploy (Preview)

Quando pronto para produção:
```bash
# Build da aplicação
dotnet build -c Release
dotnet publish -c Release

# Build do Docker
docker build -t applicationbase:latest -f Application.Web/Dockerfile .
docker push seu-registry/applicationbase:latest
```

Mais detalhes em: [PRODUCTION_DEPLOYMENT.md](./docs/PRODUCTION_DEPLOYMENT.md)

---

## 📞 Suporte

- 📖 Documentação: `docs/`
- 🏗️ Arquitetura: `docs/architecture-contract.md`
- 🔌 API: `swagger/ui` (quando backend rodando)
- 🧪 Testes: `Application.Test/`

**Última atualização:** 2024-01-15  
**Status:** ✅ Pronto para desenvolvimento local
