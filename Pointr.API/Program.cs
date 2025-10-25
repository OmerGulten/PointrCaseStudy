using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pointr.API.Models;
using Pointr.Application.Interfaces;
using Pointr.Application.Services;
using Pointr.Domain.Entities;
using Pointr.Infrastructure.Data;
using Pointr.Infrastructure.Repositories;

// for tests
namespace Pointr.API
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    });
            });

            builder.Services.AddScoped<IPageService, PageService>();
            builder.Services.AddScoped<IPageRepository, PageRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddMemoryCache();
            builder.Services.AddLogging();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.MapDelete("api/v1/sites/{siteId}/pages/{slug}", DeleteAndMaybePublish)
               .WithName("DeleteAndMaybePublish")
               .Produces(StatusCodes.Status204NoContent)
               .Produces(StatusCodes.Status404NotFound)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status409Conflict);

            app.MapGet("api/v1/sites/{siteId}/pages/{slug}/published", GetPublishedPage)
               .WithName("GetPublishedPage")
               .Produces<Page>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status404NotFound);

            app.Run();
        }

        private static async Task<IResult> DeleteAndMaybePublish(Guid siteId, string slug, [FromQuery] int? publishDraft, IPageService svc, CancellationToken ct)
        {
            try
            {
                await svc.ArchiveAndMaybePublishAsync(siteId, slug, publishDraft, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"Page {siteId}/{slug} not found." });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (DbConflictException)
            {
                return Results.Conflict(new { error = $"Concurrency conflict after max retries for page {siteId}/{slug}." });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        }

        private static async Task<IResult> GetPublishedPage(Guid siteId, string slug, IPageService svc, CancellationToken ct)
        {
            var page = await svc.GetPublishedPageAsync(siteId, slug, ct);

            if (page == null) return Results.NotFound();

            var pageDto = new PageDto
            {
                Id = page.Id,
                SiteId = page.SiteId,
                Slug = page.Slug,
                IsArchived = page.IsArchived,
                UpdatedUtc = page.UpdatedUtc,
                PagePublished = page.PagePublished == null ? null : new PagePublishedDto
                {
                    DraftId = page.PagePublished.DraftId,
                    PublishedUtc = page.PagePublished.PublishedUtc,
                    DraftNumber = page.PagePublished.Draft.DraftNumber,
                    Content = page.PagePublished.Draft.Content ?? "No Content"
                }
            };

            return Results.Ok(pageDto);
        }
    }
}