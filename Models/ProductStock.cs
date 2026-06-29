using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSystemBack
{
    [Table("product_stock")]
    public class ProductStock
    {
        [Column("stock_id")]
        public string stock_id { get; set; } = string.Empty;
        [Column("product_id")]
        public string product_id { get; set; } = string.Empty;
        [Column("stock_remain")]
        public int stock_remain { get; set; }
    }
}