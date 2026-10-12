using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CategoryResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _categoryService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryResponse>> GetById(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var category = await _categoryService.GetByIdAsync(id, cancellationToken);

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryResponse>> Create(
            [FromBody] CreateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            var category = await _categoryService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            [FromRoute] int id,
            [FromBody] UpdateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            var updated = await _categoryService.UpdateAsync(id, request, cancellationToken);

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
            var deleted = await _categoryService.DeleteAsync(id, cancellationToken);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
