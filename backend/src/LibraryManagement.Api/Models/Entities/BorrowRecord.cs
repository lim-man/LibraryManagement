using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.Models.Entities;

/// <summary>
/// 借阅记录实体 —— 对应数据库 BorrowRecords 表
/// 关联 User 和 Book，构成"哪个用户借了哪本书"
/// User 属性和 Book 属性称为"导航属性"（Navigation Property），
/// EF Core 通过它们自动生成 JOIN 查询
/// </summary>
public class BorrowRecord
{
    public int Id { get; set; }

    // 外键（Foreign Key）—— 指向 Users 表的主键
    public int UserId { get; set; }

    // 导航属性 —— 不是数据库列，而是让 EF Core 知道如何关联到 User 表
    // 通过 .Include(r => r.User) 可以一次性查出关联的用户数据（避免 N+1 查询问题）
    public User? User { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }  // 导航属性，关联到 Book 表

    public DateTime BorrowDate { get; set; } = DateTime.UtcNow;  // 借书日期

    public DateTime DueDate { get; set; }  // 应还日期（借书日期 + 30天）

    public DateTime? ReturnDate { get; set; }  // 实际归还日期，NULL 表示尚未归还

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Borrowed";  // 状态：Borrowed（借阅中）/ Returned（已归还）

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
