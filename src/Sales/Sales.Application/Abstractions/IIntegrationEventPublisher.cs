using System;
using System.Threading;
using System.Threading.Tasks;
using Sales.Application.IntegrationEvents;

namespace Sales.Application.Abstractions
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(SaleCreatedEvent evt, CancellationToken ct = default);
        Task PublishAsync(SaleModifiedEvent evt, CancellationToken ct = default);
        Task PublishAsync(SaleCancelledEvent evt, CancellationToken ct = default);
        Task PublishAsync(ItemCancelledEvent evt, CancellationToken ct = default);
    }
}