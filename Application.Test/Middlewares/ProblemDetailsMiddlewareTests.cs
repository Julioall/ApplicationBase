using System.Globalization;
using System.Text.Json;
using Application.Api.Middlewares;
using Application.Domain.Exceptions;
using Application.Domain.Localization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
        public async Task Should_return_validation_problem_details_for_validation_exception()
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
        public async Task Should_return_problem_details_for_conflict_exception()
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

        [Fact]
        public async Task Should_return_problem_details_for_unauthorized_exception()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/auth";
            context.Response.Body = new MemoryStream();

            var middleware = new ProblemDetailsMiddleware(
                _ => throw new UnauthorizedAccessException(),
                NullLogger<ProblemDetailsMiddleware>.Instance,
                _localizer);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            Assert.Equal(_localizer["UnauthorizedTitle"], problem?.Title);
            Assert.Equal(_localizer["UnauthorizedDetail"], problem?.Detail);
        }

        [Fact]
        public async Task Should_rethrow_when_response_has_already_started()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/error";
            context.Response.Body = new MemoryStream();
            var originalFeature = context.Features.Get<IHttpResponseFeature>() ?? throw new InvalidOperationException("Response feature missing");
            context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature(originalFeature));

            var middleware = new ProblemDetailsMiddleware(
                async ctx =>
                {
                    await ctx.Response.WriteAsync("started");
                    throw new Exception("after-started");
                },
                NullLogger<ProblemDetailsMiddleware>.Instance,
                _localizer);

            await Assert.ThrowsAsync<Exception>(() => middleware.InvokeAsync(context));
        }

        private sealed class StartedResponseFeature : IHttpResponseFeature
        {
            private readonly IHttpResponseFeature _inner;

            public StartedResponseFeature(IHttpResponseFeature inner)
            {
                _inner = inner;
            }

            public int StatusCode { get => _inner.StatusCode; set => _inner.StatusCode = value; }
            public string ReasonPhrase { get => _inner.ReasonPhrase; set => _inner.ReasonPhrase = value; }
            public IHeaderDictionary Headers { get => _inner.Headers; set => _inner.Headers = value; }
            public Stream Body { get => _inner.Body; set => _inner.Body = value; }
            public bool HasStarted => true;
            public void OnCompleted(Func<object, Task> callback, object state) => _inner.OnCompleted(callback, state);
            public void OnStarting(Func<object, Task> callback, object state) => _inner.OnStarting(callback, state);
        }
    }
}
