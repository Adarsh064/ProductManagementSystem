using System.Text.Json.Serialization;

namespace ProductManagementSystem.Models
{
    public abstract class UserEntity
    {
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        [JsonIgnore]
        public virtual SysUser CreatedByUser { get; set; }
        [JsonIgnore]
        public virtual SysUser UpdatedByUser { get; set; }
    }
}
