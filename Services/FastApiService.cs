using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MaRa.Api.Models.DTOs.Inference;

namespace MaRa.Api.Services;

/// <summary>
/// Client HTTP vers le service de reconnaissance Python/FastAPI (CDC §4.4.2).
/// Accès restreint au réseau interne, authentifié par clé de service (CDC §4.5).
/// En cas d'indisponibilité, retourne null pour déclencher le mode de repli (F2.4).
/// </summary>
public class FastApiService : IFastApiService
{
    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<FastApiService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public FastApiService(HttpClient http, IConfiguration cfg, ILogger<FastApiService> logger)
    {
        _http       = http;
        _serviceKey = cfg["FastApi:ServiceKey"] ?? string.Empty;
        _logger     = logger;
    }

    public async Task<InferenceProxyResponse?> RunInferenceAsync(
        string sessionId, string imageBase64, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                session_id   = sessionId,
                image_base64 = imageBase64
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inference")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.Add("X-Service-Key", _serviceKey);

            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var raw = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<InferenceProxyResponse>(raw, _jsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service FastAPI indisponible — mode de repli activé");
            return null;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/api/v1/health", ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
