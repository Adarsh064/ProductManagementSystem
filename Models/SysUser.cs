using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagementSystem.Models
{
    [Table("sysuser")]
    public class SysUser:UserEntity
    {
        [Key]
        public int UserId { get; set; }
        public string LoginId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string? Fcm { get; set; }
        public string UserType { get; set; }
    }
}
