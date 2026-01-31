# 🚀 Setup de Desenvolvimento Local

Guia para configurar o ambiente de desenvolvimento local da ApplicationBase.

---

## 📋 Pré-requisitos

- ✅ .NET 8 SDK
- ✅ Node.js 18+
- ✅ Docker + Docker Compose (opcional, para Redis/PostgreSQL)
- ✅ PostgreSQL 14+ (ou use Docker)

---

## 1️⃣ Variáveis de Ambiente

### Opção A: Docker (Recomendado)

Se você estiver usando Docker Compose, as variáveis já estão configuradas em `docker-compose.yml`.

```bash
docker compose up -d
```

A aplicação se conectará automaticamente aos serviços.

### Opção B: Desenvolvimento Local (sem Docker)

1. **Copie o arquivo de template:**
   ```bash
   cp .env.template .env.local
   ```

2. **Edite `.env.local` com valores locais:**
   ```bash
   HANGFIRE_CONNECTION_STRING=Host=localhost;Port=5432;Database=evolution;Username=evolution;Password=evolution
   REDIS_CONNECTION_STRING=localhost:6379
   ```

3. **Garanta que PostgreSQL está rodando:**
   ```bash
   # Linux/Mac
   postgres --version
   
   # Windows
   psql --version
   ```

4. **Crie o banco de dados (se não existir):**
   ```sql
   CREATE DATABASE evolution;
   CREATE USER evolution WITH PASSWORD 'evolution';
   GRANT ALL PRIVILEGES ON DATABASE evolution TO evolution;
   ```

---

## 2️⃣ Backend (ASP.NET Core 8)

### Build
```bash
dotnet build
```

### Run (Desenvolvimento)
```bash
# Usa .env.local automaticamente se existir
dotnet run --project Application.Web
```

**Esperado:**
```
[20:54:03 INF] Iniciando aplicação...
[20:54:04 INF] Connecting to Redis at localhost:6379
[20:54:12 WRN] Redis connection failed, falling back to InMemoryCacheService
[20:54:15 INF] Registrando MediatR handlers e behaviors...
[20:54:20 INF] Now listening on: https://localhost:5001
```

### Testes
```bash
dotnet test
```

---

## 3️⃣ Frontend (Angular 18)

### Build
```bash
cd Application.Client
npm install
```

### Run (Desenvolvimento)
```bash
npm start
```

**Acesso:** https://localhost:4200

---

## 4️⃣ E2E Tests (Playwright)

### Pré-requisito
Backend DEVE estar rodando em terminal separado.

### Run
```bash
cd Application.Client/e2e
npm install  # (primeira vez)
npm test
```

**Resultado esperado:**
```
✓ auth (6 testes)
✓ user (9 testes)
Total: 15 testes
```

---

## 🐛 Troubleshooting

### "PostgreSQL connection refused"
```bash
# Verificar se PostgreSQL está rodando
psql -U postgres -c "SELECT version();"

# Se não estiver, inicie (Linux/Mac)
brew services start postgresql

# Ou use Docker
docker run -d -p 5432:5432 \
  -e POSTGRES_PASSWORD=postgres \
  postgres:14
```

### "Redis connection refused"
```bash
# Verificar se Redis está rodando
redis-cli ping

# Se não estiver, inicie (Linux/Mac)
brew services start redis

# Ou use Docker
docker run -d -p 6379:6379 redis:latest
```

### "HANGFIRE_CONNECTION_STRING not defined"
```bash
# Copie e customize o arquivo
cp .env.template .env.local

# E configure a variável
HANGFIRE_CONNECTION_STRING=Host=localhost;Port=5432;Database=evolution;Username=evolution;Password=evolution
```

---

## 📊 Verificar Status

### Todos os Serviços
```bash
# Verificar se tudo está ok
curl https://localhost:5001/health/startup  # Backend health check
curl https://localhost:4200                  # Frontend
redis-cli ping                               # Redis
psql -U evolution -c "SELECT 1"              # PostgreSQL
```

---

## 🎯 Fluxo Completo (Recomendado)

### Terminal 1: Docker Services (opcional)
```bash
docker compose up
# ou se preferir sem Docker, inicie Redis/PostgreSQL manualmente
```

### Terminal 2: Backend
```bash
cd c:\Users\Julio\Desktop\Repositorios\ApplicationBase
dotnet run --project Application.Web
```

### Terminal 3: Frontend
```bash
cd Application.Client
npm start
```

### Terminal 4: E2E Tests (opcional)
```bash
cd Application.Client/e2e
npm test
```

---

## ✅ Verificação Final

Quando tudo estiver rodando, você verá:

**Backend (Terminal 2):**
```
[20:54:20 INF] Now listening on: https://localhost:5001
```

**Frontend (Terminal 3):**
```
✔ Compiled successfully.
```

**Browser:**
```
https://localhost:4200 → Application carregando
```

---

## 📚 Documentação

- Arquitetura: [.github/architecture-contract.md](.github/architecture-contract.md)
- E2E Tests: [Application.Client/e2e/QUICKSTART.md](Application.Client/e2e/QUICKSTART.md)
- Qualidade: [QUALITY_ASSURANCE.md](QUALITY_ASSURANCE.md)

---

**Última atualização:** 30 de janeiro de 2026
