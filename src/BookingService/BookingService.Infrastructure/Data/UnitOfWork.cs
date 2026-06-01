using BookingService.Application.Abtractions.Data;
using BookingService.Domain.SeedWork;
using MassTransit;

namespace BookingService.Infrastructure.Data;

public class UnitOfWork(BookingDbContext dbContext, IPublishEndpoint publishEndpoint) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var allEntries = dbContext.ChangeTracker.Entries().ToList();
        Console.WriteLine($"[UnitOfWork] Total tracked entries: {allEntries.Count}");
        foreach (var entry in allEntries)
        {
            Console.WriteLine($"[UnitOfWork] Tracked entry: {entry.Entity.GetType().FullName}, State: {entry.State}");
        }

        var domainEntities = dbContext.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        Console.WriteLine($"[UnitOfWork] Found {domainEntities.Count} entities with domain events.");
        var domainEvents = domainEntities.SelectMany(x => x.Entity.DomainEvents).ToList();
        Console.WriteLine($"[UnitOfWork] Total domain events to publish: {domainEvents.Count}");

        foreach (var entity in domainEntities)
        {
            entity.Entity.ClearDomainEvents();
        }
        foreach (var domainEvent in domainEvents)
        {
            Console.WriteLine($"[UnitOfWork] Publishing domain event: {domainEvent.GetType().FullName}");
            await publishEndpoint.Publish((object)domainEvent, cancellationToken);
        }
        var result = await dbContext.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"[UnitOfWork] SaveChangesAsync result: {result}");
        return result;
    }
}