
# Hướng dẫn Triển khai Dự án BookStore trên AWS EC2 với Docker

## Thông tin dự án
- **Repository:** https://github.com/DUYKHANH42/BookStore_ASP_NC.git
- **Backend:** ASP.NET Core 8.0 (BookStore.API)
- **Frontend:** Angular (BookStore_GiaoDien)
- **Database:** SQL Server 2019
- **Server:** AWS EC2 - Amazon Linux 2023
- **Ngày thực hiện:** 25/08/2026

---

## 1. Tạo EC2 Instance

### Thông số cấu hình
| Mục | Giá trị |
|---|---|
| AMI | Amazon Linux 2023 |
| Instance Type | c7i-flex.large (2 vCPU, 4GB RAM) |
| Region | ap-southeast-1 (Singapore) |
| Storage | 20GB EBS (gp3) |
| Key Pair | docker-server.pem |

### Security Group (Inbound Rules)
| Type | Port | Source | Mô tả |
|---|---|---|---|
| SSH | 22 | IP của bạn | Truy cập SSH |
| HTTP | 80 | 0.0.0.0/0 | Frontend Angular |
| Custom TCP | 5000 | 0.0.0.0/0 | Backend API |

> ⚠️ **Lưu ý:** KHÔNG mở port 1433 (SQL Server) ra ngoài internet!

---

## 2. SSH vào Server

```bash
ssh -i docker-server.pem ec2-user@<PUBLIC_IP>
```

---

## 3. Cài đặt Docker

```bash
# Cập nhật hệ thống
sudo yum update -y

# Cài Docker
sudo yum install docker -y

# Khởi động Docker và bật auto-start
sudo systemctl start docker
sudo systemctl enable docker

# Thêm user vào group docker
sudo usermod -aG docker ec2-user
newgrp docker

# Kiểm tra
docker --version
docker run hello-world
```

---

## 4. Cài đặt Docker Compose

```bash
# Tạo thư mục plugin
sudo mkdir -p /usr/local/lib/docker/cli-plugins

# Tải Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/lib/docker/cli-plugins/docker-compose

# Cấp quyền thực thi
sudo chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

# Kiểm tra
docker compose version
```

---

## 5. Cài đặt Docker Buildx (nếu cần)

```bash
BUILDX_VERSION=$(curl -s https://api.github.com/repos/docker/buildx/releases/latest | grep '"tag_name"' | cut -d'"' -f4)
sudo curl -L "https://github.com/docker/buildx/releases/download/${BUILDX_VERSION}/buildx-${BUILDX_VERSION}.linux-amd64" -o /usr/local/lib/docker/cli-plugins/docker-buildx
sudo chmod +x /usr/local/lib/docker/cli-plugins/docker-buildx

# Kiểm tra
docker buildx version
```

---

## 6. Tạo Swap (nếu instance RAM < 4GB)

```bash
sudo dd if=/dev/zero of=/swapfile bs=1M count=2048
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile swap swap defaults 0 0' | sudo tee -a /etc/fstab

# Kiểm tra
free -h
```

---

## 7. Mở rộng ổ đĩa EBS (nếu cần)

### Trên AWS Console:
1. EC2 → Instances → chọn instance
2. Tab Storage → click Volume ID
3. Actions → Modify Volume → tăng lên 20GB
4. Confirm

### Trên Server:
```bash
sudo growpart /dev/nvme0n1 1
sudo xfs_growfs /

# Kiểm tra
df -h
```

---

## 8. Clone dự án

```bash
sudo yum install git -y
git clone https://github.com/DUYKHANH42/BookStore_ASP_NC.git
cd BookStore_ASP_NC
```

---

## 9. Cấu trúc Dockerfile (Backend)

File: `BookStore_ASP_NC/Dockerfile`

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file và .csproj để restore (tận dụng Docker cache)
COPY BookStore.sln ./
COPY BookStore.API/BookStore.API.csproj ./BookStore.API/
COPY BookStore.Application/BookStore.Application.csproj ./BookStore.Application/
COPY BookStore.Domain/BookStore.Domain.csproj ./BookStore.Domain/
COPY BookStore.Infrastructure/BookStore.Infrastructure.csproj ./BookStore.Infrastructure/

RUN dotnet restore ./BookStore.API/BookStore.API.csproj

# Copy toàn bộ source code và publish
COPY BookStore.API/ ./BookStore.API/
COPY BookStore.Application/ ./BookStore.Application/
COPY BookStore.Domain/ ./BookStore.Domain/
COPY BookStore.Infrastructure/ ./BookStore.Infrastructure/

RUN dotnet publish ./BookStore.API/BookStore.API.csproj -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "BookStore.API.dll"]
```

---

## 10. Cấu trúc Dockerfile (Frontend Angular)

File: `BookStore_GiaoDien/Dockerfile`

```dockerfile
# Stage 1: Build Angular
FROM node:18-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build -- --configuration=production

# Stage 2: Serve with Nginx
FROM nginx:alpine
COPY --from=build /app/dist/book-store-giao-dien/browser /usr/share/nginx/html/browser
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

---

## 11. Nginx Config (Frontend)

File: `BookStore_GiaoDien/nginx.conf`

```nginx
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html/browser;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

---

## 12. Environment Frontend

File: `BookStore_GiaoDien/src/environments/environment.ts`

```typescript
export const environment = {
  apiUrl: 'http://<PUBLIC_IP>:5000/api',
  uploadUrl: 'http://<PUBLIC_IP>:5000/uploads',
  production: true
};
```

---

## 13. Docker Compose

File: `docker-compose.yml`

```yaml
services:
  frontend:
    build: ./BookStore_GiaoDien
    ports:
      - "80:80"
    depends_on:
      - bookstore-app
    restart: always

  bookstore-app:
    build: .
    ports:
      - "5000:10000"
    depends_on:
      db:
        condition: service_healthy
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=BookStoreDb;User Id=sa;Password=BookStore@Str0ng!;TrustServerCertificate=True
    restart: always

  db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=BookStore@Str0ng!
    ports:
      - "1433:1433"
    volumes:
      - db_data:/var/opt/mssql
    restart: always
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "BookStore@Str0ng!" -C -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s

volumes:
  db_data:
```

---

## 14. Build và Chạy

```bash
# Build và chạy tất cả containers
docker compose up -d --build

# Kiểm tra trạng thái
docker compose ps

# Xem logs
docker compose logs -f
docker compose logs bookstore-app --tail=20
docker compose logs db --tail=20
docker compose logs frontend --tail=20
```

---

## 15. Import Database

```bash
# Copy file SQL vào container
docker cp bookstore_db_final.sql bookstore_asp_nc-db-1:/tmp/

# Tạo database
docker exec -it bookstore_asp_nc-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "BookStore@Str0ng!" -C -Q "CREATE DATABASE BookStoreDb"

# Import data
docker exec -it bookstore_asp_nc-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "BookStore@Str0ng!" -C -d BookStoreDb -i /tmp/bookstore_db_final.sql

# Restart app để kết nối lại DB
docker compose restart bookstore-app
```

---

## 16. Truy cập ứng dụng

| Service | URL |
|---|---|
| Frontend | http://<PUBLIC_IP> |
| Backend API (Swagger) | http://<PUBLIC_IP>:5000/swagger/index.html |
| Database | Internal only (Docker network) |

---

## 17. Cập nhật code (khi push code mới lên GitHub)

```bash
cd BookStore_ASP_NC
git pull
docker compose up -d --build
```

---

## 18. Các lệnh Docker hữu ích

```bash
# Xem tất cả containers đang chạy
docker compose ps

# Dừng tất cả
docker compose down

# Dừng và xóa volumes (mất data DB!)
docker compose down -v

# Xem logs realtime
docker compose logs -f

# Restart 1 service
docker compose restart bookstore-app

# Vào trong container
docker exec -it bookstore_asp_nc-db-1 bash

# Dọn dẹp Docker (xóa images/containers không dùng)
docker system prune -a -f

# Xem dung lượng Docker
docker system df
```

---

## 19. Lưu ý quan trọng

### Bảo mật
- ❌ KHÔNG mở port 1433 (DB) ra internet
- ✅ Giới hạn SSH (port 22) chỉ cho IP của bạn
- ✅ Database chỉ truy cập qua Docker internal network
- ⚠️ HTTP không mã hóa — cần HTTPS cho production (dùng Nginx + Let's Encrypt)

### Chi phí
- Instance c7i-flex.large: ~$36/tháng
- EBS 20GB: ~$2/tháng
- **Tắt instance khi không dùng** để tiết kiệm credits!
- Mỗi lần stop/start, Public IP sẽ thay đổi (gán Elastic IP nếu muốn cố định)

### SQL Server
- Yêu cầu tối thiểu 2GB RAM
- Password phải đủ mạnh (chữ hoa + thường + số + ký tự đặc biệt)
- Dùng volume để persist data (không mất khi restart container)

---

## 20. Kiến trúc hệ thống

```
┌─────────────────────────────────────────────┐
│              EC2 Instance                     │
│         (c7i-flex.large, 4GB RAM)            │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │         Docker Compose                  │  │
│  │                                         │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────┐ │  │
│  │  │ Frontend │  │ Backend  │  │  DB   │ │  │
│  │  │ (Nginx)  │  │ (ASP.NET)│  │(MSSQL)│ │  │
│  │  │ Port 80  │  │ Port 5000│  │ 1433  │ │  │
│  │  └──────────┘  └──────────┘  └──────┘ │  │
│  │       │              │            │     │  │
│  │       └──────────────┴────────────┘     │  │
│  │            Docker Network               │  │
│  └────────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
         │              │
    Port 80        Port 5000
         │              │
    ┌────┴──────────────┴────┐
    │       Internet          │
    │   (Security Group)      │
    └─────────────────────────┘
```

---

## Tiếp theo (TODO)
- [ ] CI/CD Pipeline (GitHub Actions → tự động deploy khi push code)
- [ ] HTTPS với Let's Encrypt + Nginx reverse proxy
- [ ] Elastic IP (giữ IP cố định)
- [ ] Tách Database ra Amazon RDS
- [ ] Monitoring & Logging
