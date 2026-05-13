using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Controllers;

/// <summary>
/// 统计控制器 —— 仪表盘数据
/// 这个 Controller 直接注入 AppDbContext（而非通过 Service），
/// 因为统计查询大多是聚合查询（SUM/COUNT/GROUP BY），
/// 逻辑简单且不需要复用，直接查数据库更简洁
///
/// 但正式项目建议还是抽 Service（便于单元测试和缓存优化）
/// </summary>
[ApiController]
[Route("api/[controller]")]            // → "api/stats"
[Authorize(Roles = "Admin")]            // 只有管理员能看统计
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StatsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET api/stats/dashboard —— 仪表盘统计数据
    /// 返回匿名对象（anonymous object），因为数据只在这一处使用，不值得创建专门的 DTO 类
    /// 但缺点是返回类型为 object，丢失了编译时类型检查
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<object>>> GetDashboard()
    {
        var now = DateTime.UtcNow;

        // 使用 EF Core 的聚合函数：SumAsync、CountAsync
        // 这些都会转化为 SQL 聚合函数（SUM、COUNT）
        var totalBooks = await _db.Books.SumAsync(b => b.TotalCopies);
        var availableBooks = await _db.Books.SumAsync(b => b.AvailableCopies);
        var totalReaders = await _db.Users.CountAsync(u => u.Role == "Reader" && u.IsActive);
        var currentBorrowed = await _db.BorrowRecords.CountAsync(r => r.ReturnDate == null);
        var overdueCount = await _db.BorrowRecords.CountAsync(r => r.ReturnDate == null && r.DueDate < now);

        // 热门图书 TOP 5 —— GroupBy + Count + OrderByDescending + Take
        // SQL: SELECT TOP 5 BookId, Title, COUNT(*) as Count
        //      FROM BorrowRecords JOIN Books ON ...
        //      GROUP BY BookId, Title ORDER BY Count DESC
        var popularBooks = await _db.BorrowRecords
            .Include(r => r.Book)
            .Where(r => r.Book != null)
            .GroupBy(r => new { r.BookId, r.Book!.Title })
            .Select(g => new { g.Key.BookId, g.Key.Title, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        // 最近借阅记录 TOP 10
        var recentBorrows = await _db.BorrowRecords
            .Include(r => r.User)
            .Include(r => r.Book)
            .OrderByDescending(r => r.BorrowDate)
            .Take(10)
            .Select(r => new
            {
                r.Id,
                UserName = r.User!.Name,
                BookTitle = r.Book!.Title,
                r.BorrowDate,
                r.Status
            })
            .ToListAsync();

        // 返回匿名对象 —— C# 的 new { } 语法创建匿名类型
        return Ok(ApiResponse<object>.Ok(new
        {
            totalBooks,
            availableBooks,
            borrowedBooks = totalBooks - availableBooks,  // 借出数 = 总数 - 可借数
            totalReaders,
            currentBorrowed,
            overdueCount,
            popularBooks,
            recentBorrows
        }));
    }
}
