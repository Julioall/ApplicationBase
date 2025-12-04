using System.Globalization;
using System.IO;
using System.Text.Json;
using Application.Api.Middlewares;
using Application.Domain;
using Application.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Middlewares
{
    public class ProblemDetailsMiddlewareTests
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ProblemDetailsMiddlewareTests()
        {
            var culture = new CultureInfo("pt");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var services = new ServiceCollection();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddLogging();
            _localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResource>>();
        }

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
                NullLogger<ProblemDetailsMiddleware>.Instance,
                _localizer);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var problem = await JsonSerializer.DeserializeAsync<ValidationProblemDetails>(context.Response.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.Equal(_localizer["ValidationTitle"], problem?.Title);
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
                NullLogger<ProblemDetailsMiddleware>.Instance,
                _localizer);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            Assert.Equal(_localizer["InvalidOperationTitle"], problem?.Title);
            Assert.Equal("Conflito de e-mail", problem?.Detail);
        }
    }
}
