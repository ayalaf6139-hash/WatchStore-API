using AutoMapper;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;

namespace WatchStore.Service.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(
            ICategoryRepository categoryRepository,
            IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<CategoryResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (category == null)
                return null;

            return _mapper.Map<CategoryResponse>(category);
        }

        public async Task<List<CategoryResponse>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var categories = await _categoryRepository.GetAllAsync(
                cancellationToken);

            return _mapper.Map<List<CategoryResponse>>(categories);
        }

        public async Task<CategoryResponse> CreateAsync(
            CreateCategoryRequest request,
            CancellationToken cancellationToken = default)
        {
            var category = _mapper.Map<Category>(request);

            await _categoryRepository.AddAsync(
                category,
                cancellationToken);

            await _categoryRepository.SaveChangesAsync(
                cancellationToken);

            return _mapper.Map<CategoryResponse>(category);
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateCategoryRequest request,
            CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (category == null)
                return false;

            _mapper.Map(request, category);

            _categoryRepository.Update(category);

            await _categoryRepository.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (category == null)
                return false;

            // ה-FK מוגדר Restrict, ולכן בודקים מראש ומחזירים 409 במקום 500
            if (await _categoryRepository.HasProductsAsync(id, cancellationToken))
            {
                throw new ConflictException(
                    "לא ניתן למחוק קטגוריה שיש בה מוצרים.");
            }

            _categoryRepository.Delete(category);

            await _categoryRepository.SaveChangesAsync(
                cancellationToken);

            return true;
        }
    }
}
