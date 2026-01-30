# CQRS Pattern Implementation Guide

## 📖 Visão Geral

Este documento descreve como implementar novos Commands, Queries e seus Handlers seguindo o padrão CQRS estabelecido na **Phase 1**.

## 🏗️ Arquitetura CQRS

```
┌─────────────────────────────────────────────────────────┐
│                   APPLICATION LAYER                     │
│              (Controllers, SignalR, Jobs)               │
└─────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│                   MediatR Pipeline                      │
│         (Validates, Logs, Routes to Handlers)          │
└─────────────────────────────────────────────────────────┘
                            ▼
┌──────────────────┬──────────────────┬──────────────────┐
│    Commands      │     Queries      │    Events        │
│  (Write Model)   │  (Read Model)    │  (Domain Events) │
└──────────────────┴──────────────────┴──────────────────┘
                            ▼
┌──────────────────┬──────────────────┬──────────────────┐
│  CommandHandler  │  QueryHandler    │ EventHandler     │
│   (Business      │   (Read Access)  │   (Side Effects) │
│    Logic)        │                  │                  │
└──────────────────┴──────────────────┴──────────────────┘
                            ▼
┌──────────────────┬──────────────────┬──────────────────┐
│   Repositories   │   Services       │   External Calls │
│   (Persistence)  │   (Integrations) │   (API, Email)   │
└──────────────────┴──────────────────┴──────────────────┘
```

## 📝 Passo 1: Definir um Command

### Local
Arquivo: `Application.Service/CQRS/Commands/{FeatureName}Command.cs`

### Estrutura Básica

```csharp
using Application.Domain.CQRS;

namespace Application.Service.CQRS.Commands
{
    /// <summary>
    /// Comando para criar um novo pedido
    /// </summary>
    public record CreateOrderCommand(
        string CustomerId,
        List<OrderItemDto> Items,
        string DeliveryAddress
    ) : ICommand<CreateOrderResponse>;

    /// <summary>
    /// Resposta do comando
    /// </summary>
    public record CreateOrderResponse(
        string OrderId,
        decimal TotalAmount,
        DateTime CreatedAt
    );
}
```

### Boas Práticas

1. **Use Records** para imutabilidade garantida
2. **Nomeie claramente**: `Create{Entity}Command`
3. **Inclua DTO's**: Use `OrderItemDto` em vez de listas complexas
4. **Documente com XML**: `/// <summary>`
5. **Resposta tipada**: `ICommand<T>` onde T é o response

---

## 🔍 Passo 2: Definir uma Query

### Local
Arquivo: `Application.Service/CQRS/Queries/{FeatureName}Query.cs`

### Estrutura Básica

```csharp
using Application.Domain.CQRS;

namespace Application.Service.CQRS.Queries
{
    /// <summary>
    /// Query para obter pedidos de um cliente
    /// </summary>
    public record GetCustomerOrdersQuery(
        string CustomerId,
        int PageNumber = 1,
        int PageSize = 10
    ) : IQuery<GetCustomerOrdersResponse>;

    /// <summary>
    /// Resposta paginada
    /// </summary>
    public record GetCustomerOrdersResponse(
        List<OrderSummaryDto> Orders,
        int TotalCount,
        int PageNumber,
        int PageSize
    );
}
```

### Boas Práticas

1. **Queries não modificam estado**: Apenas leitura
2. **Suporte paginação**: `PageNumber`, `PageSize`
3. **Use DTOs**: Nunca retorne entidades diretas
4. **Filtros estruturados**: Use records para filtros complexos

---

## ⚙️ Passo 3: Implementar o Handler

### Local
Arquivo: `Application.Service/CQRS/Handlers/{FeatureName}CommandHandler.cs`

### Estrutura para Command

```csharp
using Application.Domain.CQRS;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Service.CQRS.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Service.CQRS.Handlers
{
    public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, CreateOrderResponse>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<CreateOrderResponse> Handle(
            CreateOrderCommand command,
            CancellationToken cancellationToken)
        {
            // 1️⃣ Logging de entrada
            _logger.LogInformation(
                "Criando novo pedido para cliente {CustomerId} com {ItemCount} itens",
                command.CustomerId,
                command.Items.Count);

            try
            {
                // 2️⃣ Validações
                var customer = await _customerRepository.GetByIdAsync(
                    command.CustomerId,
                    cancellationToken);

                if (customer == null)
                {
                    _logger.LogWarning("Cliente {CustomerId} não encontrado", command.CustomerId);
                    throw new NotFoundException($"Cliente {command.CustomerId} não encontrado");
                }

                // 3️⃣ Lógica de negócio
                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = command.CustomerId,
                    Items = MapItems(command.Items),
                    DeliveryAddress = command.DeliveryAddress,
                    CreatedAt = DateTime.UtcNow,
                    Status = OrderStatus.Pending
                };

                var totalAmount = order.Items.Sum(i => i.Price * i.Quantity);

                // 4️⃣ Persistência
                await _orderRepository.AddAsync(order, cancellationToken);

                // 5️⃣ Logging de sucesso
                _logger.LogInformation(
                    "Pedido criado com sucesso. OrderId: {OrderId}, Valor total: {Total}",
                    order.Id,
                    totalAmount);

                return new CreateOrderResponse(
                    order.Id,
                    totalAmount,
                    order.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao criar pedido para cliente {CustomerId}",
                    command.CustomerId);
                throw;
            }
        }

        private List<OrderItem> MapItems(List<OrderItemDto> dtos)
        {
            return dtos.Select(dto => new OrderItem
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                Price = dto.Price
            }).ToList();
        }
    }
}
```

### Estrutura para Query

```csharp
using Application.Domain.CQRS;
using Application.Domain.Interface;
using Application.Service.CQRS.Queries;
using Microsoft.Extensions.Logging;

namespace Application.Service.CQRS.Handlers
{
    public class GetCustomerOrdersQueryHandler : IQueryHandler<GetCustomerOrdersQuery, GetCustomerOrdersResponse>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetCustomerOrdersQueryHandler> _logger;

        public GetCustomerOrdersQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetCustomerOrdersQueryHandler> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<GetCustomerOrdersResponse> Handle(
            GetCustomerOrdersQuery query,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Buscando pedidos do cliente {CustomerId}, página {PageNumber}",
                query.CustomerId,
                query.PageNumber);

            var (orders, totalCount) = await _orderRepository.GetByCustomerIdAsync(
                query.CustomerId,
                query.PageNumber,
                query.PageSize,
                cancellationToken);

            _logger.LogInformation(
                "Encontrados {Count} pedidos do cliente {CustomerId}",
                orders.Count,
                query.CustomerId);

            return new GetCustomerOrdersResponse(
                MapToSummaries(orders),
                totalCount,
                query.PageNumber,
                query.PageSize);
        }

        private List<OrderSummaryDto> MapToSummaries(List<Order> orders)
        {
            return orders.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                Total = o.Items.Sum(i => i.Price * i.Quantity)
            }).ToList();
        }
    }
}
```

### Boas Práticas para Handlers

1. **Injetar via Constructor**: Todos os repositórios e serviços
2. **Logging em 3 pontos**: Entrada, sucesso, erro
3. **Tratamento de exceções**: Use exceções de domínio
4. **Validações primeiro**: Não execute lógica inválida
5. **CancellationToken**: Sempre respeite o token
6. **Métodos auxiliares**: Extraia mapeamentos

---

## 📦 Passo 4: Registrar Handler no DI

### Local: `Application.Service/DependencyInjectionModuleService.cs`

```csharp
public static IServiceCollection AddServiceModule(this IServiceCollection services)
{
    // ... código existente ...

    // Registrar MediatR handlers
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionModuleService).Assembly));

    return services;
}
```

⚠️ **Nota**: Esta configuração já foi feita em Phase 1. MediatR detecta automaticamente handlers em `Application.Service.CQRS.Handlers`.

---

## 🚀 Passo 5: Usar em Controller

### Antes (Antigo)
```csharp
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request)
{
    var order = await _orderService.CreateAsync(request);
    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}
```

### Depois (CQRS)
```csharp
private readonly IMediator _mediator;

public OrderController(IMediator mediator)
{
    _mediator = mediator;
}

[HttpPost("orders")]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> CreateOrder(
    [FromBody] CreateOrderDto request,
    CancellationToken cancellationToken)
{
    var command = new CreateOrderCommand(
        request.CustomerId,
        request.Items,
        request.DeliveryAddress);

    var response = await _mediator.Send(command, cancellationToken);

    return CreatedAtAction(nameof(GetOrder), 
        new { id = response.OrderId }, 
        response);
}

[HttpGet("orders/{customerId}")]
[ProducesResponseType(typeof(GetCustomerOrdersResponse), StatusCodes.Status200OK)]
public async Task<IActionResult> GetOrders(
    string customerId,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
{
    var query = new GetCustomerOrdersQuery(customerId, pageNumber, pageSize);
    var response = await _mediator.Send(query, cancellationToken);
    return Ok(response);
}
```

---

## 📝 Exemplo Completo: Feature Payment

### 1. Command
**Arquivo**: `Application.Service/CQRS/Commands/ProcessPaymentCommand.cs`

```csharp
public record ProcessPaymentCommand(
    string OrderId,
    string PaymentMethod,
    string CardToken
) : ICommand<ProcessPaymentResponse>;

public record ProcessPaymentResponse(
    string TransactionId,
    PaymentStatus Status,
    DateTime ProcessedAt
);
```

### 2. Query
**Arquivo**: `Application.Service/CQRS/Queries/GetPaymentStatusQuery.cs`

```csharp
public record GetPaymentStatusQuery(string TransactionId) 
    : IQuery<GetPaymentStatusResponse>;

public record GetPaymentStatusResponse(
    string TransactionId,
    PaymentStatus Status,
    decimal Amount,
    DateTime CreatedAt
);
```

### 3. Handler
**Arquivo**: `Application.Service/CQRS/Handlers/ProcessPaymentCommandHandler.cs`

```csharp
public class ProcessPaymentCommandHandler : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public async Task<ProcessPaymentResponse> Handle(ProcessPaymentCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Processando pagamento para pedido {OrderId}", command.OrderId);

        var transactionId = Guid.NewGuid().ToString();
        
        try
        {
            // Chamar gateway externo
            var result = await _paymentGateway.ProcessAsync(
                transactionId,
                command.PaymentMethod,
                command.CardToken);

            // Persistir resultado
            var payment = new Payment
            {
                TransactionId = transactionId,
                OrderId = command.OrderId,
                Status = result.Status,
                ProcessedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment, ct);

            _logger.LogInformation("Pagamento processado com sucesso. TransactionId: {TransactionId}", transactionId);

            return new ProcessPaymentResponse(transactionId, result.Status, DateTime.UtcNow);
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Erro ao processar pagamento para pedido {OrderId}", command.OrderId);
            throw new BusinessException("Falha ao processar pagamento", ex);
        }
    }
}
```

### 4. Registro (Automático via MediatR)

### 5. Uso em Controller

```csharp
[HttpPost("orders/{orderId}/process-payment")]
public async Task<IActionResult> ProcessPayment(
    string orderId,
    [FromBody] PaymentRequestDto request,
    CancellationToken cancellationToken)
{
    var command = new ProcessPaymentCommand(orderId, request.Method, request.Token);
    var response = await _mediator.Send(command, cancellationToken);
    return Ok(response);
}

[HttpGet("payments/{transactionId}")]
public async Task<IActionResult> GetPaymentStatus(
    string transactionId,
    CancellationToken cancellationToken)
{
    var query = new GetPaymentStatusQuery(transactionId);
    var response = await _mediator.Send(query, cancellationToken);
    return Ok(response);
}
```

---

## 🧪 Testando Handlers

### Unit Test Exemplo

```csharp
[TestFixture]
public class ProcessPaymentCommandHandlerTests
{
    private ProcessPaymentCommandHandler _handler;
    private Mock<IPaymentGateway> _paymentGatewayMock;
    private Mock<IPaymentRepository> _paymentRepositoryMock;
    private Mock<ILogger<ProcessPaymentCommandHandler>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _loggerMock = new Mock<ILogger<ProcessPaymentCommandHandler>>();

        _handler = new ProcessPaymentCommandHandler(
            _paymentGatewayMock.Object,
            _paymentRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Handle_ValidPayment_ReturnSuccess()
    {
        // Arrange
        var command = new ProcessPaymentCommand("order-123", "credit_card", "token");
        _paymentGatewayMock
            .Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult { Status = PaymentStatus.Approved });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo(PaymentStatus.Approved));
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## 🔍 Logging Estruturado

### Padrão de Logging

```csharp
// Entrada
_logger.LogInformation(
    "Iniciando {Operation} para {Entity} {Id}",
    operationName,
    entityType,
    entityId);

// Sucesso
_logger.LogInformation(
    "{Operation} concluído com sucesso. {Entity} {Id} = {Result}",
    operationName,
    entityType,
    entityId,
    result);

// Erro
_logger.LogError(
    ex,
    "Erro ao executar {Operation} para {Entity} {Id}",
    operationName,
    entityType,
    entityId);
```

### Output de Exemplo

```
[2026-01-30T15:57:37.1234567Z INF] [ProcessPaymentCommandHandler] Processando pagamento para pedido order-123
[2026-01-30T15:57:37.5678901Z INF] [ProcessPaymentCommandHandler] Pagamento processado com sucesso. TransactionId: txn-abc123
[MachineName: APP-SERVER-01, Environment: Production]
```

---

## ✅ Checklist para Novo CQRS Feature

- [ ] Command/Query criado com record imutável
- [ ] Response record definido
- [ ] Handler implementado com logging
- [ ] Injeção de dependência (automática via MediatR)
- [ ] Repository/Service injetado corretamente
- [ ] Validações implementadas
- [ ] Tratamento de exceções (domínio específico)
- [ ] Testes unitários criados
- [ ] Controller integrado com MediatR.Send()
- [ ] Documentação atualizada
- [ ] Build passa sem warnings
- [ ] Logs estruturados em entrada/saída/erro

---

## 📚 Referências

- [MediatR Docs](https://github.com/jbogard/MediatR)
- [CQRS Pattern - Microsoft](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Serilog Documentation](https://serilog.net/)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)

---

**Status**: ✅ PHASE 1 COMPLETE  
**Versão**: 1.0  
**Última Atualização**: 30 de janeiro de 2026
