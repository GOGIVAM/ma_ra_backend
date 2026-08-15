using System.ComponentModel.DataAnnotations;

namespace MaRa.Api.Models.DTOs.Inference;

/// <summary>Corps envoyé par le frontend React  image base64 + session.</summary>
public record InferenceProxyRequest(
    [Required] string SessionId,
    [Required] string ImageBase64   // image encodée en base64, JPEG/PNG
);
