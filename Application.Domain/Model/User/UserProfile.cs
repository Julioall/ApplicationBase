using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Domain.Model.User
{
    public class UserProfile
    {
        public required string Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public double ProfilePictureOffsetX { get; set; }
        public double ProfilePictureOffsetY { get; set; }
        public double ProfilePictureScale { get; set; } = 1;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Organization { get; set; }
        public string? Location { get; set; }
    }
}
