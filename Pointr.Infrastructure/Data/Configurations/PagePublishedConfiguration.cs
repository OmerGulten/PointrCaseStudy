using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pointr.Domain.Entities;

namespace Pointr.Infrastructure.Data.Configurations
{
    public class PagePublishedConfiguration : IEntityTypeConfiguration<PagePublished>
    {
        public void Configure(EntityTypeBuilder<PagePublished> builder)
        {
            builder.ToTable("PagePublished");

            builder.HasKey(pp => pp.PageId);
            builder.Property(pp => pp.DraftId).IsRequired();
            builder.Property(pp => pp.PublishedUtc).HasDefaultValueSql("NOW()");

            // Relationships
            builder.HasOne(pp => pp.Page)
                .WithOne(p => p.PagePublished)
                .HasForeignKey<PagePublished>(pp => pp.PageId);

            builder.HasOne(pp => pp.Draft)
                .WithOne(pd => pd.PagePublished)
                .HasForeignKey<PagePublished>(pp => pp.DraftId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
