# Lumen Store — Sales Management

Web app quản lý bán hàng cho cửa hàng văn phòng phẩm Lumen, xây dựng bằng ASP.NET Core 8 + SQL Server.

## Stack

- ASP.NET Core 8 (MVC + Razor Pages)
- Entity Framework Core 8
- SQL Server
- ASP.NET Core Identity
- Clean Architecture (Domain / Application / Infrastructure / API)

## Features

- Quản lý sản phẩm, danh mục, mã vạch
- Bán hàng: lập hóa đơn, áp khuyến mãi, in hóa đơn
- Nhập kho: phiếu nhập, theo dõi nhà cung cấp, xuất PDF
- Kiểm kê tồn kho theo kỳ, cảnh báo hàng sắp hết
- Quản lý khách hàng, đánh giá sản phẩm
- Dashboard: doanh thu, top sản phẩm, cơ cấu theo danh mục

## Project structure

```
├── BookStore.Domain/          # Entities, interfaces
├── BookStore.Application/     # Services, DTOs, business logic
├── BookStore.Infrastructure/  # EF DbContext, migrations, repository impl
└── BookStore.API/             # Controllers, middleware, config
```

## Getting started

**Prerequisites:** .NET 8 SDK, SQL Server

```bash
git clone https://github.com/DUYKHANH42/BookStore_ASP_NC.git
cd BookStore_ASP_NC
```

Cập nhật connection string trong `BookStore.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=LumenStoreDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Chạy migration:

```bash
dotnet ef migrations add InitialCreate --project BookStore.Infrastructure --startup-project BookStore.API
dotnet ef database update --project BookStore.Infrastructure --startup-project BookStore.API
```

Khởi động:

```bash
dotnet run --project BookStore.API
```

Swagger UI: `https://localhost:5001/swagger`

## Authors

Đặng Nguyễn Duy Khánh · Quãng Thành Đạt  
C24A.TH — Khoa CNTT, CĐ Giao thông Vận tải TP.HCM