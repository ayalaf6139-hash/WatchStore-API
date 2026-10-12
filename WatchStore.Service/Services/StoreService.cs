using AutoMapper;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;

namespace WatchStore.Service.Services
{
    public class StoreService : IStoreService
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IMapper _mapper;

        public StoreService(
            IStoreRepository storeRepository,
            IMapper mapper)
        {
            _storeRepository = storeRepository;
            _mapper = mapper;
        }

        public async Task<StoreResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var store = await _storeRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (store == null)
                return null;

            return _mapper.Map<StoreResponse>(store);
        }

        public async Task<List<StoreResponse>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var stores = await _storeRepository.GetAllAsync(
                cancellationToken);

            return _mapper.Map<List<StoreResponse>>(stores);
        }
    }
}
