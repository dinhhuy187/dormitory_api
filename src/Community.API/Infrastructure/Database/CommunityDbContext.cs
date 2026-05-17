using Community.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Community.API.Infrastructure.Database;

public class CommunityDbContext(DbContextOptions<CommunityDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<PostReport> PostReports => Set<PostReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityDbContext).Assembly);
    }
}