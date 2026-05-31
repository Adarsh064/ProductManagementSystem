using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagementSystem.Models
{
    [Table("usertoken")]
    public class UserToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenId { get; set; }  
        public string RefreshTokenId { get; set; }  
        public string RefreshToken { get; set; }  
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }  
        public DateTime? RevokedAt { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        [NotMapped]
        public bool IsActive => RevokedAt == null && DateTime.Now < ExpiresAt;
        [NotMapped]
        public bool IsRefreshTokenActive => RevokedAt == null && DateTime.Now < RefreshTokenExpiresAt;

    }
}
