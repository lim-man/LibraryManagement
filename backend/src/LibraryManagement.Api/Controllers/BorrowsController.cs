using System.Security.Claims;
using LibraryManagement.Api.Models.Common;
using LibraryManagement.Api.Models.DTOs.Borrows;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]    // → "api/borrows"
[Authorize]
public class BorrowsController : ControllerBase
{
    private readonly IBorrowService _borrowService;

    public BorrowsController(IBorrowService borrowService)
    {
        _borrowService = borrowService;
    }

    /// <summary>
    /// POST api/borrows/borrow —— 借书
    /// 从 JWT Token 中提取 userId 和 isAdmin，传给 Service 做权限判断
    /// 这样 Controller 只负责提取身份信息，不负责权限逻辑（单一职责原则）
    /// </summary>
    [HttpPost("borrow")]
    public async Task<ActionResult<ApiResponse<BorrowRecordDto>>> BorrowBook([FromBody] BorrowRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");  // 检查 JWT 中的 Role Claim 是否为 "Admin"

        var result = await _borrowService.BorrowBookAsync(userId, request.BookId, isAdmin, request.UserId);
        return Ok(ApiResponse<BorrowRecordDto>.Ok(result, "借书成功"));
    }

    /// <summary>
    /// POST api/borrows/return/5 —— 还书（{recordId} 来自 URL 路由）
    /// </summary>
    [HttpPost("return/{recordId}")]
    public async Task<ActionResult<ApiResponse<BorrowRecordDto>>> ReturnBook(int recordId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");

        var result = await _borrowService.ReturnBookAsync(recordId, userId, isAdmin);
        return Ok(ApiResponse<BorrowRecordDto>.Ok(result, "还书成功"));
    }

    /// <summary>
    /// GET api/borrows —— 借阅记录列表（分页 + 筛选）
    /// 管理员可选 userId / bookId / status 过滤，普通读者只能看自己的
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BorrowRecordDto>>>> GetBorrows(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] int? userId = null,
        [FromQuery] int? bookId = null)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");

        var result = await _borrowService.GetBorrowsAsync(page, pageSize, currentUserId, isAdmin, status, userId, bookId);
        return Ok(ApiResponse<PagedResult<BorrowRecordDto>>.Ok(result));
    }

    /// <summary>
    /// GET api/borrows/overdue —— 逾期记录（仅管理员）
    /// </summary>
    [HttpGet("overdue")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<BorrowRecordDto>>>> GetOverdue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _borrowService.GetOverdueRecordsAsync(page, pageSize);
        return Ok(ApiResponse<List<BorrowRecordDto>>.Ok(result));
    }
}
