namespace ProductManagementSystem.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public List<ItemDto> Items { get; set; } = new();
    }
}
