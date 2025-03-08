using DokonUz.Controllers;
using DokonUz.Data;
using DokonUz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DokonUz.Tests
{
    public class ProductsControllerTests
    {
        private readonly DokonUzDbContext _context;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DokonUzDbContext>()
                .UseInMemoryDatabase(databaseName: "EcommerceDb55")
                .Options;
            
            _context = new DokonUzDbContext(options);
            _controller = new ProductsController(_context);
        }

        [Fact]
        public async Task GetProducts_ReturnsAllProducts()
        {
            // Arrange
            _context.Products.Add(new Product { Id = 1, Name = "Laptop", Price = 1000 });
            _context.Products.Add(new Product { Id = 2, Name = "Phone", Price = 500 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProducts();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<Product>>>(result);
            var returnValue = Assert.IsType<List<Product>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetProduct_ReturnsProduct_WhenProductExists()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Laptop", Price = 1000 };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Product>>(result);
            var returnValue = Assert.IsType<Product>(actionResult.Value);
            Assert.Equal(1, returnValue.Id);
        }

        [Fact]
        public async Task PostProduct_CreatesNewProduct()
        {
            // Arrange
            var newProduct = new Product { Name = "Tablet", Price = 300 };

            // Act
            var result = await _controller.PostProduct(newProduct);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public async Task DeleteProduct_RemovesProduct_WhenProductExists()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Laptop", Price = 1000 };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
    }
}
