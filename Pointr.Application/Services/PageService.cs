using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pointr.Application.Interfaces;
using Pointr.Domain.Entities;

namespace Pointr.Application.Services
{
    public class PageService : IPageService
    {
        private readonly IPageRepository _pageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PageService> _logger;

        private const int MaxRetries = 1;

        public PageService(IPageRepository repository, IUnitOfWork unitOfWork, IMemoryCache cache, ILogger<PageService> logger)
        {
            _pageRepository = repository;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task ArchiveAndMaybePublishAsync(Guid siteId, string slug, int? publishDraftNumber, CancellationToken ct)
        {
            int retryCount = 0;
            Page? page = null;

            while (retryCount <= MaxRetries)
            {
                try
                {
                    await _unitOfWork.BeginTransactionAsync(ct);

                    page = await _pageRepository.GetPageWithPublishedAndDraftsTrackingAsync(siteId, slug, ct);

                    Thread.Sleep(1000); // Concurrency test delay
                    if (page == null)
                        throw new KeyNotFoundException($"Page with SiteId={siteId} and Slug={slug} not found.");

                    Guid? draftToPublishId = null;

                    if (publishDraftNumber.HasValue)
                    {
                        var draft = await _pageRepository.GetDraftByPageAndNumberAsync(page.Id, publishDraftNumber.Value, ct);

                        if (draft == null)
                            throw new ArgumentException($"Draft {publishDraftNumber.Value} not found or not belonging to page {page.Id}/{page.Slug}.");
                        
                        draftToPublishId = draft.Id;
                    }

                    bool isIdempotentNoOp = page.IsArchived &&
                                            publishDraftNumber.HasValue &&
                                            page.PagePublished?.DraftId == draftToPublishId;

                    if (isIdempotentNoOp)
                    {
                        InvalidatePublishedPageCache(siteId, slug);
                        await _unitOfWork.SaveChangesAsync(ct);
                        await _unitOfWork.CommitTransactionAsync(ct);
                        return;
                    }

                    page.IsArchived = true;
                    page.UpdatedUtc = DateTime.UtcNow;

                    if (publishDraftNumber.HasValue && draftToPublishId.HasValue)
                    {
                        page.PagePublished ??= new PagePublished { PageId = page.Id };
                        page.PagePublished.DraftId = draftToPublishId.Value;
                        page.PagePublished.PublishedUtc = DateTime.UtcNow;
                    }

                    await _unitOfWork.SaveChangesAsync(ct);
                    await _unitOfWork.CommitTransactionAsync(ct);
                    InvalidatePublishedPageCache(siteId, slug);
                    return;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    retryCount++;
                    _logger.LogWarning(ex, "Concurrency conflict detected for Page {PageId}. Retrying... Attempt {Attempt}", page?.Id, retryCount);

                    if (retryCount <= MaxRetries)
                    {
                        var entry = ex.Entries.Single();
                        await entry.ReloadAsync(ct);
                        _unitOfWork.ClearChangeTracker();
                        continue;
                    }

                    throw new DbConflictException($"Concurrency conflict after {MaxRetries} retries for Page {siteId}/{slug}.", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception detected for Page {PageId}. Cancelling request", page?.Id);
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    throw;
                }
            }
        }

        public async Task<Page?> GetPublishedPageAsync(Guid siteId, string slug, CancellationToken ct)
        {
            var key = $"published-page:{siteId}:{slug}";

            if (_cache.TryGetValue<Page>(key, out var page))
            {
                return page;
            }

            var publishedPage = await _pageRepository.GetPublishedPageAsync(siteId, slug, ct);

            if (publishedPage != null)
            {
                _cache.Set(key, publishedPage, TimeSpan.FromSeconds(60));
            }
            else
            {
                _cache.Set(key, (Page?)null, TimeSpan.FromSeconds(10));
            }

            return publishedPage;
        }

        private void InvalidatePublishedPageCache(Guid siteId, string slug)
        {
            var key = $"published-page:{siteId}:{slug}";
            _cache.Remove(key);
        }
    }

    public class DbConflictException(string message, Exception inner) : Exception(message, inner)
    {
    }
}