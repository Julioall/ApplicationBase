using MediatR;
using Microsoft.Extensions.Logging;
using Prometheus;
using System.Diagnostics;

namespace Application.Service.Behaviors;

/// <summary>
/// Behavior para coletar métricas de Prometheus.
/// Mede latência de handlers e taxa de sucesso/erro.
/// </summary>
public class PrometheusMetricsBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PrometheusMetricsBehavior<TRequest, TResponse>> _logger;

    // Métricas de Prometheus
    private static readonly Histogram HandlerDurationHistogram = Metrics
        .CreateHistogram(
            "handler_duration_milliseconds",
            "Duration of MediatR handler execution in milliseconds",
            new HistogramConfiguration
            {
                Buckets = new[] { 10.0, 50.0, 100.0, 250.0, 500.0, 1000.0, 5000.0, 10000.0 },
                LabelNames = new[] { "handler", "status" }
            });

    private static readonly Counter HandlerExecutionCounter = Metrics
        .CreateCounter(
            "handler_execution_total",
            "Total number of handler executions",
            new CounterConfiguration { LabelNames = new[] { "handler", "status" } });

    private static readonly Gauge HandlerInFlightGauge = Metrics
        .CreateGauge(
            "handler_in_flight",
            "Number of handlers currently executing",
            new GaugeConfiguration { LabelNames = new[] { "handler" } });

    public PrometheusMetricsBehavior(
        ILogger<PrometheusMetricsBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var handlerName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        HandlerInFlightGauge.WithLabels(handlerName).Inc();

        try
        {
            _logger.LogDebug("Starting handler execution for {HandlerName}", handlerName);

            var result = await next();

            stopwatch.Stop();

            // Registra sucesso
            HandlerDurationHistogram
                .WithLabels(handlerName, "success")
                .Observe(stopwatch.Elapsed.TotalMilliseconds);

            HandlerExecutionCounter
                .WithLabels(handlerName, "success")
                .Inc();

            _logger.LogDebug(
                "Handler {HandlerName} completed successfully in {Duration}ms",
                handlerName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Registra erro
            HandlerDurationHistogram
                .WithLabels(handlerName, "error")
                .Observe(stopwatch.Elapsed.TotalMilliseconds);

            HandlerExecutionCounter
                .WithLabels(handlerName, "error")
                .Inc();

            _logger.LogError(
                ex,
                "Handler {HandlerName} failed after {Duration}ms",
                handlerName, stopwatch.ElapsedMilliseconds);

            throw;
        }
        finally
        {
            HandlerInFlightGauge.WithLabels(handlerName).Dec();
        }
    }
}
