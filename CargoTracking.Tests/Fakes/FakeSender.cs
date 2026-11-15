using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
public sealed class FakeSender(
    IReadOnlyList<Guid> courierIds) : ISender
{
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
         IStreamRequest<TResponse> request,
         CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<object?> CreateStream(
        object request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        // Yakın kurye sorgusu gönderildiğinde testte belirlediğimiz
        // kurye ID'lerini sonuç olarak döndürüyoruz.
        if (request is FindNearbyCouriersQuery)
        {
            object result = courierIds;
            return Task.FromResult((TResponse)result);
        }

        throw new NotImplementedException(
            $"Unsupported request: {request.GetType().Name}");
    }

    public Task Send<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        throw new NotImplementedException();
    }

    public Task<object?> Send(
        object request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
