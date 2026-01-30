# Agentes

Agentes especializados que coordenam para entregar features completas.

## 📋 Índice Rápido

**Novo à estrutura?** Comece pelo [INDEX.md](INDEX.md)

- **7 agentes totais:** 1 orquestrador + 6 especialistas
- **Escopo:** Backend, Frontend, Regras de Negócio, Modelagem
- **Stack:** ASP.NET Core 8, Angular 18, RavenDB, Redis, Prometheus

## 🎯 Agentes

### Orquestrador
- **[feature-agent](feature-agent.md)** - Coordena especialistas ponta-a-ponta

### Especialistas
- **[business-analyst](business-analyst.md)** - Requisitos, regras, modelagem
- **[backend-architect](backend-architect.md)** - API design, padrões distribuídos
- **[dotnet-architect](dotnet-architect.md)** - ASP.NET Core, C#, CQRS, DDD
- **[frontend-developer](frontend-developer.md)** - Angular 18, TypeScript, Tailwind
- **[design-system-architect](design-system-architect.md)** - Tokens, componentes, theming
- **[tdd-orchestrator](tdd-orchestrator.md)** - Testes: unit, integration, E2E
- **[code-reviewer](code-reviewer.md)** - Segurança, performance, SOLID

## 📖 Referências Obrigatórias

Todos os agentes devem seguir:
- **[Architecture Contract](../architecture-contract.md)** - Padrões arquiteturais
- **[Copilot Instructions](../copilot-instructions.md)** - Setup e ciclo de trabalho

## 🚀 Como Começar

1. **Você é feature-agent?** → Leia [feature-agent.md](feature-agent.md)
2. **Você é especialista?** → Leia seu arquivo
3. **Entendendo fluxo?** → Consulte [INDEX.md](INDEX.md)

## 🔗 Útil

- `.github/architecture-contract.md` - Obrigações técnicas
- `.github/copilot-instructions.md` - Ciclo Plan→Act→Reflect
- `docs/` - Documentação do projeto
