using MaRa.Api.Models.Entities;

namespace MaRa.Api.Services;

public interface IDocumentService
{
    Task<Document> SaveAsync(IFormFile file, string typeDocument, Guid? equipementId, string? description);
    (string contentType, Stream stream) GetFileStream(Document document);
    void Delete(Document document);
}
