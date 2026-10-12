using AutoMapper;
using Microsoft.Extensions.Logging;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;

namespace WatchStore.Service.Services
{
    public class OrderService : IOrderService
    {
        public const string StockConflictMessage =
            "המלאי של המוצר כבר נתפס על ידי משתמש אחר. אנא נסה שוב.";

        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IStoreRepository storeRepository,
            IMapper mapper,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _storeRepository = storeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OrderResponse?> GetByIdAsync(
            int id,
            int? requesterStoreId,
            CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByIdWithItemsAsync(
                id,
                cancellationToken);

            if (order == null)
                return null;

            // חנות רואה רק את ההזמנות שלה
            if (requesterStoreId.HasValue && order.StoreId != requesterStoreId)
            {
                throw new ForbiddenException(
                    "אין לך הרשאה לצפות בהזמנה זו.");
            }

            return _mapper.Map<OrderResponse>(order);
        }

        public async Task<List<OrderResponse>> GetAllAsync(
            int? requesterStoreId,
            CancellationToken cancellationToken = default)
        {
            // שאילתה אחת עם Include לכל ההזמנות, הפריטים והמוצרים (ללא N+1)
            var orders = await _orderRepository.GetAllWithItemsAsync(
                requesterStoreId,
                cancellationToken);

            return _mapper.Map<List<OrderResponse>>(orders);
        }

        public async Task<OrderResponse> CreateAsync(
            int storeId,
            CreateOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);

            if (store == null || !store.IsActive)
            {
                _logger.LogWarning(
                    "Order rejected: store {StoreId} is missing or inactive",
                    storeId);

                throw new ForbiddenException("החנות אינה פעילה ואינה רשאית להזמין.");
            }

            // אותו מוצר שמופיע פעמיים בבקשה מאוחד לשורה אחת
            var requestedItems = request.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();

            // שאילתה אחת לכל המוצרים. הישויות במעקב, וה-RowVersion שנקרא כאן
            // הוא הערך ש-EF ישים בתנאי ה-WHERE של ה-UPDATE
            var products = (await _productRepository.GetByIdsAsync(
                    requestedItems.Select(i => i.ProductId),
                    cancellationToken))
                .ToDictionary(p => p.Id);

            var order = new Order
            {
                StoreId = storeId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            foreach (var item in requestedItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product) ||
                    !product.IsActive)
                {
                    throw new NotFoundException(
                        $"מוצר {item.ProductId} לא נמצא.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Order rejected: insufficient stock for product {ProductId}. " +
                        "Available {Available}, requested {Requested}",
                        product.Id,
                        product.StockQuantity,
                        item.Quantity);

                    throw new ConflictException(
                        $"אין מספיק מלאי עבור {product.Name}. " +
                        $"זמין: {product.StockQuantity}, התבקש: {item.Quantity}.");
                }

                product.StockQuantity -= item.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.WholesalePrice
                });

                order.TotalPrice += item.Quantity * product.WholesalePrice;
            }

            await _orderRepository.AddAsync(order, cancellationToken);

            // ההזמנה, הפריטים והפחתת המלאי נשמרים ב-SaveChanges אחד,
            // ש-EF Core מריץ בתוך טרנזקציה אחת: או שהכול נשמר או ששום דבר.
            // ה-UPDATE של כל מוצר כולל "WHERE RowVersion = @original" -
            // אם חנות אחרת הספיקה לשנות את המוצר, אף שורה לא תתעדכן,
            // הטרנזקציה תתבטל וההזמנה לא תיווצר
            try
            {
                await _orderRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning(
                    "Stock conflict: store {StoreId} lost the race for products {ProductIds}",
                    storeId,
                    string.Join(",", products.Keys));

                throw new ConflictException(StockConflictMessage);
            }

            _logger.LogInformation(
                "Order {OrderId} created by store {StoreId}, total {Total}",
                order.Id,
                storeId,
                order.TotalPrice);

            var created = await _orderRepository.GetByIdWithItemsAsync(
                order.Id,
                cancellationToken);

            return _mapper.Map<OrderResponse>(created);
        }

        public async Task<bool> UpdateStatusAsync(
            int id,
            UpdateOrderStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByIdAsync(id, cancellationToken);

            if (order == null)
                return false;

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Completed)
            {
                throw new BusinessRuleException(
                    $"לא ניתן לשנות הזמנה בסטטוס {order.Status}.");
            }

            order.Status = request.Status;

            await _orderRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} status changed to {Status}",
                id,
                request.Status);

            return true;
        }
    }
}
