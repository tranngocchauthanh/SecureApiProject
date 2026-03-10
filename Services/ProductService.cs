using Microsoft.EntityFrameworkCore;
using SecureApiProject.Data;
using SecureApiProject.DTOs;
using SecureApiProject.Models;

namespace SecureApiProject.Services;

public interface IProductService
{
    Task<List<ProductResponse>> GetAllAsync();
    Task<ProductResponse?> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, string createdBy);
    Task<(bool Success, string Message, ProductResponse? Product)> UpdateAsync(int id, UpdateProductRequest request, string updatedBy);
    Task<(bool Success, string Message)> DeleteAsync(int id, string deletedBy);
}

/// <summary>
/// Activity 1: Secure data access with parameterized queries (via EF Core).
/// No raw SQL to prevent SQL injection.
/// </summary>
public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ProductResponse>> GetAllAsync()
    {
        return await _db.Products
            .Where(p => !p.IsDeleted)
            .Select(p => MapToResponse(p))
            .ToListAsync();
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        return product == null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, string createdBy)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Product '{Name}' created by {User}", product.Name, createdBy);
        return MapToResponse(product);
    }

    public async Task<(bool Success, string Message, ProductResponse? Product)> UpdateAsync(
        int id, UpdateProductRequest request, string updatedBy)
    {
        var product = await _db.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            return (false, "Product not found.", null);

        if (request.Name != null) product.Name = request.Name.Trim();
        if (request.Description != null) product.Description = request.Description.Trim();
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Stock.HasValue) product.Stock = request.Stock.Value;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Product {Id} updated by {User}", id, updatedBy);
        return (true, "Product updated.", MapToResponse(product));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, string deletedBy)
    {
        var product = await _db.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            return (false, "Product not found.");

        // Soft delete — preserve data for audit trail (Activity 3)
        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Product {Id} soft-deleted by {User}", id, deletedBy);
        return (true, "Product deleted.");
    }

    private static ProductResponse MapToResponse(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Stock = p.Stock,
        CreatedBy = p.CreatedBy,
        CreatedAt = p.CreatedAt
    };
}
