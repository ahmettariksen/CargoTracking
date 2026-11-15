using CargoTracking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Domain.Shipments;
public sealed class OutboxMessageTests
{
    [Fact]
    public void MarkAsFailed_FirstFailure_SetsRetryCountAndNextRetryAt()
    {
        // Arrange
        var message = new OutboxMessage(
            "ShipmentCreatedEvent",
            """{"shipmentId":"test"}""");

        var before = DateTime.Now;

        // Act
        message.MarkAsFailed("Test error");

        var after = DateTime.Now;

        // Assert
        Assert.Equal(1, message.RetryCount);
        Assert.Equal("Test error", message.Error);
        Assert.NotNull(message.NextRetryAt);

        Assert.InRange(
            message.NextRetryAt.Value,
            before.AddSeconds(5),
            after.AddSeconds(5));
    }

    [Fact]
    public void MarkAsFailed_SecondFailure_IncreasesRetryDelay()
    {
        // Arrange
        var message = new OutboxMessage(
            "ShipmentCreatedEvent",
            """{"shipmentId":"test"}""");

        // Act
        message.MarkAsFailed("First error");
        var firstRetryAt = message.NextRetryAt;

        message.MarkAsFailed("Second error");
        var secondRetryAt = message.NextRetryAt;

        // Assert
        Assert.Equal(2, message.RetryCount);
        Assert.NotNull(firstRetryAt);
        Assert.NotNull(secondRetryAt);

        Assert.True(secondRetryAt > firstRetryAt);
    }

    [Fact]
    public void MarkAsProcessed_ClearsErrorAndNextRetryAt()
    {
        // Arrange
        var message = new OutboxMessage(
            "ShipmentCreatedEvent",
            """{"shipmentId":"test"}""");

        message.MarkAsFailed("Test error");

        // Act
        message.MarkAsProcessed();

        // Assert
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.Error);
        Assert.Null(message.NextRetryAt);
    }
}
