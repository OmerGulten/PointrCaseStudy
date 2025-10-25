namespace Pointr.API.Models
{
    public class PagePublishedDto
    {
        public Guid DraftId { get; set; }
        public int DraftNumber { get; set; }
        public DateTime PublishedUtc { get; set; }
        public string Content { get; set; }
    }
}
