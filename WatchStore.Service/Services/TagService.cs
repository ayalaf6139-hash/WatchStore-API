using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;

namespace WatchStore.Service.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<TagResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var tag = await _tagRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (tag == null)
                return null;

            return new TagResponse
            {
                Id = tag.Id,
                Name = tag.Name
            };
        }

        public async Task<List<TagResponse>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var tags = await _tagRepository.GetAllAsync(
                cancellationToken);

            return tags.Select(tag => new TagResponse
            {
                Id = tag.Id,
                Name = tag.Name
            }).ToList();
        }

        public async Task<TagResponse> CreateAsync(
            CreateTagRequest request,
            CancellationToken cancellationToken = default)
        {
            var tag = new Tag
            {
                Name = request.Name
            };

            await _tagRepository.AddAsync(
                tag,
                cancellationToken);

            await _tagRepository.SaveChangesAsync(
                cancellationToken);

            return new TagResponse
            {
                Id = tag.Id,
                Name = tag.Name
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateTagRequest request,
            CancellationToken cancellationToken = default)
        {
            var tag = await _tagRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (tag == null)
                return false;

            tag.Name = request.Name;

            _tagRepository.Update(tag);

            await _tagRepository.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var tag = await _tagRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (tag == null)
                return false;

            _tagRepository.Delete(tag);

            await _tagRepository.SaveChangesAsync(
                cancellationToken);

            return true;
        }
    }
}
