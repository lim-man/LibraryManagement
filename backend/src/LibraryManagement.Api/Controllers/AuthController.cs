using System.Security.Claims;
using LibraryManagement.Api.Models.Common;
using LibraryManagement.Api.Models.DTOs.Auth;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

/// <summary>
/// 认证控制器 —— 处理登录、获取当前用户信息、修改资料和密码
///
/// [ApiController] 的作用：
///   1. 自动模型验证（ModelState 校验失败自动返回 400）
///   2. 自动推断 [FromBody] 和 [FromQuery] 的绑定源
///   3. 强制使用属性路由（[Route]）
///
/// ControllerBase vs Controller：
///   ControllerBase 是轻量版（适合 API），Controller 继承自 ControllerBase + View 支持（适合 MVC）
///   做 REST API 永远用 ControllerBase
/// </summary>
[ApiController]
[Route("api/[controller]")]  // [controller] 会被替换为类名去掉 "Controller" 后的部分 → "auth"
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    // 构造函数注入 —— 依赖倒置：依赖接口 IAuthService，而非具体实现 AuthService
    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>
    /// POST api/auth/login —— 不需要认证（公开接口）
    /// [FromBody] 告诉框架从请求体中反序列化 JSON → LoginRequest 对象
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return BadRequest(ApiResponse<LoginResponse>.Fail("用户名或密码错误"));

        return Ok(ApiResponse<LoginResponse>.Ok(result, "登录成功"));
    }

    /// <summary>
    /// GET api/auth/me —— 获取当前登录用户的信息
    /// [Authorize] 表示调用此接口需要携带有效的 JWT Token
    /// User.FindFirstValue(ClaimTypes.NameIdentifier) 从 Token 中读取用户 ID
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        // 从 JWT 的 Claims 中提取用户 ID
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<UserInfo>.Fail("用户不存在"));

        return Ok(ApiResponse<UserInfo>.Ok(user));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.UpdateProfileAsync(userId, request);
        if (result == null)
            return NotFound(ApiResponse<UserInfo>.Fail("用户不存在"));

        return Ok(ApiResponse<UserInfo>.Ok(result, "修改成功"));
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.ChangePasswordAsync(userId, request);
        if (!result)
            return BadRequest(ApiResponse<object>.Fail("原密码错误"));

        return Ok(ApiResponse<object>.Ok("密码修改成功"));
    }
}
