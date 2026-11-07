using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MaRa.Api.Controllers;

/// <summary>Délivre un ticket court pour entrer dans une seule salle d'assistance.</summary>
[ApiController]
[Route("api/assistance")]
[Authorize]
public partial class AssistanceController : ControllerBase
{
    private readonly string _cleService;

    public AssistanceController(IConfiguration cfg) =>
        _cleService = cfg["FastApi:ServiceKey"] ?? string.Empty;

    public record TicketRequest(string Salle);

    [HttpPost("ticket")]
    public IActionResult Ticket([FromBody] TicketRequest req)
    {
        if (!SalleValide().IsMatch(req.Salle ?? string.Empty))
            return BadRequest(new { message = "Code de salle invalide." });
        if (_cleService.Length < 32)
            return StatusCode(503, new { message = "Assistance non configurée." });

        var maintenant = DateTimeOffset.UtcNow;
        var charge = new JwtPayload
        {
            ["sub"]      = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "?",
            ["username"] = User.Identity?.Name ?? "?",
            ["group"]    = User.FindFirstValue("groupe") ?? "?",
            ["type"]     = "ticket",
            ["room"]     = req.Salle,
            ["exp"]      = maintenant.AddMinutes(5).ToUnixTimeSeconds(),
        };
        var signature = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cleService)), SecurityAlgorithms.HmacSha256);

        return Ok(new { ticket = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(new JwtHeader(signature), charge)) });
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex SalleValide();
}
