namespace Pointr.API.Models
{
    public class PageDto
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string Slug { get; set; }
        public bool IsArchived { get; set; }
        public DateTime UpdatedUtc { get; set; }

        public PagePublishedDto? PagePublished { get; set; }
    }
}
