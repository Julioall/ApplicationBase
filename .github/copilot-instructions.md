# Instruções de Trabalho para Agentes Autônomos (Feature Agent)

Este documento fornece as instruções práticas e o contexto essencial para o `feature-agent` executar suas tarefas de forma autônoma.

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

O `feature-agent` DEVE executar estes comandos para validar seu trabalho antes de solicitar a revisão:

| Ação | Comando (Exemplo) | Propósito |
| :--- | :--- | :--- |
| **Build** | `dotnet build` | Compilar o backend e verificar erros de sintaxe/referência. |
| **Test** | `dotnet test` | Executar todos os testes unitários e de integração. |
| **Lint** | `npm run lint` | (Assumindo um comando padrão para o Angular/TS) Verificar padrões de código e estilo. |

## 3. Critérios de Qualidade (Definition of Done)

Um Pull Request só é considerado pronto para o `review-agent` se **TODOS** os critérios abaixo forem atendidos:

1.  **Código Implementado:** A feature está funcional e implementada de ponta a ponta (UI, API, Dados).
2.  **Testes Passando:** Todos os testes automatizados (unitários e de integração) foram escritos e estão passando (`dotnet test` sem falhas).
3.  **Aderência Arquitetural:** O código respeita integralmente o `.github/architecture-contract.md`.
4.  **Documentação Atualizada:** A documentação da feature foi criada ou atualizada em `docs/features/<feature-name>.md`.
5.  **Tratamento de Erros:** O tratamento de erros no backend segue o padrão ProblemDetails (RFC 7807).

## 4. Invocação de Especialistas (Consultoria Sob Demanda)

O `feature-agent` deve invocar agentes especializados apenas para **consultoria** em pontos de decisão ou desafios complexos.

| Agente Especialista | Exemplo de Invocação | Propósito |
| :--- | :--- | :--- |
| `@backend-architect` | `@backend-architect "Proponha um novo DTO para o endpoint /users/{id}/profile"` | Definição de contratos de API, padrões de microsserviços ou arquitetura de dados. |
| `@tdd-orchestrator` | `@tdd-orchestrator "Analise este teste de integração e sugira melhorias de cobertura"` | Estratégias complexas de teste, depuração ou refatoração. |
| `@dotnet-architect` | `@dotnet-architect "Como implementar um cache distribuído com Redis para o serviço de produtos?"` | Arquitetura e desenvolvimento de alto nível em ASP.NET Core e C#. |
| `@typescript-pro` | `@typescript-pro "Refatore esta interface para usar Mapped Types e torná-la mais flexível."` | Tipagem avançada, generics e otimização de código TypeScript. |
| `@javascript-pro` | `@javascript-pro "Sugira a melhor forma de lidar com múltiplas chamadas assíncronas em um componente React."` | Otimização de código JavaScript, padrões assíncronos e compatibilidade. |
| `@monorepo-architect` | `@monorepo-architect "Qual a melhor estratégia de build caching para o nosso pipeline de CI?"` | Configuração de monorepo, otimização de build e gerenciamento de dependências. |
| `@docs-architect` | `@docs-architect "Liste as documentações disponíveis sobre Moodle, endpoints read-only e variáveis de ambiente para Docker compose."` | Ajuda a localizar documentação e contexto operacional existente. |
| `@code-reviewer` | `@code-reviewer "Revise este trecho de código para segurança e performance."` | Análise de segurança, performance e qualidade de código. |
| `@docs-architect` | `@docs-architect "Qual a melhor estrutura para um manual técnico de 50 páginas sobre o sistema de autenticação?"` | Arquitetura de documentação técnica de longo prazo e manuais. |
| `@tutorial-engineer` | `@tutorial-engineer "Crie um tutorial passo a passo para onboarding de novos desenvolvedores na camada de dados."` | Criação de tutoriais, guias de aprendizado e conteúdo educacional. |
| `@mermaid-expert` | `@mermaid-expert "Gere um diagrama de sequência para o fluxo de login."` | Geração de diagramas (fluxo, sequência, ERD) em formato Mermaid. |
| `@c4-context` | `@c4-context "Gere o diagrama de contexto C4 para o sistema, incluindo o usuário e o serviço de pagamento externo."` | Documentação arquitetural C4 (Contexto, Container, Componente, Código). |
| `@graphql-architect` | `@graphql-architect "Proponha uma estratégia de paginação eficiente para a query de pedidos."` | Design de schema GraphQL, otimização de resolvers e padrões de API. |
| `@temporal-python-pro` | `@temporal-python-pro "Qual o padrão de workflow Temporal mais adequado para uma saga de processamento de pedidos?"` | Design de workflows duráveis e arquitetura de orquestração com Temporal em Python. |
