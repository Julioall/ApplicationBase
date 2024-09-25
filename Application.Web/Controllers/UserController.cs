using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> AddUser(User user)
        {
            if (user == null)
                return BadRequest("User cannot be null.");

            var userDoBanco = await _userService.GetByEmailAsync(user.Account.Email);
            if (userDoBanco != null)
                return BadRequest("This email is already registered.");

            await _userService.AddAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Profile.Name }, new { message = "User added successfully." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
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
        public async Task<ActionResult<User>> GetUserById(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
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
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"User with email {email} not found.");
            }

            return Ok(user);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser(User user)
        {
            if (user == null)
            {
                return BadRequest("User cannot be null.");
            }

            var existingUser = await _userService.GetByIdAsync(user.Id);
            if (existingUser == null)
            {
                return NotFound($"User with ID {user.Id} not found.");
            }

            await _userService.UpdateAsync(user);
            return Ok(new { message = "User updated successfully." });
        }
    }
}
