using Application.Domain.Interface;
using Application.Domain.Model.User;
using MongoDB.Driver;
using MongoDB.Bson;


namespace Application.Infrastructure.Repository
{
    public class UserRepositoryMongoDB : IUserRepository
    {
        private readonly IMongoCollection<User> _usersCollection;

        public UserRepositoryMongoDB(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("Users");
        }

        public async Task AddAsync(User user)
        {
            user.Id = ObjectId.GenerateNewId().ToString();
            await _usersCollection.InsertOneAsync(user);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            await _usersCollection.DeleteOneAsync(filter);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _usersCollection.Find(_ => true).ToListAsync();
        }

        public async Task<User> GetByIdAsync(string id)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User> GetByRoleAsync(string role)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Account.Role, role);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Account.Email, email);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(User user)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            await _usersCollection.ReplaceOneAsync(filter, user);
        }
    }
}
