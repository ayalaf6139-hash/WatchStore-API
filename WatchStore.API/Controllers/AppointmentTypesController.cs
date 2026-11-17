using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/appointment-types")]
    [Authorize(Roles = "Admin")]
    public class AppointmentTypesController : ControllerBase
    {
        private readonly ISchedulingService _schedulingService;

        public AppointmentTypesController(ISchedulingService schedulingService)
        {
            _schedulingService = schedulingService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AppointmentTypeResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AppointmentTypeResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var types = await _schedulingService.GetAppointmentTypesAsync(cancellationToken);

            return Ok(types);
        }
    }
}
