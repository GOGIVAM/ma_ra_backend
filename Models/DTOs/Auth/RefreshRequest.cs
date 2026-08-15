using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Auth;

public record RefreshRequest([Required] string RefreshToken);
