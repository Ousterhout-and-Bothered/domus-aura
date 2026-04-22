using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Device.Repository;

/// <summary>
/// EF Core implementation of the device repository contract.
/// Responsible for persistence concerns for devices.
/// </summary>
public sealed class SimulationRepository(SmartHomeDbContext dbContext)
    : EfRepositoryBase(dbContext), ISimulationRepository
{
        /// <inheritdoc />
        public async Task<IReadOnlyList<ITickable>> GetTickableAsync(CancellationToken cancellationToken = default)
        {
            return await dbContext.Devices
                .OfType<TickableDevice>()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ResetAllAsync(CancellationToken cancellationToken = default)
        {
            var devices = await dbContext.Devices.ToListAsync(cancellationToken);

            foreach (var device in devices)
                device.ResetToDefaults();
        }
    }
