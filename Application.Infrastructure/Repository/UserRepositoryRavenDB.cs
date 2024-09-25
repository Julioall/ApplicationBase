using Application.Domain.Interface;
using Application.Domain.Model.User;
using Raven.Client.Documents;

namespace Application.Infrastructure.Repository
{
    public class UserRepositoryRavenDB : IUserRepository
    {
        private readonly IDocumentStore _documentStore;

        public UserRepositoryRavenDB(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task AddAsync(User user)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(user);
                var userDataBase = user;
                await session.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(string id)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var user = await session.LoadAsync<User>(id.ToString());
                if (user != null)
                {
                    session.Delete(user);
                    await session.SaveChangesAsync();
                }
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                return await session.Query<User>().ToListAsync();
            }
        }

        public async Task<User> GetByIdAsync(string id)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                return await session.LoadAsync<User>(id.ToString());
            }
        }

        public async Task<User> GetByRoleAsync(string role)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                return await session.Query<User>()
                                    .FirstOrDefaultAsync(u => u.Account.Role == role);
            }
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var users = await session.Query<User>().ToListAsync();

                return users.FirstOrDefault(u => u.Account.Email == email);
            }
        }

        public async Task UpdateAsync(User user)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(user);
                await session.SaveChangesAsync();
            }
        }
    }
}
