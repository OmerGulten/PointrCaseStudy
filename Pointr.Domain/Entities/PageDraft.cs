namespace Pointr.Domain.Entities
{
    public class PageDraft
    {
        public Guid Id { get; set; }
        public Guid PageId { get; set; }
        public int DraftNumber { get; set; }
        public string Content { get; set; } = string.Empty;

        // Navigation Properties
        public Page Page { get; set; } = default!;
        public virtual PagePublished? PagePublished { get; set; }
    }
}