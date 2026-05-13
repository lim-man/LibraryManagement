using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagement.Api.Services;

/// <summary>
/// JWT Token 生成服务 —— 注册为 Singleton（整个应用共享一个实例）
/// Singleton 因为是纯函数（无状态，相同输入永远得到相同结构），线程安全
///
/// JWT（JSON Web Token）由三部分组成（用 . 分隔）：
///   Header.Payload.Signature
///   Header  = 算法信息（HS256）
///   Payload = 声明（Claims），如用户 ID、用户名、角色
///   Signature = 对前两部分的签名，防止篡改
/// </summary>
public class TokenService
{
    private readonly IConfiguration _config;

    // IConfiguration 是 .NET 内置的配置读取接口
    // 可以读取 appsettings.json、环境变量、命令行参数等
    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// 生成 JWT Token
    /// 返回值用元组（Tuple），C# 7.0+ 语法糖，等价于返回一个包含 token 和 expiresAt 的结构
    /// </summary>
    public (string token, DateTime expiresAt) GenerateToken(int userId, string username, string role)
    {
        // 从 appsettings.json 的 JwtSettings 节点读取配置
        var jwtSettings = _config.GetSection("JwtSettings");

        // 用密钥创建对称签名密钥（HMAC-SHA256）
        // 对称加密：签发和验证使用同一个密钥
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 设置过期时间
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationInMinutes"]!));

        // Claim（声明）是 JWT 中存储用户信息的方式，以键值对形式存在
        // ClaimTypes 是 .NET 定义的标准声明类型常量
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),  // "sub" 的另一种表示 → 用户唯一标识
            new Claim(ClaimTypes.Name, username),                      // 用户名
            new Claim(ClaimTypes.Role, role)                           // 角色（Admin / Reader）
        };

        // 组装 JWT Token
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],              // 签发者
            audience: jwtSettings["Audience"],          // 受众
            claims: claims,                              // 携带的声明
            expires: expires,                            // 过期时间
            signingCredentials: credentials              // 签名凭证
        );

        // 将 JwtSecurityToken 对象序列化为字符串（就是那串 Base64 编码的 Token）
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
