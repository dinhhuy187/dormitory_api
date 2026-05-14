using BookingService.Application.Abtractions.Data;
using BookingService.Domain.SeedWork;
using MassTransit;

namespace BookingService.Infrastructure.Data;

public class UnitOfWork(BookingDbContext dbContext, IPublishEndpoint publishEndpoint) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = dbContext.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();
        var domainEvents = domainEntities.SelectMany(x => x.Entity.DomainEvents).ToList();
        foreach (var entity in domainEntities)
        {
            entity.Entity.ClearDomainEvents();
        }
        foreach (var domainEvent in domainEvents)
        {
            await publishEndpoint.Publish(domainEvent, cancellationToken);
        }
        var result = await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }
}