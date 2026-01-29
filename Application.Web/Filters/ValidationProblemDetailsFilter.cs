using Application.Domain.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace Application.Api.Filters
{
    public class ValidationProblemDetailsFilter : IActionFilter
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ValidationProblemDetailsFilter(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ModelState.IsValid)
            {
                return;
            }

            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = _localizer["ValidationTitle"],
                Detail = _localizer["ValidationDetail"],
                Instance = context.HttpContext.Request.Path,
                Extensions = { ["traceId"] = traceId }
            };

            context.Result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                DeclaredType = typeof(ValidationProblemDetails)
            };
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
