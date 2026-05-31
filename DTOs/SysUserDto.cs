namespace ProductManagementSystem.DTOs
{
    public class SysUserDto
    {
        public int UserId { get; set; }
        public required string LoginId { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
        public bool IsActive { get; set; }
        public string? UserType { get; set; }
    }
}
