using Microsoft.EntityFrameworkCore;
using Pointr.Domain.Entities;
using System.Reflection;

namespace Pointr.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Page> Pages => Set<Page>();
        public DbSet<PageDraft> PageDrafts => Set<PageDraft>();
        public DbSet<PagePublished> PagePublished => Set<PagePublished>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}
