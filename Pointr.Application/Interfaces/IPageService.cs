using Pointr.Domain.Entities;

namespace Pointr.Application.Interfaces
{
    public interface IPageService
    {
        Task ArchiveAndMaybePublishAsync(Guid siteId, string slug, int? publishDraftNumber, CancellationToken ct);
        Task<Page?> GetPublishedPageAsync(Guid siteId, string slug, CancellationToken ct);
    }
}
