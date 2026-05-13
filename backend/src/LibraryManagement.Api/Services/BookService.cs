using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models.Common;
using LibraryManagement.Api.Models.DTOs.Books;
using LibraryManagement.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public interface IBookService
{
    Task<PagedResult<BookDto>> GetBooksAsync(int page, int pageSize, string? keyword, string? category);
    Task<BookDto?> GetBookByIdAsync(int id);
    Task<BookDto> CreateBookAsync(CreateBookRequest request);
    Task<BookDto?> UpdateBookAsync(int id, UpdateBookRequest request);
    Task<bool> DeleteBookAsync(int id);
    Task<List<string>> GetCategoriesAsync();
}

public class BookService : IBookService
{
    private readonly AppDbContext _db;

    public BookService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 分页查询图书列表，支持关键词搜索和分类筛选
    /// EF Core 的 IQueryable 使用"延迟执行"：只有在调用 CountAsync/ToListAsync 时才会生成 SQL
    /// 好处：可以在真正执行前动态拼接查询条件，避免不必要的数据库查询
    /// </summary>
    public async Task<PagedResult<BookDto>> GetBooksAsync(int page, int pageSize, string? keyword, string? category)
    {
        // AsQueryable() 返回 IQueryable<T>，此时还没有执行任何 SQL
        var query = _db.Books.AsQueryable();

        // 动态添加 WHERE 条件 —— 实际 SQL 在最后执行时才生成
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(b => b.Title.Contains(keyword) || b.Author.Contains(keyword) || b.ISBN.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        var total = await query.CountAsync();  // 第一条 SQL: SELECT COUNT(*) FROM Books WHERE ...

        // Skip + Take 实现分页（SQL 中的 OFFSET ... FETCH NEXT ... ROWS ONLY）
        // Select(ToDto) 将实体转换为 DTO，避免返回过多字段给客户端
        var items = await query.OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)   // 跳过前 (page-1) 页的数据
            .Take(pageSize)                 // 取当前页的数据
            .Select(b => ToDto(b))
            .ToListAsync();                 // 第二条 SQL: SELECT ... FROM Books ORDER BY ... OFFSET ... LIMIT ...

        return new PagedResult<BookDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<BookDto?> GetBookByIdAsync(int id)
    {
        var book = await _db.Books.FindAsync(id);  // FindAsync 按主键查找，优先从 EF Core 的缓存中取
        return book == null ? null : ToDto(book);
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request)
    {
        var book = new Book
        {
            ISBN = request.ISBN,
            Title = request.Title,
            Author = request.Author,
            Publisher = request.Publisher,
            Category = request.Category,
            Price = request.Price,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.TotalCopies,  // 新书：可借数 = 总册数（还没人借）
            Location = request.Location,
            CoverImageUrl = request.CoverImageUrl,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Books.Add(book);  // 标记为新记录（状态：Added）
        await _db.SaveChangesAsync();  // 生成 INSERT SQL 并执行
        return ToDto(book);
    }

    public async Task<BookDto?> UpdateBookAsync(int id, UpdateBookRequest request)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return null;

        // 校验：新的总册数不能小于当前已借出的数量
        var borrowedCount = book.TotalCopies - book.AvailableCopies;
        if (request.TotalCopies < borrowedCount)
            // throw InvalidOperationException → 被 ExceptionMiddleware 捕获 → 返回 400
            throw new InvalidOperationException($"总册数不能小于当前借出数量({borrowedCount})");

        // 逐字段更新，避免使用 _db.Entry(book).CurrentValues.SetValues() 可能导致的过度绑定攻击
        book.ISBN = request.ISBN;
        book.Title = request.Title;
        book.Author = request.Author;
        book.Publisher = request.Publisher;
        book.Category = request.Category;
        book.Price = request.Price;
        book.TotalCopies = request.TotalCopies;
        book.AvailableCopies = request.TotalCopies - borrowedCount;  // 重新计算可借数量
        book.Location = request.Location;
        book.CoverImageUrl = request.CoverImageUrl;
        book.Description = request.Description;
        book.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();  // 生成 UPDATE SQL，只更新变化的列
        return ToDto(book);
    }

    /// <summary>
    /// 删除图书 —— 有未归还借阅记录时不允许删除
    /// 这是业务规则：不能删除还有读者正在借阅的书
    /// </summary>
    public async Task<bool> DeleteBookAsync(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return false;

        // AnyAsync 检查是否存在满足条件的记录（比 CountAsync > 0 更语义化、可能更高效）
        var hasUnreturned = await _db.BorrowRecords.AnyAsync(r => r.BookId == id && r.ReturnDate == null);
        if (hasUnreturned)
            throw new InvalidOperationException("该图书有未归还的借阅记录，无法删除");

        _db.Books.Remove(book);  // 标记为删除（状态：Deleted）
        await _db.SaveChangesAsync();  // 生成 DELETE SQL
        return true;
    }

    /// <summary>
    /// 获取所有不重复的分类列表（用于前端下拉框）
    /// SQL: SELECT DISTINCT Category FROM Books WHERE Category IS NOT NULL ORDER BY Category
    /// </summary>
    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _db.Books
            .Where(b => b.Category != null)
            .Select(b => b.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    /// <summary>
    /// 实体 → DTO 映射（只暴露前端需要的字段）
    /// 不暴露 RowVersion（这是数据库内部用的并发控制字段）
    /// </summary>
    private static BookDto ToDto(Book b) => new()
    {
        Id = b.Id,
        ISBN = b.ISBN,
        Title = b.Title,
        Author = b.Author,
        Publisher = b.Publisher,
        Category = b.Category,
        Price = b.Price,
        TotalCopies = b.TotalCopies,
        AvailableCopies = b.AvailableCopies,
        Location = b.Location,
        CoverImageUrl = b.CoverImageUrl,
        Description = b.Description,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}
