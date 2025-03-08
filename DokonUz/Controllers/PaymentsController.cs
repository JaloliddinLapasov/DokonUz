using DokonUz.Data;
using DokonUz.Helpers;
using DokonUz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Threading.Tasks;

namespace DokonUz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly DokonUzDbContext _context;
        private readonly PaymentSettings _paymentSettings;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(DokonUzDbContext context, IOptions<PaymentSettings> paymentSettings, ILogger<PaymentsController> logger)
        {
            _context = context;
            _paymentSettings = paymentSettings.Value;
            _logger = logger;
        }

        private void ConfigureStripe()
        {
            StripeConfiguration.ApiKey = _paymentSettings.SecretKey;
        }

        [HttpPost("charge")]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            _logger.LogInformation("Processing payment for order ID: {OrderId}", orderId);

            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
                return NotFound("Order not found.");
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.LogWarning("Order with ID {OrderId} is already paid.", orderId);
                return BadRequest("Order is already paid.");
            }

            ConfigureStripe();

            try
            {
                var customerService = new CustomerService();
                var customer = customerService.Create(new CustomerCreateOptions
                {
                    Email = "customer@example.com",
                    Name = "John Doe"
                });

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
                {
                    Amount = (long)(order.TotalAmount * 100),
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                    Customer = customer.Id
                });

                order.PaymentStatus = PaymentStatus.Paid;
                _context.Entry(order).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment processed successfully for order ID: {OrderId}, PaymentIntent ID: {PaymentIntentId}", orderId, paymentIntent.Id);

                return Ok(new
                {
                    Message = "Payment processed successfully.",
                    PaymentIntentId = paymentIntent.Id
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error processing payment for order ID: {OrderId}", orderId);
                return BadRequest(new { Error = "Payment processing failed. Please try again." });
            }
        }
    }
}
