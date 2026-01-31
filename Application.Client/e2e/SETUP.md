# E2E Tests - Setup & Documentação

## 📦 Instalação

```bash
cd c:\Users\Julio\Desktop\Repositorios\ApplicationBase\Application.Client\e2e
npm install
```

## 🎯 Estrutura de Testes

### `tests/auth.spec.ts` - Fluxos de Autenticação
Valida todo o ciclo de autenticação JWT:
- Redirecionamento para login
- Login com credenciais válidas/inválidas
- Logout e limpeza de sessão
- Refresh token automático
- Acesso a recursos protegidos

**Casos de teste:** 6  
**Tempo estimado:** 30s

### `tests/user.spec.ts` - CRUD de Usuários
Testa operações completas de usuários:
- CREATE: Criação com validação
- READ: Listagem e busca
- UPDATE: Edição de dados
- DELETE: Remoção com confirmação
- VALIDATION: Email, senha, etc
- PERFORMANCE: Renderização em larga escala

**Casos de teste:** 9  
**Tempo estimado:** 60s

### `tests/fixtures.ts` - Helpers Compartilhados
Funções reutilizáveis:
- `loginAs(page, email, password)` - Login simples
- `logout(page)` - Logout
- `getAuthToken(page)` - Obter token JWT
- `setAuthToken(page, token)` - Injetar token
- `waitForApiResponse(page, pattern)` - Aguardar resposta da API

## 🚀 Executar Testes

### Modo padrão (headless)
```bash
npm test
```

### Modo UI (desenvolvimento)
```bash
npm run test:ui
```

### Debug interativo
```bash
npm run test:debug
```

### Apenas auth
```bash
npm run test:auth
```

### Apenas user CRUD
```bash
npm run test:user
```

## 📊 Relatórios

Após execução:
```bash
npx playwright show-report
```

Arquivos gerados:
- `test-results/` - Screenshots e vídeos de falhas
- `playwright-report/` - Relatório HTML

## ⚙️ Configuração

### `playwright.config.ts`
- **baseURL:** https://localhost:4200
- **timeout:** 30s por teste
- **retries:** 2 em CI, 0 localmente
- **browsers:** Chromium + Firefox
- **screenshot:** Apenas em falhas
- **trace:** Armazenado em falhas

## ⚙️ Configuração

### `playwright.config.ts`
- **baseURL:** https://localhost:4200
- **webServer:** Inicia `npm start` a partir de `../` (Application.Client)
- **timeout:** 30s por teste
- **retries:** 2 em CI, 0 localmente
- **browsers:** Chromium + Firefox
- **screenshot:** Apenas em falhas
- **trace:** Armazenado em falhas

### Serviços Necessários

Antes de rodar `npm test`, inicie:

```bash
# Terminal 1: Backend (OBRIGATÓRIO)
cd c:\Users\Julio\Desktop\Repositorios\ApplicationBase
dotnet run --project Application.Web

# Terminal 2: Docker services (se necessário para RavenDB, Redis, etc)
docker compose up

# Terminal 3: E2E Tests (inicia frontend automaticamente)
cd Application.Client/e2e
npm test
```

**IMPORTANTE:** O Playwright iniciará o frontend automaticamente, mas o backend DEVE estar rodando manualmente.

## 🔐 Credenciais

Os testes usam credenciais fictícias. Para usar credenciais reais, atualizar:

```typescript
// tests/auth.spec.ts
const validEmail = 'seu-email@example.com';
const validPassword = 'sua-senha-segura';
```

Ou usar variáveis de ambiente:
```bash
export TEST_EMAIL=admin@example.com
export TEST_PASSWORD=AdminPassword123!
npm test
```

## 🐛 Troubleshooting

### "Timeout esperando URL /dashboard"
- ✅ Backend rodando? (`dotnet run`)
- ✅ Frontend rodando? (`npm start`)
- ✅ Credenciais corretas?

### "Failed to connect to localhost:4200"
- ✅ Frontend iniciado? (`npm start` em Application.Client)
- ✅ Porta 4200 disponível?

### Testes passam localmente mas falham no CI
- Adicionar `--headed` em CI para ver o browser
- Aumentar timeout: `timeout: 60000` em fixtures
- Verificar credenciais em CI/CD secrets

## 📈 Próximos Passos

1. **Integração CI/CD:**
   ```yaml
   # .github/workflows/e2e.yml
   - run: npm install
     working-directory: Application.Client/e2e
   - run: npm test
     working-directory: Application.Client/e2e
   ```

2. **Testes Adicionais:**
   - [ ] Fluxos de notificação (WhatsApp, Email)
   - [ ] Integração Moodle (GET endpoints)
   - [ ] Relatórios e export
   - [ ] Testes de acessibilidade

3. **Performance:**
   - [ ] Lighthouse CI
   - [ ] Load testing com k6
   - [ ] Network throttling tests

## 📚 Recursos

- [Playwright Docs](https://playwright.dev)
- [Best Practices](https://playwright.dev/docs/best-practices)
- [CI Integration](https://playwright.dev/docs/ci)
- [Debugging](https://playwright.dev/docs/debug)

---

**Atualizado:** janeiro/2026
