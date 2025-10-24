using Order_Management_API.Features.Orders;

namespace Order_Management_API.Persistence;
using Microsoft.EntityFrameworkCore;
public class OrderManagementContext(DbContextOptions<OrderManagementContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
}