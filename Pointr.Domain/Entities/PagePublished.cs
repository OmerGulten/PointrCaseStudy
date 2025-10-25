namespace Pointr.Domain.Entities
{
    public class PagePublished
    {
        public Guid PageId { get; set; }
        public Guid DraftId { get; set; }
        public DateTime PublishedUtc { get; set; }

        // Navigation Properties
        public Page Page { get; set; } = default!;
        public virtual PageDraft Draft { get; set; } = null!;
    }
}
