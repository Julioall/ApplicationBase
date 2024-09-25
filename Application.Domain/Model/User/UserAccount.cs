using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Domain.Model.User
{
    public class UserAccount
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
        public DateTime? DateJoined { get; set; } = DateTime.Now.Date;
        public DateTime? LastLogin { get; set; }
    }
}
