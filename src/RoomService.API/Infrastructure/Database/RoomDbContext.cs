using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Entities;

namespace RoomService.API.Infrastructure.Database
{
    public class RoomDbContext(DbContextOptions<RoomDbContext> options) : DbContext(options)
    {
        public DbSet<Building> Buildings { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoomDbContext).Assembly);
        }
    }
}