# BookStore — Báo cáo Triển khai AWS & Kế hoạch Fix

> **Repo:** https://github.com/DUYKHANH42/BookStore_ASP_NC.git  
> **Stack:** ASP.NET Core 8 · Angular · SQL Server 2019 · Docker  
> **Server:** AWS EC2 c7i-flex.large (4GB RAM) · Amazon Linux 2023  
> **Elastic IP:** 13.228.219.111  
> **Cập nhật lần cuối:** 02/09/2026

---

## Mục lục

- [Phần A — Triển khai AWS (Day 1→3)](#phần-a--triển-khai-aws)
  - [A1. Hạ tầng EC2 & Docker](#a1-hạ-tầng-ec2--docker)
  - [A2. CI/CD Pipeline](#a2-cicd-pipeline)
  - [A3. Nginx Reverse Proxy](#a3-nginx-reverse-proxy)
  - [A4. CloudWatch Monitoring](#a4-cloudwatch-monitoring)
- [Phần B — Lỗi hiện tại (Chưa fix)](#phần-b--lỗi-hiện-tại)
- [Phần C — Kế hoạch Fix code (7 Task)](#phần-c--kế-hoạch-fix-code)
- [Phần D — Kiến trúc & Tham chiếu](#phần-d--kiến-trúc--tham-chiếu)

---

## Phần A — Triển khai AWS

### A1. Hạ tầng EC2 & Docker

**Ngày thực hiện:** 25/08/2026

| Mục | Giá trị |
|---|---|
| AMI | Amazon Linux 2023 |
| Instance | c7i-flex.large (2 vCPU, 4GB RAM) |
| Region | ap-southeast-1 |
| Storage | 20GB EBS gp3 |
| Key Pair | docker-server.pem |

**Setup đã thực hiện:**
1. Cài Docker + Docker Compose + Buildx
2. Tạo swap 2GB (cho SQL Server yêu cầu ≥2GB RAM)
3. Clone repo, tạo Dockerfile multi-stage (backend + frontend)
4. Docker Compose: 3 services (frontend Nginx:80, backend:10000, db MSSQL)
5. Import database từ file SQL

**Dockerfile Backend** — multi-stage build, expose port 10000  
**Dockerfile Frontend** — Node build → Nginx serve  
**docker-compose.yml** — 3 services với healthcheck cho DB

---

### A2. CI/CD Pipeline

**Ngày thực hiện:** 28/08/2026

```
Push (develop) → GitHub Actions → Build images → Push ECR → SSH deploy EC2
```

**Đã thiết lập:**

| Thành phần | Chi tiết |
|---|---|
| Elastic IP | 13.228.219.111 (miễn phí khi instance chạy) |
| IAM User | `github-actions-deployer` + policy ECR-Push-Policy |
| ECR Repos | `bookstore-api`, `bookstore-frontend` |
| Linux User | `deployer` (chỉ quyền docker + git, không sudo) |
| SSH Keys | 2 key cho deployer (GitHub Actions SSH + GitHub pull) |
| Workflow | `.github/workflows/deploy.yml` trigger on push develop |

**GitHub Secrets cần thiết:** `EC2_SSH_KEY_B64`, `EC2_HOST`, `EC2_USER`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `AWS_ACCOUNT_ID`

---

### A3. Nginx Reverse Proxy

**Ngày thực hiện:** 30/08/2026

| Route | Target |
|---|---|
| `/` | Frontend Angular |
| `/api/` | Backend ASP.NET Core :10000 |
| `/uploads/` | Backend static files |

**Thay đổi quan trọng:**
- Backend: `ports` → `expose` (chỉ nội bộ Docker network)
- DB: bỏ expose port 1433
- Frontend environment: bỏ `:5000`, tất cả qua port 80
- Security Group: chỉ còn port 22 + 80

---

### A4. CloudWatch Monitoring

**Đã cài đặt:**
- CloudWatch Agent: CPU, RAM, Disk metrics (interval 60s)
- Log group: `BookStore/system-logs` (retention 7 ngày)
- IAM Role: `EC2-CloudWatch-Role` (CloudWatchAgentServerPolicy)

**Alarms (SNS → email):**

| Alarm | Metric | Threshold |
|---|---|---|
| BookStore-High-CPU | CPUUtilization | > 80% / 5min |
| BookStore-High-Memory | mem_used_percent | > 85% / 5min |
| BookStore-High-Disk | disk_used_percent | > 80% / 5min |

---

## Phần B — Lỗi hiện tại

> [!WARNING]
> Ứng dụng đã deploy lên AWS nhưng có 2 vấn đề chính **chưa fix**:

### B1. Backend chạy sai đường dẫn
- API chưa trả response đúng khi gọi qua Nginx reverse proxy
- Cần kiểm tra: base path mapping, CORS config cho domain mới, path prefix `/api`

### B2. Auth (Đăng nhập / Đăng ký) lỗi
- Chức năng đăng nhập và đăng ký không hoạt động trên môi trường AWS
- Nguyên nhân có thể: JWT config, CORS policy, Cookie SameSite, Connection String

---

## Phần C — Kế hoạch Fix code

> **Chiến lược:** 7 task độc lập, làm theo thứ tự. Mỗi task paste kèm bối cảnh chung.

```
BỐI CẢNH CHUNG:
PROJECT: BookStore ASP.NET Core — Clean Architecture 4 layer
  Domain → Application → Infrastructure → API
TECH: .NET 8, EF Core, SQL Server, Identity, SignalR, Redis, QuestPDF
PAYMENT: VNPay, ZaloPay, PayOS
FRONTEND: Angular (repo riêng)
```

### Tổng quan 7 Task

| # | Task | Mức độ | File chính |
|---|---|---|---|
| 1 | **Bảo mật** — secrets, JWT, CORS, Cookie, Swagger | Critical | Startup.cs, appsettings, .gitignore |
| 2 | **Performance** — phân trang DB, N+1, AsNoTracking | Critical | OrderRepository, OrderService, ProductRepository |
| 3 | **Concurrency** — RowVersion chống oversell | High | Product.cs, OrderService, DbContext |
| 4 | **Custom Exceptions** — thay generic Exception | High | Exceptions.cs (mới), ExceptionMiddleware, OrderService |
| 5 | **Database Index** — index cho query phổ biến | Medium | OrderConfiguration, ProductConfiguration (mới) |
| 6 | **Refactor OrderService** — tách God Class | Medium | OrderService, PaymentGateway (mới), Startup.cs |
| 7 | **Cleanup** — Logger, dead code, magic strings | Low | AuthService, OrderConstants |

---

### Task 1: Bảo mật

| # | Việc cần làm |
|---|---|
| 1.1 | Tạo `appsettings.Template.json` thay placeholder cho secrets (JWT, Redis, Mail, Payment, Cloudinary) |
| 1.2 | Startup.cs dòng 103: bỏ fallback JWT secret → fail-fast `throw InvalidOperationException` |
| 1.3 | Startup.cs CORS: xóa `SetIsOriginAllowed`, đọc whitelist từ config `Cors:AllowedOrigins` |
| 1.4 | Cookie SameSite: `None` → `Lax`, SecurePolicy → `SameAsRequest` |
| 1.5 | Swagger chỉ bật trong `env.IsDevelopment()` |
| 1.6 | `.gitignore`: thêm `appsettings.json`, exclude Template |

---

### Task 2: Performance

| # | Việc cần làm |
|---|---|
| 2.1 | IOrderRepository: thêm `GetPagedOrdersAsync`, `GetUserOrdersPagedAsync`, `GetOrdersForReportAsync` |
| 2.2 | OrderRepository: implement phân trang DB-level (Skip/Take + CountAsync) |
| 2.3 | OrderService: sửa 3 method dùng repo mới thay vì load toàn bộ rồi filter C# |
| 2.4 | ProductRepository + OrderRepository: thêm `.AsNoTracking()` cho read-only queries |

---

### Task 3: Concurrency Control

| # | Việc cần làm |
|---|---|
| 3.1 | Product.cs: thêm `[Timestamp] byte[] RowVersion` |
| 3.2 | Tạo ProductConfiguration.cs: `IsRowVersion()` |
| 3.3 | Migration: `AddProductRowVersion` |
| 3.4 | OrderService.PlaceOrderAsync: catch `DbUpdateConcurrencyException` + retry |
| 3.5 | Áp dụng cho CreatePOSOrderAsync |

---

### Task 4: Custom Exceptions

| # | Việc cần làm |
|---|---|
| 4.1 | Tạo `Domain/Common/Exceptions.cs`: BusinessException(400), NotFoundException(404), ConflictException(409), ForbiddenException(403), InsufficientStockException |
| 4.2 | ExceptionMiddleware: pattern match exception → đúng status code + chỉ log Error cho 500 |
| 4.3 | OrderService: thay 4 chỗ `throw new Exception(...)` bằng custom exceptions |

---

### Task 5: Database Index

| # | Việc cần làm |
|---|---|
| 5.1 | OrderConfiguration: index Status, CreatedAt, OrderNumber, UserId, composite(Status+CreatedAt) |
| 5.2 | ProductConfiguration: index CategoryId, SubCategoryId, IsActive, composite(IsActive+CreatedAt) |
| 5.3 | StockHistoryConfiguration: index ProductId, CreatedAt |
| 5.4 | Migration: `AddDatabaseIndexes` |

---

### Task 6: Refactor OrderService (God Class)

| # | Việc cần làm |
|---|---|
| 6.1 | Extract `RestoreStockForCancelledOrder()` — gộp logic hoàn kho trùng lặp |
| 6.2 | Tạo `IPaymentGateway` interface + 3 implement (ZaloPay, VNPay, PayOS) |
| 6.3 | Tạo `PaymentGatewayFactory` — thay if/else chain trong ProcessCheckoutAsync |
| 6.4 | DI registration trong Startup.cs |

---

### Task 7: Cleanup

| # | Việc cần làm |
|---|---|
| 7.1 | AuthService: `Console.WriteLine` → `ILogger` |
| 7.2 | Xóa `BookStore.Tests/UnitTest1.cs` (test rỗng) |
| 7.3 | Thêm `StockConstants` vào OrderConstants.cs, thay magic strings |

---

## Phần D — Kiến trúc & Tham chiếu

### Kiến trúc hiện tại

```
                    Internet
                       │
                  Port 80 + 22
                       │
┌──────────────────────▼──────────────────────┐
│              EC2 Instance                    │
│         Elastic IP: 13.228.219.111           │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │         Docker Compose                 │  │
│  │                                        │  │
│  │  Nginx (:80) ── Reverse Proxy ──┐     │  │
│  │  ├─ /       → Angular           │     │  │
│  │  ├─ /api    → ASP.NET (:10000)──┤     │  │
│  │  └─ /uploads→ ASP.NET (:10000)  │     │  │
│  │                  │               │     │  │
│  │              SQL Server          │     │  │
│  │              (internal)          │     │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  CloudWatch Agent → CPU/RAM/Disk + Alarms    │
└──────────────────────────────────────────────┘
         │
   GitHub Actions CI/CD
   (develop → build → ECR → SSH deploy)
```

### Truy cập

| Service | URL |
|---|---|
| Frontend | http://13.228.219.111 |
| Backend API | http://13.228.219.111/api |
| Swagger | ❌ Ẩn (production) |
| CloudWatch | AWS Console |
| CI/CD | GitHub Actions tab |

### Troubleshooting đã gặp

| Vấn đề | Fix |
|---|---|
| SSH `no key found` | SSH trực tiếp + base64 encode key |
| `Permission denied (publickey)` | Ghi đè authorized_keys đúng public key |
| Backend crash khi start | DB healthcheck + `condition: service_healthy` |
| IP thay đổi stop/start | Elastic IP |
| Frontend gọi API IP cũ | Rebuild frontend (`--build`) |
| CloudWatch permission denied | `sudo tee` thay `>` redirect |

### TODO

- [ ] Fix backend routing sai đường dẫn trên AWS
- [ ] Fix auth (đăng nhập/đăng ký) lỗi
- [ ] HTTPS (Let's Encrypt + domain)
- [ ] Giới hạn SSH port 22 cho IP cụ thể
- [ ] Tách DB ra Amazon RDS
- [ ] Auto rollback khi deploy lỗi
- [ ] Container monitoring script + crontab
- [ ] Thêm unit test vào CI/CD pipeline
