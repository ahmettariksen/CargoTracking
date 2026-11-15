using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.Commands.ReceiveShipmentAtWarehouse;
public sealed class ReceiveShipmentAtWarehouseCommandValidator : AbstractValidator<ReceiveShipmentAtWarehouseCommand>
{
    public ReceiveShipmentAtWarehouseCommandValidator()
    {
        RuleFor(x => x.ShipmentId)
           .NotEmpty()
           .WithMessage("Shipment ID cannot be empty.");
    }
}
