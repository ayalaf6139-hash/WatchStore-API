using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    // חנויות נוצרות דרך /api/auth/register; ה-Admin צופה בהן
    [ApiController]
    [Route("api/stores")]
    [Authorize(Roles = "Admin")]
    public class StoresController : ControllerBase
    {
        private readonly IStoreService _storeService;

        public StoresController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<StoreResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<StoreResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _storeService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreResponse>> GetById(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var store = await _storeService.GetByIdAsync(id, cancellationToken);

            if (store == null)
                return NotFound();

            return Ok(store);
        }
    }
}
