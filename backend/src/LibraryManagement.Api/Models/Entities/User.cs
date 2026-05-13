using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.Models.Entities;

/// <summary>
/// 用户实体 —— 对应数据库 Users 表
/// EF Core 会将这个类的每个属性映射为表的列
/// [Required] [MaxLength] 等称为 Data Annotation（数据注解），
/// 用于定义数据库列的约束（非空、最大长度等）
/// </summary>
public class User
{
    // 主键（Primary Key）—— EF Core 默认将名为 Id 或 <类名>Id 的属性识别为主键
    public int Id { get; set; }

    [Required]      // 表示此列不可为 NULL
    [MaxLength(50)] // 列的最大长度（nvarchar(50)）
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    // 安全要点：永远只存密码的哈希值，不要存明文密码
    // BCrypt 哈希本身就包含了盐值（salt），每次哈希结果都不同
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }  // string? 表示此字段可为 NULL

    [MaxLength(100)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Reader";  // 角色：Admin（管理员）或 Reader（读者）

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 创建时间，默认当前 UTC 时间

    public bool IsActive { get; set; } = true;  // 软删除标记：false 表示已禁用（而非真正删除数据行）
}
