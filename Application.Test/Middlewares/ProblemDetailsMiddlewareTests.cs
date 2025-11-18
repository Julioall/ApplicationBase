using System.IO;
using System.Text.Json;
using Application.Api.Middlewares;
using Application.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Middlewares
{
    public class ProblemDetailsMiddlewareTests
    {
        [Fact]
        public async Task Deve_retornar_validationproblemdetails_para_validationexception()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/test";
            context.Response.Body = new MemoryStream();

            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Email", "Email inválido")
            };

            var middleware = new ProblemDetailsMiddleware(
                _ => throw new ValidationException(failures),
                NullLogger<ProblemDetailsMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var problem = await JsonSerializer.DeserializeAsync<ValidationProblemDetails>(context.Response.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.Equal("Erro de validação", problem?.Title);
            Assert.Equal(StatusCodes.Status400BadRequest, problem?.Status);
            Assert.NotNull(problem);
        }

        [Fact]
        public async Task Deve_retornar_problemdetails_para_conflictexception()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/test";
            context.Response.Body = new MemoryStream();

            var middleware = new ProblemDetailsMiddleware(
                _ => throw new ConflictException("Conflito de e-mail"),
                NullLogger<ProblemDetailsMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            Assert.Equal("Operação inválida", problem?.Title);
            Assert.Equal("Conflito de e-mail", problem?.Detail);
        }
    }
}
