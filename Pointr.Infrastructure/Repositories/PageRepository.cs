using Microsoft.EntityFrameworkCore;
using Pointr.Application.Interfaces;
using Pointr.Domain.Entities;
using Pointr.Infrastructure.Data;

namespace Pointr.Infrastructure.Repositories
{
    public class PageRepository : IPageRepository
    {
        private readonly ApplicationDbContext _context;

        public PageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Page?> GetPageWithPublishedAndDraftsTrackingAsync(Guid siteId, string slug, CancellationToken ct)
        {
            return _context.Pages
                .Include(p => p.PagePublished) 
                .FirstOrDefaultAsync(p => p.SiteId == siteId && p.Slug == slug, ct);
        }

        public Task<PageDraft?> GetDraftByPageAndNumberAsync(Guid pageId, int draftNumber, CancellationToken ct)
        {
            return _context.PageDrafts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.PageId == pageId && d.DraftNumber == draftNumber, ct);
        }
        
        public Task<Page?> GetPublishedPageAsync(Guid siteId, string slug, CancellationToken ct)
        {
            return _context.Pages
                .AsNoTracking()
                .Include(p => p.PagePublished)
                    .ThenInclude(pp => pp.Draft)
                .Where(p => p.PagePublished != null)
                .FirstOrDefaultAsync(p => p.SiteId == siteId && p.Slug == slug && !p.IsArchived, ct);
        }
    }
}