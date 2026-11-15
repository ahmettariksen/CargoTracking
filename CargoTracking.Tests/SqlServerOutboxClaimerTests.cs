using CargoTracking.Domain.Entities;
using CargoTracking.Infrastructure.BackgroundJobs;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests;
public sealed class SqlServerOutboxClaimerTests
{
    [Fact]
    public async Task ClaimMessagesAsync_WhenAnotherWorkerAlreadyClaimedMessage_ShouldNotReturnSameMessage()
    {
        // Arrange
        await SqlServerTestDatabaseInitializer.InitializeAsync();

        var message = new OutboxMessage(
            "TestOutboxEvent",
            """{"message":"test"}""");

        await using (var setupContext = SqlServerTestDbContextFactory.Create())
        {
            setupContext.OutboxMessages.Add(message);
            await setupContext.SaveChangesAsync();
        }

        await using var firstContext = SqlServerTestDbContextFactory.Create();
        await using var secondContext = SqlServerTestDbContextFactory.Create();

        var firstClaimer = new SqlServerOutboxMessageClaimer(firstContext);
        var secondClaimer = new SqlServerOutboxMessageClaimer(secondContext);

        var firstLockId = Guid.NewGuid();
        var secondLockId = Guid.NewGuid();

        // Act
        var firstMessages = await firstClaimer.ClaimMessagesAsync(
            1000,
            5,
            firstLockId,
            CancellationToken.None);

        var secondMessages = await secondClaimer.ClaimMessagesAsync(
            1000,
            5,
            secondLockId,
            CancellationToken.None);

        // Assert
        Assert.Contains(firstMessages, x => x.Id == message.Id);

        Assert.DoesNotContain(secondMessages, x => x.Id == message.Id);

        var processedByWrongWorker = await secondClaimer.MarkAsProcessedAsync(
            message.Id,
            secondLockId,
            CancellationToken.None);

        Assert.False(processedByWrongWorker);

        var processedByOwner = await firstClaimer.MarkAsProcessedAsync(
            message.Id,
            firstLockId,
            CancellationToken.None);

        Assert.True(processedByOwner);

        await using var verificationContext = SqlServerTestDbContextFactory.Create();

        var savedMessage = await verificationContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync(x => x.Id == message.Id);

        Assert.NotNull(savedMessage.ProcessedAt);
        Assert.Null(savedMessage.LockId);
        Assert.Null(savedMessage.LockedUntil);
    }
}
