using System.Net;
using System.Text.Json;
using LibraryManagement.Api.Models.Common;

namespace LibraryManagement.Api.Middleware;

/// <summary>
/// 全局异常处理中间件 —— ASP.NET Core 中间件管道中的第一道关口
///
/// 中间件（Middleware）是 ASP.NET Core 的核心概念：
/// 每个 HTTP 请求会按注册顺序依次穿过所有中间件，然后反向穿过回到客户端
/// 这就像一个洋葱：
///   Request  → [Middleware1] → [Middleware2] → [Middleware3] → Controller → Response
///   Response ← [Middleware1] ← [Middleware2] ← [Middleware3] ←
///
/// 这个中间件的作用：捕获所有未处理的异常，转换为统一的 JSON 响应
/// 而不是让框架返回 500 错误页面（前端根本解析不了）
/// </summary>
public class ExceptionMiddleware
{
    // RequestDelegate 是一个委托（delegate），代表管道中的下一个中间件
    // 调用 _next(context) 就是把请求传递给下一个中间件
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 中间件的核心方法 —— 每个请求到达时都会执行
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // 先正常执行后续中间件 + Controller
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            // 业务异常（我们在 Service 层主动抛出的）→ 返回 400 Bad Request
            // 将异常消息直接返回给前端（因为这些是我们预设的友好提示）
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";
            var response = ApiResponse<object>.Fail(ex.Message);
            // JsonSerializer.Serialize 将 C# 对象序列化为 JSON 字符串
            // PropertyNamingPolicy = CamelCase 确保属性名是小驼峰（code/message/data）
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
        catch (Exception)
        {
            // 未知异常（如数据库挂了、空引用等）→ 返回 500
            // 安全要点：不暴露真实错误信息给客户端（可能泄露敏感信息）
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            var response = ApiResponse<object>.Error("服务器内部错误");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
