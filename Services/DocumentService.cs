using MaRa.Api.Models.Entities;

namespace MaRa.Api.Services;

public class DocumentService : IDocumentService
{
    private readonly string _root;
    private readonly long _maxSize;

    private static readonly Dictionary<string, string> _mimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"]  = "application/pdf",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".fbx"]  = "model/fbx",
        [".gltf"] = "model/gltf+json",
        [".glb"]  = "model/gltf-binary",
        [".zip"]  = "application/zip",
    };

    public DocumentService(IConfiguration cfg)
    {
        _root    = cfg["Storage:UploadsPath"] ?? "uploads";
        _maxSize = long.Parse(cfg["Storage:MaxFileSizeBytes"] ?? "52428800");
        Directory.CreateDirectory(_root);
    }

    public async Task<Document> SaveAsync(
        IFormFile file, string typeDocument, Guid? equipementId, string? description)
    {
        if (file.Length > _maxSize)
            throw new InvalidOperationException($"Fichier trop volumineux (max {_maxSize / 1024 / 1024} Mo).");

        var ext      = Path.GetExtension(file.FileName);
        var stored   = $"{Guid.NewGuid()}{ext}";
        var subDir   = Path.Combine(_root, typeDocument);
        Directory.CreateDirectory(subDir);
        var fullPath = Path.Combine(subDir, stored);

        await using var fs = File.Create(fullPath);
        await file.CopyToAsync(fs);

        return new Document
        {
            Id              = Guid.NewGuid(),
            NomFichier      = file.FileName,
            TypeDocument    = typeDocument,
            CheminStockage  = Path.Combine(typeDocument, stored),
            TailleOctets    = file.Length,
            Description     = description,
            EquipementId    = equipementId,
        };
    }

    public (string contentType, Stream stream) GetFileStream(Document document)
    {
        var fullPath = Path.Combine(_root, document.CheminStockage);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Fichier introuvable.", fullPath);

        var ext  = Path.GetExtension(document.NomFichier);
        var mime = _mimeMap.GetValueOrDefault(ext, "application/octet-stream");
        return (mime, File.OpenRead(fullPath));
    }

    public void Delete(Document document)
    {
        var fullPath = Path.Combine(_root, document.CheminStockage);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
