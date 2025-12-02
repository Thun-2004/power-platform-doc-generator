
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace backendApi.Tests
{
    public class backendControllerTests : IClassFixture<WebApplicationFactory<backendApi.Program>>
    {
        private readonly WebApplicationFactory<backendApi.Program> _factory;
        public backendControllerTests(WebApplicationFactory<backendApi.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetProducts_ReturnsOkResponse()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            // Act
            var response = await client.GetAsync("/api/products");
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProductById_ReturnsProduct_WhenIdIsValid() {
            // Arrange
            var client = _factory.CreateClient();
            var productId = 1;
            // Act
            var response = await client.GetAsync($"/api/products/{productId}");
            // Assert
            response.EnsureSuccessStatusCode();
            var product = await response.Content.ReadFromJsonAsync<backendApi.Models.Product>();
            Assert.Equal(productId, product.Id);
        }

    }
}