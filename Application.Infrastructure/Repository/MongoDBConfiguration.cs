using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Xml.Linq;

namespace Application.Infrastructure.Repository
{
    public static class MongoDBConfiguration
    {
        public static void AddMongoDB(this IServiceCollection services)
        {
            // Here you should provide your connection string to access the database
            const string connectionString = "";

            // Here you should specify the name of the created database
            var databaseName = "";

            var mongoClient = new MongoClient(connectionString);
            var database = mongoClient.GetDatabase(databaseName);

            services.AddSingleton(database);
        }
    }
}
