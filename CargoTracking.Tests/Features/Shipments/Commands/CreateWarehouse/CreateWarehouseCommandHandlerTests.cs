using CargoTracking.Application.Features.Warehouses.Commands.CreateWarehouse;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Shipments.Commands.CreateWarehouse;
public sealed class CreateWarehouseCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateWarehouse()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var command = new CreateWarehouseCommand(
            "Düzce Merkez Depo",
            "Düzce",
            "Merkez",
            "Örnek depo adresi",
            40.8438,
            31.1565);

        var handler = new CreateWarehouseCommandHandler(context);

        // Act
        var warehouseId = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var warehouse = await context.Warehouses
            .SingleOrDefaultAsync(x => x.Id == warehouseId);

        Assert.NotNull(warehouse);
        Assert.Equal("Düzce Merkez Depo", warehouse.Name);
        Assert.Equal("Düzce", warehouse.City);
        Assert.Equal("Merkez", warehouse.District);
        Assert.Equal("Örnek depo adresi", warehouse.Address);
        Assert.Equal(40.8438, warehouse.Latitude);
        Assert.Equal(31.1565, warehouse.Longitude);
        Assert.True(warehouse.IsActive);
    }
}
