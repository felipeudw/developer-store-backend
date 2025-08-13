using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Sales.Api.Common;
using Sales.Api.Features.Sales;
using Sales.Domain.Entities;
using Sales.Domain.Repositories;
using Sales.Application.Abstractions;
using Sales.Application.IntegrationEvents;

namespace Sales.Api.Features.Sales
{
    [ApiController]
    [Route("sales")]
    [Authorize]
    public sealed class SalesController : ControllerBase
    {
        private readonly ISaleRepository _repo;
        private readonly ILogger<SalesController> _logger;
        private readonly IIntegrationEventPublisher _publisher;
        private readonly IMapper _mapper;

        public SalesController(ISaleRepository repo, ILogger<SalesController> logger, IIntegrationEventPublisher publisher, IMapper mapper)
        {
            _repo = repo;
            _logger = logger;
            _publisher = publisher;
            _mapper = mapper;
        }

        // GET /sales?page=1&pageSize=20
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<SaleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var items = await _repo.ListAsync(page, pageSize, ct);
            var result = items.ToPaginatedResponse<Sale, SaleResponse>(page, pageSize, _mapper);
            return Ok(result);
        }

        // GET /sales/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct = default)
        {
            var entity = await _repo.GetByIdAsync(id, ct);
            if (entity is null) return NotFound();
            return Ok(_mapper.Map<SaleResponse>(entity));
        }

        // POST /sales
        [HttpPost]
        [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateSaleRequest request, CancellationToken ct = default)
        {

            var items = request.Items.Select(i => new Sale.NewItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToArray();

            var sale = Sale.Create(
                saleNumber: request.SaleNumber,
                saleDate: request.SaleDate ?? DateTime.UtcNow,
                customerId: request.CustomerId,
                customerName: request.CustomerName,
                branchId: request.BranchId,
                branchName: request.BranchName,
                items: items
            );

            await _repo.AddAsync(sale, ct);

            await _publisher.PublishAsync(new SaleCreatedEvent(
                sale.Id,
                sale.SaleNumber,
                sale.SaleDate,
                sale.CustomerId,
                sale.CustomerName,
                sale.BranchId,
                sale.BranchName,
                sale.TotalAmount
            ), ct);

            _logger.LogInformation("SaleCreated: {SaleId} Number={SaleNumber} Total={Total}", sale.Id, sale.SaleNumber, sale.TotalAmount);

            var response = _mapper.Map<SaleResponse>(sale);
            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, response);
        }

        // PUT /sales/{id}
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSaleRequest request, CancellationToken ct = default)
        {
            var current = await _repo.GetByIdAsync(id, ct);
            if (current is null) return NotFound();


            var items = request.Items.Select(i => new Sale.NewItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToArray();

            current.Update(
                saleNumber: request.SaleNumber ?? current.SaleNumber,
                saleDate: request.SaleDate ?? current.SaleDate,
                customerId: request.CustomerId ?? current.CustomerId,
                customerName: request.CustomerName ?? current.CustomerName,
                branchId: request.BranchId ?? current.BranchId,
                branchName: request.BranchName ?? current.BranchName,
                items: items
            );

            await _repo.UpdateAsync(current, ct);

            await _publisher.PublishAsync(new SaleModifiedEvent(
                current.Id,
                current.SaleNumber,
                current.SaleDate,
                current.CustomerId,
                current.CustomerName,
                current.BranchId,
                current.BranchName,
                current.TotalAmount
            ), ct);

            _logger.LogInformation("SaleModified: {SaleId} Number={SaleNumber} Total={Total}", current.Id, current.SaleNumber, current.TotalAmount);

            return Ok(_mapper.Map<SaleResponse>(current));
        }

        // DELETE /sales/{id}
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct = default)
        {
            await _repo.DeleteAsync(id, ct);
            _logger.LogInformation("SaleDeleted: {SaleId}", id);
            return NoContent();
        }

        // POST /sales/{id}/cancel
        [HttpPost("{id:guid}/cancel")]
        [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelSale([FromRoute] Guid id, CancellationToken ct = default)
        {
            var current = await _repo.GetByIdAsync(id, ct);
            if (current is null) return NotFound();

            current.CancelSale();
            await _repo.UpdateAsync(current, ct);

            await _publisher.PublishAsync(new SaleCancelledEvent(
                current.Id,
                DateTime.UtcNow,
                current.SaleNumber,
                current.CustomerId,
                current.CustomerName,
                current.BranchId,
                current.BranchName,
                current.TotalAmount
            ), ct);

            _logger.LogInformation("SaleCancelled: {SaleId}", id);

            return Ok(_mapper.Map<SaleResponse>(current));
        }

        // POST /sales/{id}/items/{itemId}/cancel
        [HttpPost("{id:guid}/items/{itemId:guid}/cancel")]
        [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelItem([FromRoute] Guid id, [FromRoute] Guid itemId, CancellationToken ct = default)
        {
            var current = await _repo.GetByIdAsync(id, ct);
            if (current is null) return NotFound();

            current.CancelItem(itemId);
            await _repo.UpdateAsync(current, ct);

            var item = current.Items.First(i => i.Id == itemId);

            await _publisher.PublishAsync(new ItemCancelledEvent(
                current.Id,
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.DiscountPercent
            ), ct);

            _logger.LogInformation("ItemCancelled: {SaleId} ItemId={ItemId}", id, itemId);

            return Ok(_mapper.Map<SaleResponse>(current));
        }


    }
}