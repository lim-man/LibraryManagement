using System.Security.Claims;
using LibraryManagement.Api.Models.Common;
using LibraryManagement.Api.Models.DTOs.Users;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]    // → "api/users"
[Authorize]                     // 整个 Controller 需要认证
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// GET api/users —— 用户列表（分页 + 搜索 + 角色筛选）
    /// 只有管理员可以查看用户列表
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? role = null)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, keyword, role);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.Fail("用户不存在"));

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        return Ok(ApiResponse<UserDto>.Ok(user, "新增用户成功"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(id, request);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.Fail("用户不存在"));

        return Ok(ApiResponse<UserDto>.Ok(user, "更新用户成功"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("用户不存在"));

        return Ok(ApiResponse<object>.Ok("删除用户成功"));
    }

    /// <summary>
    /// PUT api/users/5/reset-password —— 管理员重置用户密码（不需验证旧密码）
    /// </summary>
    [HttpPut("{id}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var result = await _userService.ResetPasswordAsync(id, request.NewPassword);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("用户不存在"));

        return Ok(ApiResponse<object>.Ok("密码重置成功"));
    }

    /// <summary>
    /// PUT api/users/change-password —— 用户自己修改密码（需要验证旧密码）
    /// 注意：这个接口不需要 Admin 角色，任何登录用户都可以操作
    /// 类上有 [Authorize]，但没有 [Authorize(Roles = "Admin")]，所以所有角色都能访问
    /// </summary>
    [HttpPut("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
        if (!result)
            return BadRequest(ApiResponse<object>.Fail("原密码错误"));

        return Ok(ApiResponse<object>.Ok("密码修改成功"));
    }
}

/// <summary>
/// 修改密码的请求体 —— 这个 DTO 只在这个文件中使用，所以定义在文件底部
/// 小型 DTO 可以就近定义，不必都放在 Models/DTOs 下
/// </summary>
public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
