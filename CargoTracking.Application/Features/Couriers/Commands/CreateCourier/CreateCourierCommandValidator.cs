using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Couriers.Commands.CreateCourier;
public sealed class CreateCourierCommandValidator:AbstractValidator<CreateCourierCommand>
{
    public CreateCourierCommandValidator()
    {
        RuleFor(x => x.FirstName)
           .NotEmpty()
           .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.VehiclePlate)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.VehicleType)
            .IsInEnum();
    }
}
