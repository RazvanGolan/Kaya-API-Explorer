using Microsoft.AspNetCore.Mvc;
using Demo.WebApi.Models;

namespace Demo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly List<User> _users =
    [
        new User 
        { 
            Id = 1, 
            Name = "John Doe", 
            Email = "john@example.com", 
            CreatedAt = DateTime.UtcNow.AddDays(-30), 
            IsActive = true,
            Role = UserRole.Admin,
            ContactInfo = new ContactInfo(
                "john@example.com",
                "+1-555-0123",
                new Address("123 Main St", "New York", "NY", "10001", "USA")
            ),
            Tags = ["premium", "early-adopter"]
        },
        new User 
        { 
            Id = 2, 
            Name = "Jane Smith", 
            Email = "jane@example.com", 
            CreatedAt = DateTime.UtcNow.AddDays(-15), 
            IsActive = true,
            Role = UserRole.User,
            ContactInfo = new ContactInfo(
                "jane@example.com",
                "+1-555-0456",
                new Address("456 Oak Ave", "Los Angeles", "CA", "90210", "USA")
            ),
            Tags = ["new-user"]
        },
        new User 
        { 
            Id = 3, 
            Name = "Bob Johnson", 
            Email = "bob@example.com", 
            CreatedAt = DateTime.UtcNow.AddDays(-7), 
            IsActive = false,
            Role = UserRole.User,
            Tags = ["inactive"]
        }
    ];

    /// <summary>
    /// Gets all users with optional filtering
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="role">Filter by user role</param>
    /// <param name="search">Search in name or email</param>
    /// <returns>List of users</returns>
    [HttpGet]
    public ActionResult<IEnumerable<User>> GetUsers(
        [FromQuery] bool? isActive = null, 
        [FromQuery] UserRole? role = null,
        [FromQuery] string? search = null)
    {
        var users = _users.AsEnumerable();

        if (isActive.HasValue)
        {
            users = users.Where(u => u.IsActive == isActive.Value);
        }

        if (role.HasValue)
        {
            users = users.Where(u => u.Role == role.Value);
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
            IsActive = true,
            Role = request.Role,
            ContactInfo = request.ContactInfo,
            Preferences = request.Preferences ?? new UserPreferences(),
            Tags = request.Tags
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

        if (request.Role.HasValue)
        {
            user.Role = request.Role.Value;
        }

        if (request.ContactInfo != null)
        {
            user.ContactInfo = request.ContactInfo;
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
        var averageAgeDays = _users.Any() ? 
            _users.Average(u => (DateTime.UtcNow - u.CreatedAt).TotalDays) : 0;

        return Ok(new
        {
            TotalUsers = _users.Count,
            ActiveUsers = _users.Count(u => u.IsActive),
            InactiveUsers = _users.Count(u => !u.IsActive),
            AverageAccountAgeDays = Math.Round(averageAgeDays, 1),
            UsersByRole = _users.GroupBy(u => u.Role).ToDictionary(g => g.Key.ToString(), g => g.Count())
        });
    }

    /// <summary>
    /// Test method with multiple HTTP attributes
    /// This method should appear as two separate endpoints in the API Explorer
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Different responses based on HTTP method</returns>
    [HttpGet("test/{id}")]
    [HttpPost("test/{id}/action")]
    public ActionResult<object> TestMultipleRoutes(int id)
    {
        var httpMethod = HttpContext.Request.Method;
        var user = _users.FirstOrDefault(u => u.Id == id);
        
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return httpMethod switch
        {
            "GET" => Ok(new { Method = "GET", User = user, Message = "Retrieved user via GET" }),
            "POST" => Ok(new { Method = "POST", User = user, Message = "Performed action via POST", ActionTime = DateTime.UtcNow }),
            _ => BadRequest("Unsupported method")
        };
    }

    /// <summary>
    /// Test endpoint that returns a very long response in a single line
    /// This is used to test the overflow handling in the API Explorer UI
    /// </summary>
    /// <returns>A response with extremely long content to test UI overflow behavior</returns>
    [HttpGet("test-long-response")]
    public ActionResult<object> GetLongResponse()
    {
        var longString = string.Join("", Enumerable.Range(1, 1000).Select(i => $"VeryLongWordNumber{i}WithNoSpacesToBreakTheLineAndCauseHorizontalScrollingIssues"));
        
        var longArray = Enumerable.Range(1, 100).Select(i => new
        {
            Id = i,
            VeryLongPropertyNameThatShouldNotBreakTheLayout = $"ExtremelyLongValueWithNoSpaces{i}ThatWouldNormallyStretchTheContainerBeyondItsLimits",
            AnotherLongProperty = $"ThisIsAnotherVeryLongStringWith{i}NumbersThatShouldBeContainedWithinTheResponseContainer",
            SingleLineData = longString.Substring(0, Math.Min(500, longString.Length)), // Truncate for each item
            Metadata = new
            {
                ProcessingTime = DateTime.UtcNow,
                VeryLongMetadataField = "ThisFieldContainsExtremelyLongContentThatWouldNormallyBreakTheUILayoutAndCauseHorizontalScrollingProblemsButShouldBeContainedWithinTheResponseContainer"
            }
        }).ToArray();

        return Ok(new
        {
            Message = "This response contains extremely long content to test UI overflow behavior",
            SingleLineLongString = longString,
            LongArray = longArray,
            TestData = new
            {
                VeryLongPropertyName = "ExtremelyLongValueThatShouldNotBreakTheLayoutOrCauseThePageToStretchBeyondItsNormalBoundariesBecauseOfOverflowConstraints",
                Instructions = "This endpoint is specifically designed to test how the API Explorer handles very long responses that could potentially break the layout",
                ExpectedBehavior = "The response should be contained within a scrollable area without stretching the page or breaking the layout"
            }
        });
    }
}
