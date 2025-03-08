using DokonUz.Data;
using DokonUz.DTOs;
using DokonUz.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Serilog;
namespace DokonUz.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly DokonUzDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(DokonUzDbContext context, IMapper mapper, ILogger<ProductsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger; // Loggerni inject qilish
    }

    // GET: api/Products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts()
    {
        var products = await _context.Products
            .Include(p => p.Category) // Related ma'lumotni ham olish
            .ToListAsync();

        _logger.LogInformation("Fetched {ProductCount} products from the database.", products.Count);
        

        return Ok(_mapper.Map<IEnumerable<ProductDTO>>(products));
    }

    // GET: api/Products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDTO>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category) // Related ma'lumotni ham olish
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found.", id);
            return NotFound();
        }

        _logger.LogInformation("Fetched product with ID {ProductId}.", id);

        return Ok(_mapper.Map<ProductDTO>(product));
    }

    // POST: api/Products
    [HttpPost]
    public async Task<ActionResult<ProductDTO>> PostProduct([FromBody] ProductCreateDTO productDto)
    {
        var product = _mapper.Map<Product>(productDto);

        var category = await _context.Categories.FindAsync(productDto.CategoryId);
        if (category == null)
        {
            _logger.LogWarning("Category with ID {CategoryId} not found during product creation.", productDto.CategoryId);
            return BadRequest("The specified category does not exist.");
        }

        product.Category = category;

        _context.Products.Add(product);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Product created with ID {ProductId}.", product.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error occurred while saving product with name {ProductName}.", product.Name);
            return StatusCode(500, "An error occurred while saving the product.");
        }

        var savedProduct = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        return CreatedAtAction(nameof(GetProduct), new { id = savedProduct!.Id }, _mapper.Map<ProductDTO>(savedProduct));
    }
}

