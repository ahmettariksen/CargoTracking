using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using CargoTracking.Application.Features.Warehouses.Commands.Queries;
using CargoTracking.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
public sealed class FakePickupCourierSender(
    IReadOnlyList<Guid> courierIds,
    NearestWarehouseResponse nearestWarehouse) : ISender
{
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public IAsyncEnumerable<object?> CreateStream(
        object request,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        object result = request switch
        {
            FindNearbyCouriersQuery => courierIds,
            SelectNearestWarehouseQuery => nearestWarehouse,
            _ => throw new NotImplementedException(
                $"Unsupported request: {request.GetType().Name}")
        };

        return Task.FromResult((TResponse)result);
    }

    public Task Send<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest
        => throw new NotImplementedException();

    public Task<object?> Send(
        object request,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
