using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureApiProject.DTOs;
using SecureApiProject.Services;
using System.Security.Claims;

namespace SecureApiProject.Controllers;

/// <summary>
/// Activity 2: Role-based authorization demo.
/// - GET endpoints: Any authenticated user
/// - POST/PUT/DELETE: Admin only
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>Get all products. Requires authentication.</summary>
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductResponse>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllAsync();
        return Ok(ApiResponse<List<ProductResponse>>.Ok(products));
    }

    /// <summary>Get product by ID. Requires authentication.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        // Activity 3: Validate route param
        if (id <= 0)
            return BadRequest(ApiResponse<object>.Fail("Invalid product ID."));

        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound(ApiResponse<object>.Fail($"Product {id} not found."));

        return Ok(ApiResponse<ProductResponse>.Ok(product));
    }

    /// <summary>Create a new product. Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var createdBy = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var product = await _productService.CreateAsync(request, createdBy);

        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            ApiResponse<ProductResponse>.Ok(product, "Product created successfully."));
    }

    /// <summary>Update a product. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        if (id <= 0)
            return BadRequest(ApiResponse<object>.Fail("Invalid product ID."));

        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed."));

        var updatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var (success, message, product) = await _productService.UpdateAsync(id, request, updatedBy);

        if (!success)
            return NotFound(ApiResponse<object>.Fail(message));

        return Ok(ApiResponse<ProductResponse>.Ok(product!, message));
    }

    /// <summary>Delete a product (soft delete). Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
            return BadRequest(ApiResponse<object>.Fail("Invalid product ID."));

        var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var (success, message) = await _productService.DeleteAsync(id, deletedBy);

        if (!success)
            return NotFound(ApiResponse<object>.Fail(message));

        return NoContent();
    }
}
