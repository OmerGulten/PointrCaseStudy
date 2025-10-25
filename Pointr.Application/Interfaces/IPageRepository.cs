using Pointr.Domain.Entities;

namespace Pointr.Application.Interfaces
{
    public interface IPageRepository
    {
        Task<Page?> GetPageWithPublishedAndDraftsTrackingAsync(Guid siteId, string slug, CancellationToken ct);
        Task<PageDraft?> GetDraftByPageAndNumberAsync(Guid pageId, int draftNumber, CancellationToken ct);
        Task<Page?> GetPublishedPageAsync(Guid siteId, string slug, CancellationToken ct);
    }
}
