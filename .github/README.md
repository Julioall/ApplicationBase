# GitHub Configuration

Este diretório contém configuração de agentes, arquitetura e padrões para o projeto ApplicationBase.

---

## 📋 Índice

1. **[Contrato Arquitetural](#contrato-arquitetural)** - Obrigações de implementação
2. **[Agentes](#agentes)** - Papéis e responsabilidades
3. **[Instruções Copilot](#instruções-copilot)** - Setup e boas práticas
4. **[Fluxo de Trabalho](#fluxo-de-trabalho)** - Como trabalhar com agentes
5. **[Skills Opcionais](#skills-opcionais)** - Técnicas avançadas

---

## 📖 Contrato Arquitetural

**Arquivo:** [`architecture-contract.md`](architecture-contract.md)

**Obrigatório** para todos os agentes. Define:
- Stack tecnológico (ASP.NET Core 8, Redis, Prometheus, etc)
- Padrões arquiteturais (Clean Architecture, CQRS)
- Modelos de domínio (classes anêmicas obrigatórias)
- Cache distribuído (Redis, Cache-Aside)
- Observabilidade (Prometheus, Serilog)
- Resiliência (Polly policies)
- Testes (Unit, Integration, E2E)
- Boas práticas (Logging, HTTP Status Codes, DTOs)

---

## 🤖 Agentes

### Agente Core (Orquestrador)

#### **feature-agent** 
**Arquivo:** [`agents/feature-agent.md`](agents/feature-agent.md)

Agente orquestrador que coordena especialistas.

**Responsabilidades:**
- Planejar feature ponta-a-ponta
- Invocar especialistas para cada atividade
- Integrar as soluções
- Validar entrega final (build, testes)
- Entregar Pull Request

**Fluxo:**
```
Issue → Plan → Invoca Especialistas → Integra → Valida → PR
```

---

### Especialistas (Executores)

#### **business-analyst**
**Análise, Modelagem, Regras de Negócio**

- Requisitos e casos de uso
- Regras de negócio
- Modelagem de dados (UML)
- Histórias de usuário

---

#### **backend-architect**
**Design de API e Arquitetura Backend**

- API design (REST, GraphQL)
- Padrões distribuídos
- Microsserviços
- Estrutura de dados

---

#### **dotnet-architect**
**Implementação Backend C# / ASP.NET Core**

- Implementação em ASP.NET Core 8
- CQRS e DDD
- Padrões avançados
- Handlers e Services

---

#### **frontend-developer**
**Frontend com Angular 18**

- Componentes Angular
- TypeScript avançado
- Tailwind CSS
- ngx-translate (i18n)

---

#### **design-system-architect**
**Design System e Tokens**

- Design tokens (primitivos, semânticos)
- Componentes reutilizáveis
- Theming e customização
- Documentação de padrões

---

#### **tdd-orchestrator**
**Testes: Unit, Integration, E2E**

- Estratégia de testes
- Cobertura > 80%
- E2E tests com Playwright
- Depuração de falhas

---

#### **code-reviewer**
**Qualidade de Código**

- Segurança
- Performance
- SOLID principles
- Boas práticas

---

## 📝 Instruções Copilot

**Arquivo:** [`copilot-instructions.md`](copilot-instructions.md)

Setup e instruções essenciais para o agente feature-agent:
- Contexto do projeto
- Stack tecnológico
- Comandos de validação (build, test, lint)
- Critérios de qualidade (Definition of Done)
- Como invocar especialistas

---

## 🔄 Fluxo de Trabalho

### Ciclo Padrão

```
1. Issue Criada
   ↓
2. feature-agent (Orquestrador)
   ├─ Plan: Entender escopo
   ├─ Invoca: @business-analyst → @backend-architect → @design-system-architect
   ├─ Invoca: @dotnet-architect → @frontend-developer
   ├─ Invoca: @tdd-orchestrator
   ├─ Valida: dotnet build, dotnet test (deve passar)
   └─ Entrega: Pull Request
   ↓
3. code-reviewer (Validação Final)
   ├─→ Revisa segurança, performance, SOLID
   ├─→ Valida arquitetura e documentação
   └─→ Aprova ou pede correções
   ↓
4. Merge (se aprovado)
```

### Como Invocar Especialistas

O **feature-agent** invoca especialistas assim:

```
@business-analyst "Defina as regras de negócio para criar um novo usuário"
@backend-architect "Desenhe a API REST para criar usuários"
@design-system-architect "Defina tokens de cores e componentes de formulário"
@dotnet-architect "Implemente o handler CreateUserCommand"
@frontend-developer "Crie a página de cadastro de usuários"
@tdd-orchestrator "Escreva testes para o fluxo de cadastro"
@code-reviewer "Revise o código antes do merge"
```
@dotnet-architect "Implemente o handler CreateUserCommand"
@frontend-developer "Crie a página de cadastro de usuários"
@tdd-orchestrator "Escreva testes para o fluxo de cadastro"
```

---

## 🎯 Skills Opcionais

**Diretório:** [`skills/`](skills/)

Técnicas avançadas relevantes para o projeto:
- **api-design-principles** - Design de APIs REST/GraphQL
- **architecture-patterns** - Padrões arquitecturais (Clean, CQRS, DDD)
- **cqrs-implementation** - Implementação CQRS com MediatR
- **design-system-patterns** - Padrões de design system
- **microservices-patterns** - Padrões de microsserviços
- **responsive-design** - Design responsivo com Tailwind
- **tailwind-design-system** - Sistema de design com Tailwind CSS
- **typescript-advanced-types** - TypeScript avançado (generics, utility types)
- **web-component-design** - Padrões de componentes web

Use apenas quando explicitamente solicitado por especialistas.

---

## ✅ Checklist para Agentes

Antes de qualquer implementação, agentes devem:

- [ ] Ler [`architecture-contract.md`](architecture-contract.md) completamente
- [ ] Entender o padrão de classes anêmicas (lógica em Services)
- [ ] Conhecer ordem crítica dos behaviors MediatR
- [ ] Validar com: `dotnet build` e `dotnet test`
- [ ] Garantir E2E tests para fluxos críticos
- [ ] Usar ProblemDetails para erros
- [ ] Documentar arquitetura e impactos
- [ ] Invocar especialistas quando necessário

---

## 📚 Referências Rápidas

- **Contrato Arquitetural:** [`architecture-contract.md`](architecture-contract.md)
- **Copilot Instructions:** [`copilot-instructions.md`](copilot-instructions.md)
- **Feature Agent:** [`agents/feature-agent.md`](agents/feature-agent.md)
- **Code Reviewer:** [`agents/code-reviewer.md`](agents/code-reviewer.md)

---

## 🚀 Status

- ✅ Agentes core definidos (8 agentes: 1 orquestrador + 7 especialistas)
- ✅ Arquitetura contrato (v2.0) com Redis, Prometheus, E2E, Classes Anêmicas
- ✅ Instruções Copilot completas
- ✅ Especialistas disponíveis para consultoria
- ⏳ Skills (em desenvolvimento)
