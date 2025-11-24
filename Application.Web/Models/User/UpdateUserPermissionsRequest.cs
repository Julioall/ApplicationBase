using System.Collections.Generic;
using System.Linq;

namespace Application.Api.Models.User
{
    public class UpdateUserPermissionsRequest
    {
        public IEnumerable<string> Permissions { get; set; } = Enumerable.Empty<string>();
    }
}
