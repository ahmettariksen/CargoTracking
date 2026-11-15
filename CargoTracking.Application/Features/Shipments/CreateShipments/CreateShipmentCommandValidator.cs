using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.CreateShipments;
public sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.SenderAddressId)
            .NotEmpty()
            .WithMessage("Sender address is required.");

        RuleFor(x => x.ReceiverAddressId)
            .NotEmpty()
            .WithMessage("Receiver address is required.");

        //RuleFor(x => x.SenderId)
        //   .NotEqual(x => x.ReceiverId)
        //   .WithMessage("Sender and receiver cannot be the same user.");

        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .WithMessage("Shipment weight must be greater than zero.");

        RuleFor(x => x.EstimatedDeliveryDate)
            .Must(date => !date.HasValue || date.Value >= DateTime.Now)
            .WithMessage("Estimated delivery date cannot be in the past.");
    }
}
