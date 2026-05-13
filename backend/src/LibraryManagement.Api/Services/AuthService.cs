using LibraryManagement.Api.Models.DTOs.Auth;
using LibraryManagement.Api.Models.Entities;
using LibraryManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

/// <summary>
/// 接口定义：认证相关业务
/// 为什么需要接口？—— 面向接口编程（依赖倒置原则 DIP）
/// Controller 依赖 IAuthService 接口，而不是 AuthService 实现类
/// 好处：方便单元测试（可以 mock 接口）、方便替换实现
/// </summary>
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserInfo?> GetCurrentUserAsync(int userId);
    Task<UserInfo?> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    // 构造函数注入 —— .NET 框架自动从 DI 容器中取出 AppDbContext 和 TokenService 传进来
    public AuthService(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    /// <summary>
    /// 登录逻辑：
    /// 1. 根据用户名查找用户
    /// 2. 验证是否激活
    /// 3. 用 BCrypt 验证密码（注意：是比较哈希值，不是比较明文）
    /// 4. 生成 JWT Token 返回
    /// 返回 null 表示登录失败（用户名不存在、密码错误、或用户被禁用）
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // FirstOrDefaultAsync：查找第一条匹配的记录，没找到返回 null
        // 等价 SQL: SELECT TOP 1 * FROM Users WHERE Username = @username
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !user.IsActive)
            return null;

        // BCrypt.Verify：将输入的明文密码与数据库中的哈希值比较
        // 永远不要用 == 直接比较密码或哈希值！BCrypt 内部有专门的常量时间比较算法
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // 生成 JWT（元组解构赋值 —— C# 7.0 语法）
        var (token, expiresAt) = _tokenService.GenerateToken(user.Id, user.Username, user.Role);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Role = user.Role
            }
        };
    }

    public async Task<UserInfo?> GetCurrentUserAsync(int userId)
    {
        // FindAsync 是按主键查找的快捷方法（比 FirstOrDefault 更高效）
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        return new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role
        };
    }

    public async Task<UserInfo?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        // 只更新允许修改的字段（不允许修改 Username、Role 等敏感字段）
        user.Name = request.Name;
        user.Phone = request.Phone;
        user.Email = request.Email;

        // SaveChangesAsync 将所有挂起的更改一次性写入数据库
        await _db.SaveChangesAsync();

        return new UserInfo { Id = user.Id, Username = user.Username, Name = user.Name, Role = user.Role };
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        // 先验证旧密码是否正确（安全策略：修改密码前必须确认身份）
        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return false;

        // BCrypt.HashPassword：生成加盐哈希值
        // 每次调用产生的哈希值都不同（因为盐值随机），所以同一个密码两次 Hash 的结果也不一样
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
        return true;
    }
}
