using SecureApiProject.Models;

namespace SecureApiProject.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Users.Any()) return;

        // Seed admin user — password hashed with BCrypt (Activity 1: Secure Code)
        context.Users.AddRange(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@secureapi.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "johndoe",
                Email = "john@secureapi.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123456"),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            }
        );

        context.Products.AddRange(
            new Product { Id = 1, Name = "Laptop Pro", Description = "High-performance laptop", Price = 1299.99m, Stock = 50, CreatedBy = "admin" },
            new Product { Id = 2, Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 29.99m, Stock = 200, CreatedBy = "admin" },
            new Product { Id = 3, Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard", Price = 89.99m, Stock = 100, CreatedBy = "admin" }
        );

        context.SaveChanges();
    }
}
