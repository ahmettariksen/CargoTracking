using CargoTracking.Application.Features.Shipments.CreateShipments;
using CargoTracking.Application.Interfaces;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CargoTracking.Tests.Features.Shipments.Commands.CreateShipment;
public sealed class CreateShipmentCommandValidatorTests
{
    private readonly CreateShipmentCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_ValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateShipmentCommand(
            //Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2.5m,
            DateTime.Now.AddDays(3));

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_ZeroWeight_ShouldHaveError()
    {
        // Arrange
        var command = new CreateShipmentCommand(
            //Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            DateTime.Now.AddDays(3));

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(CreateShipmentCommand.Weight));
    }

    //[Fact]
    //public async Task ValidateAsync_SameSenderAndReceiver_ShouldHaveError()
    //{
    //    // Arrange
    //    var userId = Guid.NewGuid();

    //    var command = new CreateShipmentCommand(
    //        //userId,
    //        userId,
    //        Guid.NewGuid(),
    //        Guid.NewGuid(),
    //        2.5m,
    //        DateTime.Now.AddDays(3));

    //    // Act
    //    var result = await _validator.ValidateAsync(command);

    //    // Assert
    //    Assert.Contains(result.Errors,
    //        error => error.PropertyName == nameof(CreateShipmentCommand));

    //}

    [Fact]
    public async Task Handle_SameSenderAndReceiver_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var userId = Guid.NewGuid();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = userId
        };
        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());
        var handler = new CreateShipmentCommandHandler(
            context,
            currentUserService, fakeSender);

        var command = new CreateShipmentCommand(
            ReceiverId: userId,
            SenderAddressId: Guid.NewGuid(),
            ReceiverAddressId: Guid.NewGuid(),
            Weight: 2.5m,
            EstimatedDeliveryDate: DateTime.Now.AddDays(3));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "Gönderen ve alıcı aynı kullanıcı olamaz.",
            exception.Message);

        Assert.Empty(await context.Shipments.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    [Fact]
    public async Task ValidateAsync_EmptyAddressIds_ShouldHaveErrors()
    {
        // Arrange
        var command = new CreateShipmentCommand(
        //Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.Empty,
        Guid.Empty,
        2.5m,
        DateTime.Now.AddDays(3));

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(CreateShipmentCommand.SenderAddressId));

        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(CreateShipmentCommand.ReceiverAddressId));

    }

    [Fact]
    public async Task ValidateAsync_PastEstimatedDeliveryDate_ShouldHaveError()
    {
        // Arrange
        var command = new CreateShipmentCommand(
       // Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        2.5m,
        DateTime.Now.AddDays(-1));

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(CreateShipmentCommand.EstimatedDeliveryDate));

    }
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; }
    }
}
