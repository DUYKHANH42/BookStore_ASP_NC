# 📋 BookStore Refactoring Blueprint — Copy-Paste cho AI Model khác

> **Mục đích**: Tài liệu này chứa **tất cả context + hướng dẫn kỹ thuật chi tiết** để bạn copy-paste vào một AI model khác (Claude, GPT, Gemini...) và model đó code được ngay **không cần hỏi thêm**. Chia thành các **task độc lập**, mỗi task là 1 prompt riêng.

> [!IMPORTANT]
> **Chiến lược tiết kiệm token**: Chia nhỏ thành 7 task độc lập → mỗi lần chỉ paste 1 task + phần "BỐI CẢNH CHUNG" → model nhận đủ context mà không tốn token dư thừa.

---

## BỐI CẢNH CHUNG (paste kèm mỗi task)

```
PROJECT: BookStore ASP.NET Core — Clean Architecture 4 layer
- BookStore.Domain: Entities, Interfaces (IGenericRepository, IUnitOfWork, IOrderRepository...)
- BookStore.Application: Services (OrderService, ProductService...), DTOs, Interfaces
- BookStore.Infrastructure: Repositories, Persistence (BookStoreDbContext : IdentityDbContext), Services
- BookStore.API: Controllers, Startup.cs, Middleware, Hubs

TECH: .NET (ASP.NET Core), EF Core, SQL Server, Identity, SignalR, Redis, QuestPDF
PAYMENT: VNPay, ZaloPay, PayOS (3 cổng thanh toán)
FRONTEND: Angular (repo riêng BookStore_GiaoDien, không nằm trong scope)

CẤU TRÚC HIỆN TẠI:
- GenericRepository<T> : IGenericRepository<T> — GetAllAsync() trả IEnumerable<T> (đã ToListAsync)
- UnitOfWork chứa tất cả repository properties
- OrderService (567 dòng) là God Class: 6 dependency injection
- Domain entities thuần POCO, không có behavior
- DbContext kế thừa IdentityDbContext<ApplicationUser>
```

---

## TASK 1: BẢO MẬT — XỬ LÝ TRƯỚC TIÊN

### Prompt copy-paste cho model:

```
Tôi có project ASP.NET Core BookStore. Giúp tôi sửa các lỗi bảo mật sau.

[Paste BỐI CẢNH CHUNG ở trên]

### Yêu cầu cụ thể:

**1.1. Tạo appsettings.Template.json** (thay thế appsettings.json)
- Copy cấu trúc từ appsettings.json hiện tại nhưng thay TẤT CẢ giá trị nhạy cảm bằng placeholder
- File hiện tại (BookStore.API/appsettings.json) chứa:
  - ConnectionStrings.DefaultConnection: "Server=DUYKHANH\\DUYKHANH;Database=BookStoreDb;User Id=sa;Password=sa;..."
  - ConnectionStrings.Redis: "redis-18592.crce302...password=araB9NWC5hFeiYyocciIMQQ57l76xDKU..."
  - JWT.Secret: "Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_2024_@123"
  - MailSettings.Password: "upjw zqhr ajsv fpxp"
  - ZaloPay.Key1, Key2
  - PayOS.ApiKey, ChecksumKey  
  - VnPay.HashSecret
  - Cloudinary.ApiSecret
- Tạo file mới BookStore.API/appsettings.Template.json với placeholder dạng "<YOUR_xxx_HERE>"
- QUAN TRỌNG: Không xóa file appsettings.json gốc (tôi tự xóa sau khi setup xong)

**1.2. Sửa Startup.cs dòng 103** — bỏ fallback JWT secret
```csharp
// HIỆN TẠI (dòng 103):
var key = System.Text.Encoding.UTF8.GetBytes(Configuration["JWT:Secret"] ?? "Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_123");

// SỬA THÀNH:
var jwtSecret = Configuration["JWT:Secret"] 
    ?? throw new InvalidOperationException("JWT:Secret is not configured. Application cannot start without a valid JWT signing key.");
var key = System.Text.Encoding.UTF8.GetBytes(jwtSecret);
```

**1.3. Sửa CORS trong Startup.cs dòng 153-167**
```csharp
// HIỆN TẠI:
services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        builder => builder
            .WithOrigins("http://localhost:4200", "http://localhost:53214", "https://book-lumen.vercel.app/")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(options => true));  // <-- XÓA DÒNG NÀY
});

// SỬA THÀNH (đọc whitelist từ config):
var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:4200" };
services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        builder => builder
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    // KHÔNG CÓ SetIsOriginAllowed
});
```
Thêm vào appsettings.Template.json:
```json
"Cors": {
  "AllowedOrigins": ["http://localhost:4200", "https://your-production-domain.com"]
}
```

**1.4. Sửa Cookie SameSite trong Startup.cs dòng 109-118**
```csharp
// Đổi SameSite từ None sang Lax:
options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
// Có thể bỏ SecurePolicy.Always nếu dev trên HTTP:
options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
```

**1.5. Swagger chỉ bật ở Development** — Startup.cs dòng 211-212
```csharp
// HIỆN TẠI (dòng 211-212, nằm NGOÀI if block):
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookStore.API v1"));

// SỬA: di chuyển vào trong if (env.IsDevelopment()) block:
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookStore.API v1"));
}
```

**1.6. Thêm vào .gitignore** (file ở root project):
```
# Secrets
BookStore.API/appsettings.json
BookStore.API/appsettings.*.json
!BookStore.API/appsettings.Template.json
```

### Output mong muốn:
- File mới: BookStore.API/appsettings.Template.json
- File sửa: BookStore.API/Startup.cs (4 chỗ sửa: JWT fail-fast, CORS, Cookie SameSite, Swagger)
- File sửa: .gitignore (thêm 3 dòng)
```

---

## TASK 2: SỬA PERFORMANCE — PHÂN TRANG & N+1

### Prompt copy-paste cho model:

```
Tôi có project ASP.NET Core BookStore. Giúp tôi sửa 2 lỗi Critical về performance.

[Paste BỐI CẢNH CHUNG ở trên]

### CODE HIỆN TẠI cần biết:

**IOrderRepository.cs** (BookStore.Domain/Interfaces/):
```csharp
public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetOrderByIdWithDetailsAsync(int orderId);
    Task<IEnumerable<Order>> GetUserOrderHistoryAsync(string userId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task UpdateStatusAsync(int orderId, OrderStatus status);
    Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<bool> HasPurchasedProductAsync(string userId, int productId);
}
```

**IGenericRepository.cs**:
```csharp
public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    IQueryable<T> GetQueryable();
}
```

**GenericRepository.cs** (BookStore.Infrastructure/Repositories/):
```csharp
public async Task<IEnumerable<T>> GetAllAsync()
    => await _context.Set<T>().ToListAsync();  // <-- VẤN ĐỀ: load toàn bộ bảng
```

**OrderRepository.cs** (hiện tại):
```csharp
public async Task<Order?> GetOrderByIdWithDetailsAsync(int orderId)
{
    return await _context.Orders
        .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
        .Include(o => o.User)
        .FirstOrDefaultAsync(o => o.Id == orderId);
}

public async Task<IEnumerable<Order>> GetUserOrderHistoryAsync(string userId)
{
    return await _context.Orders
        .Where(o => o.UserId == userId)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();  // <-- Load hết, không phân trang
}
```

### Yêu cầu cụ thể:

**2.1. Thêm method phân trang vào IOrderRepository** — phân trang tại DB level:
```csharp
// Thêm vào IOrderRepository:
Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedOrdersAsync(
    int page, int pageSize, OrderStatus? status = null, string? search = null);

Task<(IEnumerable<Order> Items, int TotalCount)> GetUserOrdersPagedAsync(
    string userId, int page, int pageSize);

Task<IEnumerable<Order>> GetOrdersForReportAsync(OrderStatus? status = null, string? search = null);
```

**2.2. Implement trong OrderRepository.cs**:
```csharp
// GetPagedOrdersAsync — filter + paging tại DB
public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedOrdersAsync(
    int page, int pageSize, OrderStatus? status = null, string? search = null)
{
    var query = _context.Orders.AsNoTracking().AsQueryable();
    
    if (status.HasValue) 
        query = query.Where(o => o.Status == status.Value);
    if (!string.IsNullOrEmpty(search)) 
        query = query.Where(o => o.OrderNumber.Contains(search));
    
    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(o => o.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return (items, totalCount);
}

// GetUserOrdersPagedAsync — phân trang cho user
public async Task<(IEnumerable<Order> Items, int TotalCount)> GetUserOrdersPagedAsync(
    string userId, int page, int pageSize)
{
    var query = _context.Orders
        .AsNoTracking()
        .Where(o => o.UserId == userId);
    
    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(o => o.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return (items, totalCount);
}

// GetOrdersForReportAsync — 1 query thay vì N+1
public async Task<IEnumerable<Order>> GetOrdersForReportAsync(
    OrderStatus? status = null, string? search = null)
{
    var query = _context.Orders
        .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
        .Include(o => o.User)
        .AsNoTracking()
        .AsQueryable();
    
    if (status.HasValue) 
        query = query.Where(o => o.Status == status.Value);
    if (!string.IsNullOrEmpty(search)) 
        query = query.Where(o => o.OrderNumber.Contains(search));
    
    return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
}
```

**2.3. Sửa OrderService.cs** — 3 method cần sửa:

Method 1: `GetPagedOrdersAsync` (dòng 268-310) — thay thế:
```csharp
public async Task<PagedResultDTO<OrderDTO>> GetPagedOrdersAsync(int page, int pageSize, string status = "", string search = "")
{
    OrderStatus? orderStatus = null;
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        orderStatus = parsed;

    var (items, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(
        page, pageSize, orderStatus, string.IsNullOrEmpty(search) ? null : search);

    var orders = items.Select(o => new OrderDTO
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        TotalPrice = o.TotalPrice,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt
    }).ToList();

    return new PagedResultDTO<OrderDTO>
    {
        Items = orders,
        TotalItems = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        CurrentPage = page,
        PageSize = pageSize
    };
}
```

Method 2: `GetUserOrdersPagedAsync` (dòng 341-366) — thay thế:
```csharp
public async Task<PagedResultDTO<OrderDTO>> GetUserOrdersPagedAsync(string userId, int page = 1, int pageSize = 5)
{
    var (items, totalCount) = await _unitOfWork.Orders.GetUserOrdersPagedAsync(userId, page, pageSize);

    var orders = items.Select(o => new OrderDTO
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        TotalPrice = o.TotalPrice,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt
    }).ToList();

    return new PagedResultDTO<OrderDTO>
    {
        Items = orders,
        TotalItems = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        CurrentPage = page,
        PageSize = pageSize
    };
}
```

Method 3: `GetAllOrdersForReportAsync` (dòng 388-435) — thay thế:
```csharp
public async Task<IEnumerable<OrderFullDetailDTO>> GetAllOrdersForReportAsync(string status = "", string search = "")
{
    OrderStatus? orderStatus = null;
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        orderStatus = parsed;

    var orders = await _unitOfWork.Orders.GetOrdersForReportAsync(
        orderStatus, string.IsNullOrEmpty(search) ? null : search);

    return orders.Select(o => new OrderFullDetailDTO
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        UserId = o.UserId ?? "Guest",
        CreatedAt = o.CreatedAt,
        Status = o.Status.ToString(),
        TotalPrice = o.TotalPrice,
        PaymentMethod = o.PaymentMethod.ToString(),
        ShippingName = o.ShippingName,
        ShippingPhone = o.ShippingPhone,
        ShippingAddress = o.ShippingAddress,
        Items = o.OrderDetails.Select(od => new OrderItemDetailDTO
        {
            ProductId = od.ProductId,
            ProductName = od.Product?.Name ?? "N/A",
            Price = od.Price,
            Quantity = od.Quantity
        }).ToList()
    }).ToList();
}
```

**2.4. Thêm AsNoTracking cho ProductRepository.cs** dòng 30:
```csharp
// THÊM .AsNoTracking() sau .AsQueryable():
var result = _context.Products
    .Include(b => b.Category)
    .Include(b => b.SubCategory)
    .Include(b => b.FlashSales).ThenInclude(f => f.FlashSaleCampaign)
    .AsNoTracking()   // <-- THÊM DÒNG NÀY
    .AsQueryable();
```

Cũng thêm .AsNoTracking() cho GetOrderByIdWithDetailsAsync trong OrderRepository.cs khi dùng cho hiển thị (tạo overload):
```csharp
public async Task<Order?> GetOrderByIdReadOnlyAsync(int orderId)
{
    return await _context.Orders
        .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
        .Include(o => o.User)
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.Id == orderId);
}
```

### Output mong muốn:
- File sửa: BookStore.Domain/Interfaces/IOrderRepository.cs (thêm 3 method)
- File sửa: BookStore.Infrastructure/Repositories/OrderRepository.cs (implement 3 method + 1 overload)
- File sửa: BookStore.Application/Services/OrderService.cs (sửa 3 method)
- File sửa: BookStore.Infrastructure/Repositories/ProductRepository.cs (thêm AsNoTracking)
```

---

## TASK 3: CONCURRENCY CONTROL — CHỐNG OVERSELL

### Prompt copy-paste cho model:

```
Tôi có project ASP.NET Core BookStore dùng EF Core + SQL Server. 
Giúp tôi thêm concurrency control cho tồn kho sản phẩm để chống oversell.

[Paste BỐI CẢNH CHUNG ở trên]

### CODE HIỆN TẠI:

**Product.cs** (BookStore.Domain/Entities/):
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public int Quantity { get; set; }  // <-- KHÔNG CÓ CONCURRENCY TOKEN
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? SKU { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int? SubCategoryId { get; set; }
    public SubCategory? SubCategory { get; set; }
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public virtual ICollection<FlashSale> FlashSales { get; set; } = new List<FlashSale>();
}
```

**OrderService.cs** trừ kho hiện tại (dòng 56-107 trong PlaceOrderAsync):
```csharp
var product = item.Product;
if (product.Quantity < item.Quantity)
    throw new Exception($"Sản phẩm {product.Name} không đủ số lượng trong kho.");
// ... 
product.Quantity -= item.Quantity;  // <-- RACE CONDITION
```

Cùng pattern xảy ra ở `CreatePOSOrderAsync` dòng 512-534.

**BookStoreDbContext.cs** (BookStore.Infrastructure/Persistence/):
```csharp
public class BookStoreDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    // ... DbSet properties ...
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookStoreDbContext).Assembly);
    }
}
```

### Yêu cầu cụ thể:

**3.1. Thêm RowVersion vào Product entity**:
```csharp
// Thêm vào cuối class Product:
[Timestamp]
public byte[] RowVersion { get; set; } = null!;
```

**3.2. Cấu hình trong DbContext (hoặc tạo ProductConfiguration)**:
Tạo file mới BookStore.Infrastructure/Persistence/Configurations/ProductConfiguration.cs:
```csharp
using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.RowVersion)
            .IsRowVersion();
    }
}
```

**3.3. Tạo EF Core Migration**:
Chạy lệnh: `dotnet ef migrations add AddProductRowVersion -p BookStore.Infrastructure -s BookStore.API`

**3.4. Sửa OrderService.PlaceOrderAsync** — thêm retry logic:
Wrap phần trừ kho trong try-catch DbUpdateConcurrencyException với retry (tối đa 3 lần).

Pattern:
```csharp
// Trong PlaceOrderAsync, sau khi tính xong order, trước SaveChangesAsync:
try
{
    await _unitOfWork.SaveChangesAsync();
}
catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
{
    // Reload entities và retry (hoặc throw business exception rõ ràng)
    throw new Exception("Sản phẩm vừa được cập nhật bởi giao dịch khác. Vui lòng thử lại.");
}
```

Hoặc tốt hơn — tách thành helper method:
```csharp
// Thêm private method vào OrderService:
private async Task DecreaseStockWithConcurrencyAsync(Product product, int quantity, string orderNumber, string changedBy)
{
    if (product.Quantity < quantity)
        throw new Exception($"Sản phẩm {product.Name} không đủ số lượng trong kho.");
    
    product.Quantity -= quantity;
    
    await _unitOfWork.StockHistories.AddAsync(new StockHistory
    {
        ProductId = product.Id,
        ChangeQuantity = -quantity,
        Reason = $"Bán hàng (Đơn hàng {orderNumber})",
        CreatedAt = TimeHelper.GetVnTime(),
        ChangedBy = changedBy
    });
}
```

**3.5. Áp dụng cùng pattern cho CreatePOSOrderAsync** (dòng 485-563).

### Output mong muốn:
- File sửa: BookStore.Domain/Entities/Product.cs (thêm RowVersion property)
- File mới: BookStore.Infrastructure/Persistence/Configurations/ProductConfiguration.cs
- File sửa: BookStore.Application/Services/OrderService.cs (PlaceOrderAsync + CreatePOSOrderAsync)
- Lệnh migration cần chạy
```

---

## TASK 4: CUSTOM EXCEPTIONS — THAY THẾ GENERIC EXCEPTION

### Prompt copy-paste cho model:

```
Tôi có project ASP.NET Core BookStore. Giúp tôi tạo custom exception hierarchy thay cho generic Exception.

[Paste BỐI CẢNH CHUNG ở trên]

### VẤN ĐỀ HIỆN TẠI:
- OrderService.cs throw generic Exception cho lỗi nghiệp vụ: `throw new Exception("Sản phẩm ... không đủ số lượng")`
- ExceptionMiddleware.cs catch tất cả Exception và trả 500 Internal Server Error
- Không phân biệt được lỗi nghiệp vụ (400) với lỗi hệ thống thật (500)

**ExceptionMiddleware.cs hiện tại**:
```csharp
public async Task InvokeAsync(HttpContext context)
{
    try { await _next(context); }
    catch (Exception ex)
    {
        _logger.LogError(ex, ex.Message);
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // Luôn 500
        // ...
    }
}
```

### Yêu cầu cụ thể:

**4.1. Tạo custom exceptions** — File mới: BookStore.Domain/Common/Exceptions.cs
```csharp
namespace BookStore.Domain.Common;

// Base cho lỗi nghiệp vụ (trả 400)
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

// Không tìm thấy resource (trả 404)
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key) 
        : base($"{entityName} with key '{key}' was not found.") { }
}

// Lỗi validation (trả 400)  
public class ValidationException : BusinessException
{
    public ValidationException(string message) : base(message) { }
}

// Lỗi conflict/concurrency (trả 409)
public class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message) { }
}

// Lỗi unauthorized (trả 403)
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to perform this action.") 
        : base(message) { }
}

// Lỗi hết hàng — dùng cho race condition trừ kho
public class InsufficientStockException : BusinessException
{
    public string ProductName { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(string productName, int requested, int available)
        : base($"Sản phẩm {productName} không đủ số lượng. Yêu cầu: {requested}, Còn: {available}")
    {
        ProductName = productName;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}
```

**4.2. Sửa ExceptionMiddleware.cs** — phân biệt status code:
```csharp
public async Task InvokeAsync(HttpContext context)
{
    try { await _next(context); }
    catch (Exception ex)
    {
        var (statusCode, logLevel) = ex switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, LogLevel.Warning),
            BusinessException => ((int)HttpStatusCode.BadRequest, LogLevel.Warning),
            ConflictException => ((int)HttpStatusCode.Conflict, LogLevel.Warning),
            ForbiddenException => ((int)HttpStatusCode.Forbidden, LogLevel.Warning),
            _ => ((int)HttpStatusCode.InternalServerError, LogLevel.Error)
        };

        _logger.Log(logLevel, ex, ex.Message);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = _env.IsDevelopment()
            ? new ApiException(statusCode, ex.Message, ex.StackTrace?.ToString())
            : new ApiException(statusCode, statusCode == 500 ? "Internal Server Error" : ex.Message);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
```
Thêm `using BookStore.Domain.Common;` ở đầu file.

**4.3. Thay thế generic Exception trong OrderService.cs**:
Các chỗ cần sửa (search "throw new Exception" trong file):
- Dòng 61: `throw new Exception($"Sản phẩm {product.Name} không đủ số lượng...")` 
  → `throw new InsufficientStockException(product.Name, item.Quantity, product.Quantity);`
- Dòng 510: `throw new Exception($"Không tìm thấy sản phẩm ID {item.ProductId}")` 
  → `throw new NotFoundException("Product", item.ProductId);`
- Dòng 513: `throw new Exception($"Sản phẩm {product.Name} không đủ số lượng...")` 
  → `throw new InsufficientStockException(product.Name, item.Quantity, product.Quantity);`
- Dòng 529: `throw new Exception($"Sản phẩm '{product.Name}' đã hết suất Flash Sale.")` 
  → `throw new BusinessException($"Sản phẩm '{product.Name}' đã hết suất Flash Sale.");`

### Output mong muốn:
- File mới: BookStore.Domain/Common/Exceptions.cs
- File sửa: BookStore.API/Middleware/ExceptionMiddleware.cs
- File sửa: BookStore.Application/Services/OrderService.cs (thay 4 chỗ throw)
```

---

## TASK 5: DATABASE INDEX — THÊM INDEX CHO QUERY THƯỜNG DÙNG

### Prompt copy-paste cho model:

```
Tôi có project ASP.NET Core BookStore dùng EF Core + SQL Server.
Giúp tôi thêm database index cho các cột thường dùng trong filter/search.

[Paste BỐI CẢNH CHUNG ở trên]

### THÔNG TIN:
- DbContext ở: BookStore.Infrastructure/Persistence/BookStoreDbContext.cs
- Đã có ApplyConfigurationsFromAssembly trong OnModelCreating
- Thư mục Configurations: BookStore.Infrastructure/Persistence/Configurations/
- Hiện chỉ có 2 file: FavoriteConfiguration.cs, ProductImageConfiguration.cs

### Yêu cầu: Tạo các EntityTypeConfiguration cho index

**5.1. OrderConfiguration.cs** (file mới):
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Index cho filter theo status (dùng trong GetPagedOrdersAsync)
        builder.HasIndex(o => o.Status).HasDatabaseName("IX_Orders_Status");
        
        // Index cho sort theo ngày tạo (hầu hết query đều OrderByDescending CreatedAt)
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_Orders_CreatedAt");
        
        // Index cho search theo OrderNumber
        builder.HasIndex(o => o.OrderNumber).HasDatabaseName("IX_Orders_OrderNumber");
        
        // Composite index cho filter + sort phổ biến nhất
        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_Orders_Status_CreatedAt");
        
        // Index cho lấy order theo user
        builder.HasIndex(o => o.UserId).HasDatabaseName("IX_Orders_UserId");

        // Cascade behavior tường minh
        builder.HasMany(o => o.OrderDetails)
            .WithOne(od => od.Order)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**5.2. ProductIndexConfiguration.cs** (file mới — tách riêng vì ProductConfiguration có thể đã có từ Task 3):
Nếu đã tạo ProductConfiguration ở Task 3, thêm vào đó. Nếu chưa, tạo mới:
```csharp
// Thêm vào ProductConfiguration.Configure():
builder.HasIndex(p => p.CategoryId).HasDatabaseName("IX_Products_CategoryId");
builder.HasIndex(p => p.SubCategoryId).HasDatabaseName("IX_Products_SubCategoryId");
builder.HasIndex(p => p.IsActive).HasDatabaseName("IX_Products_IsActive");
builder.HasIndex(p => new { p.IsActive, p.CreatedAt })
    .HasDatabaseName("IX_Products_IsActive_CreatedAt");
```

**5.3. StockHistoryConfiguration.cs** (file mới):
```csharp
public class StockHistoryConfiguration : IEntityTypeConfiguration<StockHistory>
{
    public void Configure(EntityTypeBuilder<StockHistory> builder)
    {
        builder.HasIndex(s => s.ProductId).HasDatabaseName("IX_StockHistory_ProductId");
        builder.HasIndex(s => s.CreatedAt).HasDatabaseName("IX_StockHistory_CreatedAt");
    }
}
```

**5.4. Tạo migration**:
`dotnet ef migrations add AddDatabaseIndexes -p BookStore.Infrastructure -s BookStore.API`

### Output mong muốn:
- File mới/sửa: OrderConfiguration.cs, ProductConfiguration.cs (hoặc merge), StockHistoryConfiguration.cs
- Lệnh migration
```

---

## TASK 6: REFACTOR ORDERSERVICE — TÁCH GOD CLASS

### Prompt copy-paste cho model:

```
Tôi có project ASP.NET Core BookStore. Giúp tôi refactor OrderService God Class 
thành các service nhỏ hơn.

[Paste BỐI CẢNH CHUNG ở trên]

### OrderService HIỆN TẠI (567 dòng, 6 dependencies):
Dependencies: IUnitOfWork, IZaloPayService, IPayOSService, IVnPayService, INotificationService, UserManager<ApplicationUser>

Methods:
1. PlaceOrderAsync — tạo order, trừ kho, ghi stock log
2. ProcessCheckoutAsync — gọi PlaceOrderAsync + xử lý payment gateway + notification
3. ProcessZaloPayCallbackAsync — callback từ ZaloPay
4. UpdateOrderStatusAsync — admin cập nhật status + hoàn kho khi cancel
5. GetPagedOrdersAsync — lấy danh sách phân trang
6. GetOrderDetailsAsync — lấy chi tiết 1 order
7. GetUserOrdersPagedAsync — phân trang cho user
8. CancelOrderForUserAsync — user tự hủy
9. GetAllOrdersForReportAsync — báo cáo
10. CancelExpiredOrderAsync — job dọn order quá hạn
11. CreatePOSOrderAsync — tạo đơn POS tại cửa hàng

### KẾ HOẠCH TÁCH:

**6.1. Tách logic hoàn kho trùng lặp** — extract method:
Logic hoàn kho + hoàn flash sale lặp lại giữa `CancelExpiredOrderAsync` (dòng 448-472) 
và `UpdateOrderStatusAsync` khi cancel (dòng 230-257). Gộp thành 1 private method:

```csharp
// Thêm private method vào OrderService:
private async Task RestoreStockForCancelledOrder(Order order, string operatorName)
{
    foreach (var detail in order.OrderDetails)
    {
        var product = detail.Product ?? await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
        if (product == null) continue;

        product.Quantity += detail.Quantity;

        if (detail.FlashSaleId.HasValue)
        {
            var flashSale = await _unitOfWork.FlashSales.GetByIdAsync(detail.FlashSaleId.Value);
            if (flashSale != null)
                flashSale.SoldCount = Math.Max(0, flashSale.SoldCount - detail.Quantity);
        }

        await _unitOfWork.StockHistories.AddAsync(new StockHistory
        {
            ProductId = product.Id,
            ChangeQuantity = detail.Quantity,
            Reason = $"Hoàn kho (Hủy đơn hàng {order.OrderNumber})",
            CreatedAt = TimeHelper.GetVnTime(),
            ChangedBy = operatorName
        });
    }
}
```

Sau đó sửa `UpdateOrderStatusAsync`:
```csharp
if (newStatus == OrderStatus.Cancelled)
{
    await RestoreStockForCancelledOrder(order, operatorName);
}
```

Và sửa `CancelExpiredOrderAsync`:
```csharp
// Thay toàn bộ foreach loop (dòng 449-472) bằng:
await RestoreStockForCancelledOrder(order, "System-Cleanup");
```

**6.2. Tạo PaymentStrategyFactory** — thay if/else chain:

File mới: BookStore.Application/Interfaces/IPaymentGateway.cs
```csharp
namespace BookStore.Application.Interfaces;

public interface IPaymentGateway
{
    PaymentMethod SupportedMethod { get; }
    Task<string?> CreatePaymentAsync(int orderId, decimal amount, string orderNumber, 
        Microsoft.AspNetCore.Http.HttpContext? httpContext = null);
}
```

File mới: BookStore.Application/Services/Payment/ZaloPayGateway.cs
```csharp
namespace BookStore.Application.Services.Payment;

public class ZaloPayGateway : IPaymentGateway
{
    private readonly IZaloPayService _zaloPayService;
    public ZaloPayGateway(IZaloPayService zaloPayService) => _zaloPayService = zaloPayService;
    public PaymentMethod SupportedMethod => PaymentMethod.ZaloPay;
    
    public async Task<string?> CreatePaymentAsync(int orderId, decimal amount, string orderNumber, 
        Microsoft.AspNetCore.Http.HttpContext? httpContext = null)
    {
        return await _zaloPayService.CreateOrderAsync(orderId, amount, orderNumber);
    }
}
```

Tương tự cho VNPayGateway.cs và PayOSGateway.cs.

File mới: BookStore.Application/Services/Payment/PaymentGatewayFactory.cs
```csharp
namespace BookStore.Application.Services.Payment;

public class PaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    
    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways;
    }
    
    public IPaymentGateway? GetGateway(PaymentMethod method)
    {
        return _gateways.FirstOrDefault(g => g.SupportedMethod == method);
    }
}
```

DI registration trong Startup.cs:
```csharp
services.AddScoped<IPaymentGateway, ZaloPayGateway>();
services.AddScoped<IPaymentGateway, VNPayGateway>();
services.AddScoped<IPaymentGateway, PayOSGateway>();
services.AddScoped<PaymentGatewayFactory>();
```

Sau đó trong OrderService.ProcessCheckoutAsync, thay if/else chain:
```csharp
// THAY THẾ toàn bộ if/else if chain (dòng 138-160) bằng:
if (checkoutDto.PaymentMethod != PaymentMethod.COD)
{
    var gateway = _paymentGatewayFactory.GetGateway(checkoutDto.PaymentMethod);
    if (gateway == null)
    {
        await _unitOfWork.RollbackAsync();
        return new CheckoutResultDTO { Success = false, Message = $"Phương thức {checkoutDto.PaymentMethod} không được hỗ trợ." };
    }
    paymentUrl = await gateway.CreatePaymentAsync(orderDto.Id, orderDto.TotalPrice, orderDto.OrderNumber, httpContext);
    if (string.IsNullOrEmpty(paymentUrl))
    {
        await _unitOfWork.RollbackAsync();
        return new CheckoutResultDTO { Success = false, Message = $"Không thể khởi tạo giao dịch {checkoutDto.PaymentMethod}." };
    }
}
```

### Output mong muốn:
- File sửa: BookStore.Application/Services/OrderService.cs (extract method + inject factory)
- File mới: BookStore.Application/Interfaces/IPaymentGateway.cs
- File mới: BookStore.Application/Services/Payment/ZaloPayGateway.cs
- File mới: BookStore.Application/Services/Payment/VNPayGateway.cs  
- File mới: BookStore.Application/Services/Payment/PayOSGateway.cs
- File mới: BookStore.Application/Services/Payment/PaymentGatewayFactory.cs
- File sửa: BookStore.API/Startup.cs (DI registration)
```

---

## TASK 7: LOGGING + DEAD CODE CLEANUP

### Prompt copy-paste cho model:

```
Tôi có project ASP.NET Core BookStore. Giúp tôi:
1. Thay Console.WriteLine bằng ILogger
2. Xóa dead code
3. Thay magic strings bằng constants

[Paste BỐI CẢNH CHUNG ở trên]

### Yêu cầu cụ thể:

**7.1. AuthService.cs** — thay Console.WriteLine bằng ILogger
Tìm mọi chỗ `Console.WriteLine` trong BookStore.Application/Services/AuthService.cs
và thay bằng `_logger.LogError(...)` hoặc `_logger.LogWarning(...)`.
- Inject ILogger<AuthService> qua constructor nếu chưa có.

**7.2. Xóa test rỗng**
File: BookStore.Tests/UnitTest1.cs — xóa toàn bộ file này (test rỗng, không có assertion).

**7.3. Magic strings → Constants**
File có sẵn: BookStore.Domain/Common/OrderConstants.cs:
```csharp
public static class OrderStatusConstants
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
    public const string Shipping = "Shipping";
    public const string Completed = "Completed";
}
```

Thêm constants mới vào file này (hoặc tạo file constants riêng):
```csharp
public static class StockConstants
{
    public const string SystemCleanup = "System-Cleanup";
    public const string AdminOperator = "Admin";
}
```

Sau đó search trong OrderService.cs các magic strings:
- "System-Cleanup" (dòng 468) → StockConstants.SystemCleanup
- "Admin" trong các chỗ mặc định → StockConstants.AdminOperator

### Output mong muốn:
- File sửa: BookStore.Application/Services/AuthService.cs (Console.WriteLine → ILogger)
- File xóa: BookStore.Tests/UnitTest1.cs
- File sửa: BookStore.Domain/Common/OrderConstants.cs (thêm StockConstants)
- File sửa: BookStore.Application/Services/OrderService.cs (dùng constants)
```

---

## HƯỚNG DẪN SỬ DỤNG

> [!TIP]
> ### Cách sử dụng hiệu quả nhất
> 
> 1. **Luôn paste "BỐI CẢNH CHUNG"** kèm theo mỗi task prompt
> 2. **Làm theo thứ tự**: Task 1 → 2 → 3 → 4 → 5 → 6 → 7 (các task sau có thể depend vào file đã sửa ở task trước)
> 3. **Mỗi lần 1 task**: Không paste tất cả cùng lúc — tốn token vô ích
> 4. **Sau mỗi task**: Build project để kiểm tra biên dịch trước khi làm task tiếp (`dotnet build BookStore.sln`)
> 5. **Task 3** (concurrency) cần chạy migration → cần DB connection string hợp lệ
> 6. **Task 5** (index) cũng cần migration

> [!WARNING]
> ### Những gì CHƯA bao gồm (cần làm riêng sau)
> 
> - **Domain behavior** (thêm method vào entity như `Order.Cancel()`, `Product.DecreaseStock()`) — đây là refactor lớn nhất, cần plan riêng
> - **Full test coverage** — cần viết test cho ProductService, CartService, InventoryService, FlashSaleService
> - **API Versioning** — thêm `/api/v1/`
> - **Full-Text Search** — thay `LIKE '%..%'` cho search sản phẩm
> - **Anti-Forgery Token** cho Admin Area — cần kiểm tra frontend Admin
> - **IRedisService** nên di chuyển từ Domain sang Application/Infrastructure interface

### ƯỚC TÍNH TOKEN

| Task | Prompt size | Expected output | Tổng ~token |
|------|-------------|----------------|-------------|
| Task 1 (Security) | ~1.5K | ~2K | ~3.5K |
| Task 2 (Performance) | ~3K | ~3K | ~6K |
| Task 3 (Concurrency) | ~2K | ~2K | ~4K |
| Task 4 (Exceptions) | ~2K | ~2K | ~4K |
| Task 5 (Index) | ~1.5K | ~1.5K | ~3K |
| Task 6 (Refactor) | ~3K | ~4K | ~7K |
| Task 7 (Cleanup) | ~1K | ~1K | ~2K |
| **Tổng** | | | **~29.5K** |

So với paste toàn bộ code + review document mỗi lần (~20K/lần × 7 lần = 140K), tiết kiệm ~80% token.
