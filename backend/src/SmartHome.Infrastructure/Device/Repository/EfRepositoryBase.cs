using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Device.Repository;

public abstract class EfRepositoryBase(SmartHomeDbContext dbContext)
{
    protected readonly SmartHomeDbContext dbContext = dbContext;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateThermostatException("this location (database constraint violation)");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("Unique constraint failed") ?? false;
    }
}