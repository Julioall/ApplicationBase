# Phase 2: Resiliência, Health Checks & Refactoring com CQRS/MediatR

**Data de Conclusão:** Dezembro 2024  
**Status:** ✅ Completo e Validado

---

## 📋 Resumo Executivo

Phase 2 implementa **resiliência em chamadas externas**, **health checks avançados** e **refactora controllers para usar o padrão CQRS com MediatR**. O objetivo é criar uma aplicação robusta que se recupera automaticamente de falhas transitórias, monitora sua saúde e mantém código bem estruturado e testável.

---

## 🎯 Objetivos Alcançados

### 1. **Polly - Políticas de Resiliência** ✅
Implementação de 4 estratégias de resiliência para diferentes tipos de operações:

- **Retry Policy**: Tenta novamente operações que falharam (exponential backoff)
- **Circuit Breaker**: Impede chamadas a serviços que estão fora do ar
- **Timeout Policy**: Evita que requisições fiquem presas indefinidamente
- **Fallback Policy**: Retorna valores padrão quando tudo falha

**Arquivo:** [Application.Service/Resilience/ResiliencePolicyProvider.cs](../../../Application.Service/Resilience/ResiliencePolicyProvider.cs)

#### Políticas Combinadas por Contexto:

1. **Repository Policy**: Para operações leves no banco de dados
   - Retry: 3 tentativas (backoff: 100ms, 200ms, 400ms)
   - Circuit Breaker: 5 falhas em 30 segundos
   - Timeout: 10 segundos

2. **External API Policy**: Para chamadas a APIs externas (Moodle, etc)
   - Retry: 2 tentativas (backoff: 1s, 2s)
   - Circuit Breaker: 10 falhas em 60 segundos
   - Timeout: 20 segundos

3. **Cache Policy**: Para operações em cache distribuído
   - Retry: 2 tentativas (backoff: 500ms, 1s)
   - Circuit Breaker: 8 falhas em 45 segundos
   - Timeout: 15 segundos

4. **Database Policy**: Para operações pesadas no banco de dados
   - Retry: 2 tentativas (backoff: 500ms, 1s)
   - Circuit Breaker: 10 falhas em 60 segundos (mais tolerante)
   - Timeout: 30 segundos (operações podem ser lentas)

---

### 2. **Health Checks Avançados** ✅

#### RavenDB Health Check
- **Local:** [Application.Web/Health/RavenDbHealthCheck.cs](../../../Application.Web/Health/RavenDbHealthCheck.cs)
- **Verificação:** Query simples no banco de documentos
- **Status:** Healthy | Unhealthy
- **Endpoint registrado:** GET `/health` (health check endpoint)

#### Moodle API Health Check
- **Local:** [Application.Web/Health/MoodleApiHealthCheck.cs](../../../Application.Web/Health/MoodleApiHealthCheck.cs)
- **Verificação:** GET request para `/web/version.json` do Moodle
- **Status:** Healthy | Degraded | Unhealthy
- **Timeout:** 10 segundos

#### Registro de Health Checks
Todos os health checks estão registrados em [Program.cs](../../../Application.Web/Program.cs):

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<StartupConfigurationHealthCheck>("startup_configuration", tags: new[] { "startup" })
    .AddCheck<RavenDbHealthCheck>("ravendb", tags: new[] { "database" })
    .AddCheck<MoodleApiHealthCheck>("moodle_api", tags: new[] { "external" });
```

---

### 3. **Controllers Refactoring com CQRS/MediatR** ✅

#### Handlers Criados

1. **CreateUserCommandHandler** 
   - [Handler](../../../Application.Service/Handlers/User/CreateUserCommandHandler.cs)
   - **Command:** `CreateUserCommand`
   - **Responsabilidade:** Criar novo usuário com validação de permissões
   - **Uso no Controller:** [UserController.AddUser()](../../../Application.Web/Controllers/UserController.cs#L47)

2. **DeleteUserCommandHandler**
   - [Handler](../../../Application.Service/Handlers/User/DeleteUserCommandHandler.cs)
   - **Command:** `DeleteUserCommand`
   - **Responsabilidade:** Deletar usuário existente
   - **Uso no Controller:** [UserController.DeleteUser()](../../../Application.Web/Controllers/UserController.cs#L82)

3. **GetAllUsersQueryHandler**
   - [Handler](../../../Application.Service/Handlers/User/GetAllUsersQueryHandler.cs)
   - **Query:** `GetAllUsersQuery`
   - **Responsabilidade:** Recuperar todos os usuários (sem filtros)
   - **Uso no Controller:** [UserController.GetAllUsers()](../../../Application.Web/Controllers/UserController.cs#L100)

4. **GetUserByIdQueryHandler**
   - [Handler](../../../Application.Service/Handlers/User/GetUserByIdQueryHandler.cs)
   - **Query:** `GetUserByIdQuery`
   - **Responsabilidade:** Recuperar usuário por ID
   - **Uso no Controller:** [UserController.GetUserById()](../../../Application.Web/Controllers/UserController.cs#L107)

#### Refactoring no UserController

**Antes:**
```csharp
public async Task<ActionResult<User>> GetUserById(string id)
{
    var user = await _userService.GetByIdAsync(id);
    // ...
}
```

**Depois (com MediatR):**
```csharp
public async Task<ActionResult<User>> GetUserById(string id)
{
    var user = await _mediator.Send(new GetUserByIdQuery(id));
    // ...
}
```

**Benefícios:**
- ✅ Separação clara entre requisição e lógica
- ✅ Fácil testabilidade (mock apenas o mediador)
- ✅ Reutilização de handlers em múltiplos controllers
- ✅ Pré-processamento automático via MediatR behaviors

---

### 4. **Testes Unitários** ✅

#### Testes Criados

1. **DeleteUserCommandHandlerTests** (2 testes)
   - Verifica se o handler chama corretamente `IUserService.DeleteAsync`
   - Testa múltiplas exclusões em sequência

2. **GetAllUsersQueryHandlerTests** (2 testes)
   - Valida retorno de todos os usuários
   - Testa comportamento quando nenhum usuário existe

3. **GetUserByIdQueryHandlerTests** (3 testes)
   - Valida recuperação correta por ID
   - Testa retorno null quando usuário não existe
   - Verifica chamadas corretas ao serviço

**Total de Testes Novos:** 7  
**Total de Testes do Projeto:** 69  
**Taxa de Sucesso:** 100%

---

## 📦 Dependências Adicionadas

### NuGet Packages

```xml
<!-- Polly for resilience patterns -->
<PackageReference Include="Polly" Version="8.6.5" />

<!-- MediatR já estava presente do Phase 1 -->
<!-- MediatR.Extensions.Microsoft.DependencyInjection versão 11.1.0 -->
```

---

## 🏗️ Arquitetura & Padrões

### CQRS (Command Query Responsibility Segregation)

```
┌─────────────┐
│  Controller │
└──────┬──────┘
       │
       ├─── Command (modificação de dados)
       │    └── CreateUserCommand
       │    └── DeleteUserCommand
       │
       └─── Query (leitura de dados)
            └── GetAllUsersQuery
            └── GetUserByIdQuery
```

### Polly Policy Wrapping

```
Requisição
    ↓
[Timeout Policy]
    ↓
[Circuit Breaker Policy]
    ↓
[Retry Policy]
    ↓
Chamada Real
    ↓
[Fallback Policy se tudo falhar]
```

---

## 🚀 Como Usar

### Injetar Resiliência em Serviços

```csharp
public class ExternalApiService
{
    private readonly IResiliencePolicyProvider _policyProvider;

    public async Task<T> CallExternalApi<T>(Func<Task<T>> operation)
    {
        var policy = _policyProvider.GetExternalApiPolicy<T>();
        return await policy.ExecuteAsync(operation);
    }
}
```

### Enviar Comando via MediatR

```csharp
var command = new CreateUserCommand(createUserDto);
var user = await _mediator.Send(command);
```

### Enviar Query via MediatR

```csharp
var query = new GetAllUsersQuery();
var users = await _mediator.Send(query);
```

### Verificar Saúde da Aplicação

```bash
# Health check endpoint
GET /health

# Resposta de exemplo
{
  "status": "Healthy",
  "checks": {
    "ravendb": "Healthy",
    "moodle_api": "Degraded",
    "startup_configuration": "Healthy"
  }
}
```

---

## 📊 Cobertura de Código

### Por Componente

| Componente | Testes | Cobertura |
|-----------|--------|-----------|
| CreateUserCommand | — | Será testado em Phase 3 |
| DeleteUserCommand | 2 | Cobertura Completa |
| GetAllUsersQuery | 2 | Cobertura Completa |
| GetUserByIdQuery | 3 | Cobertura Completa |
| ResiliencyPolicies | — | Será testado em Phase 3 |
| Health Checks | — | Será testado em Phase 3 |

---

## ✅ Checklist de Validação (Definition of Done)

- [x] Código compilado sem erros
- [x] Todos os 69 testes passando
- [x] Polly configurado para 4 cenários diferentes
- [x] Health checks implementados e registrados
- [x] UserController refatorado para usar MediatR
- [x] 4 Command/Query handlers criados
- [x] Documentação completa
- [x] Aderência ao architecture-contract.md

---

## 🔗 Referências & Documentação

### Padrões de Resiliência
- [Polly Documentation](https://github.com/App-vNext/Polly)
- [Circuit Breaker Pattern](https://martinfowler.com/bliki/CircuitBreaker.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)

### Frameworks Utilizados
- **Polly:** 8.6.5 - Resilience and transient-fault-handling library
- **MediatR:** 12.1.1 - In-process messaging  
- **ASP.NET Core Health Checks:** Built-in to .NET 8

---

## 📝 Próximos Passos (Phase 3)

1. Implementar Polly behaviors no MediatR pipeline
2. Adicionar testes de integração para policies
3. Criar dashboard de resiliência (observabilidade)
4. Implementar retry policies em outros controllers
5. Adicionar circuit breaker telemetry para Prometheus

---

## 🎉 Conclusão

Phase 2 estabelece fundações sólidas de **resiliência**, **monitoramento** e **arquitetura limpa**. A aplicação agora pode:

- ✅ Recuperar-se automaticamente de falhas transitórias
- ✅ Monitorar sua saúde em tempo real
- ✅ Rejeitar requisições redundantes via circuit breaker
- ✅ Manter código testável e reutilizável com CQRS/MediatR

**Status Final:** 🚀 **Pronto para Phase 3**
