using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSystemBack
{
    [Table("shopping_cart")]
    public class ShoppingCart
    {
        [Key]
        [Column("cart_id")]
        public int cart_id { get; set; }
        public string product_id { get; set; } = string.Empty;
        public int qty { get; set; }
    }
}