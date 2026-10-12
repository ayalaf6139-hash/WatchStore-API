using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.API.Extensions;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // Admin רואה את כל ההזמנות, חנות רק את שלה
        [HttpGet]
        [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<OrderResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var orders = await _orderService.GetAllAsync(
                RequesterStoreScope(),
                cancellationToken);

            return Ok(orders);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> GetById(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var order = await _orderService.GetByIdAsync(
                id,
                RequesterStoreScope(),
                cancellationToken);

            if (order == null)
                return NotFound();

            return Ok(order);
        }

        // הפעולה על המשאב המוגבל: מחזירה 409 כשהמלאי נתפס על ידי חנות אחרת
        [HttpPost]
        [Authorize(Roles = "Store")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<OrderResponse>> Create(
            [FromBody] CreateOrderRequest request,
            CancellationToken cancellationToken)
        {
            // החנות נלקחת מה-token, לא מגוף הבקשה
            var storeId = User.GetStoreId();

            if (storeId == null)
                return Forbid();

            var order = await _orderService.CreateAsync(
                storeId.Value,
                request,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(
            [FromRoute] int id,
            [FromBody] UpdateOrderStatusRequest request,
            CancellationToken cancellationToken)
        {
            var updated = await _orderService.UpdateStatusAsync(id, request, cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }

        // null = Admin (ללא הגבלה); אחרת - החנות של המשתמש המחובר.
        // משתמש Store בלי storeId ב-token מקבל 0, כלומר לא רואה כלום
        private int? RequesterStoreScope() =>
            User.IsAdmin() ? null : User.GetStoreId() ?? 0;
    }
}
