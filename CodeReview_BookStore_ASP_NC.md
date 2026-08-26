# CODE REVIEW REPORT — BookStore_ASP_NC
**Reviewer góc nhìn:** Principal Engineer review PR trước khi merge production
**Phạm vi:** BookStore.Domain, BookStore.Application, BookStore.Infrastructure, BookStore.API, BookStore.Tests
**Ngày review:** 2026-07-16

> Ghi chú phương pháp: mọi nhận xét dưới đây trích dẫn trực tiếp từ source code đã đọc (đường dẫn file + số dòng). Những phần chưa đọc trực tiếp (ví dụ toàn bộ 60+ file DTO, một số controller Customer) sẽ được nêu rõ là "không đủ dữ liệu để kết luận" thay vì suy diễn.

---

## 1. TỔNG QUAN PROJECT

Đây là hệ thống bán sách online dạng "Clean Architecture 4 layer" (Domain / Application / Infrastructure / API) viết bằng ASP.NET Core, EF Core, Identity, SignalR, Redis, tích hợp 3 cổng thanh toán (VNPay, ZaloPay, PayOS) + Cloudinary + SendGrid/SMTP. Có frontend riêng (BookStore_GiaoDien) không thuộc phạm vi review kỹ thuật .NET này.

Về mặt tổ chức thư mục, project **trông** giống Clean Architecture, nhưng khi đọc code thực tế thì đây là kiến trúc **Layered/N-Tier truyền thống mặc áo Clean Architecture** — tách file đúng theo tên thư mục nhưng không tuân thủ nguyên tắc cốt lõi (Dependency Rule, Rich Domain Model, tách biệt business logic khỏi hạ tầng). Đây là nhận định quan trọng nhất của bài review này và sẽ được chứng minh ở mục 2-4.

---

## 2. KIẾN TRÚC

### 2.1 Clean Architecture — KHÔNG đạt

- **Domain layer hoàn toàn không có behavior**, chỉ là POCO với getter/setter (xem mục 3). Đây là dấu hiệu rõ nhất của **Anemic Domain Model**, vi phạm chính nguyên tắc mà Clean Architecture / DDD hướng tới (Domain phải chứa business rule, không phải chỉ chứa data).
- **Application layer đang gánh toàn bộ business logic** (tính giá, trừ kho, xử lý flash sale, gọi 3 cổng thanh toán, gửi notification) — đúng ra một phần lớn logic này (ví dụ "một Order không thể giảm quá số lượng tồn kho", "không thể huỷ Order đã Completed") phải nằm trong Domain (entity method / domain service), không phải nằm rải rác trong `OrderService`.
- **Infrastructure leak vào Domain interface**: `BookStore.Domain/Interfaces/IRedisService.cs` là một interface hạ tầng thuần tuý (cache) nhưng lại đặt trong Domain. Domain layer không nên biết về khái niệm Redis/cache — đây là vi phạm Dependency Rule trực tiếp (Domain phải là layer trong cùng, không phụ thuộc khái niệm hạ tầng cụ thể).
- **Domain phụ thuộc vào ASP.NET Identity**: `ApplicationUser` (BookStore.Domain/Entities/ApplicationUser.cs) kế thừa/liên kết trực tiếp với `Microsoft.AspNetCore.Identity`, và `BookStoreDbContext` kế thừa `IdentityDbContext`. Domain model bị trói vào framework — muốn thay Identity bằng giải pháp khác là phải sửa Domain, đây chính là điều Clean Architecture cố tránh.
- **`BookStoreDbContext` (Infrastructure) được reference trực tiếp trong tên namespace của services** ở một số chỗ — mức độ tách biệt Application/Infrastructure là tương đối, chấp nhận được vì đi qua interface `IUnitOfWork`, nhưng interface đó lại là "leaky abstraction" trả về `IEnumerable<T>` full-load thay vì `IQueryable` (xem mục 4).

**Kết luận mục 2.1:** Đây **không phải** Clean Architecture đúng nghĩa. Đây là kiến trúc 4-layer với Domain rỗng và Application đóng vai trò "Fat Service Layer" kiểu Layered Architecture cũ.

### 2.2 SOLID — vi phạm nhiều nguyên tắc

| Nguyên tắc | Đánh giá | File vi phạm điển hình |
|---|---|---|
| **S — Single Responsibility** | Vi phạm nặng | `OrderService.cs` (566 dòng): vừa tạo order, vừa trừ kho, vừa gọi 3 payment gateway, vừa gửi SignalR notification, vừa build báo cáo, vừa xử lý POS. Một class có ít nhất 5 lý do để thay đổi. |
| **O — Open/Closed** | Vi phạm | `ProcessCheckoutAsync` (OrderService.cs dòng 124-182) dùng chuỗi `if/else if` theo `PaymentMethod` enum. Thêm cổng thanh toán mới bắt buộc sửa method này thay vì extend qua strategy pattern. |
| **L — Liskov** | Không đủ dữ liệu để kết luận (không có class kế thừa entity/service đáng chú ý để đánh giá). |
| **I — Interface Segregation** | Tạm ổn | Mỗi repository có interface riêng (`IOrderRepository`, `IProductRepository`...) — điểm cộng thực sự. |
| **D — Dependency Inversion** | Vi phạm một phần | `GenericRepository<T>.GetQueryable()` trả `IQueryable<T>` thẳng ra ngoài (Domain/Interfaces/IGenericRepository.cs), nghĩa là Application layer có thể viết LINQ trực tiếp lên EF Core — che giấu chi tiết ORM thất bại, Application phụ thuộc ngược vào chi tiết Infrastructure (EF Core translation behavior). |

### 2.3 God Class

- **`OrderService` (566 dòng, 6 dependency injection)** là God Class rõ ràng nhất trong project: `IUnitOfWork`, `IZaloPayService`, `IPayOSService`, `IVnPayService`, `INotificationService`, `UserManager<ApplicationUser>`. Nó biết về đơn hàng, tồn kho, flash sale, 3 cổng thanh toán, và thông báo — 5+ trách nhiệm không liên quan trực tiếp tới nhau.
- **`FlashSaleService` (313 dòng)`, `ReportExportService` (285 dòng)`** cũng khá lớn, cần xem thêm nhưng không đến mức nghiêm trọng như OrderService.

### 2.4 Anemic Domain Model — XÁC NHẬN

Toàn bộ entity đã đọc (`Order`, `Product`, `Cart`, `CartItem`) chỉ có auto-property, không có:
- Constructor có validate.
- Method thể hiện hành vi nghiệp vụ (`order.Cancel()`, `product.DecreaseStock(qty)`...).
- Invariant được bảo vệ (ví dụ `Product.Quantity` có thể bị set âm từ bất kỳ đâu vì là public setter không kiểm soát).

Toàn bộ logic "trừ kho", "tính lại flash sale slot", "không cho huỷ khi đã Completed" nằm trong `OrderService`, nghĩa là **bất kỳ đoạn code nào khác** cũng có thể set `product.Quantity = -100` mà không entity nào ngăn cản. Đây là hệ quả trực tiếp của Anemic Domain Model, không phải vấn đề lý thuyết suông — nó tạo ra rủi ro thực tế về toàn vẹn dữ liệu.

---

## 3. DOMAIN

| Hạng mục | Đánh giá |
|---|---|
| Entity | Thuần POCO, không có behavior/encapsulation (mục 2.4) |
| Aggregate | Không có khái niệm Aggregate Root rõ ràng — ví dụ `Order` và `OrderDetail` lẽ ra phải là 1 aggregate với `Order` là root, chỉ được sửa qua `Order`, nhưng thực tế `OrderService` sửa `OrderDetail`, `Product`, `FlashSale`, `StockHistory` trực tiếp và độc lập nhau trong cùng transaction — không có ranh giới consistency rõ ràng. |
| Value Object | Không tồn tại. Các khái niệm lẽ ra nên là VO (`Money`, `Address`, `PhoneNumber`, `OrderNumber`) đang là `string`/`decimal` thô (`Order.ShippingAddress` là `string`, `Order.TotalPrice` là `decimal` trần không qua VO nào kiểm soát dấu âm). |
| Domain Service | Không có. Logic tính giá có `PricingService` nhưng nằm ở Application layer, không phải Domain Service thực thụ vì phụ thuộc `IUnitOfWork`. |
| Business Rule | Nằm sai chỗ — 100% nằm trong Application Service thay vì Domain (xem OrderService.PlaceOrderAsync: kiểm tra tồn kho, tính flash sale, trừ kho đều nằm ở đây). |
| Encapsulation | Không có — mọi property `public { get; set; }`, mọi trường có thể bị gán sai từ bất kỳ layer nào có reference tới entity. |
| Validation | Không có validate ở entity. Validate (nếu có) chỉ nằm ở DTO qua Data Annotations (chưa kiểm tra hết, không đủ dữ liệu để kết luận toàn diện) hoặc if-check rải rác trong Service. |

**Mức độ:** Major — đây là vấn đề kiến trúc nền tảng, sửa sau này sẽ tốn effort lớn vì phải refactor toàn bộ Application layer.

---

## 4. APPLICATION / REPOSITORY / EF CORE — CÁC LỖI CỤ THỂ

### 🔴 CRITICAL #1 — N+1 Query nghiêm trọng trong `GetAllOrdersForReportAsync`
**File:** `BookStore.Application/Services/OrderService.cs`, dòng 389-435

```csharp
var query = await _unitOfWork.Orders.GetAllAsync();   // load TOÀN BỘ orders vào memory
...
var orders = query.OrderByDescending(o => o.CreatedAt).ToList();
foreach (var order in orders)
{
    var detailedOrder = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(order.Id); // 1 query/order
    ...
}
```
**Vì sao sai:** `GetAllAsync()` (GenericRepository.cs dòng 19-20) đã materialize toàn bộ bảng `Orders` bằng `ToListAsync()` — không filter, không phân trang ở DB. Sau đó với **mỗi** order lại gọi thêm 1 query `GetOrderByIdWithDetailsAsync` để lấy chi tiết → tổng cộng N+1 query, N = tổng số order trong hệ thống.

**Hậu quả nếu deploy production:** Với 10.000 đơn hàng, đây là **10.001 round-trip DB** cho một API report. Với 1 triệu đơn hàng (mục tiêu 10 triệu record theo yêu cầu review), API này sẽ timeout hoặc làm sập connection pool DB — đây không phải rủi ro lý thuyết, đây là bug sẽ bùng nổ chắc chắn khi data tăng.

**Cách sửa:** Viết 1 query duy nhất với `Include`/`ThenInclude` + filter + (nếu cần) phân trang, thực hiện toàn bộ ở DB level:
```csharp
public async Task<IEnumerable<Order>> GetOrdersForReportAsync(OrderStatus? status, string? search)
{
    var query = _context.Orders
        .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
        .AsNoTracking()
        .AsQueryable();

    if (status.HasValue) query = query.Where(o => o.Status == status);
    if (!string.IsNullOrEmpty(search)) query = query.Where(o => o.OrderNumber.Contains(search));

    return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
}
```

---

### 🔴 CRITICAL #2 — Phân trang thực hiện trong memory (không phải trong DB)
**File:** `BookStore.Application/Services/OrderService.cs`, `GetPagedOrdersAsync` (dòng 268-310) và `GetUserOrdersPagedAsync` (dòng 341-366)

```csharp
var query = await _unitOfWork.Orders.GetAllAsync();  // <-- IEnumerable<Order>, đã ToListAsync() rồi
...
var orders = query.OrderByDescending(...).Skip(...).Take(...) // Skip/Take chạy trên LINQ-to-Objects, KHÔNG phải LINQ-to-Entities
```

**Vì sao sai:** `IOrderRepository.GetAllAsync()` trả về `IEnumerable<Order>` đã materialize (từ `GenericRepository.GetAllAsync()` gọi `ToListAsync()`). Khi gọi tiếp `.Where()/.Skip()/.Take()` trên kết quả này, EF Core **không** dịch được thành SQL `WHERE/OFFSET/FETCH` nữa — toàn bộ bảng đã được load lên RAM ứng dụng trước, sau đó .NET mới lọc/phân trang trong memory.

**Hậu quả:** Đây chính là kiểu lỗi "trang 1 load hết 10 triệu dòng rồi mới lấy 20 dòng đầu" — tốn RAM server, tốn băng thông DB→App, độ trễ tăng tuyến tính theo tổng số record chứ không phải theo page size. Đây là lỗi **thường gặp nhất** khi hỏi "nếu 10 triệu record thì sao" — và project này mắc đúng lỗi đó ở tính năng lõi (danh sách đơn hàng).

**Cách sửa:** Repository phải expose `IQueryable<Order>` (hoặc nhận filter/paging parameter và trả `(items, totalCount)` như `ProductRepository.GetFilteredPagedAsync` đã làm đúng — xem mục Điểm mạnh), rồi filter + `Skip/Take` + `CountAsync` chạy hoàn toàn ở DB.

---

### 🟠 MAJOR #3 — Race Condition khi trừ tồn kho (không có concurrency control)
**File:** `OrderService.cs`, `PlaceOrderAsync` dòng 56-107 và `CreatePOSOrderAsync` dòng 485-563

```csharp
if (product.Quantity < item.Quantity) throw new Exception(...);
...
product.Quantity -= item.Quantity;
```

**Vì sao sai:** Đây là pattern kinh điển "check-then-act" không an toàn với concurrency. Hai request đặt hàng cùng lúc cho cùng 1 sản phẩm còn 1 unit tồn kho đều có thể pass qua check `product.Quantity < item.Quantity` trước khi transaction nào commit, dẫn tới `Quantity` âm. Không thấy `[Timestamp]`/`RowVersion` trên `Product` entity, không thấy `DbUpdateConcurrencyException` được catch ở đâu trong OrderService.

**Hậu quả production:** Bán vượt tồn kho (oversell) khi có traffic đồng thời — với sách bán chạy hoặc flash sale (nơi race condition dễ xảy ra nhất vì nhiều người cùng mua 1 lúc), đây là lỗi nghiệp vụ nghiêm trọng, ảnh hưởng trực tiếp uy tín và vận hành kho.

**Cách sửa:** Thêm `RowVersion` (concurrency token) vào `Product`, dùng `ExecuteUpdateAsync` với điều kiện `WHERE Quantity >= @qty` (atomic update), hoặc dùng optimistic concurrency + retry, hoặc pessimistic lock (`SELECT ... FOR UPDATE` tương đương trong SQL Server là `UPDLOCK, ROWLOCK`) đặc biệt cho Flash Sale.

---

### 🟠 MAJOR #4 — Thiếu `AsNoTracking()` cho toàn bộ query chỉ-đọc
**File:** `ProductRepository.cs` dòng 28-35, và nhiều nơi khác

Query `GetFilteredPagedAsync` (dùng cho trang danh sách sản phẩm — endpoint có traffic cao nhất hệ thống) không có `AsNoTracking()`, khiến EF Core snapshot toàn bộ entity trả về vào Change Tracker dù chỉ để đọc và map sang DTO. `GenericRepository.GetPagedAsync` (dòng 45-55) thì **có** dùng đúng — cho thấy đội dev biết kỹ thuật này nhưng áp dụng không nhất quán.

**Hậu quả:** Overhead CPU/RAM không cần thiết trên mọi request GET danh sách sản phẩm — tăng dần theo lượng traffic.

**Cách sửa:** Thêm `.AsNoTracking()` cho mọi query chỉ đọc, đặc biệt các query trả DTO như `GetFilteredPagedAsync`, `GetOrderByIdWithDetailsAsync` (khi dùng cho mục đích hiển thị, không update).

---

### 🟡 MINOR #5 — Over-fetching trong danh sách sản phẩm
**File:** `ProductRepository.cs` dòng 30-35

```csharp
var result = _context.Products
    .Include(b => b.Category)
    .Include(b => b.SubCategory)
    .Include(b => b.FlashSales).ThenInclude(f => f.FlashSaleCampaign)
    .AsQueryable();
```
Mọi request danh sách sản phẩm đều `Include` cả FlashSale + Campaign kể cả khi `query.IsFlashSale == false` — tải dữ liệu không cần thiết cho phần lớn traffic (browse sản phẩm bình thường).

**Cách sửa:** Dùng `.Select()` projection sang DTO ngay tại repository (thay vì load full entity rồi map ở Service) để EF Core chỉ SELECT đúng cột cần, và chỉ `Include` FlashSale khi `IsFlashSale == true`.

---

### 🟡 MINOR #6 — Non-sargable search query
**File:** `ProductRepository.cs` dòng 39: `b.Name.Contains(query.Search) || b.Brand.Contains(query.Search)`

`Contains` dịch sang `LIKE '%...%'` — không dùng được index B-Tree thông thường. Chấp nhận được ở tầm dữ liệu nhỏ nhưng ở tầm 10 triệu record cần Full-Text Search (SQL Server Full-Text Index) hoặc external search engine (Elasticsearch) thay vì `LIKE %...%` trên 2 cột string.

---

## 5. DATABASE

- **Không có index tuỳ chỉnh nào được cấu hình** — `BookStoreDbContext.OnModelCreating` (39 dòng) chỉ gọi `ApplyConfigurationsFromAssembly`, và chỉ có 2 file Configuration (`FavoriteConfiguration`, `ProductImageConfiguration`) được tìm thấy trong toàn bộ `Persistence/Configurations`. Không có index trên `Order.Status`, `Order.CreatedAt`, `Order.OrderNumber` (dùng để search/filter thường xuyên ở mục 4) — EF Core mặc định chỉ tạo index cho FK và unique key của Identity.
- **Không có cấu hình `OnDelete`/cascade behavior tường minh** — toàn bộ dựa vào default convention của EF Core (Cascade cho quan hệ required). Với các quan hệ như `Order → OrderDetail → Product`, cascade delete mặc định có thể gây xoá dây chuyền ngoài ý muốn nếu ai đó xoá `Product` đang có trong đơn hàng cũ — nên cân nhắc `DeleteBehavior.Restrict` cho các bảng lịch sử giao dịch (Order, OrderDetail, StockHistory) để tránh mất dữ liệu audit.
- **10 triệu record thì sao?** Với cấu hình hiện tại: (1) API danh sách đơn hàng sập vì phân trang trong memory (Critical #2), (2) query search sản phẩm chậm dần vì thiếu full-text index, (3) không có chiến lược archiving/partitioning cho bảng `StockHistory`/`AdminActivityLog` (loại bảng log tăng vô hạn theo thời gian) — không thấy TTL hay archive job nào. Không đủ dữ liệu để đánh giá chi tiết hơn vì chưa xem file migration đầy đủ.

---

## 6. SECURITY — PHẦN NGHIÊM TRỌNG NHẤT CỦA REVIEW NÀY

### 🔴🔴 CRITICAL #7 — Secret thật bị hardcode và commit vào source control
**File:** `BookStore.API/appsettings.json`

File này (không phải `appsettings.Development.json` — đây là file **mặc định load ở mọi môi trường**) chứa:
- Mật khẩu SQL Server: `User Id=sa; Password=sa`
- Redis connection string kèm password thật: `password=araB9NWC5hFeiYyocciIMQQ57l76xDKU`
- JWT signing secret: `"Secret": "Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_2024_@123"`
- Gmail App Password thật: `"Password": "upjw zqhr ajsv fpxp"`
- ZaloPay `Key1`/`Key2` thật
- PayOS `ApiKey`/`ChecksumKey` thật
- VNPay `HashSecret` thật
- Cloudinary `ApiSecret` thật

**Vì sao sai:** Đây không phải "code smell", đây là **rò rỉ credential thật vào source control**. Bất kỳ ai có quyền đọc repo (kể cả sau này khi repo public, fork, hoặc leak) đều có thể: đăng nhập Redis, ký giả JWT token (biết secret → tự tạo token Admin bất kỳ), gửi email giả danh hệ thống, gọi API ZaloPay/PayOS/VNPay bằng credential thật, thao túng Cloudinary.

**Hậu quả nếu deploy production với file này:** Chiếm quyền Admin toàn hệ thống (chỉ cần biết JWT Secret là tự ký token với role Admin, không cần mật khẩu ai cả — xem mục 6.2), truy cập trái phép Redis (session/token store), giao dịch tài chính giả mạo qua các cổng thanh toán.

**Cách sửa:** 
1. Revoke/xoay vòng (rotate) **toàn bộ** secret đã lộ ngay lập tức — coi như chúng đã bị lộ vĩnh viễn vì đã nằm trong Git history.
2. Xoá khỏi Git history bằng `git filter-repo` hoặc BFG.
3. Chuyển sang User Secrets (dev) / Azure Key Vault, AWS Secrets Manager, hoặc environment variable (production).
4. Thêm `appsettings*.json` chứa secret vào `.gitignore`, chỉ commit `appsettings.Template.json` không có giá trị thật.

### 🔴 CRITICAL #8 — JWT signing key có giá trị fallback hardcode trong code
**File:** `BookStore.API/Startup.cs` dòng 103

```csharp
var key = Encoding.UTF8.GetBytes(Configuration["JWT:Secret"] ?? "Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_123");
```
**Vì sao sai:** Nếu config `JWT:Secret` vì lý do gì đó (deploy sai, thiếu biến môi trường) không load được, hệ thống **âm thầm fallback** sang secret hardcode trong compiled binary — mọi người có decompile file DLL đều lấy được key này, và vì đây là fallback "vô hình" nên đội vận hành khó phát hiện hệ thống đang chạy với key yếu.

**Cách sửa:** Không dùng `??` fallback cho secret. Nếu thiếu config, phải fail-fast (throw exception khi start-up) thay vì chạy với key mặc định.

### 🔴 CRITICAL #9 — Cấu hình CORS cho phép mọi origin kèm credentials, tạo lỗ hổng CSRF
**File:** `Startup.cs` dòng 153-167

```csharp
options.AddPolicy("AllowAngular", builder => builder
    .WithOrigins("http://localhost:4200", ...)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .SetIsOriginAllowed(options => true));   // <-- dòng này override WithOrigins ở trên
```
**Vì sao sai:** `SetIsOriginAllowed(_ => true)` khiến toàn bộ danh sách `WithOrigins(...)` bên trên trở nên **vô nghĩa** — CORS middleware sẽ chấp nhận **bất kỳ Origin nào**. Kết hợp với `.AllowCredentials()`, browser sẽ gửi cookie/Authorization header kèm theo request cross-origin từ **bất kỳ website nào khác**.

Nghiêm trọng hơn: hệ thống dùng **Cookie Authentication cho Admin** (`AddCookie("Cookies")`, dòng 109-118) với `SameSite = SameSiteMode.None` (dòng 115) — nghĩa là cookie Admin **được gửi kèm trong request cross-site**. Không tìm thấy bất kỳ cơ chế Anti-Forgery Token nào trong toàn bộ `BookStore.API` (đã grep toàn repo, không có kết quả). 

**Hậu quả production:** Đây là điều kiện đủ cho một cuộc tấn công **CSRF cổ điển**: một trang web độc hại bất kỳ có thể âm thầm gửi request tới các endpoint Admin (ví dụ `POST /Admin/Product/Upsert`, `POST /Admin/Order/UpdateStatus`) trong khi nạn nhân (Admin) đang đăng nhập ở tab khác — browser tự động đính kèm cookie Admin, CORS cho phép origin lạ, không có CSRF token nào để chặn. Kẻ tấn công có thể thao túng sản phẩm, đơn hàng, tồn kho mà Admin không hề hay biết.

**Cách sửa:**
1. Xoá `SetIsOriginAllowed(_ => true)`, chỉ giữ whitelist domain cụ thể qua `WithOrigins`.
2. Đổi `SameSite` của cookie Admin về `Strict` hoặc tối thiểu `Lax`.
3. Thêm Anti-Forgery Token (`[ValidateAntiForgeryToken]` hoặc header-based CSRF token) cho mọi action thay đổi state trong Admin Area dùng Cookie Auth.
4. Không nên trộn lẫn 2 cơ chế xác thực (Cookie cho Admin, JWT cho Customer) trong cùng 1 API mà không kiểm soát CORS/CSRF riêng cho từng cơ chế.

### 🟠 MAJOR #10 — Swagger UI bật không điều kiện, kể cả production
**File:** `Startup.cs` dòng 211-212 (nằm ngoài khối `if (env.IsDevelopment())`)

```csharp
app.UseSwagger();
app.UseSwaggerUI(...);
```
**Vì sao sai:** Swagger expose toàn bộ danh sách endpoint, request/response schema (kể cả các trường nội bộ) công khai ở mọi môi trường kể cả Production, giúp kẻ tấn công dễ dàng do thám bề mặt tấn công (attack surface) của hệ thống mà không cần đoán.

**Cách sửa:** Bọc `UseSwagger()/UseSwaggerUI()` trong `if (env.IsDevelopment())`, hoặc bảo vệ bằng Authorization nếu cần giữ ở staging.

### 🟢 Điểm được làm đúng về Security (ghi nhận công bằng)
- Cơ chế Refresh Token: có TokenVersion + reuse detection + revoke toàn bộ session khi phát hiện refresh token bị dùng lại (`AuthRepository.cs` dòng 98-109) — đây là pattern đúng chuẩn (tốt hơn phần lớn code tôi thấy ở mức fresher/junior).
- Access token có thời hạn ngắn (15 phút, dòng 221) + refresh token rotation — thiết kế hợp lý.
- Password đổi thì tăng `TokenVersion` để revoke token cũ (dòng 149-155) — đúng.
- Có Rate Limiter cho endpoint `forgot-password` (Startup.cs dòng 179-188) — cho thấy có ý thức chống brute-force/spam email, dù chỉ áp dụng cho 1 endpoint.
- Dùng `RandomNumberGenerator` (CSPRNG) để tạo refresh token thay vì `Random` thông thường (AuthRepository.cs dòng 192-193) — đúng chuẩn.

---

## 7. API DESIGN

- Route đặt tên tương đối rõ ràng (`GetProduct/{id}`, `Upsert`, `ToggleStatus`), nhưng **không thuần RESTful** — `Upsert` gộp cả Create/Update vào 1 endpoint (không phân biệt PUT/POST theo semantic REST chuẩn), `ToggleStatus` là action-based route thay vì resource-based (RESTful chuẩn sẽ là `PATCH /products/{id}` với body chứa field cần đổi). Đây là điểm chấp nhận được ở nhiều team thực tế (pragmatic REST), không phải lỗi nghiêm trọng nhưng nên biết đây không phải REST thuần.
- Không thấy versioning API (`/api/v1/...`) — sẽ khó khi cần breaking change sau này.
- Chưa đủ dữ liệu để đánh giá toàn diện status code convention và error response format trên tất cả controller (chỉ xem 2/23 controller).

---

## 8. CLEAN CODE

- **Naming**: đa số rõ nghĩa, dùng comment tiếng Việt giải thích intent khá tốt (dễ đọc với dev Việt Nam), nhưng có trộn ngôn ngữ (biến/class tiếng Anh, comment tiếng Việt) — chấp nhận được cho team nội bộ, không nên nếu là sản phẩm quốc tế hoá.
- **Exception dùng generic `Exception`** thay vì custom exception (`OrderService.cs` dòng 61: `throw new Exception($"Sản phẩm {product.Name} không đủ số lượng...")`) — không phân biệt được lỗi nghiệp vụ (400) với lỗi hệ thống (500) ở tầng gọi, buộc `ExceptionMiddleware` phải catch generic Exception rồi đoán mã lỗi qua message string, rất dễ vỡ khi đổi message.
- **Magic string**: nhiều chỗ dùng string literal cho status/reason (`"System-Cleanup"`, `"Admin"`) thay vì constant tập trung — đã có `OrderConstants.cs` trong Domain/Common nhưng không được áp dụng nhất quán ở các chỗ này.
- **Duplicate code**: logic hoàn kho + hoàn flash sale lặp lại gần như y hệt giữa `CancelExpiredOrderAsync` (dòng 437-484) và `UpdateOrderStatusAsync` khi status = Cancelled (dòng 227-258) — nên gộp thành 1 private method dùng chung.
- **Console.WriteLine dùng để log lỗi** (`AuthService.cs` dòng 145) thay vì `ILogger` — không nhất quán, mất log khi chạy production (stdout không được thu thập tuỳ hạ tầng), không có structured logging.

---

## 9. TESTING

`BookStore.Tests` chỉ có 4 file, 293 dòng, trong đó có 1 file (`UnitTest1.cs`) là **test rỗng chưa xoá** (`Test1()` không có nội dung, không có assertion). Chỉ có test cho `OrderService`, `AuthService`, `CategoriesService` — hoàn toàn **không có test** cho `ProductService`, `CartService`, `InventoryService`, `FlashSaleService`, và không có test cho bất kỳ Repository hay Controller nào.

**Đánh giá:** Với một hệ thống có business logic phức tạp như trừ kho + flash sale + 3 payment gateway (chính là nơi có race condition CRITICAL #3 ở trên), **không có test coverage tương xứng với rủi ro** — đây là dấu hiệu rõ ràng "chưa production-ready" theo đúng nghĩa đen.

---

## 10. CODE SMELL TỔNG HỢP

| Code Smell | Có xuất hiện? | Vị trí |
|---|---|---|
| God Object | ✅ | `OrderService` |
| Long Method | ✅ | `PlaceOrderAsync`, `CreatePOSOrderAsync` (~80 dòng mỗi hàm, nhiều nhánh) |
| Primitive Obsession | ✅ | `Money`/`Address`/`PhoneNumber` đều là string/decimal trần |
| Feature Envy | ✅ | `OrderService` thao túng trực tiếp field của `Product`, `FlashSale` thay vì gọi method của chính entity đó |
| Data Class | ✅ | Toàn bộ Entity trong Domain |
| Shotgun Surgery | ✅ | Thêm payment method mới phải sửa: `PaymentMethod` enum, `ProcessCheckoutAsync` if/else, `Startup.cs` DI, cấu hình appsettings |
| Tight Coupling | ✅ | `OrderService` coupling trực tiếp 3 payment service cụ thể thay vì 1 abstraction `IPaymentGateway` chung |
| Circular Dependency | Không đủ dữ liệu để kết luận (chưa build solution để kiểm tra) |
| Duplicate Code | ✅ | Logic hoàn kho lặp lại (mục 8) |
| Dead Code | ✅ | `UnitTest1.cs` test rỗng |
| Lazy Class | Không đủ dữ liệu để kết luận |
| Speculative Generality | Có dấu hiệu nhẹ | `IGenericRepository<T>` + generic repository pattern nhưng phần lớn repository đều override gần hết method, giá trị của lớp generic bị giảm |

---

## 11. CHẤM ĐIỂM (thang 10)

| Hạng mục | Điểm | Ghi chú ngắn |
|---|---|---|
| Kiến trúc | 4/10 | Đúng tên gọi Clean Architecture, sai bản chất |
| Domain Design | 3/10 | Anemic hoàn toàn |
| Application | 5/10 | Chạy được, nhưng God Class + business logic đặt sai layer |
| Infrastructure | 5/10 | Cấu trúc repository rõ ràng nhưng thiếu tối ưu |
| Repository | 5/10 | Tách interface tốt, nhưng leak IQueryable + GetAllAsync in-memory paging |
| EF Core | 4/10 | Thiếu AsNoTracking nhất quán, thiếu concurrency token |
| Database | 4/10 | Không có index tuỳ biến, không cấu hình cascade tường minh |
| API Design | 6/10 | Dùng được, chưa RESTful chuẩn, thiếu versioning |
| Performance | 3/10 | 2 lỗi Critical về N+1/in-memory paging ở tính năng lõi |
| Security | 2/10 | Secret thật bị lộ + CORS/CSRF nghiêm trọng — điểm thấp nhất toàn bài review |
| Maintainability | 4/10 | God Class khiến sửa đổi rủi ro cao |
| Readability | 6/10 | Comment tiếng Việt rõ ràng, dễ đọc theo luồng nghiệp vụ |
| Production Ready | 2/10 | Không thể deploy với secret hiện tại và lỗ hổng CORS/CSRF |
| Testing Ready | 2/10 | Test coverage gần như không tồn tại cho business logic quan trọng |
| Clean Code | 5/10 | Không tệ nhưng nhiều magic string, generic Exception |
| SOLID | 4/10 | Vi phạm S, O, D rõ ràng |

**Tổng điểm trung bình: 4.0/10**

---

## 12. ĐÁNH GIÁ TRÌNH ĐỘ

Dựa hoàn toàn trên source code (không dựa CV/kinh nghiệm khai báo):

### **Junior+ → cận Mid-Level**

Lý do:
- **Điểm cộng cho thấy vượt mức Junior thuần**: refresh token rotation với reuse detection, TokenVersion revoke, rate limiting, SignalR real-time notification, tích hợp 3 payment gateway thực tế hoạt động, transaction (`BeginTransactionAsync/CommitAsync/RollbackAsync`) được dùng đúng chỗ cho checkout flow, repository pattern + Unit of Work triển khai đầy đủ, biết dùng `AsNoTracking` (dù không nhất quán) — đây không phải kiến thức của người mới học framework, đây là người đã từng đọc/áp dụng best practice thực tế.
- **Điểm trừ khiến chưa tới Mid-Level thực thụ**: không nhận ra sự khác biệt giữa `IQueryable` và `IEnumerable` khi phân trang (Critical #2) — đây là kiến thức nền tảng EF Core mà Mid-Level bắt buộc phải nắm; thiết kế Domain hoàn toàn anemic cho thấy chưa từng thực hành DDD dù đặt tên thư mục theo DDD; lỗ hổng CORS+CSRF cho thấy thiếu kinh nghiệm thực chiến về bảo mật web (biết dùng JWT đúng cách nhưng không hiểu tại sao CORS wildcard + Cookie SameSite=None lại nguy hiểm); hardcode secret vào git là lỗi mà một Mid/Senior sẽ tự động tránh theo phản xạ nghề nghiệp, không cần ai nhắc.

**Kết luận:** Trình độ hiện tại tương đương lập trình viên đã có kinh nghiệm thực chiến khoảng 6 tháng – 1.5 năm, tự học rộng (đọc nhiều tutorial/best-practice khác nhau) nhưng chưa có người mentor/senior review để chỉ ra các lỗi nền tảng về concurrency, ORM query behavior, và security fundamentals. Đây là profile rất thường gặp ở sinh viên mới ra trường có làm dự án cá nhân lớn/thực tập.

---

## 13. ĐÁNH GIÁ PHỎNG VẤN (nếu đây là bài test tuyển dụng)

### **Lean Hire** (cho vị trí Junior/Fresher IT — KHÔNG áp dụng nếu vị trí là Mid/Senior)

Giải thích:
- Nếu vị trí tuyển là **Junior Developer/Fresher**: đây là bài test **trên trung bình** so với mặt bằng chung — ứng viên chứng minh được khả năng tự học, đọc tài liệu, dựng được một hệ thống end-to-end phức tạp (nhiều module, tích hợp thật với 3rd-party), không chỉ làm CRUD đơn giản. → **Hire**.
- Nếu vị trí tuyển là **Mid-Level trở lên**: các lỗi Critical về Security (hardcode secret, CORS/CSRF) và Performance (N+1, in-memory paging ở tính năng lõi) là **loại lỗi mà Mid-Level không được phép mắc**, vì đây là kiến thức nền tảng bắt buộc, không phải edge-case hiếm gặp. → **Lean No Hire / No Hire** tuỳ mức độ kỳ vọng.
- Vì bối cảnh review (theo trí nhớ hội thoại) là ứng viên **thực tập/mới tốt nghiệp ứng tuyển vị trí IT Support/Fresher**, không phải vị trí .NET Developer chính thức — nếu đây là sản phẩm **phụ** dùng để chứng minh năng lực lập trình nói chung (không phải công việc hàng ngày), thì mức **Lean Hire** là hợp lý: đủ để thấy tiềm năng và tinh thần tự học, nhưng không nên dùng làm chuẩn đánh giá "đã sẵn sàng làm Backend Developer full-time".

---

## PHẦN TỔNG KẾT

### Điểm mạnh
1. Refresh token flow với reuse-detection + token versioning — thiết kế đúng chuẩn, hiếm thấy ở người tự học.
2. `ProductRepository.GetFilteredPagedAsync` filter đúng ở tầng `IQueryable` trước khi phân trang (khác với lỗi ở OrderService) — cho thấy hiểu đúng nguyên lý ở ít nhất 1 chỗ, chỉ là chưa áp dụng nhất quán.
3. Transaction (Begin/Commit/Rollback) dùng đúng cho luồng checkout đa bước.
4. Tách interface theo Interface Segregation cho từng repository.
5. Tích hợp thực tế nhiều dịch vụ ngoài (3 cổng thanh toán, Cloudinary, SendGrid, Redis, SignalR) và chạy được — độ phức tạp tích hợp không nhỏ.
6. Rate limiting cho endpoint nhạy cảm (forgot-password).

### Điểm yếu
1. Domain Model hoàn toàn anemic — không có DDD thực chất dù đặt tên theo DDD.
2. `OrderService` là God Class ôm quá nhiều trách nhiệm.
3. 2 lỗi Critical về performance (N+1, in-memory paging) nằm ngay ở tính năng lõi (quản lý đơn hàng).
4. Secret thật bị lộ vào source control — nghiêm trọng nhất toàn bài review.
5. CORS + Cookie config tạo lỗ hổng CSRF cho khu vực Admin.
6. Không có concurrency control khi trừ tồn kho → race condition/oversell.
7. Test coverage gần như không tồn tại cho business logic quan trọng.

### 20 lỗi nghiêm trọng nhất (ưu tiên từ cao xuống thấp)
1. Secret thật (DB, JWT, Redis, payment gateway) commit vào `appsettings.json` — CRITICAL
2. JWT secret có fallback hardcode trong code khi thiếu config — CRITICAL
3. CORS `SetIsOriginAllowed(_ => true)` + `AllowCredentials` + Cookie `SameSite=None` → CSRF cho Admin — CRITICAL
4. `GetPagedOrdersAsync`/`GetUserOrdersPagedAsync` phân trang trong memory — CRITICAL
5. `GetAllOrdersForReportAsync` N+1 query — CRITICAL
6. Race condition khi trừ `Product.Quantity` không có concurrency token — MAJOR
7. Swagger bật ở mọi môi trường kể cả production — MAJOR
8. Không có Anti-Forgery Token cho Admin Area dùng Cookie Auth — MAJOR
9. Domain hoàn toàn anemic, business rule không được entity bảo vệ — MAJOR
10. `OrderService` God Class (6 dependency, 5+ trách nhiệm) — MAJOR
11. Thiếu `AsNoTracking()` trên query danh sách sản phẩm (traffic cao nhất hệ thống) — MAJOR
12. Không có index tuỳ biến cho `Order.Status`/`CreatedAt`/`OrderNumber` — MAJOR
13. Test coverage gần như 0% cho business logic (Product, Cart, Inventory, FlashSale) — MAJOR
14. Generic `Exception` dùng cho lỗi nghiệp vụ, không phân biệt 400 vs 500 — MINOR/MAJOR
15. Duplicate logic hoàn kho giữa 2 method trong OrderService — MINOR
16. Over-fetching Include FlashSale+Campaign cho mọi query sản phẩm — MINOR
17. `Contains()` search non-sargable, không scale với dữ liệu lớn — MINOR
18. Không cấu hình `OnDelete`/cascade tường minh cho bảng audit/lịch sử — MINOR
19. `Console.WriteLine` thay vì `ILogger` trong AuthService — MINOR
20. Payment method mở rộng cần sửa nhiều nơi (if/else, enum, DI) — vi phạm Open/Closed — MINOR

### 20 điểm cần cải thiện
1. Chuyển toàn bộ secret sang User Secrets/Key Vault, xoay vòng key đã lộ.
2. Sửa CORS: bỏ `SetIsOriginAllowed(_ => true)`, dùng whitelist thật.
3. Thêm Anti-Forgery Token cho Admin Area.
4. Sửa repository trả `IQueryable` thay vì `IEnumerable` đã materialize cho các query cần filter/paging.
5. Thêm `RowVersion`/concurrency check cho `Product.Quantity`.
6. Refactor `OrderService` thành nhiều service nhỏ hơn theo trách nhiệm (OrderCreation, OrderFulfillment, PaymentOrchestration...).
7. Thêm behavior vào entity (`Order.Cancel()`, `Product.DecreaseStock()`) thay vì set property trực tiếp từ Service.
8. Áp dụng Strategy Pattern cho payment gateway (`IPaymentGateway` chung).
9. Thêm `AsNoTracking()` nhất quán cho mọi query chỉ đọc.
10. Thêm index cho các cột filter/search thường dùng.
11. Viết test cho `ProductService`, `CartService`, `InventoryService`, `FlashSaleService`.
12. Xoá `UnitTest1.cs` rỗng.
13. Thay generic `Exception` bằng custom exception hierarchy (`BusinessException`, `NotFoundException`...).
14. Tắt Swagger ở production hoặc bảo vệ bằng auth.
15. Chuẩn hoá logging qua `ILogger` thay vì `Console.WriteLine`.
16. Gộp logic hoàn kho trùng lặp thành 1 method dùng chung.
17. Thêm projection (`.Select()`) ở repository để giảm over-fetching.
18. Cân nhắc Full-Text Search cho tìm kiếm sản phẩm khi data lớn.
19. Thêm API versioning.
20. Định nghĩa rõ `DeleteBehavior` cho các quan hệ liên quan tới bảng lịch sử/audit.

### Những phần làm rất tốt
- Cơ chế Refresh Token + reuse detection (AuthRepository.cs).
- `ProductRepository.GetFilteredPagedAsync` — ví dụ đúng về filter trước khi phân trang ở tầng IQueryable.
- Transaction handling cho luồng checkout đa bước.

### Những phần chưa đạt chuẩn production
- Toàn bộ mục Security (mục 6) — không thể deploy nguyên trạng.
- Luồng quản lý đơn hàng (danh sách, báo cáo) — sẽ sập khi data lớn.
- Luồng trừ kho — có thể oversell khi có traffic đồng thời.

### Những phần nên refactor ngay
1. Bảo mật (secret, CORS, CSRF) — làm ngay trước khi làm bất cứ việc gì khác.
2. Repository trả `IQueryable`/paging đúng chuẩn cho Order.
3. Concurrency control cho tồn kho.

### Kế hoạch refactor theo thứ tự ưu tiên
1. **Tuần 1 (bắt buộc trước khi deploy bất kỳ đâu công khai):** xoay vòng toàn bộ secret, sửa CORS, thêm CSRF protection, tắt Swagger production.
2. **Tuần 2:** sửa 2 lỗi phân trang/N+1 trong OrderService, thêm `AsNoTracking()` nhất quán.
3. **Tuần 3:** thêm concurrency token cho `Product`, xử lý oversell.
4. **Tuần 4+:** refactor `OrderService` thành các service nhỏ hơn, đưa business rule vào entity (từng bước, không cần rewrite toàn bộ Domain cùng lúc).
5. **Song song:** viết test cho các Service chưa có test, ưu tiên OrderService/InventoryService/FlashSaleService vì rủi ro nghiệp vụ cao nhất.

### Chấm điểm cuối cùng
**4.0/10** — Chạy được, có nhiều điểm sáng về kỹ thuật tích hợp, nhưng có ít nhất 3 lỗi Critical (secret lộ, CORS/CSRF, in-memory paging + N+1) khiến dự án **không sẵn sàng production** ở dạng hiện tại.

### Đánh giá trình độ lập trình viên
**Junior+, cận Mid-Level.** Có nền tảng tốt hơn Junior thuần nhờ khả năng tự học và tích hợp hệ thống thực tế, nhưng thiếu kinh nghiệm thực chiến về concurrency, ORM query behavior và security fundamentals — những thứ thường chỉ có được qua code review với Senior/Mid, không phải qua tự học một mình.

### Kết luận
Dự án thể hiện rõ một lập trình viên có tinh thần chủ động cao — dám làm hệ thống lớn, tích hợp nhiều dịch vụ thật, áp dụng được một số pattern nâng cao (refresh token rotation, transaction, repository pattern). Tuy nhiên, khoảng cách giữa "trông giống Clean Architecture/DDD" và "thực sự là Clean Architecture/DDD" còn khá xa — đây không phải điều bất thường ở người tự học vì tài liệu về pattern thường dạy cấu trúc thư mục dễ hơn dạy nguyên tắc đằng sau. Rủi ro lớn nhất không nằm ở kiến trúc mà nằm ở 3 lỗi Critical về bảo mật và hiệu năng — đây là những lỗi **phải sửa trước tiên**, độc lập với việc có refactor kiến trúc hay không.
