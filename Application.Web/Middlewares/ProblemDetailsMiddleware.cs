using System.Diagnostics;
using System.Net;
using Application.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Application.Api.Middlewares
{
    public class ProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProblemDetailsMiddleware> _logger;

        public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response has already started, skipping problem details.");
                    throw;
                }

                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
                var problem = MapToProblemDetails(context, ex, traceId);

                _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);

                context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            }
        }

        private static ProblemDetails MapToProblemDetails(HttpContext context, Exception exception, string traceId)
        {
            switch (exception)
            {
                case ValidationException validationException:
                    var validationErrors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    return new ValidationProblemDetails(validationErrors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Erro de validação",
                        Detail = "Um ou mais campos estão inválidos.",
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };

                case DomainException domainException:
                    return new ProblemDetails
                    {
                        Status = domainException.StatusCode,
                        Title = "Operação inválida",
                        Detail = domainException.Message,
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };

                case UnauthorizedAccessException:
                    return new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Não autorizado",
                        Detail = "Credenciais inválidas ou ausentes.",
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };

                default:
                    return new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Erro interno",
                        Detail = "Ocorreu um erro inesperado.",
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };
            }
        }
    }
}
