using Application.Domain.Model.Dtos;

namespace Application.Service.Interface
{
    public interface ITokenService
    {
        Task<TokenResponseDto?> GenerateTokens(LoginDto loginDto);
        Task<TokenResponseDto?> GenerateMoodleTokens(MoodleLoginDto loginDto);
        Task<TokenResponseDto?> RefreshAsync(string refreshToken);
    }
}
