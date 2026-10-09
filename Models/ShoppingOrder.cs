using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSystemBack
{
    [Table("shopping_order")]
    public class ShoppingOrder
    {
        [Key]
        [Column("order_id")]
        public int order_id { get; set; }
        public string product_id { get; set; } = string.Empty;
        public int qty { get; set; }
        public DateTime order_date { get; set; }
    }
}