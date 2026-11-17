using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize(Roles = "Admin")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet("lookup")]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerResponse>> Lookup(
            [FromQuery] string phone,
            CancellationToken cancellationToken)
        {
            var customer = await _customerService.FindByPhoneAsync(phone, cancellationToken);

            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerResponse>> GetById(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetByIdAsync(id, cancellationToken);

            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        [HttpGet("{id:int}/appointments")]
        [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<AppointmentResponse>>> GetAppointments(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var appointments = await _customerService.GetAppointmentsAsync(id, cancellationToken);

            return Ok(appointments);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CustomerResponse>> Create(
            [FromBody] CreateCustomerRequest request,
            CancellationToken cancellationToken)
        {
            var customer = await _customerService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            [FromRoute] int id,
            [FromBody] UpdateCustomerRequest request,
            CancellationToken cancellationToken)
        {
            var updated = await _customerService.UpdateAsync(id, request, cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }
    }
}
