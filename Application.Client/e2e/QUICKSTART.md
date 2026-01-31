# ⚡ Guia Rápido - E2E Tests

## 🚀 Iniciar E2E Tests (Windows)

### Opção 1️⃣: Automática (Recomendado)
```powershell
# Terminal 1: Backend
cd c:\Users\Julio\Desktop\Repositorios\ApplicationBase
dotnet run --project Application.Web

# Terminal 2: E2E Tests (inicia Angular automaticamente)
cd Application.Client\e2e
npm test
```

### Opção 2️⃣: Manual (Debug)
```powershell
# Terminal 1: Backend
cd c:\Users\Julio\Desktop\Repositorios\ApplicationBase
dotnet run --project Application.Web

# Terminal 2: Frontend (opcional, para debug)
cd Application.Client
npm start

# Terminal 3: E2E Tests
cd Application.Client\e2e
npm test
```

---

## 📝 Scripts Disponíveis

| Comando | Descrição |
|---------|-----------|
| `npm test` | Executa todos os testes (headless) |
| `npm run test:ui` | Modo UI (visual, para desenvolvimento) |
| `npm run test:auth` | Apenas testes de autenticação |
| `npm run test:user` | Apenas testes de CRUD |
| `npm run test:debug` | Debug interativo |

---

## 🔍 Verificar Status

```powershell
# Listar testes
npm test -- --list

# Executar um teste específico
npm test -- auth.spec.ts

# Modo verbose (mais informações)
npm test -- --verbose
```

---

## 📊 Relatório

```powershell
# Ver último relatório HTML
npx playwright show-report

# Limpar resultados antigos
rm -r test-results
rm -r playwright-report
```

---

## 🐛 Troubleshooting

### "Error: Process from config.webServer was not able to start"
```powershell
# Certifique-se de que está no diretório correto
cd Application.Client\e2e

# Verifique se npm start funciona
cd ..
npm start
# (Ctrl+C para parar)

# Retorne para e2e
cd e2e
```

### "Connection refused - localhost:4200"
- ✅ Frontend deve estar rodando (iniciado automaticamente)
- ✅ Aguarde ~5-10s para Angular compilar
- ✅ Se manual, execute `npm start` em Application.Client

### "Connection refused - localhost:5000 (backend)"
- ✅ Backend DEVE estar rodando: `dotnet run --project Application.Web`
- ✅ Verifique se está em Development mode
- ✅ Cheque se RavenDB/Redis estão acessíveis

---

## 💡 Dicas

1. **Desenvolvimento:** Use `npm run test:ui` para UI interativa
2. **Debugging:** Use `npm run test:debug` para parar em breakpoints
3. **CI/CD:** Adicione `--headed=false` para modo headless
4. **Performance:** Testes rodando em paralelo por padrão

---

## 📚 Documentação

- Guia completo: [README.md](README.md)
- Setup detalhado: [SETUP.md](SETUP.md)
- Fixtures reutilizáveis: [tests/fixtures.ts](tests/fixtures.ts)
- Testes de auth: [tests/auth.spec.ts](tests/auth.spec.ts)
- Testes de CRUD: [tests/user.spec.ts](tests/user.spec.ts)

---

**Última atualização:** janeiro/2026
