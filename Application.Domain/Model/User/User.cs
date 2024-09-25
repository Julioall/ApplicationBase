using MongoDB.Bson.Serialization.Attributes;

namespace Application.Domain.Model.User
{
    public class User
    {
        [BsonId]
        public string? Id { get; set; }
        public UserAccount Account { get; set; }
        public UserProfile Profile { get; set; }
    }
}
