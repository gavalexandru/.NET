using AutoMapper;
using Microsoft.Extensions.Localization;
using Order_Management_API.Common.Localization;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;

namespace Order_Management_API.Common.Mapping;

public class LocalizedCategoryDisplayResolver : IValueResolver<Order, OrderProfileDto, string>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedCategoryDisplayResolver(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            OrderCategory.Fiction => _localizer["Cat_Fiction"],
            OrderCategory.NonFiction => _localizer["Cat_NonFiction"],
            OrderCategory.Technical => _localizer["Cat_Technical"],
            OrderCategory.Children => _localizer["Cat_Children"],
            _ => "Uncategorized"
        };
    }
}

public class LocalizedAvailabilityStatusResolver : IValueResolver<Order, OrderProfileDto, string>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedAvailabilityStatusResolver(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable) return _localizer["Status_OutOfStock"];
        if (source.StockQuantity == 0) return _localizer["Status_OutOfStock"];
        if (source.StockQuantity == 1) return _localizer["Status_LastCopy"];
        if (source.StockQuantity <= 5) return _localizer["Status_Limited"];
        return _localizer["Status_InStock"];
    }
}