using Pointr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pointr.Infrastructure.Data.Configurations
{
    public class PageConfiguration : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            builder.ToTable("Pages");

            builder.HasKey(p => p.Id);
            builder.HasIndex(p => new { p.SiteId, p.Slug }).IsUnique();
            builder.Property(p => p.SiteId).IsRequired();
            builder.Property(p => p.IsArchived).HasDefaultValue(false);
            builder.Property(p => p.RowVersion).IsConcurrencyToken().IsRowVersion().ValueGeneratedOnAddOrUpdate().HasColumnType("bytea").HasDefaultValueSql("'\\x0102030405060708'::bytea");

            // Relationships
            builder.HasMany(p => p.PageDrafts)
                .WithOne(d => d.Page)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
