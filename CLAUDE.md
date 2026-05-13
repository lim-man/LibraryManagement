# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 常用命令

### 后端（.NET 9 Web API）

```bash
# 启动后端（监听 http://localhost:5000）
cd backend/src/LibraryManagement.Api && dotnet run

# 编译检查
dotnet build

# Swagger 文档地址
# http://localhost:5000/swagger
```

### 前端（Vue 3 + Vite）

```bash
# 安装依赖
cd frontend && npm install

# 启动前端开发服务器（监听 http://localhost:5173）
npm run dev

# 生产构建
npm run build
```

### 数据库

- SQL Server，连接字符串在 `backend/src/LibraryManagement.Api/appsettings.json`
- 使用 Windows 认证：`Server=localhost;Database=LibraryDB;Trusted_Connection=True`
- 首次运行自动创建数据库并写入种子数据（`EnsureCreated`）
- 预置账号：`admin` / `Admin@123456`（管理员）、`reader` / `Reader@123456`（读者）

## 架构总览

```
前端 (Vue 3 + Element Plus)    后端 (.NET 9 Web API)      数据库 (SQL Server)
  port 5173                      port 5000
  Vite proxy /api → :5000       三层架构
                                Controller → Service → EF Core → DB
```

### 后端分层

| 层 | 目录 | 职责 |
|---|---|---|
| Controller | `Controllers/` | 接收 HTTP 请求，提取 JWT 身份，调用 Service，返回 `ApiResponse<T>` |
| Service | `Services/` | 全部业务逻辑（验证、事务、权限判断）。每个 Service 有接口（`IXxxService`），通过 DI 注入 |
| Data / EF Core | `Data/AppDbContext.cs` | 数据库上下文，Fluent API 配置表关系、索引、约束 |
| Entity | `Models/Entities/` | 数据库表映射（User、Book、BorrowRecord） |
| DTO | `Models/DTOs/` | 前后端数据传输格式，与 Entity 分离，不暴露敏感字段（如 PasswordHash） |
| Middleware | `Middleware/ExceptionMiddleware.cs` | 全局异常捕获：`InvalidOperationException` → 400，其他 → 500 |

### 前端分层

| 层 | 目录 | 职责 |
|---|---|---|
| Views | `src/views/` | 页面组件，按角色分目录（`admin/` 子目录为管理员页面） |
| Components | `src/components/` | 通用组件（`Layout.vue` 为主布局壳） |
| API | `src/api/` | Axios 封装，`request.js` 为实例（基地址 `/api`，自动附带 JWT Token，401 自动跳转登录） |
| Router | `src/router/index.js` | 路由定义 + `beforeEach` 守卫（Token 校验、角色校验） |
| Store | `src/stores/auth.js` | Pinia 状态管理，管理 Token/用户信息/登录登出 |

### 统一响应格式

所有 API 返回 `{ code: number, message: string, data: T }`，前端拦截器根据 `code !== 200` 判断失败。分页接口返回 `PagedResult<T>`（含 `Items`、`Total`、`Page`、`PageSize`、`TotalPages`）。

## 核心功能模块

### 1. 认证与授权
- JWT Bearer 认证（HS256），Token 有效期 120 分钟
- BCrypt 密码哈希（`BCrypt.Net.BCrypt.HashPassword` / `Verify`）
- `[Authorize]` 控制认证，`[Authorize(Roles = "Admin")]` 控制授权
- Controller 从 JWT Claims 提取 userId 和角色，传递给 Service 做权限判断

### 2. 图书管理（仅管理员）
- CRUD + 分页 + 关键词搜索（书名/作者/ISBN）+ 分类筛选
- 更新时校验：总册数不能小于已借出数量
- 删除时校验：有未归还记录则拒绝
- 分类列表接口（去重排序，供前端下拉框使用）
- 并发控制：Book 表有 `[Timestamp] RowVersion`，借书时捕获 `DbUpdateConcurrencyException`

### 3. 借阅管理
- **借书**：校验用户状态（激活且非管理员）、库存（`AvailableCopies > 0`）、不重复借阅。使用数据库事务（扣库存 + 创建记录原子操作），借期 30 天
- **还书**：校验记录状态、权限（读者只能还自己的）。事务中恢复库存 + 更新状态
- **借阅列表**：管理员可按用户/图书/状态筛选，普通读者只能看自己的
- **逾期管理**：查询 `ReturnDate IS NULL AND DueDate < now`，仅管理员可见

### 4. 用户管理（仅管理员）
- CRUD + 分页 + 搜索 + 角色筛选
- 密码重置（管理员免旧密码验证）
- 软删除：`IsActive = false`（保留借阅历史的外键关联）
- 删除前校验：有未归还记录则拒绝

### 5. 仪表盘（仅管理员）
- 直接注入 `AppDbContext` 做聚合查询（SUM/COUNT/GroupBy）
- 返回：藏书总数、可借数量、当前借出、逾期数、热门图书 TOP5、最近借阅 TOP10

### 6. 数据完整性保障
- 数据库级别：`AvailableCopies <= TotalCopies` 的 CHECK 约束
- 唯一过滤索引：`(UserId, BookId)` 在 `ReturnDate IS NULL` 时唯一（防重复借阅）
- 外键 `DeleteBehavior.Restrict`（有借阅记录时禁止删除用户/图书）
- EF Core 事务 + 并发异常回滚

## 关键设计约定

- **密码安全**：Entity/DTO 永远不包含 `PasswordHash` 在返回数据中；BCrypt 自带盐值，每次 Hash 结果不同
- **DTO 映射**：每个 Service 有私有 `ToDto()` 方法，手动逐字段映射（不用 AutoMapper）
- **异常处理**：Service 抛 `InvalidOperationException` 表达业务错误，Middleware 统一转为 400 JSON 响应
- **前端 Vite 代理**：开发时 `/api` 和 `/uploads` 请求代理到 `localhost:5000`，生产环境需 Nginx 反向代理
- **前端路由守卫**：`/admin/*` 路由仅 `userRole === 'Admin'` 可访问，但后端才是真正的权限防线
