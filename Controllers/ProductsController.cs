using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using StockSystemBack.Data;


namespace StockSystemBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _context.ProductMaster.AsNoTracking().ToListAsync();
                var stocks = await _context.ProductStock.AsNoTracking().ToListAsync();

                var result = products.Select(p => {
                    var find_product_stock = stocks.FirstOrDefault(s => s.product_id == p.product_id);

                    return new
                    {
                        productId = p.product_id.Trim(),
                        productName = p.product_name,
                        productPrice = p.product_price,
                        productStock = find_product_stock != null ? find_product_stock.stock_remain : 0,
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("update-stock")]
        public async Task<IActionResult> UpdateStock([FromBody] List<stockCheckOutItems> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest("No Product in your Cart!");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in cartItems)
                {
                    var stockRecord = await _context.ProductStock
                                    .FirstOrDefaultAsync(s => s.product_id.Trim() == item.productId.Trim());

                    if (stockRecord == null)
                    {
                        return NotFound($"Not Found Item : {item.productId}");
                    }

                    stockRecord.stock_remain -= item.qty;                    
                    _context.Entry(stockRecord).Property(x => x.stock_remain).IsModified = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Update Stock Success" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); 
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}