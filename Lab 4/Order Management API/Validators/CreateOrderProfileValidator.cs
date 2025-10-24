using Microsoft.Extensions.Logging;

namespace Order_Management_API.Validators;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Persistence;

public class CreateOrderProfileValidator : AbstractValidator<CreateOrderProfileRequest>
{
    private readonly OrderManagementContext _context;
    private readonly ILogger<CreateOrderProfileValidator> _logger;

    public CreateOrderProfileValidator(OrderManagementContext context, ILogger<CreateOrderProfileValidator> logger)
    {
        _context = context;
        _logger = logger;
        
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title must not be empty.")
            .Length(1, 200)
            .MustAsync(BeValidTitle).WithMessage("Title contains inappropriate content.")
            .MustAsync(BeUniqueTitle).WithMessage("This title already exists for this author.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author must not be empty.")
            .Length(2, 100)
            .Must(BeValidAuthorName).WithMessage("Author name contains invalid characters.");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN must not be empty.")
            .Must(BeValidISBN).WithMessage("ISBN format is invalid. It must be 10 or 13 digits, optionally with hyphens.")
            .MustAsync(BeUniqueISBN).WithMessage("An order with this ISBN already exists.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A valid category is required.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThan(10000);

        RuleFor(x => x.PublishedDate)
            .LessThan(DateTime.UtcNow).WithMessage("Published date cannot be in the future.")
            .GreaterThan(new DateTime(1400, 1, 1)).WithMessage("Published date cannot be before the year 1400.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100000).WithMessage("Stock quantity is unreasonably high.");
            
        RuleFor(x => x.CoverImageUrl)
            .Must(BeValidImageUrl).When(x => !string.IsNullOrEmpty(x.CoverImageUrl))
            .WithMessage("CoverImageUrl must be a valid HTTP/HTTPS URL pointing to a common image format.");

        RuleFor(x => x)
            .MustAsync(PassBusinessRules).WithMessage("The order violates one or more business rules.");
            
        // Conditional Validation
        When(x => x.Category == Features.Orders.OrderCategory.Technical, () =>
        {
            RuleFor(x => x.Price).GreaterThanOrEqualTo(20).WithMessage("Technical orders must have a minimum price of $20.00.");
            RuleFor(x => x.PublishedDate).Must(d => d > DateTime.UtcNow.AddYears(-5)).WithMessage("Technical orders must be published within the last 5 years.");
        });
        
        When(x => x.Category == Features.Orders.OrderCategory.Children, () =>
        {
            RuleFor(x => x.Price).LessThanOrEqualTo(50).WithMessage("Children's orders cannot exceed $50.00.");
            RuleFor(x => x.Title).Must(t => !t.Contains("inappropriate", StringComparison.OrdinalIgnoreCase)).WithMessage("Title is not appropriate for children.");
        });
        
        // Cross-field Validation
        When(x => x.Price > 100, () => {
            RuleFor(x => x.StockQuantity).LessThanOrEqualTo(20).WithMessage("Expensive orders (>$100) must have a stock of 20 or less.");
        });
    }

    private Task<bool> BeValidTitle(string title, CancellationToken token)
    {
        // Dummy list of inappropriate words
        var inappropriateWords = new List<string> { "badword", "profanity" };
        return Task.FromResult(!inappropriateWords.Any(w => title.Contains(w, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<bool> BeUniqueTitle(CreateOrderProfileRequest request, string title, CancellationToken token)
    {
        using (_logger.BeginScope("Validation:BeUniqueTitle for {Title} by {Author}", title, request.Author))
        {
            return !await _context.Orders.AnyAsync(o => o.Title == title && o.Author == request.Author, token);
        }
    }
    
    private bool BeValidAuthorName(string author)
    {
        return Regex.IsMatch(author, @"^[\p{L}\s\-\'\.]+$");
    }

    private bool BeValidISBN(string isbn)
    {
        var sanitized = isbn.Replace("-", "").Replace(" ", "");
        return (sanitized.Length == 10 || sanitized.Length == 13) && sanitized.All(char.IsDigit);
    }

    private async Task<bool> BeUniqueISBN(string isbn, CancellationToken token)
    {
        using (_logger.BeginScope("Validation:BeUniqueISBN for {ISBN}", isbn))
        {
             return !await _context.Orders.AnyAsync(o => o.ISBN == isbn, token);
        }
    }

    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
               && (url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".png") || url.EndsWith(".gif") || url.EndsWith(".webp"));
    }

    private async Task<bool> PassBusinessRules(CreateOrderProfileRequest request, CancellationToken token)
    {
        // Rule 1: Daily order addition limit
        var today = DateTime.UtcNow.Date;
        var ordersToday = await _context.Orders.CountAsync(o => o.CreatedAt.Date == today, token);
        if (ordersToday >= 500) return false;

        // Rule 4: High-value order stock limit
        if (request.Price > 500 && request.StockQuantity > 10) return false;

        return true;
    }
}