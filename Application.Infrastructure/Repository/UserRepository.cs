using Application.Domain.Interface;
using Application.Domain.Model.User;
using Raven.Client.Documents;
using System.IO;

namespace Application.Infrastructure.Repository
{
    public class UserRepository : IUserRepository
    {
        private const string ProfilePictureAttachmentName = "profile-picture";
        private readonly IServiceRavenDB _serviceRavenDb;

        public UserRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task AddAsync(User user)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(user);
        }

        public async Task DeleteAsync(string id)
        {
            var user = await _serviceRavenDb.AsyncSession.LoadAsync<User>(id.ToString());
            if (user != null)
            {
                _serviceRavenDb.AsyncSession.Delete(user);
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _serviceRavenDb.AsyncSession.Query<User>().ToListAsync();
        }

        public async Task<User> GetByIdAsync(string id)
        {
            return await _serviceRavenDb.AsyncSession.LoadAsync<User>(id.ToString());
        }

        public async Task<User> GetByRoleAsync(string role)
        {
            return await _serviceRavenDb.AsyncSession.Query<User>()
                                   .FirstOrDefaultAsync(u => u.Account.Role == role);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            var users = await _serviceRavenDb.AsyncSession.Query<User>().ToListAsync();
            return users.FirstOrDefault(u => u.Account.Email == email);
        }

        public async Task<User> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _serviceRavenDb.AsyncSession.Query<User>()
                .FirstOrDefaultAsync(u => u.Account.RefreshToken == refreshToken);
        }

        public async Task UpdateAsync(User user)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(user);
        }

        public async Task UploadProfilePictureAsync(string userId, Stream stream, string contentType)
        {
            ArgumentNullException.ThrowIfNull(stream);
            stream.Position = 0;
            _serviceRavenDb.AsyncSession.Advanced.Attachments.Store(userId, ProfilePictureAttachmentName, stream, contentType);
            await _serviceRavenDb.AsyncSession.SaveChangesAsync();
        }

        public async Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId)
        {
            using var attachment = await _serviceRavenDb.AsyncSession.Advanced.Attachments.GetAsync(userId, ProfilePictureAttachmentName);
            if (attachment == null)
            {
                return null;
            }

            using var memory = new MemoryStream();
            await attachment.Stream.CopyToAsync(memory);
            return (memory.ToArray(), attachment.Details.ContentType);
        }

        public async Task DeleteProfilePictureAsync(string userId)
        {
            _serviceRavenDb.AsyncSession.Advanced.Attachments.Delete(userId, ProfilePictureAttachmentName);
            await _serviceRavenDb.AsyncSession.SaveChangesAsync();
        }
    }
}
