using AutoMapper;
using Microsoft.Extensions.Logging;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;

namespace WatchStore.Service.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository repository,
            ICategoryRepository categoryRepository,
            IMapper mapper,
            ILogger<ProductService> logger)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProductResponse> CreateAsync(
            CreateProductRequest request,
            CancellationToken cancellationToken = default)
        {
            await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
            await EnsureModelNumberIsFreeAsync(request.ModelNumber, null, cancellationToken);

            var product = _mapper.Map<Product>(request);
            product.IsActive = true;
            product.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(product, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product {ProductId} ({ModelNumber}) created with stock {Stock}",
                product.Id,
                product.ModelNumber,
                product.StockQuantity);

            // טעינה מחדש כדי להחזיר גם את שם הקטגוריה
            var created = await _repository.GetByIdWithDetailsAsync(
                product.Id,
                cancellationToken);

            return _mapper.Map<ProductResponse>(created);
        }

        public async Task<PagedResult<ProductListResponse>> GetPagedAsync(
            ProductQueryParameters query,
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            var (products, totalCount) = await _repository.GetPagedAsync(
                query,
                includeInactive,
                cancellationToken);

            return new PagedResult<ProductListResponse>
            {
                Items = _mapper.Map<List<ProductListResponse>>(products),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ProductResponse?> GetByIdAsync(
            int id,
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            var product = await _repository.GetByIdWithDetailsAsync(
                id,
                cancellationToken);

            // מוצר לא פעיל "לא קיים" מבחינת חנות
            if (product == null || (!product.IsActive && !includeInactive))
                return null;

            return _mapper.Map<ProductResponse>(product);
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var product = await _repository.GetByIdAsync(id, cancellationToken);

            if (product == null)
                return false;

            await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
            await EnsureModelNumberIsFreeAsync(request.ModelNumber, id, cancellationToken);

            // מעתיק את ערכי הבקשה לתוך הישות הקיימת
            _mapper.Map(request, product);

            try
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                // הזמנה של חנות שינתה את המלאי בין הקריאה לשמירה
                _logger.LogWarning(
                    "Concurrency conflict while updating product {ProductId}",
                    id);

                throw new ConflictException(
                    "המוצר השתנה על ידי משתמש אחר בזמן העדכון. רעננו ונסו שוב.");
            }

            _logger.LogInformation("Product {ProductId} updated", id);

            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var product = await _repository.GetByIdAsync(id, cancellationToken);

            if (product == null)
                return false;

            // מחיקה רכה: מוצר שכבר הוזמן חייב להישאר בהיסטוריית ההזמנות,
            // ולכן הוא רק מוסתר מהקטלוג
            product.IsActive = false;

            try
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning(
                    "Concurrency conflict while deleting product {ProductId}",
                    id);

                throw new ConflictException(
                    "המוצר השתנה על ידי משתמש אחר. רעננו ונסו שוב.");
            }

            _logger.LogInformation("Product {ProductId} deactivated", id);

            return true;
        }

        private async Task EnsureCategoryExistsAsync(
            int categoryId,
            CancellationToken cancellationToken)
        {
            if (!await _categoryRepository.ExistsAsync(categoryId, cancellationToken))
            {
                throw new BusinessRuleException(
                    $"קטגוריה {categoryId} לא קיימת.");
            }
        }

        private async Task EnsureModelNumberIsFreeAsync(
            string modelNumber,
            int? excludeProductId,
            CancellationToken cancellationToken)
        {
            if (await _repository.ModelNumberExistsAsync(
                    modelNumber,
                    excludeProductId,
                    cancellationToken))
            {
                throw new ConflictException(
                    $"כבר קיים מוצר עם מספר הדגם {modelNumber}.");
            }
        }
    }
}
