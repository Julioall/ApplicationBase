using Application.Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service.Interface
{
    public interface IUserService
    {
        Task AddAsync(User user);

        Task DeleteAsync(string id);

        Task UpdateAsync(User user);

        Task<IEnumerable<User>> GetAllAsync();

        Task<User> GetByIdAsync(string id);

        Task<User> GetByEmailAsync(string email);

        Task<User> GetByRoleAsync(string role);
    }
}
