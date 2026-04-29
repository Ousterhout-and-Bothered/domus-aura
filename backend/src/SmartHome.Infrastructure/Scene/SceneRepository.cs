using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Scene;
using SmartHome.Infrastructure.Device.Repository;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Scene;

/// <summary>
/// EF Core implementation of <see cref="ISceneRepository"/>. Persists <see cref="DeviceScene"/>
/// aggregates together with their action collections.
/// </summary>
public sealed class SceneRepository(SmartHomeDbContext dbContext)
    : EfRepositoryBase(dbContext), ISceneRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DeviceScene>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Scenes
            .AsNoTracking()
            .Include(s => s.Actions.OrderBy(a=>a.OrderIndex))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DeviceScene?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Tracked: callers may mutate via Rename/ReplaceActions and persist through SaveChangesAsync.
        return await dbContext.Scenes
            .Include(s => s.Actions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(DeviceScene scene, CancellationToken cancellationToken = default)
    {
        await dbContext.Scenes.AddAsync(scene, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(DeviceScene scene, CancellationToken cancellationToken = default)
    {
        foreach (var action in scene.Actions)
        {
            var entry = dbContext.Entry(action);
            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }
        // The aggregate has already been mutated by the service layer. EF's change tracker,
        // combined with cascade-delete-orphans on the Actions navigation (see DbContext),
        // handles the wholesale replacement of actions when SaveChangesAsync is called.
        // This method exists to satisfy the interface contract and is intentionally a no-op.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scene = await dbContext.Scenes
            .Include(s => s.Actions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (scene is null) return false;

        dbContext.Scenes.Remove(scene);
        return true;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> RemoveActionsForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var scenes = await dbContext.Scenes
            .Include(s => s.Actions)
            .Where(s => s.Actions.Any(a => a.DeviceId == deviceId))
            .ToListAsync(cancellationToken);

        var affectedSceneNames = new List<string>();

        foreach (var scene in scenes)
        {
            var remainingActions = scene.Actions
                .Where(a => a.DeviceId != deviceId)
                .ToList();

            if (remainingActions.Count != scene.Actions.Count)
            {
                affectedSceneNames.Add(scene.Name);
            }

            if (remainingActions.Count == 0)
            {
                dbContext.Scenes.Remove(scene);
            }
            else
            {
                scene.ReplaceActions(remainingActions);
                await UpdateAsync(scene, cancellationToken);
            }
        }

        return affectedSceneNames;
    }
}