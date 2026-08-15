using MaRa.Api.Models.DTOs.Inference;

namespace MaRa.Api.Services;

public interface IFastApiService
{
    /// <summary>
    /// Transmet une frame image au service FastAPI de reconnaissance.
    /// Retourne null si le service est indisponible (CDC §4.5  mode de repli).
    /// </summary>
    Task<InferenceProxyResponse?> RunInferenceAsync(string sessionId, string imageBase64, CancellationToken ct = default);

    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
