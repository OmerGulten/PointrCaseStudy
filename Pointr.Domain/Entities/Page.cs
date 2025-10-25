namespace Pointr.Domain.Entities
{
    public class Page
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public bool IsArchived { get; set; } = false;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public DateTime UpdatedUtc { get; set; }

        // Navigation Properties
        public ICollection<PageDraft> PageDrafts { get; set; } = new List<PageDraft>();
        public PagePublished? PagePublished { get; set; }
    }
}