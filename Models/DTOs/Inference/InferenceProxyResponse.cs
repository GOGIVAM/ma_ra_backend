namespace MaRa.Api.Models.DTOs.Inference;

/// <summary>Réponse renvoyée au frontend React après passage par FastAPI.</summary>
public record InferenceProxyResponse(
    string SessionId,
    IEnumerable<DetectionOut> Detections,
    bool FallbackQrRecommended,
    double InferenceMs,
    string PipelineVersion
);

public record DetectionOut(
    string Class,
    double ScoreFinal,
    double ScoreDet,
    double ScoreCls,
    double ScoreSmoothed,
    bool PreActivateVuforia,
    BoundingBoxOut BoundingBox
);

public record BoundingBoxOut(double X1, double Y1, double X2, double Y2, double Confidence);
