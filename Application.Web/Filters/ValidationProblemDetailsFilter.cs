using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace Application.Api.Filters
{
    public class ValidationProblemDetailsFilter : IActionFilter
    {
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
                Title = "Erro de validação",
                Detail = "Um ou mais campos estão inválidos.",
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
