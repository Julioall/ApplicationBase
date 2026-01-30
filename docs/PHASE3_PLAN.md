# Phase 3: Performance & Observabilidade (Planejamento)

**Data de Início:** 30 de janeiro de 2026  
**Status:** 📋 Planejamento em Andamento

---

## 📋 Resumo Executivo

Phase 3 foca em **otimização de performance**, **caching distribuído com Redis**, **integração de Polly no MediatR pipeline** e **observabilidade avançada** com dashboards de resiliência. O objetivo é criar uma aplicação altamente responsiva com visibilidade completa sobre falhas e recuperação.

---

## 🎯 Objetivos Principais

### 1. **Integração de Polly com MediatR Pipeline**
- Criar behavior de MediatR que aplica políticas de Polly automaticamente
- Aplicar Retry, Circuit Breaker e Timeout sem boilerplate em cada handler
- Documentar padrão para uso em novos handlers

### 2. **Caching Distribuído com Redis**
- Integrar Redis como cache distribuído
- Implementar cache-aside pattern para queries frequentes
- Invalidar cache inteligentemente após mutations
- Documentar estratégia de cache por feature

### 3. **Observabilidade & Telemetria**
- Circuit breaker metrics via Prometheus
- Latência e throughput por endpoint
- Taxa de erros por tipo de exceção
- Dashboard Grafana com KPIs principais

### 4. **Testes de Integração**
- Validar Polly behaviors em contexto de MediatR
- Teste de fallback quando circuit breaker abre
- Teste de cache invalidation
- Teste de timeout em operações lentas

### 5. **Documentação Completa**
- Guia de uso de cache em novos handlers
- Estratégia de resiliência por camada
- Exemplos de dashboard Grafana/Prometheus

---

## 📊 Estrutura de Entrega

```
Phase 3
├── Behavioral Pattern (MediatR + Polly)
│   ├── ResiliencePolicyBehavior.cs
│   ├── CachingBehavior.cs
│   └── Testes unitários
├── Cache Layer
│   ├── RedisCacheService.cs
│   ├── CacheKeyGenerator.cs
│   └── InvalidationStrategy
├── Metrics & Observability
│   ├── PrometheusMetrics.cs
│   ├── CircuitBreakerObserver.cs
│   └── Dashboard definitions (JSON)
└── Integration Tests
    ├── PollyBehaviorTests
    ├── CachingBehaviorTests
    └── ResiliencyIntegrationTests
```

---

## 🔧 Tecnologias Adicionadas

| Componente | Versão | Propósito |
|-----------|--------|----------|
| StackExchange.Redis | Latest | Cache distribuído |
| Polly.CircuitBreaker | (já existe) | Observabilidade de circuit breaker |
| Prometheus.Client | Latest | Métricas e telemetria |
| MediatR.Behaviors | (já existe) | Pipeline de behaviors |

---

## 📋 Tarefas Detalhadas

### Tarefa 1: Integração Polly + MediatR
**Responsável:** Feature Agent  
**Duração Estimada:** 4h

1. [ ] Criar `ResiliencePolicyBehavior<TRequest, TResponse>` que:
   - Obtém política de Polly via `IResiliencePolicyProvider`
   - Executa handler dentro do contexto da política
   - Registra tentativas e fallbacks

2. [ ] Registrar behavior em DependencyInjection

3. [ ] Testar com CreateUserCommand como exemplo

**Referência:** [Application.Service/Resilience/ResiliencePolicyProvider.cs](../Application.Service/Resilience/ResiliencePolicyProvider.cs)

---

### Tarefa 2: Implementar Caching Distribuído
**Responsável:** Feature Agent  
**Duração Estimada:** 6h

1. [ ] Criar `RedisCacheService` implementando `IDistributedCache`

2. [ ] Criar `CachingBehavior<TRequest, TResponse>` que:
   - Intercepta queries (implementam `ICachedQuery`)
   - Verifica cache antes de executar handler
   - Invalida cache após mutations

3. [ ] Registrar behavior após ResiliencePolicyBehavior

4. [ ] Criar `ICachedQuery` marker interface

5. [ ] Implementar `GetAllUsersQuery : ICachedQuery` como exemplo

**Cache Keys:** `{Handler}:{QueryParams}` (JSON hash)

---

### Tarefa 3: Métricas e Observabilidade
**Responsável:** Feature Agent  
**Duração Estimada:** 4h

1. [ ] Criar `CircuitBreakerMetricsObserver` que:
   - Registra mudanças de estado (Closed → Open → HalfOpen)
   - Exporta para Prometheus

2. [ ] Adicionar métricas de latência ao behavior de MediatR

3. [ ] Registrar em Prometheus

4. [ ] Documentar formato dos metrics

**Métricas Principais:**
- `polly_circuit_breaker_state_changes_total{policy_name}`
- `mediatr_handler_duration_seconds{handler_name}`
- `mediatr_handler_errors_total{handler_name, error_type}`

---

### Tarefa 4: Testes de Integração
**Responsável:** Feature Agent  
**Duração Estimada:** 4h

Criar em `Application.Test/Handlers/`:

1. [ ] `ResiliencyBehaviorIntegrationTests.cs`
   - Teste que handler executa dentro de política
   - Teste de retry automático
   - Teste de circuit breaker aberto

2. [ ] `CachingBehaviorIntegrationTests.cs`
   - Teste de hit de cache
   - Teste de invalidação após mutation
   - Teste de TTL expirado

3. [ ] `MetricsIntegrationTests.cs`
   - Validar que métricas estão sendo registradas

---

### Tarefa 5: Documentação
**Responsável:** Feature Agent  
**Duração Estimada:** 2h

Criar `docs/PHASE3_DELIVERY.md` com:

1. [ ] Implementações completadas
2. [ ] Como usar caching em novos handlers
3. [ ] Exemplos de resiliência aplicada
4. [ ] Guia de leitura de métricas Prometheus
5. [ ] Queries Grafana pre-built

---

## 📈 Métricas de Sucesso (Definition of Done)

- ✅ Polly behaviors integrados ao MediatR sem impacto em handlers existentes
- ✅ Redis em cache com invalidation automática
- ✅ Métricas de resiliência exportadas para Prometheus
- ✅ Todos os testes de integração passando
- ✅ Documentação completa com exemplos
- ✅ `dotnet build` sem erros
- ✅ `dotnet test` sem falhas

---

## 🔗 Dependências e Pré-requisitos

- ✅ Phase 2 Completa (Polly + MediatR + Health Checks)
- ✅ Docker Compose com Redis (será adicionado se necessário)
- ✅ Prometheus configurado (para métricas)

---

## 🚀 Próximas Fases Após Phase 3

### Phase 4 (Planned)
- Event Sourcing com RavenDB
- CQRS Read Models
- Distributed Transactions

---

## 📝 Notas

- Todas as mudanças respeitarão o `architecture-contract.md`
- Behaviors de MediatR são não-intrusivos (zero impacto em código existente)
- Redis será opcional via configuração
- Métricas são exportadas em background

