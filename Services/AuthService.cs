using Microsoft.EntityFrameworkCore;
using SecureApiProject.Data;
using SecureApiProject.DTOs;
using SecureApiProject.Models;

namespace SecureApiProject.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string Message, AuthResponse? Response)> LoginAsync(LoginRequest request, string ipAddress);
}

/// <summary>
/// Activity 1 & 2: Secure authentication service.
/// - Passwords are hashed with BCrypt (never stored in plain text)
/// - Account lockout after 5 failed attempts (brute-force protection)
/// - Audit logging for all auth events
/// </summary>
public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        // Activity 3: Input validation — check for existing user
        var exists = await _db.Users
            .AnyAsync(u => u.Email == request.Email.ToLower() || u.Username == request.Username);

        if (exists)
            return (false, "Email or username already exists.", null);

        // Activity 1: Hash password with BCrypt (work factor 12)
        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        // Audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "REGISTER",
            Details = $"New user registered: {user.Email}",
            Success = true,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        var expirationMinutes = 60;

        return (true, "Registration successful.", new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        });
    }

    public async Task<(bool Success, string Message, AuthResponse? Response)> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        // Activity 3: Timing-safe check — don't reveal whether email exists
        if (user == null)
        {
            await LogAuditAsync("LOGIN_FAILED", null, ipAddress, "Email not found", false);
            return (false, "Invalid email or password.", null);
        }

        // Activity 2: Account lockout check
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            var remaining = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            await LogAuditAsync("LOGIN_LOCKED", user.Id.ToString(), ipAddress, "Account locked", false);
            return (false, $"Account is locked. Try again in {remaining} minutes.", null);
        }

        if (!user.IsActive)
            return (false, "Account has been deactivated.", null);

        // Activity 1: BCrypt verification
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("Account locked for {Email} after {Attempts} failed attempts.", user.Email, user.FailedLoginAttempts);
            }

            await _db.SaveChangesAsync();
            await LogAuditAsync("LOGIN_FAILED", user.Id.ToString(), ipAddress, "Wrong password", false);
            return (false, "Invalid email or password.", null);
        }

        // Successful login — reset counters
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await LogAuditAsync("LOGIN_SUCCESS", user.Id.ToString(), ipAddress, "Login successful", true);

        var token = _tokenService.GenerateToken(user);

        return (true, "Login successful.", new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    private async Task LogAuditAsync(string action, string? userId, string ipAddress, string details, bool success)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            UserId = userId,
            IpAddress = ipAddress,
            Details = details,
            Success = success,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
