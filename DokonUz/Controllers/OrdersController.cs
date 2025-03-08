using DokonUz.Data;
using DokonUz.DTOs;
using DokonUz.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Add for logging
using AutoMapper;  // Mapping uchun kerak

namespace DokonUz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly DokonUzDbContext _context;
        private readonly IMapper _mapper;  // AutoMapper dependency
        private readonly ILogger<OrdersController> _logger; // Declare ILogger

        public OrdersController(DokonUzDbContext context, IMapper mapper, ILogger<OrdersController> logger)
        {
            _context = context;
            _mapper = mapper;  // Constructor orqali mapperni olish
            _logger = logger;  // Initialize ILogger
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrders()
        {
            _logger.LogInformation("Fetching all orders.");

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems!)
                .ThenInclude(oi => oi.Product!)
                .ThenInclude(p => p.Category)
                .ToListAsync();

            var orderDTOs = _mapper.Map<IEnumerable<OrderDTO>>(orders); // Mappingni qo'llash
            _logger.LogInformation("Fetched {OrderCount} orders.", orders.Count);

            return Ok(orderDTOs);
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDTO>> GetOrder(int id)
        {
            _logger.LogInformation("Fetching order with ID: {OrderId}", id);

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)!
                .ThenInclude(oi => oi.Product!)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order with ID: {OrderId} not found.", id);
                return NotFound();
            }

            var orderDTO = _mapper.Map<OrderDTO>(order); // Mappingni qo'llash
            _logger.LogInformation("Fetched order with ID: {OrderId}", id);

            return Ok(orderDTO);
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(OrderCreateDTO orderDto)
        {
            _logger.LogInformation("Creating new order for customer ID: {CustomerId}", orderDto.CustomerId);

            var customer = await _context.Customers.FindAsync(orderDto.CustomerId);
            if (customer == null)
            {
                _logger.LogWarning("Customer with ID: {CustomerId} not found.", orderDto.CustomerId);
                return BadRequest("Customer not found.");
            }

            var order = new Order
            {
                CustomerId = orderDto.CustomerId,
                OrderDate = DateTime.UtcNow,
                OrderItems = new List<OrderItem>()
            };

            decimal totalAmount = 0;
            foreach (var itemDto in orderDto.OrderItems!)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null || product.Stock < itemDto.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for product ID {ProductId}.", itemDto.ProductId);
                    return BadRequest($"Insufficient stock for product ID {itemDto.ProductId}");
                }

                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    Price = product.Price * itemDto.Quantity,
                    Product = product
                };

                order.OrderItems.Add(orderItem);
                totalAmount += orderItem.Price;
                product.Stock -= itemDto.Quantity; // Stockni kamaytirish
            }

            order.TotalAmount = totalAmount;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order with ID: {OrderId} created successfully.", order.Id);

            var result = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems!)
                .ThenInclude(oi => oi.Product!)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, result);
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            _logger.LogInformation("Updating order with ID: {OrderId}", id);

            if (id != order.Id)
            {
                return BadRequest();
            }

            var existingOrder = await _context.Orders.FindAsync(id);
            if (existingOrder == null)
            {
                _logger.LogWarning("Order with ID: {OrderId} not found for update.", id);
                return NotFound();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order with ID: {OrderId} updated successfully.", id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            _logger.LogInformation("Deleting order with ID: {OrderId}", id);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order with ID: {OrderId} not found for deletion.", id);
                return NotFound();
            }

            // O'chirilgan order itemlari uchun stockni qaytarish
            foreach (var item in order.OrderItems!)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                }
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order with ID: {OrderId} deleted successfully.", id);

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(o => o.Id == id);
        }
    }
}
