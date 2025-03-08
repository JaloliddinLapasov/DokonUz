using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DokonUz.Data;
using DokonUz.DTOs;
using DokonUz.Models;
using Microsoft.Extensions.Logging; // Loglash uchun kerak

namespace DokonUz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly DokonUzDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomersController> _logger; // ILogger ni qo'shdik

        public CustomersController(DokonUzDbContext context, IMapper mapper, ILogger<CustomersController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger; // Loggerni constructor orqali olish
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDTO>>> GetCustomers()
        {
            _logger.LogInformation("Barcha mijozlar ro'yxati olinmoqda.");

            var customers = await _context.Customers.ToListAsync();
            _logger.LogInformation("{CustomerCount} ta mijoz topildi.", customers.Count);

            return Ok(_mapper.Map<IEnumerable<CustomerDTO>>(customers));
        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDTO>> GetCustomer(int id)
        {
            _logger.LogInformation("Mijoz ID: {CustomerId} olinmoqda.", id);

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                _logger.LogWarning("Mijoz ID: {CustomerId} topilmadi.", id);
                return NotFound();
            }

            _logger.LogInformation("Mijoz ID: {CustomerId} muvaffaqiyatli olindi.", id);
            return _mapper.Map<CustomerDTO>(customer);
        }

        // POST: api/Customers
        [HttpPost]
        public async Task<ActionResult<CustomerDTO>> PostCustomer(CustomerCreateDTO customerDto)
        {
            _logger.LogInformation("Yangi mijoz qo'shilyapti.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Mijozni qo'shishda xatolik yuz berdi: {ErrorDetails}", ModelState.Values);
                return BadRequest(ModelState);
            }

            var customer = _mapper.Map<Customer>(customerDto);
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Mijoz ID: {CustomerId} muvaffaqiyatli qo'shildi.", customer.Id);

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, _mapper.Map<CustomerDTO>(customer));
        }

        // PUT: api/Customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, CustomerCreateDTO customerDto)
        {
            _logger.LogInformation("Mijoz ID: {CustomerId} ma'lumotlari yangilanmoqda.", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Yangilashda xatolik: {ErrorDetails}", ModelState.Values);
                return BadRequest(ModelState);
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                _logger.LogWarning("Mijoz ID: {CustomerId} topilmadi.", id);
                return NotFound();
            }

            _mapper.Map(customerDto, customer); // DTO dan mavjud mijozga ma'lumotlarni o'tkazish

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Mijoz ID: {CustomerId} muvaffaqiyatli yangilandi.", id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
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

        // DELETE: api/Customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            _logger.LogInformation("Mijoz ID: {CustomerId} o'chirilmoqda.", id);

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                _logger.LogWarning("Mijoz ID: {CustomerId} topilmadi.", id);
                return NotFound();
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Mijoz ID: {CustomerId} muvaffaqiyatli o'chirildi.", id);

            return Ok();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
