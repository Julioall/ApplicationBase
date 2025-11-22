using Application.Domain.Model.User;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class User_ByEmail : AbstractIndexCreationTask<User>
    {
        public User_ByEmail()
        {
            Map = users => from user in users
                           select new
                           {
                               Account_Email = user.Account.Email
                           };
        }
    }
}
