using AutoMapper; 
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Order_Management_API.Features.Orders.DTOs; 
using Order_Management_API.Persistence;

namespace Order_Management_API.Features.Orders;

public class GetOrdersByCategoryHandler
{
    private readonly OrderManagementContext _context;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper; 

    public GetOrdersByCategoryHandler(OrderManagementContext context, IMemoryCache cache, IMapper mapper)
    {
        _context = context;
        _cache = cache;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(OrderCategory category)
    {
        string cacheKey = $"orders_category_{category}";

        if (!_cache.TryGetValue(cacheKey, out List<OrderProfileDto>? orderDtos)) 
        {
            var orders = await _context.Orders
                .Where(o => o.Category == category)
                .ToListAsync();

            
            orderDtos = _mapper.Map<List<OrderProfileDto>>(orders);

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(cacheKey, orderDtos, cacheEntryOptions);
        }

        return Results.Ok(orderDtos);
    }
}