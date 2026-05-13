namespace LibraryManagement.Api.Models.Common;

/// <summary>
/// 统一 API 响应格式 —— 所有接口都返回这个结构
/// 泛型 T 代表实际数据的类型，如 ApiResponse<LoginResponse>、ApiResponse<List<BookDto>>
///
/// 前端 Axios 拦截器根据 Code 判断成功与否：
///   Code == 200 → 成功，取 Data
///   Code != 200 → 失败，显示 Message
/// </summary>
public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }

    public ApiResponse(int code, string message, T? data = default)
    {
        Code = code;
        Message = message;
        Data = data;
    }

    // 静态工厂方法 —— 让调用方代码更简洁
    // 例如：return ApiResponse<BookDto>.Ok(book, "新增成功");
    // 而不用写：return new ApiResponse<BookDto>(200, "新增成功", book);

    public static ApiResponse<T> Ok(T data, string message = "操作成功")
        => new(200, message, data);

    public static ApiResponse<T> Ok(string message = "操作成功")
        => new(200, message, default);

    public static ApiResponse<T> Fail(string message, int code = 400)
        => new(code, message, default);

    public static ApiResponse<T> Error(string message = "服务器内部错误")
        => new(500, message, default);
}

/// <summary>
/// 分页结果 —— 用于需要分页的列表查询
/// TotalPages 是计算属性（只读），根据 Total 和 PageSize 自动算出
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();  // 当前页的数据
    public int Total { get; set; }               // 数据总量（用于前端显示"共 X 条"）
    public int Page { get; set; }                // 当前页码
    public int PageSize { get; set; }            // 每页条数
    // 计算属性（只有 getter，基于其他字段自动计算）
    // Math.Ceiling 向上取整，保证 (Total / PageSize) 有余数时多显示一页
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
