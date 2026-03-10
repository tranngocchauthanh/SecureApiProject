using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureApiProject.DTOs;
using SecureApiProject.Services;
using System.Security.Claims;

namespace SecureApiProject.Controllers;

/// <summary>
/// Activity 2: Authentication endpoints — Register and Login.
/// Returns JWT tokens for authenticated requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var (success, message, response) = await _authService.RegisterAsync(request);

        if (!success)
            return BadRequest(ApiResponse<AuthResponse>.Fail(message));

        return CreatedAtAction(nameof(GetProfile), null,
            ApiResponse<AuthResponse>.Ok(response!, message));
    }

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Invalid input."));

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var (success, message, response) = await _authService.LoginAsync(request, ipAddress);

        if (!success)
            return Unauthorized(ApiResponse<AuthResponse>.Fail(message));

        return Ok(ApiResponse<AuthResponse>.Ok(response!, message));
    }

    /// <summary>Get current user profile. Requires authentication.</summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
    [ProducesResponseType(401)]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var profile = new UserProfileResponse
        {
            Id = int.TryParse(userId, out var id) ? id : 0,
            Username = username ?? string.Empty,
            Email = email ?? string.Empty,
            Role = role ?? string.Empty
        };

        return Ok(ApiResponse<UserProfileResponse>.Ok(profile));
    }
}
