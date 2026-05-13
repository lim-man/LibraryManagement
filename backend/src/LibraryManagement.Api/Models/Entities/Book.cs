using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.Models.Entities;

/// <summary>
/// 图书实体 —— 对应数据库 Books 表
/// </summary>
public class Book
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string ISBN { get; set; } = string.Empty;  // 国际标准书号，每本书唯一

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;  // 书名

    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;  // 作者

    [MaxLength(100)]
    public string? Publisher { get; set; }  // 出版社

    [MaxLength(50)]
    public string? Category { get; set; }  // 分类（计算机、文学、历史等）

    public decimal? Price { get; set; }  // 定价，decimal 类型适合金额（不会浮点精度丢失）

    public int TotalCopies { get; set; }     // 总册数

    public int AvailableCopies { get; set; } // 可借册数 = 总册数 - 已借出册数

    [MaxLength(100)]
    public string? Location { get; set; }  // 书架位置（如 "A区-1排-1层"）

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }  // 封面图片 URL

    public string? Description { get; set; }  // 内容简介，没有 MaxLength 则默认为 nvarchar(max)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // [Timestamp] 是 EF Core 的乐观并发控制标记
    // 每次更新此行时，数据库自动生成新的 RowVersion 值
    // 如果两个请求同时修改同一本书，后提交的会检测到冲突并抛出 DbUpdateConcurrencyException
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
