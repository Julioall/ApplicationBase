using Application.Domain.Model.User;

namespace Application.Domain.Interface
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task DeleteAsync(string id);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(string id);
        Task<User> GetByRoleAsync(string role);
        Task<User> GetByEmailAsync(string username);
        Task<User> GetByRefreshTokenAsync(string refreshToken);
        Task UpdateAsync(User user);
    }
}
