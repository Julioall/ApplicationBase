# Contrato Arquitetural (Obrigatório)

Este contrato define **como** construir software neste repositório. Ele não define regras de negócio.
Todos os agentes devem ler e respeitar este documento antes de planejar, implementar, testar ou revisar.

**Última Atualização:** Phase 4 (30 de janeiro de 2026)  
**Status:** Versão 2.0 - Inclui Redis, Prometheus, E2E Tests

## Stack

### Backend
- ASP.NET Core 8
- Clean / Onion Architecture
- FluentValidation
- ProblemDetails (RFC 7807)
- JWT Authentication
- RavenDB (document database / L1 cache)
- Redis (distributed L2 cache via StackExchange.Redis)
- Prometheus (metrics e observabilidade)
- Polly (resilience: retry, circuit-breaker, timeout, fallback)
- MediatR (CQRS + Pipeline Behaviors)

### Frontend
- Angular 18
- Tailwind CSS
- ngx-translate (i18n)
- Interceptors para loading e error handling

## Princípios Obrigatórios
- Não inventar regras de negócio ou requisitos funcionais.
- Implementar soluções completas ponta‑a‑ponta quando aplicável (API + UI + dados).
- Respeitar fronteiras arquiteturais e camadas (não cruzar dependências indevidas).
- Preferir mudanças pequenas e incrementais.
- Documentar contratos e impactos antes de implementar.

### Modelos de Domínio (✅ OBRIGATÓRIO: Classes Anêmicas)

**Classes Anêmicas são o padrão OBRIGATÓRIO neste projeto.**

As entidades devem ser apenas **representantes de dados**. Toda lógica de negócio, validação e comportamento pertence aos **serviços**.

✅ **CORRETO - Classe Anêmica (apenas dados):**
```csharp
// Application.Domain/Model/User.cs
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Apenas properties - sem comportamento
    // Sem validação
    // Sem métodos de negócio
}
```

❌ **ERRADO - Classe com Comportamento (proibido):**
```csharp
public class User
{
    private string _passwordHash;
    
    public static User Create(string email, string passwordPlain) { /* ... */ }
    public bool ValidatePassword(string passwordPlain) { /* ... */ }
    public void UpdateEmail(string newEmail) { /* ... */ }
    
    // ❌ PROIBIDO: Lógica de negócio na entidade
}
```

**Fluxo Correto:**
1. **DTO (Controller)** → Recebe dados do frontend
2. **Validação** → Service executa validações (FluentValidation)
3. **Lógica de Negócio** → Service implementa regras
4. **Entidade (anêmica)** → Apenas armazena resultado
5. **Persistência** → Repository salva em RavenDB

**Exemplo Completo:**
```csharp
// ✅ Entidade: apenas dados
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; }
}

// ✅ DTO: para transferência
public class CreateUserRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

// ✅ Validator: validação de entrada
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).EmailAddress().NotEmpty();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}

// ✅ Service: toda a lógica de negócio
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;
    
    public async Task<User> CreateAsync(CreateUserRequest request)
    {
        // Validação
        if (await _repository.ExistsByEmailAsync(request.Email))
            throw new ConflictException("Email já registrado");
        
        // Lógica de negócio
        var passwordHash = _hasher.Hash(request.Password);
        
        // Criar entidade anêmica
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            PasswordHash = passwordHash,
            IsActive = true
        };
        
        // Persistir
        await _repository.AddAsync(user);
        return user;
    }
    
    public async Task<bool> ValidateLoginAsync(string email, string password)
    {
        // ✅ Lógica de login NO SERVIÇO
        var user = await _repository.GetByEmailAsync(email);
        if (user == null || !user.IsActive)
            return false;
        
        return _hasher.Verify(password, user.PasswordHash);
    }
}
```

**Princípios Obrigatórios:**
- ✅ Entidades são apenas containers de dados (properties públicas OK)
- ✅ Toda validação de negócio vai em Service + FluentValidation
- ✅ Toda regra de negócio vai em Service
- ✅ Sem métodos em entidades que fazem lógica
- ✅ Sem estado complexo nas entidades
- ✅ DTOs para transferência entre camadas (API ↔ UI)
- ❌ Não colocar validação ou comportamento em entidades
- ❌ Não usar métodos estáticos ou factory methods em entidades
- ❌ Não ter dependências injetadas em entidades

## Cache Distribuído (Redis)

### Estratégia Cache-Aside (Lazy Loading)
```
Request
  ↓
CachingBehavior: Verificar Redis
  ├─→ Cache HIT: Retornar → Response
  └─→ Cache MISS: Executar Handler
     ↓
     Handler executa (RavenDB)
     ↓
     CachingBehavior: Armazenar em Redis com TTL
     ↓
Response
```

### Obrigações:

1. **Queries Cacheáveis** devem implementar `ICachedQuery`:
```csharp
public class GetAllUsersQuery : IRequest<IReadOnlyCollection<User>>, ICachedQuery
{
    public int CacheDurationSeconds => 600; // 10 minutos
    public string? CacheKey => null; // Gerado automaticamente se null
}
```

2. **Commands (mutações)** NÃO devem ser cacheados
   - Apenas queries podem implementar `ICachedQuery`
   - Commands devem invalidar cache quando necessário

3. **Cache Keys são determinísticas**
   - Geradas via SHA256 do nome da query + parâmetros
   - Não necessário especificar manualmente

4. **Fallback Automático**
   - Se Redis está DOWN: usar InMemoryCacheService
   - Sistema continua funcionando (graceful degradation)
   - Log deve registrar fallback (warning level)

5. **Variáveis de Ambiente**
```env
REDIS_CONNECTION_STRING=redis:6379          # Docker Compose
REDIS_CONNECTION_STRING=localhost:6379      # Local dev
REDIS_CONNECTION_STRING=redis-cluster:6379  # Produção (cluster)
```

## Observabilidade (Prometheus + Serilog)

### Métricas Obrigatórias

Todas as métricas são coletadas automaticamente via `PrometheusMetricsBehavior`:

1. **handler_duration_milliseconds** (Histogram)
   - Latência de cada handler
   - Buckets: [10, 50, 100, 250, 500, 1000, 5000, 10000] ms
   - Labels: handler_name, status (success|error)

2. **handler_execution_total** (Counter)
   - Total de requisições por handler
   - Labels: handler_name, status

3. **handler_in_flight** (Gauge)
   - Handlers executando simultaneamente
   - Label: handler_name

### Comportamentos MediatR - ORDEM CRÍTICA ⚠️

A ordem de registro dos behaviors é **ESSENCIAL** para funcionamento correto:

```csharp
// ✅ CORRETO
options.AddOpenBehavior(typeof(PrometheusMetricsBehavior<,>));    // 1º: Capturar todas as métricas
options.AddOpenBehavior(typeof(CachingBehavior<,>));              // 2º: Verificar cache
options.AddOpenBehavior(typeof(ResiliencePolicyBehavior<,>));     // 3º: Aplicar policies

// ❌ ERRADO - Prometheus nunca executa se não estiver primeiro!
options.AddOpenBehavior(typeof(CachingBehavior<,>));
options.AddOpenBehavior(typeof(PrometheusMetricsBehavior<,>));    // ERRADO!
```

**Por quê?** Behaviors executam em cadeia. Se Cache retorna antes, Prometheus não executa.

### Acessar Métricas

```bash
# Prometheus format (compatível com Prometheus server)
curl http://localhost:5095/metrics

# Health check
curl http://localhost:5095/health/startup
```

## Resiliência (Polly)

### Políticas Aplicadas Automaticamente

1. **Query Handlers** (GET/READ)
   - Retry: 3 tentativas com backoff (100ms, 200ms, 400ms)
   - Circuit Breaker: Abre após 5 falhas em 30s
   - Timeout: 10 segundos
   - Fallback: Retorna null/default se falhar

2. **Command Handlers** (POST/PUT/DELETE)
   - Retry: 2 tentativas com backoff (500ms, 1s)
   - Circuit Breaker: Abre após 3 falhas em 20s
   - Timeout: 20 segundos
   - Fallback: Relança exceção (não silencia)

3. **External API Calls** (Moodle, WhatsApp, etc)
   - Retry: 2 tentativas com backoff (500ms, 1s)
   - Circuit Breaker: Abre após 3 falhas em 20s
   - Timeout: 5 segundos
   - Fallback: Cache local (RavenDB)

4. **Cache Operations** (Redis)
   - Retry: Nenhum
   - Circuit Breaker: Abre rapidamente
   - Timeout: 2 segundos
   - Fallback: InMemoryCacheService
- Usar FluentValidation para validações de entrada.
- Padronizar erros com ProblemDetails (RFC 7807).
- Garantir mensagens úteis sem expor dados sensíveis.

## Integração e Dados
- Toda mudança de contrato deve atualizar DTOs, endpoints e documentação.
- Considerar persistência e cache em RavenDB quando pertinente.
- Garantir consistência entre API, UI e modelos de domínio.
- Sincronizações ou workloads pesados contra o Moodle devem ser delegados a processos em background para não sobrecarregar o pipeline HTTP, usando caches locais (RavenDB) e filas internas para ações realizadas e dados derivados.
- Quando necessário, priorizar serviços de background (por exemplo, `BackgroundService` do ASP.NET Core) para reunir dados do Moodle, aplicar transformações e invalidar cache sem aumentar latência percebida.
- Avaliar a introdução de filas (ex.: RabbitMQ) para orquestrar cargas assíncronas e desacoplar consumidores quando sincronizações com Moodle ficarem mais complexas.

## Frontend
- Manter i18n via ngx-translate.
- Usar interceptors para loading e erros globais.
- Evitar lógica de negócio no frontend; focar em orquestração e UX.

## Boas Práticas Obrigatórias

### Logging (Serilog)
```csharp
// ✅ CORRETO: Informação útil sem dados sensíveis
_logger.LogInformation("User {UserId} logged in successfully", userId);
_logger.LogWarning("Cache fallback to InMemory for key {CacheKey}", key);
_logger.LogError(ex, "Failed to fetch users from external API after {Retries} retries", retries);

// ❌ ERRADO: Expor informações sensíveis
_logger.LogInformation("User password hash: {Hash}", passwordHash);
_logger.LogInformation($"User {user.Email} logged in"); // Sem estrutura
```

### HTTP Status Codes
- **200 OK:** Sucesso
- **201 Created:** Recurso criado
- **400 Bad Request:** Validação falhou (FluentValidation)
- **401 Unauthorized:** Sem autenticação
- **403 Forbidden:** Sem autorização
- **404 Not Found:** Recurso não existe
- **409 Conflict:** Violação de constraint (Ex.: Email duplicado)
- **500 Internal Server Error:** Erro não esperado
- **503 Service Unavailable:** Dependência down (Redis, RavenDB)

### DTOs vs Entities

**Separação clara e obrigatória:**

```csharp
// ❌ NUNCA faça isso:
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    // Entidade com lógica = PROIBIDO!
    public bool ValidateEmail() { /* ... */ }
}

// ✅ CORRETO:

// 1. Entity: apenas dados
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; }
}

// 2. DTOs: para transferência entre camadas
public class UserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    // ❌ Nunca incluir PasswordHash, tokens, dados sensíveis
}

public class CreateUserRequest
{
    public string Email { get; set; }
    public string Password { get; set; } // Plain password, só em DTO de entrada
}

// 3. Lógica: vai no Service
public class UserService
{
    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        // Validação aqui
        // Lógica de negócio aqui
        // Hashing de senha aqui
        
        var user = new User { /* ... */ }; // Entidade anêmica
        await _repository.AddAsync(user);
        
        return new UserDto { /* ... */ }; // Retornar DTO
    }
}
```

**Fluxo de Dados:**
```
API Request (DTO) → Validation → Service (lógica) → Entity (persistência) → Repository (BD)
↓
API Response (DTO)
```

### Injeção de Dependência (DI)
- Registrar em `DependencyInjectionModule*.cs` da camada específica
- Usar `TryAddScoped` para não sobrescrever registros anteriores
- Sempre usar interfaces, nunca classes concretas
- Respeitar ciclo de vida: Singleton → Scoped → Transient

## Checklist de Implementação (Para Agents)

Antes de fazer PR, validar:

- [ ] **Código**
  - [ ] Build compila sem erros (`dotnet build`)
  - [ ] Sem warnings não explicados
  - [ ] Segue Clean Architecture (dependências apontam inward)
  - [ ] **Entidades são anêmicas (apenas dados)**
  - [ ] **Toda lógica de negócio está em Services**
  - [ ] DTOs usados para transferência entre camadas
  - [ ] Sem duplicação de código
  
- [ ] **Cache**
  - [ ] Queries implementam `ICachedQuery` com `CacheDurationSeconds`
  - [ ] Commands não são cacheados
  - [ ] Cache keys são determinísticas
  - [ ] Fallback para InMemory se Redis down
  
- [ ] **Resiliência**
  - [ ] Behaviors MediatR em ordem correta (Prometheus → Cache → Resilience)
  - [ ] Policies Polly aplicadas (retry, circuit-breaker, timeout)
  - [ ] Graceful degradation em caso de falha
  
- [ ] **Observabilidade**
  - [ ] PrometheusMetricsBehavior coletando métricas
  - [ ] `/metrics` endpoint acessível
  - [ ] Health checks registrados
  - [ ] Logging estruturado (Serilog)
  
- [ ] **Validação & Erros**
  - [ ] FluentValidation para inputs
  - [ ] ProblemDetails para erros (RFC 7807)
  - [ ] Mensagens úteis, sem expor dados sensíveis
  
- [ ] **Testes**
  - [ ] Unit tests para lógica complexa
  - [ ] Integration tests para comportamentos críticos
  - [ ] E2E tests para fluxos críticos
  - [ ] Mínimo 80% cobertura em lógica crítica
  - [ ] Todos os testes passando (`dotnet test`)
  
- [ ] **Documentação**
  - [ ] README.md atualizado
  - [ ] Contrato de API documentado (DTOs, endpoints)
  - [ ] Impactos arquiteturais documentados
  - [ ] Exemplos de uso para features complexas
  
- [ ] **Integração Moodle** (se aplicável)
  - [ ] Apenas endpoints GET (leitura)
  - [ ] Sem modificação de dados no Moodle
  - [ ] Resiliência configurada para timeouts longos
  - [ ] Cache de resultados em RavenDB
  - [ ] Workloads pesados em background jobs

## Testes

### Obrigações Gerais
- Testes automatizados são **OBRIGATÓRIOS** para serviços/casos de uso críticos, endpoints e fluxos críticos de UI
- Testes devem validar integração e comportamento esperado sem reimplementar regras de negócio
- Mínimo 80% de cobertura em lógica crítica

### Tipos de Teste

#### Unit Tests (Rápidos, Isolados)
- Testam uma função/método em isolamento
- Usam mocks para dependências externas
- Executam em < 1 segundo cada
- **Exemplo:** Testar validação de email sem banco de dados

#### Integration Tests (Médio, Com Dependências Reais)
- Testam múltiplos componentes trabalhando juntos
- Usam Redis real, RavenDB real
- Validam comportamento de behaviors (Caching + Resilience + Prometheus)
- **Exemplo:** Testar se cache-aside pattern funciona com Redis real

#### E2E Tests (Lentos, Fluxo Completo) - OBRIGATÓRIO para Crítico
- Testam fluxo **COMPLETO** da aplicação
- Simulam usuário real usando API
- Incluem API rodando, cache funcionando, database acessível
- Validam integração entre todas as camadas

**Obrigação:** E2E tests DEVEM estar implementados para:
- ✅ Fluxos de autenticação
- ✅ Queries cacheáveis (validar cache hit/miss)
- ✅ Mutations (commands que alteram dados)
- ✅ Health checks (Redis, RavenDB, Moodle)
- ✅ Integração com Prometheus (métricas sendo coletadas)
- ✅ Fallback de cache (Redis down → InMemory)

**Exemplo E2E Test:**
```csharp
[TestFixture]
public class GetAllUsersQueryE2ETests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private IConnectionMultiplexer _redis;
    
    [SetUp]
    public async Task Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        _redis = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
        await _redis.GetDatabase().FlushDatabaseAsync(); // Limpar cache
    }
    
    [Test]
    public async Task GetAllUsers_FirstRequest_ShouldCacheMiss()
    {
        // Arrange: Cache vazio
        var cacheKey = "GetAllUsersQuery"; // Ou a chave específica
        var cached = await _redis.GetDatabase().StringGetAsync(cacheKey);
        Assert.That(cached.IsNull, Is.True);
        
        // Act: Fazer requisição
        var response = await _client.GetAsync("/api/users");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(200));
        
        // Validar que cache foi preenchido
        cached = await _redis.GetDatabase().StringGetAsync(cacheKey);
        Assert.That(cached.IsNull, Is.False);
    }
    
    [Test]
    public async Task GetAllUsers_SecondRequest_ShouldCacheHit()
    {
        // Arrange: Cache já preenchido
        await _client.GetAsync("/api/users");
        
        // Act: Segunda requisição
        var response = await _client.GetAsync("/api/users");
        
        // Assert: Deve ser muito rápido (cache hit)
        Assert.That(response.StatusCode, Is.EqualTo(200));
        // Tempo < 10ms indica cache hit
    }
    
    [Test]
    public async Task GetAllUsers_PrometheusMetrics_ShouldBeRecorded()
    {
        // Arrange
        var metricsResponse = await _client.GetAsync("/metrics");
        var metricsBefore = await metricsResponse.Content.ReadAsStringAsync();
        
        // Act: Fazer requisição
        await _client.GetAsync("/api/users");
        
        // Assert: Métricas devem ser registradas
        metricsResponse = await _client.GetAsync("/metrics");
        var metricsAfter = await metricsResponse.Content.ReadAsStringAsync();
        
        Assert.That(metricsAfter, Does.Contain("handler_execution_total"));
        Assert.That(metricsAfter, Does.Contain("GetAllUsersQuery"));
        Assert.That(metricsAfter, Does.Contain("status=\"success\""));
    }
    
    [Test]
    public async Task HealthCheck_Redis_ShouldBeHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/startup");
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(200));
        Assert.That(content, Does.Contain("\"status\":\"Healthy\""));
        Assert.That(content, Does.Contain("redis"));
    }
}
