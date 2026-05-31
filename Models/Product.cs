using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagementSystem.Models
{
    [Table("Product")]
    public class Product:UserEntity
    {
        [Key]
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public  DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<Item> Items { get; set; }= new List<Item>();

    }
}
