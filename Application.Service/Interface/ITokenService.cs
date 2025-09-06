using Application.Domain.Model.Dtos;

namespace Application.Service.Interface
{
    public interface ITokenService
    {
        Task<string> GenerateToken(LoginDto loginDto);
    }
}
