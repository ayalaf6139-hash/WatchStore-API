using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.API.Extensions;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET /api/products?search=casio&categoryId=4&sortBy=price&sortDirection=desc&page=1&pageSize=10
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductListResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ProductListResponse>>> GetAll(
            [FromQuery] ProductQueryParameters query,
            CancellationToken cancellationToken)
        {
            // רק Admin רואה גם מוצרים שהוסרו מהקטלוג
            var products = await _productService.GetPagedAsync(
                query,
                User.IsAdmin(),
                cancellationToken);

            return Ok(products);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponse>> GetById(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var product = await _productService.GetByIdAsync(
                id,
                User.IsAdmin(),
                cancellationToken);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProductResponse>> Create(
            [FromBody] CreateProductRequest request,
            CancellationToken cancellationToken)
        {
            var product = await _productService.CreateAsync(request, cancellationToken);

            // 201 + Location: /api/products/{id}
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(
            [FromRoute] int id,
            [FromBody] UpdateProductRequest request,
            CancellationToken cancellationToken)
        {
            var updated = await _productService.UpdateAsync(id, request, cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var deleted = await _productService.DeleteAsync(id, cancellationToken);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
