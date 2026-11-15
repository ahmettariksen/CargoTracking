using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
internal sealed class CreateShipmentFakeSender : ISender
{
    private readonly Guid _courierId;

    public CreateShipmentFakeSender(Guid courierId)
    {
        _courierId = courierId;
    }

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        if (request is AutoAssignPickupCourierCommand)
        {
            object result = _courierId;
            return Task.FromResult((TResponse)result);
        }

        throw new NotSupportedException(
            $"Desteklenmeyen istek: {request.GetType().Name}");
    }

    public Task Send<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        throw new NotSupportedException();
    }

    public Task<object?> Send(
        object request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<object?> CreateStream(
        object request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
