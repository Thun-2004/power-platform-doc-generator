using Microsoft.AspNetCore.Mvc;
namespace MyFirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    
    // Sample data - in real apps, this would come from a database
    private static List<Product> products = new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 999.99m },
        new Product { Id = 2, Name = "Mouse", Price = 29.99m },
        new Product { Id = 3, Name = "Keyboard", Price = 79.99m }
    };


    // GET: api/products
    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetAllProducts()
    {
        return Ok(products);
    }


    // GET: api/products/1
    [HttpGet("{id}")]
    public ActionResult<Product> GetProduct(int id)
    {
        var product = products.FirstOrDefault(p => p.Id == id);
        
        if (product == null)
            return NotFound(new { message = "Product not found" });
            
        return Ok(product);
    }


    // POST: api/products
    [HttpPost]
    public ActionResult<Product> CreateProduct(Product product)
    {
        product.Id = products.Max(p => p.Id) + 1;
        products.Add(product);
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }


    // PUT: api/products/1
    [HttpPut("{id}")]
    public ActionResult UpdateProduct(int id, Product updatedProduct)
    {
        var product = products.FirstOrDefault(p => p.Id == id);
        
        if (product == null)
            return NotFound(new { message = "Product not found" });
            
        product.Name = updatedProduct.Name;
        product.Price = updatedProduct.Price;
        
        return Ok(product);
    }


    // DELETE: api/products/1
    [HttpDelete("{id}")]
    public ActionResult DeleteProduct(int id)
    {
        var product = products.FirstOrDefault(p => p.Id == id);
        
        if (product == null)
            return NotFound(new { message = "Product not found" });
            
        products.Remove(product);
        
        return Ok(new { message = "Product deleted successfully" });
    }
}
// Product model
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}