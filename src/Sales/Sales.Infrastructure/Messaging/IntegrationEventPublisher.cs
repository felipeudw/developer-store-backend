using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Sales.Application.Abstractions;
using Sales.Application.IntegrationEvents;

namespace Sales.Infrastructure.Messaging
{
    public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly IBus _bus;

        public IntegrationEventPublisher(IBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public Task PublishAsync(SaleCreatedEvent evt, CancellationToken ct = default)
            => _bus.Publish(evt);

        public Task PublishAsync(SaleModifiedEvent evt, CancellationToken ct = default)
            => _bus.Publish(evt);

        public Task PublishAsync(SaleCancelledEvent evt, CancellationToken ct = default)
            => _bus.Publish(evt);

        public Task PublishAsync(ItemCancelledEvent evt, CancellationToken ct = default)
            => _bus.Publish(evt);
    }
}