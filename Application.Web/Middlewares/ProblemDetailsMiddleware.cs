using System.Diagnostics;
using Application.Domain;
using Application.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Application.Api.Middlewares
{
    public class ProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProblemDetailsMiddleware> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger, IStringLocalizer<SharedResource> localizer)
        {
            _next = next;
            _logger = logger;
            _localizer = localizer;
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

        private ProblemDetails MapToProblemDetails(HttpContext context, Exception exception, string traceId)
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
                        Title = _localizer["ValidationTitle"],
                        Detail = _localizer["ValidationDetail"],
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };

                case DomainException domainException:
                    return new ProblemDetails
                    {
                        Status = domainException.StatusCode,
                        Title = _localizer["InvalidOperationTitle"],
                        Detail = domainException.Message,
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };

                case UnauthorizedAccessException:
                    return new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = _localizer["UnauthorizedTitle"],
                        Detail = _localizer["UnauthorizedDetail"],
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };

                default:
                    return new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = _localizer["InternalErrorTitle"],
                        Detail = _localizer["InternalErrorDetail"],
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = traceId }
                    };
            }
        }
    }
}
