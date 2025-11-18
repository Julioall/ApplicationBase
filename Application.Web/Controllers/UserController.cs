using Application.Domain.Exceptions;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            if (user == null)
            {
                return Problem(title: "Requisição inválida", detail: "User não pode ser nulo.", statusCode: StatusCodes.Status400BadRequest);
            }

            await _userService.AddAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { message = "User added successfully." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {id} not found.");
            }

            await _userService.DeleteAsync(id);
            return NoContent();
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("get/{id}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetUserById(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {id} not found.");
            }

            return Ok(user);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("role/{role}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersByRole(string role)
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("username/{username}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetUserByEmail(string username)
        {
            var user = await _userService.GetByEmailAsync(username);
            if (user == null)
            {
                throw new NotFoundException($"User with email {username} not found.");
            }

            return Ok(user);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            if (user.Id.IsNullOrEmpty())
            {
                return Problem(title: "Requisição inválida", detail: "User não pode ser nulo.", statusCode: StatusCodes.Status400BadRequest);
            }

            var existingUser = await _userService.GetByIdAsync(user.Id);
            if (existingUser == null)
            {
                throw new NotFoundException($"User with ID {user.Id} not found.");
            }

            await _userService.UpdateAsync(user);
            return Ok(new { message = "User updated successfully." });
        }
    }
}
