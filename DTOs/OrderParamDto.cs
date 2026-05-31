namespace ProductManagementSystem.DTOs
{
    public class OrderParamDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? OrderColumn { get; set; } = string.Empty;
        public string Order { get; set; } = "asc";
        public string? SearchText { get; set; }
    }
}
