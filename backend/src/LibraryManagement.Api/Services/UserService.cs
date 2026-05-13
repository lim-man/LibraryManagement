using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models.Common;
using LibraryManagement.Api.Models.DTOs.Users;
using LibraryManagement.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? keyword, string? role);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ResetPasswordAsync(int id, string newPassword);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto request);
}

public class UpdateProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? keyword, string? role)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => u.Username.Contains(keyword) || u.Name.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => ToDto(u))
            .ToListAsync();

        return new PagedResult<UserDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : ToDto(user);
    }

    /// <summary>
    /// 创建用户 —— 注意：密码使用 BCrypt 哈希存储
    /// 数据库中永远不存明文密码
    /// </summary>
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),  // 哈希密码
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        // 管理员可以修改用户的姓名、电话、邮箱、角色和启用状态
        // 但不能修改用户名和密码（密码有专门的 ResetPassword 接口）
        user.Name = request.Name;
        user.Phone = request.Phone;
        user.Email = request.Email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    /// <summary>
    /// 删除用户 —— "软删除"模式（Soft Delete）
    /// 不是真的删除数据库行，而是将 IsActive 设为 false
    /// 好处：保留历史借阅记录中的用户信息完整性（外键关联不中断）
    /// 删除前校验：有未归还借阅记录的用户不能禁用
    /// </summary>
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        var hasUnreturned = await _db.BorrowRecords.AnyAsync(r => r.UserId == id && r.ReturnDate == null);
        if (hasUnreturned)
            throw new InvalidOperationException("该用户有未归还的借阅记录，无法删除");

        user.IsActive = false;  // 软删除：只改变状态标记
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 管理员重置用户密码 —— 不需要验证旧密码（管理员特权操作）
    /// </summary>
    public async Task<bool> ResetPasswordAsync(int id, string newPassword)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 用户自己修改密码 —— 必须验证旧密码（非管理员操作）
    /// 这与 AuthService.ChangePasswordAsync 功能相同，提供给不同的调用方
    /// </summary>
    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        user.Name = request.Name;
        user.Phone = request.Phone;
        user.Email = request.Email;
        await _db.SaveChangesAsync();

        return ToDto(user);
    }

    /// <summary>
    /// 实体 → DTO —— 刻意不暴露 PasswordHash
    /// 无论什么情况，密码哈希值都不应该返回给客户端
    /// </summary>
    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Name = u.Name,
        Phone = u.Phone,
        Email = u.Email,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
        // 注意：没有 PasswordHash！即使 API 返回了 UserDto，
        // 密码哈希也不会泄露到前端
    };
}
