namespace Order_Management_API.Common.Mapping;
using System.Globalization;
using AutoMapper;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Common.Mapping; // Ensure access to LocalizedResolvers

public class AdvancedOrderMappingProfile : Profile
{
    public AdvancedOrderMappingProfile()
    {
        // Map Create Request to Order Entity
        CreateMap<CreateOrderProfileRequest, Order>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Map Order Entity to Order DTO
        CreateMap<Order, OrderProfileDto>()
            // --- Use Localized Resolvers ---
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<LocalizedCategoryDisplayResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<LocalizedAvailabilityStatusResolver>())
            
            // --- Standard Resolvers ---
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<PublishedAgeResolver>())
            .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<AuthorInitialsResolver>())
            
            // Conditional Mapping for Price (10% discount for Children)
            .ForMember(dest => dest.Price, opt =>
            {
                opt.Condition(src => src.Category == OrderCategory.Children);
                opt.MapFrom(src => src.Price * 0.9m);
            })
            // Conditional Mapping for CoverImageUrl (null for Children)
             .ForMember(dest => dest.CoverImageUrl, opt =>
             {
                 opt.Condition(src => src.Category != OrderCategory.Children);
                 opt.MapFrom(src => src.CoverImageUrl);
             });
    }
}

// Standard Resolvers kept for logic like Price/Age/Initials

public class PriceFormatterResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        var priceToFormat = source.Category == OrderCategory.Children ? source.Price * 0.9m : source.Price;
        return priceToFormat.ToString("C2", new CultureInfo("en-US"));
    }
}

public class PublishedAgeResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        var age = DateTime.UtcNow - source.PublishedDate;
        var days = age.TotalDays;

        if (days < 30) return "New Release";
        if (days < 365) return $"{(int)(days / 30)} months old";
        if (days < 1825) return $"{(int)(days / 365)} years old";
        return "Classic";
    }
}

public class AuthorInitialsResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Author)) return "?";
        var names = source.Author.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length > 1) return $"{char.ToUpper(names[0][0])}{char.ToUpper(names[^1][0])}";
        if (names.Length == 1) return $"{char.ToUpper(names[0][0])}";
        return "?";
    }
}