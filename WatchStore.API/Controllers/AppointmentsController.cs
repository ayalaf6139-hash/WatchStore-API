using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize(Roles = "Admin")]
    public class AppointmentsController : ControllerBase
    {
        private readonly ISchedulingService _schedulingService;
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(
            ISchedulingService schedulingService,
            IAppointmentService appointmentService)
        {
            _schedulingService = schedulingService;
            _appointmentService = appointmentService;
        }

        [HttpGet("availability")]
        [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AvailabilityResponse>> GetAvailability(
            [FromQuery] AvailabilityQuery query,
            CancellationToken cancellationToken)
        {
            var availability = await _schedulingService.GetAvailabilityAsync(query, cancellationToken);

            return Ok(availability);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AppointmentResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<AppointmentResponse>>> GetAll(
            [FromQuery] AppointmentQueryParameters query,
            CancellationToken cancellationToken)
        {
            var appointments = await _appointmentService.GetPagedAsync(query, cancellationToken);

            return Ok(appointments);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentResponse>> GetById(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);

            if (appointment == null)
                return NotFound();

            return Ok(appointment);
        }

        [HttpPost]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentResponse>> Book(
            [FromBody] BookAppointmentRequest request,
            CancellationToken cancellationToken)
        {
            var appointment = await _schedulingService.BookAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(
            [FromRoute] int id,
            [FromBody] CancelAppointmentRequest request,
            CancellationToken cancellationToken)
        {
            var cancelled = await _appointmentService.CancelAsync(id, request, cancellationToken);

            if (!cancelled)
                return NotFound();

            return NoContent();
        }
    }
}
