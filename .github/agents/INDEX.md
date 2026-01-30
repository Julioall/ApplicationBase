# Índice de Agentes

Total: **8 agentes** (orquestrador + 7 especialistas)

---

## 🎯 Orquestrador

| Agente | Arquivo | Responsabilidade |
|--------|---------|------------------|
| **feature-agent** | [`feature-agent.md`](feature-agent.md) | Coordena especialistas ponta-a-ponta |

---

## 🛠️ Especialistas Executores

### 📊 Modelagem & Regras de Negócio

| Agente | Arquivo | Responsabilidade |
|--------|---------|------------------|
| **business-analyst** | [`business-analyst.md`](business-analyst.md) | Requisitos, regras, modelagem de dados |

### 🔧 Backend

| Agente | Arquivo | Responsabilidade |
|--------|---------|------------------|
| **backend-architect** | [`backend-architect.md`](backend-architect.md) | API design, padrões distribuídos |
| **dotnet-architect** | [`dotnet-architect.md`](dotnet-architect.md) | ASP.NET Core 8, C#, CQRS, DDD |

### 🎨 Frontend & Design

| Agente | Arquivo | Responsabilidade |
|--------|---------|------------------|
| **frontend-developer** | [`frontend-developer.md`](frontend-developer.md) | Angular 18, TypeScript, Tailwind |
| **design-system-architect** | [`design-system-architect.md`](design-system-architect.md) | Tokens, componentes, theming |

### ✅ Testes & Qualidade

| Agente | Arquivo | Responsabilidade |
|--------|---------|------------------|
| **tdd-orchestrator** | [`tdd-orchestrator.md`](tdd-orchestrator.md) | Testes: unit, integration, E2E |
| **code-reviewer** | [`code-reviewer.md`](code-reviewer.md) | Segurança, performance, SOLID |

---

## 📋 Fluxo Típico

```
Issue
 ↓
@feature-agent invoca:
 ├─ @business-analyst         → Entende requisitos
 ├─ @backend-architect        → Desenha API
 ├─ @design-system-architect  → Define tokens e componentes
 ├─ @dotnet-architect         → Implementa backend
 ├─ @frontend-developer       → Implementa frontend
 ├─ @tdd-orchestrator         → Testa tudo
 └─ @code-reviewer            → Valida qualidade
 ↓
Pull Request pronto
```

---

## 🚫 Agentes Removidos (Fora do Escopo)

**Razão:** Foco em backend (ASP.NET Core), frontend (Angular 18), design system e regras de negócio apenas.

---

## 💡 Como Usar

1. **Você é feature-agent?** Leia [`feature-agent.md`](feature-agent.md)
2. **Você é especialista?** Leia o arquivo correspondente
3. **Dúvida?** Consulte este INDEX.md

---

**Última atualização:** 30 jan 2026
