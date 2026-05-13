using System.Text;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Middleware;
using LibraryManagement.Api.Models.Entities;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// ================================================================
// Program.cs —— ASP.NET Core 应用的入口点（.NET 6+ 的 Minimal Hosting 模式）
//
// 这个文件替代了传统的 Program.cs + Startup.cs 两个文件
// WebApplication 是 .NET 6+ 引入的简化启动方式
// ================================================================

var builder = WebApplication.CreateBuilder(args);  // 创建 Web 应用构建器

// ==================== 服务注册区域 ====================
// AddDbContext：注册数据库上下文到 DI 容器
// options.UseSqlServer：指定使用 SQL Server 数据库提供程序
// 连接字符串从 appsettings.json 的 ConnectionStrings:DefaultConnection 读取
// 生命周期默认是 Scoped（每个 HTTP 请求一个实例）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- JWT 认证配置 ----
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

// AddAuthentication：注册认证服务
// JwtBearerDefaults.AuthenticationScheme：告诉框架使用 JWT Bearer 认证方案
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // TokenValidationParameters：配置 JWT 的验证规则
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,           // 验证签发者（谁发的 Token）
        ValidateAudience = true,         // 验证受众（Token 是给谁用的）
        ValidateLifetime = true,         // 验证是否过期
        ValidateIssuerSigningKey = true,  // 验证签名（确保 Token 未被篡改）
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)  // 用于验证签名的密钥
    };
});

builder.Services.AddAuthorization();  // 注册授权服务（配合 [Authorize] 标签使用）

// ---- 业务服务注册 ----
// Singleton（单例）：整个应用生命周期内只创建一个实例，所有请求共享
//   → TokenService 是无状态的纯计算函数，线程安全，适合 Singleton
builder.Services.AddSingleton<TokenService>();

// Scoped（作用域）：每个 HTTP 请求创建一个实例，请求结束后销毁
//   → 大多数业务 Service 都是 Scoped，因为它们依赖 DbContext（也是 Scoped）
builder.Services.AddScoped<IAuthService, AuthService>();       // 接口 → 实现，方便单元测试 mock
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBorrowService, BorrowService>();

// ---- Controller 注册 ----
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 将所有 JSON 响应的属性名转为小驼峰命名（camelCase）
        // 例如：C# 的 PasswordHash (PascalCase) → JSON 的 passwordHash (camelCase)
        // 这是 JavaScript/前端约定的命名方式
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ---- Swagger 配置 ----
builder.Services.AddEndpointsApiExplorer();  // 启用 API 端点探索（用于 Swagger 生成文档）
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "图书管理系统 API", Version = "v1" });

    // 在 Swagger UI 中添加 JWT Bearer 认证支持
    // 这样开发者可以在 Swagger 页面输入 Token 来测试需要认证的接口
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",          // HTTP 请求头名称
        In = ParameterLocation.Header,   // Token 放在请求头中
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== 应用构建 ====================
var app = builder.Build();  // 将上面的所有配置编译成一个 WebApplication 实例

// ==================== 数据库初始化 & 种子数据 ====================
// CreateScope 手动创建一个 DI 作用域（因为 Program.cs 没有 HTTP 请求，不存在自动的 Scoped 生命周期）
// 应用启动时自动创建数据库（EnsureCreated）并插入初始数据（Seed Data）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // EnsureCreated：如果数据库不存在则创建（使用 EF Core 模型推导出的表结构）
    // 注意：生产环境应该用 Migration（dotnet ef migrations add / update），而不是 EnsureCreated
    db.Database.EnsureCreated();

    // ---- 种子数据：默认管理员 ----
    if (!db.Users.Any(u => u.Username == "admin"))
    {
        db.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            Name = "系统管理员",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
    }

    // ---- 种子数据：默认读者 ----
    if (!db.Users.Any(u => u.Username == "reader"))
    {
        db.Users.Add(new User
        {
            Username = "reader",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Reader@123456"),
            Name = "张三",
            Phone = "13800138000",
            Email = "zhangsan@example.com",
            Role = "Reader",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
    }

    // ---- 种子数据：8 本默认图书 ----
    if (!db.Books.Any())
    {
        var books = new List<Book>
        {
            new() { ISBN = "978-7-111-11111-1", Title = "C#程序设计基础", Author = "李伟", Publisher = "机械工业出版社", Category = "计算机", Price = 59.00m, TotalCopies = 5, AvailableCopies = 5, Location = "A区-1排-1层", Description = "本书全面介绍C#编程语言的基础知识和面向对象编程思想。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { ISBN = "978-7-111-11111-2", Title = "ASP.NET Core实战", Author = "王芳", Publisher = "人民邮电出版社", Category = "计算机", Price = 79.00m, TotalCopies = 3, AvailableCopies = 3, Location = "A区-1排-2层", Description = "基于.NET的Web开发实战指南，涵盖MVC、Web API、EF Core等核心技术。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { ISBN = "978-7-111-11111-3", Title = "数据库系统概论", Author = "刘明", Publisher = "高等教育出版社", Category = "计算机", Price = 45.00m, TotalCopies = 4, AvailableCopies = 4, Location = "A区-1排-3层", Description = "全面讲述数据库系统的基本概念、原理和技术。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { ISBN = "978-7-111-11111-4", Title = "红楼梦", Author = "曹雪芹", Publisher = "人民文学出版社", Category = "文学", Price = 68.00m, TotalCopies = 6, AvailableCopies = 6, Location = "B区-2排-1层", Description = "中国古典四大名著之一，以贾宝玉、林黛玉的爱情悲剧为主线。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { ISBN = "978-7-111-11111-5", Title = "百年孤独", Author = "加西亚·马尔克斯", Publisher = "南海出版公司", Category = "文学", Price = 55.00m, TotalCopies = 4, AvailableCopies = 4, Location = "B区-2排-2层", Description = "魔幻现实主义代表作，讲述布恩迪亚家族七代人的传奇故事。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { ISBN = "978-7-111-11111-6", Title = "全球通史", Author = "斯塔夫里阿诺斯", Publisher = "北京大学出版社", Category = "历史", Price = 88.00m, TotalCopies = 3, AvailableCopies = 3, Location = "C区-3排-1层", Description = "全球视角下的世界历史，从史前到21世纪。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { ISBN = "978-7-111-11111-7", Title = "时间简史", Author = "斯蒂芬·霍金", Publisher = "湖南科学技术出版社", Category = "科学", Price = 45.00m, TotalCopies = 5, AvailableCopies = 5, Location = "C区-3排-2层", Description = "霍金科普经典，从大爆炸到黑洞的时空探索。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { ISBN = "978-7-111-11111-8", Title = "三体", Author = "刘慈欣", Publisher = "重庆出版社", Category = "科幻", Price = 93.00m, TotalCopies = 5, AvailableCopies = 5, Location = "D区-4排-1层", Description = "中国科幻文学的里程碑，雨果奖获奖作品。", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        db.Books.AddRange(books);  // AddRange 批量添加（一条 INSERT 语句插入多行）
    }

    db.SaveChanges();  // 将所有挂起的种子数据一次性写入数据库
}

// ==================== 中间件管道配置 ====================
// 中间件执行顺序：按 UseXxx 的调用顺序从上到下执行
// 请求 → ExceptionMiddleware → Swagger → Authentication → Authorization → Controllers

// 全局异常处理 —— 必须放在最前面（成为管道的"最外层洋葱皮"）
app.UseMiddleware<ExceptionMiddleware>();

// Swagger 中间件（仅在开发环境有效，但这里没有加环境判断）
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "图书管理系统 API v1");
});

// 认证中间件：解析请求头中的 JWT Token，填充 HttpContext.User
// 必须放在 Authorization 之前
app.UseAuthentication();

// 授权中间件：根据 [Authorize] 标签校验当前 User 是否有权访问
app.UseAuthorization();

// 将 Controller 中的路由映射到 HTTP 请求
// 这是管道的终点——如果到这里没有匹配的路由，就返回 404
app.MapControllers();

// ==================== 启动应用 ====================
app.Run();  // 开始监听 HTTP 请求（默认 http://localhost:5000）
