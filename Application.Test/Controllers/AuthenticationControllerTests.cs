using Application.Api.Controllers;
using Application.Domain;
using Application.Domain.Model.Dtos;
using Application.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Application.Tests.Controllers
{
    public class AuthenticationControllerTests
    {
        private sealed class FakeLocalizer : IStringLocalizer<SharedResource>
        {
            public LocalizedString this[string name] => new(name, name);
            public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
            public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
        }

        private sealed class FakeTokenService : ITokenService
        {
            public TokenResponseDto? LoginResponse { get; set; }
            public TokenResponseDto? RefreshResponse { get; set; }

            public Task<TokenResponseDto?> GenerateTokens(LoginDto loginDto) => Task.FromResult(LoginResponse);
            public Task<TokenResponseDto?> RefreshAsync(string refreshToken) => Task.FromResult(RefreshResponse);
        }

        private readonly FakeTokenService _tokenService = new();
        private readonly IStringLocalizer<SharedResource> _localizer = new FakeLocalizer();

        [Fact]
        public async Task Login_Should_Return_BadRequest_When_Payload_Is_Null()
        {
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Login(null!) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            Assert.Equal("InvalidRequestTitle", ((ProblemDetails)result.Value!).Title);
        }

        [Fact]
        public async Task Login_Should_Return_BadRequest_When_Email_Is_Empty()
        {
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Login(new LoginDto { Email = "   ", Password = "pwd" }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            var problemDetails = (ProblemDetails)result.Value!;
            Assert.Equal("InvalidRequestTitle", problemDetails.Title);
            Assert.Equal("EmailRequired", problemDetails.Detail);
        }

        [Fact]
        public async Task Login_Should_Return_BadRequest_When_Password_Is_Empty()
        {
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Login(new LoginDto { Email = "a@b.com", Password = "" }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            var problemDetails = (ProblemDetails)result.Value!;
            Assert.Equal("InvalidRequestTitle", problemDetails.Title);
            Assert.Equal("PasswordRequired", problemDetails.Detail);
        }

        [Fact]
        public async Task Login_Should_Return_Unauthorized_When_Service_Returns_Null()
        {
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Login(new LoginDto { Email = "a@b.com", Password = "pwd" }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, result!.StatusCode);
            Assert.Equal("UnauthorizedTitle", ((ProblemDetails)result.Value!).Title);
        }

        [Fact]
        public async Task Login_Should_Return_Ok_With_Token()
        {
            _tokenService.LoginResponse = new TokenResponseDto
            {
                Token = "token",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Login(new LoginDto { Email = "a@b.com", Password = "pwd" }) as OkObjectResult;

            Assert.NotNull(result);
            var payload = result!.Value!;
            Assert.Equal("token", payload.GetType().GetProperty("token")?.GetValue(payload) as string);
            Assert.Equal("refresh", payload.GetType().GetProperty("refreshToken")?.GetValue(payload) as string);
        }

        [Fact]
        public async Task Refresh_Should_Return_Unauthorized_When_Service_Returns_Null()
        {
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "invalid" }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, result!.StatusCode);
            Assert.Equal("UnauthorizedTitle", ((ProblemDetails)result.Value!).Title);
        }

        [Fact]
        public async Task Refresh_Should_Return_BadRequest_When_Payload_Is_Null_Or_Empty()
        {
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Refresh(null!) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            Assert.Equal("InvalidRequestTitle", ((ProblemDetails)result.Value!).Title);

            result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "" }) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            Assert.Equal("InvalidRequestTitle", ((ProblemDetails)result.Value!).Title);
        }

        [Fact]
        public async Task Refresh_Should_Return_Ok_With_New_Token()
        {
            _tokenService.RefreshResponse = new TokenResponseDto
            {
                Token = "token2",
                RefreshToken = "refresh2",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            var controller = new AuthenticationController(_tokenService, _localizer);

            var result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "refresh" }) as OkObjectResult;

            Assert.NotNull(result);
            var payload = result!.Value!;
            Assert.Equal("token2", payload.GetType().GetProperty("token")?.GetValue(payload) as string);
            Assert.Equal("refresh2", payload.GetType().GetProperty("refreshToken")?.GetValue(payload) as string);
        }
    }
}
