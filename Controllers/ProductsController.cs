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
                    var find_product_stock = stocks.FirstOrDefault(s => s.product_id.Trim() == p.product_id.Trim());

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
    
        [HttpGet("shopping-cart")]
        public async Task<IActionResult> GetShoppingCart()
        {
            try
            {
                var carts = await _context.ShoppingCart.AsNoTracking().ToListAsync();
                var products = await _context.ProductMaster.AsNoTracking().ToListAsync();

                var result = carts.Select(c => 
                {
                    var findProduct = products.FirstOrDefault(p => p.product_id.Trim() == c.product_id.Trim());

                    return new
                    {
                        cartId = c.cart_id,
                        productId = c.product_id != null ? c.product_id.Trim() : "",
                        productName = findProduct != null ? findProduct.product_name : "",
                        productPrice = findProduct != null ? findProduct.product_price : 0,
                        qty = c.qty,
                        totalPrice = c.qty * (findProduct != null ? findProduct.product_price : 0)
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("shopping-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddShoppingCartRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.productId) || request.qty <= 0) {
                return BadRequest("Invalid product or quantity.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var productId = request.productId.Trim();

                // ตรวจว่ามีสินค้าไหม
                var product = await _context.ProductMaster.FirstOrDefaultAsync(p => p.product_id.Trim() == productId);

                if (product == null) {
                    return NotFound($"Not Found Item : {productId}");
                }

                // ตรวจ stock
                var stock = await _context.ProductStock.FirstOrDefaultAsync(s => s.product_id.Trim() == productId);

                if (stock == null)
                {
                    return NotFound($"Stock Not Found : {productId}");
                }

                // หา cart เดิม
                var cart = await _context.ShoppingCart.FirstOrDefaultAsync(c => c.product_id.Trim() == productId);

                int newQty;

                if (cart == null)
                {
                    // ยังไม่มี -> INSERT
                    newQty = request.qty;

                    if (newQty > stock.stock_remain)
                    {
                        return Conflict(new
                        {
                            message = $"จำนวนสินค้าที่เลือกเกิน stock คงเหลือ {stock.stock_remain} ชิ้น",
                            productId,
                            productStock = stock.stock_remain,
                            requestedQty = newQty
                        });
                    }

                    cart = new ShoppingCart
                    {
                        product_id = productId,
                        qty = newQty
                    };

                    _context.ShoppingCart.Add(cart);
                }
                else
                {
                    // มีแล้ว -> UPDATE
                    newQty = cart.qty + request.qty;

                    if (newQty > stock.stock_remain)
                    {
                        return Conflict(new
                        {
                            message = $"จำนวนสินค้าในตะกร้าเกิน stock คงเหลือ {stock.stock_remain} ชิ้น",
                            productId,
                            productStock = stock.stock_remain,
                            currentCartQty = cart.qty,
                            addQty = request.qty,
                            requestedQty = newQty
                        });
                    }

                    cart.qty = newQty;
                    _context.Entry(cart).Property(x => x.qty).IsModified = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Add to cart success",
                    cartId = cart.cart_id,
                    productId = productId,
                    qty = newQty
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("shopping-cart/{productId}")]
        public async Task<IActionResult> UpdateCartQty(string productId, [FromBody] UpdateShoppingCartRequest request)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return BadRequest("Product ID is required.");
            }

            if (request == null || request.qty < 0)
            {
                return BadRequest("Invalid quantity.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                productId = productId.Trim();

                var cart = await _context.ShoppingCart.FirstOrDefaultAsync(c => c.product_id.Trim() == productId);

                if (cart == null)
                {
                    return NotFound($"Cart item not found : {productId}");
                }

                // ถ้า qty = 0 ให้ลบออกจาก cart
                if (request.qty == 0)
                {
                    _context.ShoppingCart.Remove(cart);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        message = "Remove cart success",
                        productId
                    });
                }

                // ตรวจ stock
                var stock = await _context.ProductStock.FirstOrDefaultAsync(s => s.product_id.Trim() == productId);

                if (stock == null)
                {
                    return NotFound($"Stock Not Found : {productId}");
                }

                if (request.qty > stock.stock_remain)
                {
                    return Conflict(new
                    {
                        message = $"จำนวนสินค้าเกิน stock คงเหลือ {stock.stock_remain} ชิ้น",
                        productId,
                        productStock = stock.stock_remain,
                        requestedQty = request.qty
                    });
                }

                cart.qty = request.qty;

                _context.Entry(cart).Property(x => x.qty).IsModified = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Update cart success",
                    productId,
                    qty = request.qty
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("shopping-cart/{productId}")]
        public async Task<IActionResult> DeleteCartItem(string productId){
            try
            {
                productId = productId.Trim();

                var cart = await _context.ShoppingCart.FirstOrDefaultAsync(c => c.product_id.Trim() == productId);

                if (cart == null)
                {
                    return NotFound($"Cart item not found : {productId}");
                }

                _context.ShoppingCart.Remove(cart);

                await _context.SaveChangesAsync();

                return Ok(new{message = "Remove cart success"});
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("shopping-cart")]
        public async Task<IActionResult> ClearShoppingCart()
        {
            try
            {
                var carts = await _context.ShoppingCart.ToListAsync();

                if (carts.Any())
                {
                    _context.ShoppingCart.RemoveRange(carts);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Clear cart success"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var carts = await _context.ShoppingCart.ToListAsync();

                if (!carts.Any())
                {
                    return BadRequest("No Product in your Cart!");
                }

                var orderDate = DateTime.Now;

                foreach (var cart in carts)
                {
                    var productId = cart.product_id.Trim();

                    var stock = await _context.ProductStock.FirstOrDefaultAsync(s => s.product_id.Trim() == productId);

                    if (stock == null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound(new { message = $"Stock Not Found : {productId}", productId});
                    }

                    // ตรวจ stock อีกครั้งก่อนตัดจริง
                    if (cart.qty > stock.stock_remain)
                    {
                        await transaction.RollbackAsync();

                        return Conflict(new
                        {
                            message = $"สินค้า {productId} มี stock ไม่เพียงพอ",
                            productId,
                            cartQty = cart.qty,
                            productStock = stock.stock_remain
                        });
                    }

                    // บันทึก order
                    var order = new ShoppingOrder
                    {
                        product_id = productId,
                        qty = cart.qty,
                        order_date = orderDate
                    };

                    _context.ShoppingOrder.Add(order);

                    // หัก stock
                    stock.stock_remain -= cart.qty;
                    _context.Entry(stock).Property(x => x.stock_remain).IsModified = true;
                }

                // ตัด stock สำเร็จ -> clear cart
                _context.ShoppingCart.RemoveRange(carts);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Checkout Success"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Checkout Error: {ex.Message}" );
            }
        }

        [HttpGet("shopping-order")]
        public async Task<IActionResult> GetShoppingOrder()
        {
            try
            {
                var orders = await _context.ShoppingOrder
                    .AsNoTracking()
                    .OrderByDescending(x => x.order_date)
                    .ThenByDescending(x => x.order_id)
                    .ToListAsync();
                var products = await _context.ProductMaster.AsNoTracking().ToListAsync();

                var result = orders.Select(od =>
                {
                    var findProduct = products.FirstOrDefault(p => p.product_id.Trim() == od.product_id.Trim());

                    return new
                    {
                        orderId = od.order_id,
                        productId = od.product_id != null ? od.product_id.Trim() : "",
                        productName = findProduct != null ? findProduct.product_name : "",
                        productPrice = findProduct != null ? findProduct.product_price : 0,
                        qty = od.qty,
                        totalPrice = od.qty * (findProduct != null ? findProduct.product_price : 0),
                        orderDate = od.order_date
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        public class AddShoppingCartRequest
        {
            public string productId { get; set; } = "";
            public int qty { get; set; }
        }

        public class UpdateShoppingCartRequest
        {
            public int qty { get; set; }
        }
    }
}