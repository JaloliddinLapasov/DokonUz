using AutoMapper;
using DokonUz.Controllers;
using DokonUz.Data;
using DokonUz.DTOs;
using DokonUz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DokonUz.Tests
{
    public class OrdersControllerTests
    {
        private readonly DokonUzDbContext _context;
        private readonly OrdersController _controller;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<OrdersController>> _mockLogger;

        public OrdersControllerTests()
        {
            var options = new DbContextOptionsBuilder<DokonUzDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            
            _context = new DokonUzDbContext(options);
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<OrdersController>>();

            _controller = new OrdersController(_context, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetOrders_ReturnsAllOrders()
        {
            // Arrange
            _context.Orders.Add(new Order { Id = 1, CustomerId = 1, TotalAmount = 500 });
            _context.Orders.Add(new Order { Id = 2, CustomerId = 2, TotalAmount = 1200 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetOrders();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<OrderDTO>>>(result);
            var returnValue = Assert.IsType<List<OrderDTO>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task PostOrder_CreatesNewOrder()
        {
            // Arrange
            var newOrderDto = new OrderCreateDTO 
            { 
                CustomerId = 1,
                OrderItems = new List<OrderItemDTO>
                {
                    new OrderItemDTO { ProductId = 1, Quantity = 2 }
                }
            };

            var mappedOrder = new Order 
            { 
                Id = 1, 
                CustomerId = 1, 
                TotalAmount = 600, 
                OrderItems = new List<OrderItem> 
                { 
                    new OrderItem { ProductId = 1, Quantity = 2, Price = 300 }
                } 
            };

            _mockMapper.Setup(m => m.Map<Order>(It.IsAny<OrderCreateDTO>()))
                       .Returns(mappedOrder);

            _context.Orders.Add(mappedOrder);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.PostOrder(newOrderDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnValue = Assert.IsType<OrderDTO>(createdResult.Value);
            Assert.Equal(1, returnValue.Id);
        }
    }
}
