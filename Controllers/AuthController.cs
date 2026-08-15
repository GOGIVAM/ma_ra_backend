using MaRa.Api.Models.DTOs.Auth;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MaRa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly IAuditService _audit;

    // Stockage en mémoire des refresh tokens (production : table dédiée ou Redis)
    private static readonly Dictionary<string, (string UserId, DateTime Expiry)> _refreshStore = new();

    public AuthController(UserManager<ApplicationUser> users, ITokenService tokens, IAuditService audit)
    {
        _users  = users;
        _tokens = tokens;
        _audit  = audit;
    }

    /// <summary>Authentification par identifiant/mot de passe (CDC F1.1).</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var ip   = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "?";
        var user = await _users.FindByNameAsync(req.UserName);

        if (user is null || !user.IsActive || !await _users.CheckPasswordAsync(user, req.Password))
        {
            _audit.LogEchec(req.UserName, ip, user is null ? "utilisateur_inconnu" : "mot_de_passe_incorrect");
            return Unauthorized(new { message = "Identifiants invalides." });
        }

        var expiresDays  = 30;
        var accessToken  = _tokens.GenerateAccessToken(user);
        var refreshToken = _tokens.GenerateRefreshToken();

        _refreshStore[refreshToken] = (user.Id, DateTime.UtcNow.AddDays(expiresDays));
        _audit.LogConnexion(user.Id, user.UserName!, user.Groupe, ip);

        return Ok(new TokenResponse(accessToken, refreshToken, _tokens.AccessTokenExpiry, user.UserName!, user.Groupe));
    }

    /// <summary>Renouvellement du jeton d'accès via refresh token.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        if (!_refreshStore.TryGetValue(req.RefreshToken, out var entry) || entry.Expiry < DateTime.UtcNow)
        {
            _refreshStore.Remove(req.RefreshToken);
            return Unauthorized(new { message = "Refresh token invalide ou expiré." });
        }

        var user = await _users.FindByIdAsync(entry.UserId);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Compte inactif." });

        _refreshStore.Remove(req.RefreshToken);
        var newRefresh = _tokens.GenerateRefreshToken();
        _refreshStore[newRefresh] = (user.Id, DateTime.UtcNow.AddDays(30));

        return Ok(new TokenResponse(
            _tokens.GenerateAccessToken(user), newRefresh,
            _tokens.AccessTokenExpiry, user.UserName!, user.Groupe));
    }

    /// <summary>Profil de l'utilisateur connecté.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "");
        if (user is null) return Unauthorized();

        return Ok(new UserResponse(user.Id, user.UserName!, user.Email ?? "", user.Groupe, user.IsActive, user.CreatedAt));
    }

    /// <summary>Révocation du refresh token (déconnexion propre).</summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout([FromBody] RefreshRequest req)
    {
        _refreshStore.Remove(req.RefreshToken);
        return NoContent();
    }
}
