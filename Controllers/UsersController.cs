using MaRa.Api.Models.DTOs.Auth;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Controllers;

/// <summary>Gestion des comptes utilisateurs  réservé ADMIN (CDC F1.3).</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private static readonly string[] ValidGroupes = ["DMAT", "DIF", "CI", "CIF", "ADMIN"];

    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuditService _audit;

    public UsersController(UserManager<ApplicationUser> users, IAuditService audit)
    {
        _users = users;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? groupe = null)
    {
        var query = _users.Users.AsQueryable();
        if (!string.IsNullOrEmpty(groupe))
            query = query.Where(u => u.Groupe == groupe);

        var list = await query
            .OrderBy(u => u.Groupe).ThenBy(u => u.UserName)
            .Select(u => new UserResponse(u.Id, u.UserName!, u.Email ?? "", u.Groupe, u.IsActive, u.CreatedAt))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest req)
    {
        if (!ValidGroupes.Contains(req.Groupe))
            return BadRequest(new { message = $"Groupe invalide. Valeurs : {string.Join(", ", ValidGroupes)}" });

        var user = new ApplicationUser
        {
            UserName = req.UserName,
            Email    = req.Email,
            Groupe   = req.Groupe,
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "CREATE_USER", $"username={req.UserName} groupe={req.Groupe}");

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new UserResponse(user.Id, user.UserName!, user.Email ?? "", user.Groupe, user.IsActive, user.CreatedAt));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        return Ok(new UserResponse(user.Id, user.UserName!, user.Email ?? "", user.Groupe, user.IsActive, user.CreatedAt));
    }

    [HttpPatch("{id}/active")]
    public async Task<IActionResult> SetActive(string id, [FromBody] bool isActive)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = isActive;
        await _users.UpdateAsync(user);

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, isActive ? "ACTIVATE_USER" : "DEACTIVATE_USER", $"userId={id}");

        return NoContent();
    }

    [HttpPatch("{id}/groupe")]
    public async Task<IActionResult> SetGroupe(string id, [FromBody] string groupe)
    {
        if (!ValidGroupes.Contains(groupe))
            return BadRequest(new { message = $"Groupe invalide." });

        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.Groupe = groupe;
        await _users.UpdateAsync(user);

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "CHANGE_GROUPE", $"userId={id} groupe={groupe}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Désactivation plutôt que suppression pour garder la traçabilité des logs
        user.IsActive = false;
        await _users.UpdateAsync(user);

        var caller = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "?";
        _audit.LogAction(caller, "DELETE_USER", $"userId={id} username={user.UserName}");

        return NoContent();
    }
}
