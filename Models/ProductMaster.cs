using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSystemBack
{
    [Table("product_master")]
    public class ProductMaster
    {
        [Key]
        [Column("product_id")]
        public string product_id { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public decimal product_price { get; set; }
    }
}