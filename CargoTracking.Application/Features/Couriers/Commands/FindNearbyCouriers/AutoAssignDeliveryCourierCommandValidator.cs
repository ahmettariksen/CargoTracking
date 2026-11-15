using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
public sealed class AutoAssignDeliveryCourierCommandValidator
    : AbstractValidator<AutoAssignDeliveryCourierCommand>
{
    public AutoAssignDeliveryCourierCommandValidator()
    {
        RuleFor(x => x.ShipmentId)
            .NotEmpty()
            .WithMessage("Shipment ID cannot be empty.");
    }
}

