using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pointr.Domain.Entities;

namespace Pointr.Infrastructure.Data.Configurations
{
    public class PageDraftConfiguration : IEntityTypeConfiguration<PageDraft>
    {
        public void Configure(EntityTypeBuilder<PageDraft> builder)
        {
            builder.ToTable("PageDrafts");

            builder.HasKey(pd => pd.Id);
            builder.HasIndex(pd => new { pd.PageId, pd.DraftNumber }).IsUnique();
            builder.Property(pd => pd.PageId).IsRequired();
            builder.Property(pd => pd.DraftNumber).IsRequired();
            builder.Property(pd => pd.Content).IsRequired();
        }
    }
}