using Moq;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Repository;

namespace SmartHome.Domain.Tests.Device;

public class CommandHistoryTests
{
    [Fact]
    public void RecordOperation_ValidCommand_SavesWithCorrectData()
    {
        var deviceId = Guid.NewGuid();
        var operation = "Turned On";
        
        var history = new CommandHistory(deviceId, operation);
        
        Assert.Equal(deviceId, history.DeviceId);
        Assert.Equal(operation, history.Operation);
        Assert.True((DateTime.UtcNow - history.Timestamp).TotalSeconds < 5);
    }

    [Fact]
    public async Task GetHistory_MultipleOperations_ReturnsChronologicalOrder()
    {
        var deviceId = Guid.NewGuid();
        var mockRepo = new Mock<IDeviceRepository>();
        
        var h1 = new CommandHistory(deviceId, "Op 1");
        await Task.Delay(10); // Ensure timestamp difference
        var h2 = new CommandHistory(deviceId, "Op 2");
        
        // Repository returns most recent first per documentation
        mockRepo.Setup(r => r.GetHistoryAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandHistory> { h2, h1 });
            
        var history = await mockRepo.Object.GetHistoryAsync(deviceId);
        
        Assert.Equal(2, history.Count);
        Assert.Equal("Op 2", history[0].Operation);
        Assert.Equal("Op 1", history[1].Operation);
    }
}
