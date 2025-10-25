using Microsoft.EntityFrameworkCore;
using Pointr.Domain.Entities;
using System.Net;
using Xunit;

namespace Pointr.Tests
{
    public class ArchivePublishTests : IClassFixture<TestingFactory>
    {
        private readonly HttpClient _client;
        private readonly TestingFactory _factory;

        private readonly Guid _siteId = Guid.NewGuid();
        private const string Slug = "test-page";

        public ArchivePublishTests(TestingFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<(Page Page, PageDraft Draft1, PageDraft Draft2)> SeedDataAsync()
        {
            using var context = _factory.CreateDbContext();

            context.Pages.RemoveRange(context.Pages);
            context.PageDrafts.RemoveRange(context.PageDrafts);
            context.PagePublished.RemoveRange(context.PagePublished);
            await context.SaveChangesAsync();

            var pageId = Guid.NewGuid();

            var page = new Page
            {
                Id = pageId,
                SiteId = _siteId,
                Slug = Slug,
                IsArchived = false,
                UpdatedUtc = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            var draft1 = new PageDraft
            {
                Id = Guid.NewGuid(),
                PageId = pageId,
                DraftNumber = 1,
                Content = "Draft 1 Content"
            };

            var draft2 = new PageDraft
            {
                Id = Guid.NewGuid(),
                PageId = pageId,
                DraftNumber = 2,
                Content = "Draft 2 Content"
            };

            var published = new PagePublished
            {
                PageId = pageId,
                DraftId = draft2.Id,
                PublishedUtc = DateTime.UtcNow
            };

            context.Pages.Add(page);
            context.PageDrafts.AddRange(draft1, draft2);
            context.PagePublished.Add(published);
            await context.SaveChangesAsync();

            var freshPage = await context.Pages.SingleAsync(p => p.Id == pageId);

            return (freshPage, draft1, draft2);
        }

        private async Task<Page?> GetPageStatusAsync(Guid pageId)
        {
            using var context = _factory.CreateDbContext();
            return await context.Pages
                .AsNoTracking()
                .Include(p => p.PagePublished)
                .SingleOrDefaultAsync(p => p.Id == pageId);
        }
        
        [Fact]
        public async Task IdempotencyTest_RepeatedRequestShouldBe204AndStateCorrect()
        {
            var (initialPage, draft1, _) = await SeedDataAsync();
            var url = $"api/v1/sites/{_siteId}/pages/{Slug}?publishDraft=1";

            var response1 = await _client.DeleteAsync(url);

            Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);

            var state1 = await GetPageStatusAsync(initialPage.Id);

            var response2 = await _client.DeleteAsync(url);

            Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);

            var state2 = await GetPageStatusAsync(initialPage.Id);

            Assert.True(state2!.IsArchived);
            Assert.NotNull(state2.PagePublished);
            Assert.Equal(draft1.Id, state2.PagePublished!.DraftId);

            Assert.True(state1!.UpdatedUtc > initialPage.UpdatedUtc);
            Assert.Equal(state1.UpdatedUtc, state2.UpdatedUtc);
        }
        
        [Fact]
        public async Task ConcurrencyTest_TwoOverlappingRequests_OneShouldSucceed_OneShouldSuccedAfterRetryOnce()
        {
            await SeedDataAsync();
            var url = $"api/v1/sites/{_siteId}/pages/{Slug}?publishDraft=1";

            var task1 = Task.Run(() => _client.DeleteAsync(url));
            var task2 = Task.Run(() => _client.DeleteAsync(url));

            var responses = await Task.WhenAll(task1, task2);

            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.NoContent);
            var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

            Console.WriteLine("successCount: " + successCount);
            Console.WriteLine("conflictCount: " + conflictCount);
            Assert.Equal(2, successCount);
            Assert.Equal(0, conflictCount);
        }
        
        [Fact]
        public async Task NotFoundTest_ShouldReturn404IfPageIsMissing()
        {
            await SeedDataAsync();
            var nonExistentSlug = "non-existent-slug";
            var url = $"api/v1/sites/{_siteId}/pages/{nonExistentSlug}";

            var response = await _client.DeleteAsync(url);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task BadRequestTest_ShouldReturn400IfDraftDoesNotBelongToPage()
        {
            await SeedDataAsync();
            var url = $"api/v1/sites/{_siteId}/pages/{Slug}?publishDraft=999";

            var response = await _client.DeleteAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SuccessfulArchiveAndPublish_ShouldReturn204AndSetStateCorrectly()
        {
            var (initialPage, draft1, _) = await SeedDataAsync();
            var url = $"api/v1/sites/{_siteId}/pages/{Slug}?publishDraft=1";

            var response = await _client.DeleteAsync(url);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var finalPage = await GetPageStatusAsync(initialPage.Id);

            Assert.NotNull(finalPage);
            Assert.True(finalPage!.IsArchived);
            Assert.NotNull(finalPage.PagePublished);
            Assert.Equal(draft1.Id, finalPage.PagePublished!.DraftId);
        }

        [Fact]
        public async Task CacheInvalidation_ShouldClearCacheAfterSuccessfulOperation()
        {
            await SeedDataAsync();
            var urlPublish = $"api/v1/sites/{_siteId}/pages/{Slug}?publishDraft=1";
            var urlGet = $"api/v1/sites/{_siteId}/pages/{Slug}/published";

            await _client.GetAsync(urlGet);

            await _client.DeleteAsync(urlPublish);

            var getResponse = await _client.GetAsync(urlGet);

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }
}