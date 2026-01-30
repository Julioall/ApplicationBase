# Instruções de Trabalho para Agentes (Feature Agent & Especialistas)

Este documento fornece as instruções práticas e o contexto essencial para os agentes executarem suas tarefas de forma coordenada.

## 1. Contexto do Projeto e Stack Tecnológica

O projeto é uma aplicação full-stack que segue o Contrato Arquitetural (`.github/architecture-contract.md`).

- **Documentação e Recursos Importantes**
- Consulte `docs/features/moodle.md`, `docs/moodle.md`, `docs/plan/<feature>.md` e `docs/agents/external-access.md` apenas quando surgir uma dúvida específica sobre um endpoint ou configuração do Moodle; não é obrigatório ler todo o conteúdo para cada tarefa.
- Todas as variáveis de ambiente relevantes estão listadas em `docker-compose.yml` e `.env.template`. O sistema roda dentro do compose especificado pelo repositório; inspeção desse arquivo é obrigatória ao trabalhar com integrações em Moodle.
- A regra mestre: qualquer chamada que possa modificar dados no Moodle é proibida. Use apenas endpoints de leitura (GET) e nunca execute comandos que criem, atualizem ou deletem registros no Moodle. Em caso de dúvida, leia primeiro a documentação mencionada e evite supor permissões.

| Camada | Tecnologia | Versão | Observações |
| :--- | :--- | :--- | :--- |
| **Backend** | ASP.NET Core | 8 | Arquitetura Clean/Onion. Usa RavenDB para persistência. |
| **Frontend** | Angular | 18 | Usa Tailwind CSS para estilização e ngx-translate para i18n. |
| **Banco de Dados** | RavenDB | - | Usado como banco de documentos e cache. |

## 2. Comandos Essenciais de Validação

Todos os agentes DEVEM validar seu trabalho antes de entregar:

| Ação | Comando | Propósito |
| :--- | :--- | :--- |
| **Build** | `dotnet build` | Compilar backend e verificar erros. |
| **Test** | `dotnet test` | Executar testes unitários e integração. |
| **Lint** | `npm run lint` | Verificar padrões de código (Angular/TypeScript). |

## 3. Papéis e Responsabilidades

### Feature-Agent (Orquestrador)

**Não implementa direto.** Coordena especialistas:

1. **Plan** - Analisa issue, entende escopo
2. **Orquestre** - Invoca especialistas corretos
3. **Reflect** - Valida entrega final

**Fluxo típico:**
```
@business-analyst "Defina regras de negócio"
  ↓
@backend-architect "Desenhe API"
  ↓
@design-system-architect "Defina tokens e componentes"
  ↓
@dotnet-architect "Implemente backend"
  ↓
@frontend-developer "Implemente frontend"
  ↓
@tdd-orchestrator "Escreva testes"
  ↓
@code-reviewer "Valide código"
  ↓
Pull Request
```

### Especialistas (Executores)

Cada especialista é invocado pelo feature-agent para sua atividade específica:

- **business-analyst:** Requisitos, regras de negócio, modelagem
- **backend-architect:** API design, padrões distribuídos, microsserviços
- **dotnet-architect:** Implementação ASP.NET Core, C#, CQRS, DDD
- **design-system-architect:** Design tokens, componentes, theming
- **frontend-developer:** Implementação Angular 18, TypeScript, Tailwind
- **tdd-orchestrator:** Testes unit, integration, E2E
- **code-reviewer:** Segurança, performance, SOLID

## 4. Critérios de Qualidade (Definition of Done)

Um Pull Request só é pronto para merge se **TODOS** os critérios forem atendidos:

2.  **Testes Passando:** Todos os testes (unitários e integração) passando (`dotnet test` sem falhas).
3.  **Aderência Arquitetural:** Código respeita `architecture-contract.md`.
4.  **Documentação Atualizada:** Documentação em `docs/features/<feature-name>.md`.
5.  **Erro Handling:** Backend segue padrão ProblemDetails (RFC 7807).
6.  **E2E Tests:** Fluxos críticos cobertos com Playwright.

## 5. Como Feature-Agent Invoca Especialistas

O **feature-agent** coordena assim:

```
@business-analyst "Defina as regras de negócio para [requisito]"
```
Retorna: Especificação de requisitos, DTOs, validadores

```
@backend-architect "Desenhe a API para [requisito]"
```
Retorna: Especificação de endpoints, padrões

```
@design-system-architect "Defina tokens e componentes para [requisito]"
```
Retorna: Design tokens, componentes reutilizáveis

```
@dotnet-architect "Implemente [command/query] com [requisitos]"
```
Retorna: Handler, Service, Repository, Testes

```
@frontend-developer "Crie [página/componente] com [requisitos]"
```
Retorna: Componentes, Serviços, Testes E2E

```
@tdd-orchestrator "Escreva testes para [fluxo]"
```
Retorna: Testes unit, integration, E2E

```
@code-reviewer "Revise código para segurança e performance"
```
Retorna: Feedback e aprovação

## 6. Stack Tecnológico (Obrigatório)
