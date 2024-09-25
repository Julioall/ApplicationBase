using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service.Dtos
{
    public record LoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
