using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models.Common;
using LibraryManagement.Api.Models.DTOs.Borrows;
using LibraryManagement.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public interface IBorrowService
{
    Task<BorrowRecordDto> BorrowBookAsync(int userId, int bookId, bool isAdmin, int? targetUserId);
    Task<BorrowRecordDto> ReturnBookAsync(int recordId, int userId, bool isAdmin);
    Task<PagedResult<BorrowRecordDto>> GetBorrowsAsync(int page, int pageSize, int userId, bool isAdmin, string? status, int? filterUserId, int? filterBookId);
    Task<List<BorrowRecordDto>> GetOverdueRecordsAsync(int page, int pageSize);
}

public class BorrowService : IBorrowService
{
    private readonly AppDbContext _db;

    public BorrowService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 借书 —— 最核心也最复杂的业务逻辑
    /// 使用数据库事务保证数据一致性：扣库存 + 创建借阅记录 要么全部成功，要么全部回滚
    ///
    /// 参数说明：
    ///   userId       = 当前操作者的用户 ID（从 JWT 中提取）
    ///   isAdmin      = 当前操作者是否为管理员
    ///   targetUserId = 管理员帮读者借书时指定的读者 ID（读者自己借书时为 null）
    /// </summary>
    public async Task<BorrowRecordDto> BorrowBookAsync(int userId, int bookId, bool isAdmin, int? targetUserId)
    {
        // 确定实际的借书人：管理员可以指定目标读者，普通读者只能给自己借
        var actualUserId = isAdmin && targetUserId.HasValue ? targetUserId.Value : userId;

        // 一系列业务校验 —— 任何校验失败都会抛异常 → 被 ExceptionMiddleware 捕获 → 返回 400
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == actualUserId && u.IsActive)
            ?? throw new InvalidOperationException("用户不存在或已禁用");

        if (user.Role == "Admin")
            throw new InvalidOperationException("管理员不能借书");

        var book = await _db.Books.FindAsync(bookId)
            ?? throw new InvalidOperationException("图书不存在");

        if (book.AvailableCopies <= 0)
            throw new InvalidOperationException("该图书库存不足");

        // 检查是否已借过同一本书且未归还（数据库有唯一索引兜底，但提前检查可以给出更友好的错误提示）
        var alreadyBorrowed = await _db.BorrowRecords
            .AnyAsync(r => r.UserId == actualUserId && r.BookId == bookId && r.ReturnDate == null);

        if (alreadyBorrowed)
            throw new InvalidOperationException("您已借阅该图书且尚未归还");

        // 开启数据库事务 —— 确保以下两个操作原子执行
        // BeginTransactionAsync 对应 SQL: BEGIN TRANSACTION
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 操作1：扣减图书库存
            book.AvailableCopies--;
            book.UpdatedAt = DateTime.UtcNow;

            // 操作2：创建借阅记录（借期30天）
            var record = new BorrowRecord
            {
                UserId = actualUserId,
                BookId = bookId,
                BorrowDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),  // 30天借阅期限
                Status = "Borrowed",
                CreatedAt = DateTime.UtcNow
            };

            _db.BorrowRecords.Add(record);
            await _db.SaveChangesAsync();   // 生成两条 SQL：UPDATE Books + INSERT BorrowRecords
            await transaction.CommitAsync(); // COMMIT TRANSACTION —— 确认提交

            return new BorrowRecordDto
            {
                Id = record.Id,
                UserId = record.UserId,
                UserName = user.Name,
                BookId = record.BookId,
                BookTitle = book.Title,
                BookISBN = book.ISBN,
                BorrowDate = record.BorrowDate,
                DueDate = record.DueDate,
                Status = record.Status
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            // 乐观并发冲突：另一个请求同时修改了这本书（例如同时借最后一本）
            // RollbackAsync 对应 SQL: ROLLBACK TRANSACTION
            await transaction.RollbackAsync();
            throw new InvalidOperationException("操作冲突，请稍后重试");
        }
    }

    /// <summary>
    /// 还书 —— 同样使用事务保证库存恢复 + 记录更新的一致性
    /// </summary>
    public async Task<BorrowRecordDto> ReturnBookAsync(int recordId, int userId, bool isAdmin)
    {
        // Include 方法实现"贪婪加载"：一次性查出关联的 User 和 Book 数据
        // SQL: SELECT * FROM BorrowRecords r
        //      JOIN Users u ON r.UserId = u.Id
        //      JOIN Books b ON r.BookId = b.Id
        //      WHERE r.Id = @recordId
        var record = await _db.BorrowRecords
            .Include(r => r.User)
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == recordId)
            ?? throw new InvalidOperationException("借阅记录不存在");

        if (record.ReturnDate != null)
            throw new InvalidOperationException("该记录已归还");

        // 权限检查：读者只能还自己的书，管理员可以还任何人的书
        if (!isAdmin && record.UserId != userId)
            throw new InvalidOperationException("无权操作他人的借阅记录");

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            record.ReturnDate = DateTime.UtcNow;
            record.Status = "Returned";

            // 恢复库存
            var book = record.Book!;
            book.AvailableCopies++;
            book.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new BorrowRecordDto
            {
                Id = record.Id,
                UserId = record.UserId,
                UserName = record.User!.Name,
                BookId = record.BookId,
                BookTitle = record.Book!.Title,
                BookISBN = record.Book.ISBN,
                BorrowDate = record.BorrowDate,
                DueDate = record.DueDate,
                ReturnDate = record.ReturnDate,
                Status = record.Status
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;  // 重新抛出原异常，保持堆栈信息
        }
    }

    /// <summary>
    /// 借阅记录列表 —— 管理员可以看到所有人的记录并可按用户/图书/状态筛选
    /// 普通读者只能看到自己的记录
    /// </summary>
    public async Task<PagedResult<BorrowRecordDto>> GetBorrowsAsync(int page, int pageSize, int userId, bool isAdmin, string? status, int? filterUserId, int? filterBookId)
    {
        var query = _db.BorrowRecords
            .Include(r => r.User)
            .Include(r => r.Book)
            .AsQueryable();

        // 非管理员只能看自己的记录（后端权限控制，不能仅靠前端路由守卫）
        if (!isAdmin)
            query = query.Where(r => r.UserId == userId);

        // 管理员可选筛选条件
        if (isAdmin && filterUserId.HasValue)
            query = query.Where(r => r.UserId == filterUserId.Value);

        if (isAdmin && filterBookId.HasValue)
            query = query.Where(r => r.BookId == filterBookId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.BorrowDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToDto(r))
            .ToListAsync();

        return new PagedResult<BorrowRecordDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    /// <summary>
    /// 逾期记录 —— ReturnDate 为 NULL 且 DueDate 已过当前时间
    /// 只有管理员可以查看
    /// </summary>
    public async Task<List<BorrowRecordDto>> GetOverdueRecordsAsync(int page, int pageSize)
    {
        var now = DateTime.UtcNow;
        return await _db.BorrowRecords
            .Include(r => r.User)
            .Include(r => r.Book)
            .Where(r => r.ReturnDate == null && r.DueDate < now)
            .OrderBy(r => r.DueDate)  // 按逾期最久的排前
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToDto(r))
            .ToListAsync();
    }

    // 使用导航属性的数据来填充 DTO 中的冗余字段（UserName, BookTitle, BookISBN）
    // 这样前端不需要再额外请求用户/图书接口就能显示完整信息
    private static BorrowRecordDto ToDto(BorrowRecord r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.User?.Name ?? "",      // ?. 是空安全操作符，User 为 null 时不抛异常
        BookId = r.BookId,
        BookTitle = r.Book?.Title ?? "",
        BookISBN = r.Book?.ISBN ?? "",
        BorrowDate = r.BorrowDate,
        DueDate = r.DueDate,
        ReturnDate = r.ReturnDate,
        Status = r.Status
    };
}
