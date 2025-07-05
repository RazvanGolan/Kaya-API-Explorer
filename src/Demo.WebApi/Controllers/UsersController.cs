using Microsoft.AspNetCore.Mvc;
using Demo.WebApi.Models;

namespace Demo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow.AddDays(-30), IsActive = true },
        new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.UtcNow.AddDays(-15), IsActive = true },
        new User { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", CreatedAt = DateTime.UtcNow.AddDays(-7), IsActive = false }
    };

    /// <summary>
    /// Gets all users with optional filtering
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="search">Search in name or email</param>
    /// <returns>List of users</returns>
    [HttpGet]
    public ActionResult<IEnumerable<User>> GetUsers([FromQuery] bool? isActive = null, [FromQuery] string? search = null)
    {
        var users = _users.AsEnumerable();

        if (isActive.HasValue)
        {
            users = users.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            users = users.Where(u => u.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                   u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(users);
    }

    /// <summary>
    /// Gets a specific user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    public ActionResult<User> GetUser(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return Ok(user);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">User creation data</param>
    /// <returns>Created user</returns>
    [HttpPost]
    public ActionResult<User> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email))
        {
            return BadRequest("Name and Email are required");
        }

        var user = new User
        {
            Id = _users.Max(u => u.Id) + 1,
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _users.Add(user);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">User update data</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id}")]
    public ActionResult<User> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            user.Name = request.Name;
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            user.Email = request.Email;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        return Ok(user);
    }

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    public ActionResult DeleteUser(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        _users.Remove(user);
        return Ok(new { message = $"User {user.Name} deleted successfully" });
    }

    /// <summary>
    /// Gets user statistics
    /// </summary>
    /// <returns>User statistics</returns>
    [HttpGet("stats")]
    public ActionResult<object> GetUserStats()
    {
        return Ok(new
        {
            TotalUsers = _users.Count,
            ActiveUsers = _users.Count(u => u.IsActive),
            InactiveUsers = _users.Count(u => !u.IsActive),
            AverageAge = DateTime.UtcNow.Subtract(_users.Average(u => u.CreatedAt.Ticks)).Days
        });
    }
}
