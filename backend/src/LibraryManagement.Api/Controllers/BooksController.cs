using LibraryManagement.Api.Models.Common;
using LibraryManagement.Api.Models.DTOs.Books;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]     // → "api/books"
[Authorize]                      // 整个 Controller 下的所有接口都需要认证
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    /// <summary>
    /// GET api/books?page=1&pageSize=10&keyword=&category=
    /// [FromQuery] 表示参数从 URL 查询字符串中读取
    /// 任何认证用户都可以浏览图书
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BookDto>>>> GetBooks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null)
    {
        var result = await _bookService.GetBooksAsync(page, pageSize, keyword, category);
        return Ok(ApiResponse<PagedResult<BookDto>>.Ok(result));
    }

    /// <summary>
    /// GET api/books/categories —— 返回所有图书分类（用于前端下拉框）
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
    {
        var result = await _bookService.GetCategoriesAsync();
        return Ok(ApiResponse<List<string>>.Ok(result));
    }

    /// <summary>
    /// GET api/books/5 —— {id} 是路由参数，自动绑定到方法参数 id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BookDto>>> GetBook(int id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        if (book == null)
            return NotFound(ApiResponse<BookDto>.Fail("图书不存在"));

        return Ok(ApiResponse<BookDto>.Ok(book));
    }

    /// <summary>
    /// POST api/books —— 新增图书
    /// [Authorize(Roles = "Admin")] 覆盖类级别的 [Authorize]，额外限制只有管理员可操作
    /// [FromBody] 从请求体 JSON 反序列化
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BookDto>>> CreateBook([FromBody] CreateBookRequest request)
    {
        var book = await _bookService.CreateBookAsync(request);
        return Ok(ApiResponse<BookDto>.Ok(book, "新增图书成功"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BookDto>>> UpdateBook(int id, [FromBody] UpdateBookRequest request)
    {
        var book = await _bookService.UpdateBookAsync(id, request);
        if (book == null)
            return NotFound(ApiResponse<BookDto>.Fail("图书不存在"));

        return Ok(ApiResponse<BookDto>.Ok(book, "更新图书成功"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBook(int id)
    {
        var result = await _bookService.DeleteBookAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("图书不存在"));

        return Ok(ApiResponse<object>.Ok("删除图书成功"));
    }
}
