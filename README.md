 BookStore Management System (.NET 8)
Dự án Hệ thống Quản lý Nhà sách được xây dựng bằng công nghệ ASP.NET Core Web API theo kiến trúc Clean Architecture. Dự án tập trung vào tính mở rộng, bảo trì dễ dàng và hiệu suất cao với phân trang dữ liệu từ Database.
🚀 Tính năng nổi bật
Clean Architecture: Phân tách rõ ràng giữa Domain, Application, Infrastructure và API.
Repository Pattern & Unit of Work: Quản lý truy xuất dữ liệu chuyên nghiệp và đảm bảo tính toàn vẹn dữ liệu (Transaction).
ASP.NET Core Identity: Tích hợp quản lý người dùng, phân quyền và bảo mật.
Pagination (SQL Level): Phân trang dữ liệu trực tiếp từ SQL Server (Skip/Take), giúp tối ưu hóa hiệu năng cho dữ liệu lớn.
DTOs & Mapping: Sử dụng Data Transfer Objects để bảo mật dữ liệu và giải quyết lỗi vòng lặp JSON (Circular Reference).
Swagger UI: Tài liệu API tương tác dễ dàng để kiểm thử.
CORS Configuration: Cấu hình sẵn sàng kết nối với Frontend (Angular/React).
🛠 Công nghệ sử dụng
Backend: .NET 8 SDK, ASP.NET Core Web API.
Database: Microsoft SQL Server.
ORM: Entity Framework Core 8.
Security: Microsoft Identity.
Tools: Swagger (OpenAPI), NuGet Packages (SqlServer, Tools, Design).
📂 Cấu trúc dự án
code
Text
├── BookStore.Domain         # Entities, Enums, Repository Interfaces, Unit of Work Interface
├── BookStore.Application    # Services, DTOs (Data Transfer Objects), Business Logic
├── BookStore.Infrastructure # DbContext, Migrations, Repository Implementation, Unit of Work Implementation
└── BookStore.API            # Controllers, Configuration (Startup/Program), Middleware
⚙️ Hướng dẫn cài đặt
1. Yêu cầu hệ thống
.NET 8 SDK.
SQL Server (LocalDB hoặc SQL Express).
2. Cấu hình Database
Mở file appsettings.json trong project BookStore.API và cập nhật chuỗi kết nối:
code
JSON
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=BookStoreDb;Trusted_Connection=True;TrustServerCertificate=True"
}
3. Chạy Migration (Tạo bảng)
Mở Package Manager Console và thực hiện:
code
Powershell
Add-Migration InitialCreate -Project BookStore.Infrastructure -StartupProject BookStore.API
Update-Database -Project BookStore.Infrastructure -StartupProject BookStore.API
4. Chạy ứng dụng
Nhấn F5 hoặc dùng lệnh:
code
Bash
dotnet run --project BookStore.API
📡 API Endpoints (Mẫu)
Method	Endpoint	Description
GET	/api/books	Lấy danh sách sách (có phân trang)
GET	/api/books/{id}	Lấy chi tiết một cuốn sách
GET	/api/books/category/{id}	Lấy sách theo danh mục (có phân trang)
GET	/api/books/subcategory/{id}	Lấy sách theo danh mục con (có phân trang)
📝 Liên hệ
Tác giả: Duy Khánh
GitHub: DUYKHANH42
Project Link: BookStore_ASP_NC
