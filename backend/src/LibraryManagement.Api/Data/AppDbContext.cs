using LibraryManagement.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Data;

/// <summary>
/// 数据库上下文 —— EF Core 的核心类
/// 继承自 DbContext，相当于"数据库连接 + 所有表的集合 + 表关系配置"
///
/// 生命周期：在 Program.cs 中注册为 Scoped，即每个 HTTP 请求创建一个新实例
/// </summary>
public class AppDbContext : DbContext
{
    // DbSet<T> 代表数据库中的一张表
    // Set<T>() 是获取表对象的方法，和直接写 new DbSet<T> 等价
    // EF Core 会在首次访问时自动生成对应的 SQL 查询
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();

    // 构造函数接收 DbContextOptions（包含连接字符串等配置）
    // 这个 options 由 Program.cs 中的 AddDbContext 自动传入
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// OnModelCreating 是 EF Core 的 Fluent API 配置入口
    /// 这里用代码定义表关系、索引、约束等，而不是用数据注解（Data Annotations）
    /// Fluent API 比数据注解更强大，能做数据注解做不到的事情（如复合索引、过滤索引）
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---- User 表配置 ----
        modelBuilder.Entity<User>(e =>
        {
            // 在 Username 列上创建唯一索引，防止重名用户
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasMaxLength(20);
        });

        // ---- Book 表配置 ----
        modelBuilder.Entity<Book>(e =>
        {
            // ISBN 必须唯一
            e.HasIndex(b => b.ISBN).IsUnique();
            // Price 列精度设为 (10, 2)，即最多 8 位整数 + 2 位小数
            e.Property(b => b.Price).HasPrecision(10, 2);
            // CHECK 约束：数据库级别的约束，确保可借册数永远不会超过总册数
            // 这相当于最后一道防线，即使代码有 bug，数据库也不会产生非法数据
            e.ToTable(t => t.HasCheckConstraint("CK_Books_Available_LE_Total", "[AvailableCopies] <= [TotalCopies]"));
        });

        // ---- BorrowRecord 表配置 ----
        modelBuilder.Entity<BorrowRecord>(e =>
        {
            // DeleteBehavior.Restrict：当尝试删除一个用户/图书时，
            // 如果还有关联的借阅记录，数据库会拒绝删除（抛异常），保护数据完整性
            e.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Book)
                .WithMany()
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // 过滤唯一索引（Filtered Unique Index）：
            // 在 (UserId, BookId) 上建唯一索引，但只对 ReturnDate IS NULL 的行生效
            // 效果：同一用户不能同时借阅同一本书两次（还没归还就不能再借）
            // 但归还后可以再次借阅（因为归还不在此过滤范围内）
            e.HasIndex(r => new { r.UserId, r.BookId })
                .IsUnique()
                .HasFilter("[ReturnDate] IS NULL");

            e.Property(r => r.Status).HasMaxLength(20);
        });
    }
}
