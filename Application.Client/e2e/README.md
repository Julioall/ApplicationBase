# E2E Tests - ApplicationBase

Testes de ponta-a-ponta com **Playwright** para validar fluxos críticos da aplicação.

## 📋 Fluxos Testados

### 1. **Autenticação (Auth Flow)** - `auth.spec.ts`
- ✅ Redirecionamento para login (usuário não autenticado)
- ✅ Login com credenciais válidas
- ✅ Login com credenciais inválidas
- ✅ Logout e limpeza de token
- ✅ Refresh token automático
- ✅ Navegação protegida (401)

### 2. **User CRUD** - `user.spec.ts`
- ✅ **CREATE:** Criar novo usuário
- ✅ **READ:** Listar usuários com paginação
- ✅ **READ:** Buscar usuário por email
- ✅ **UPDATE:** Atualizar dados
- ✅ **DELETE:** Deletar usuário com confirmação
- ✅ **VALIDATION:** Email duplicado
- ✅ **VALIDATION:** Email inválido
- ✅ **VALIDATION:** Senha fraca
- ✅ **PERFORMANCE:** Renderização de 1000+ usuários

## 🚀 Instalação

```bash
cd Application.Client/e2e
npm install
```

### Requisitos de Ambiente

Antes de executar os testes, certifique-se de que:

1. **Backend está rodando**
   ```bash
   # Terminal 1
   dotnet run --project Application.Web
   ```

2. **Frontend será iniciado automaticamente pelo Playwright**
   - O comando `npm test` inicia `npm start` em `../` (Application.Client)
   - Acessa https://localhost:4200

3. **Docker (se necessário)**
   ```bash
   # Terminal (se precisar de RavenDB, Redis, etc)
   docker compose up
   ```

## 📝 Executar Testes

### Executar todos os testes
```bash
npm test
```

### Modo debug (interativo)
```bash
npm run test:debug
```

### UI Mode (recomendado para desenvolvimento)
```bash
npm run test:ui
```

### Apenas testes de autenticação
```bash
npm run test:auth
```

### Apenas testes de usuário
```bash
npm run test:user
```

## 🔧 Configuração

### `playwright.config.ts`
- **baseURL:** https://localhost:4200 (ajuste conforme necessário)
- **browsers:** Chromium + Firefox
- **screenshots:** Apenas em falhas
- **trace:** Armazenado em falhas (debugging)

### Variáveis de Ambiente

Criar arquivo `.env` se necessário:

```env
BASEURL=https://localhost:4200
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=AdminPassword123!
```

## 📊 Relatórios

Após execução, os relatórios estão em:

```bash
# HTML Report
npx playwright show-report

# Testes com falha
# - Screenshots em: test-results/
# - Videos em: test-results/ (se configurado)
```

## 🎯 Estrutura

```
e2e/
├── tests/
│   ├── auth.spec.ts       # Fluxos de autenticação
│   ├── user.spec.ts       # CRUD de usuários
│   └── [outros].spec.ts   # Adicione conforme necessário
├── playwright.config.ts    # Configuração do Playwright
├── package.json
└── README.md
```

## 💡 Boas Práticas

1. **Isolamento:** Cada teste cria seus próprios dados
2. **Limpeza:** Dados de teste devem ser removidos ao final
3. **Padrão AAA:** Arrange → Act → Assert
4. **Selectors:** Preferir `getByRole`, `getByPlaceholder`, `getByText` em vez de CSS
5. **Esperas:** Usar `waitForURL`, `waitForLoadState`, não `waitForTimeout`

## 🐛 Debugging

### Ver logs detalhados
```bash
npx playwright test --debug
```

### Gravar vídeos de falhas
Já configurado em `playwright.config.ts`:
```typescript
use: {
  video: 'retain-on-failure',
}
```

### Inspecionar elementos
```bash
npx playwright codegen https://localhost:4200
```

## 🔐 Credenciais para Testes

Use variáveis de ambiente ou atualize os testes com credenciais válidas:

```typescript
const validEmail = process.env.TEST_EMAIL || 'test@example.com';
const validPassword = process.env.TEST_PASSWORD || 'TestPassword123!';
```

## 📌 Próximos Passos

- [ ] Adicionar testes de notificações (WhatsApp, Email)
- [ ] Adicionar testes de integração com Moodle (GET only)
- [ ] Adicionar testes de relatórios (export, gráficos)
- [ ] Integrar com CI/CD (GitHub Actions)
- [ ] Testes de acessibilidade (a11y)

## 📚 Recursos

- [Playwright Docs](https://playwright.dev)
- [Test Best Practices](https://playwright.dev/docs/best-practices)
- [Debugging Tests](https://playwright.dev/docs/debug)
- [CI/CD Integration](https://playwright.dev/docs/ci)

---

**Última atualização:** janeiro/2026  
**Mantido por:** Feature Agent (Orquestrador)
