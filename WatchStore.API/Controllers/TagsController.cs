using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/tags")]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<ActionResult<List<TagResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _tagService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TagResponse>> GetById(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var tag = await _tagService.GetByIdAsync(id, cancellationToken);

            if (tag == null)
                return NotFound();

            return Ok(tag);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TagResponse>> Create(
            [FromBody] CreateTagRequest request,
            CancellationToken cancellationToken)
        {
            var tag = await _tagService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = tag.Id }, tag);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            [FromRoute] int id,
            [FromBody] UpdateTagRequest request,
            CancellationToken cancellationToken)
        {
            var updated = await _tagService.UpdateAsync(id, request, cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var deleted = await _tagService.DeleteAsync(id, cancellationToken);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
