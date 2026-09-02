
# Report Day 3: Nginx Reverse Proxy + CloudWatch Monitoring

## Thông tin dự án
- **Repository:** https://github.com/DUYKHANH42/BookStore_ASP_NC.git
- **Server:** AWS EC2 - Amazon Linux 2023 (c7i-flex.large)
- **Elastic IP:** 13.228.219.111
- **Ngày thực hiện:** 30/08/2026

---

## 1. Nginx Reverse Proxy

### Vấn đề trước khi setup
- Frontend chạy port 80, Backend chạy port 5000 → **2 port khác nhau**
- Port 5000 mở ra internet → **rủi ro bảo mật**
- Swagger UI lộ ra ngoài → không nên ở production

### Kiến trúc sau khi setup

```
Client truy cập http://13.228.219.111
       │
       ▼
  Nginx (port 80)
       ├── /          → Frontend (Angular)
       ├── /api       → Backend (ASP.NET Core :10000)
       └── /uploads   → Backend (static files)
```

### Sửa Nginx Config

File: `BookStore_GiaoDien/nginx.conf`

```nginx
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html/browser;
    index index.html;

    # Frontend Angular
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Reverse Proxy → Backend API
    location /api/ {
        proxy_pass http://bookstore-app:10000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Reverse Proxy → Uploads
    location /uploads/ {
        proxy_pass http://bookstore-app:10000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Sửa Frontend Environment

File: `BookStore_GiaoDien/src/environments/environment.ts`

```typescript
export const environment = {
  apiUrl: 'http://13.228.219.111/api',
  uploadUrl: 'http://13.228.219.111/uploads',
  production: true
};
```

> Không cần port 5000 nữa — tất cả qua port 80!

### Sửa docker-compose.yml

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
    expose:
      - "10000"
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

**Thay đổi quan trọng:**
- Backend: `ports` → `expose` (chỉ mở trong Docker network, không ra internet)
- DB: bỏ `ports: 1433` (không expose ra ngoài)
- DB: thêm `healthcheck` để backend chờ DB ready mới start

### Rebuild và chạy

```bash
docker compose down
docker compose up -d --build
```

### Kiểm tra

```bash
# Test API qua Nginx proxy
curl http://localhost/api/categories
```

### Xóa port 5000 khỏi Security Group

Vào **AWS Console → EC2 → Security Groups** → xóa Inbound Rule port 5000.

### Kết quả

| Trước | Sau |
|---|---|
| Frontend: `http://13.228.219.111` (port 80) | Frontend: `http://13.228.219.111` (port 80) |
| Backend: `http://13.228.219.111:5000/api` | Backend: `http://13.228.219.111/api` |
| Swagger: `http://13.228.219.111:5000/swagger` | ❌ Ẩn (bảo mật) |
| Mở port: 80, 5000, 1433 | **Chỉ port 80 + 22** |

---

## 2. Healthcheck cho Database

### Vấn đề
- Khi start containers, backend khởi động trước DB ready → kết nối lỗi → crash → restart
- `depends_on` mặc định chỉ chờ container start, không chờ service ready

### Giải pháp
Thêm `healthcheck` cho DB container trong `docker-compose.yml`:

```yaml
db:
  healthcheck:
    test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "BookStore@Str0ng!" -C -Q "SELECT 1" || exit 1
    interval: 10s
    timeout: 5s
    retries: 10
    start_period: 30s
```

Backend dùng `condition: service_healthy`:

```yaml
bookstore-app:
  depends_on:
    db:
      condition: service_healthy
```

### Kết quả

| Trước | Sau |
|---|---|
| Backend crash → restart 1-2 lần | Backend chờ DB healthy rồi mới start |
| ~30-60s (do restart loop) | ~30s (start sạch 1 lần) |

---

## 3. CloudWatch Monitoring

### 3.1 Cài đặt CloudWatch Agent

```bash
sudo yum install amazon-cloudwatch-agent -y
```

### 3.2 Tạo Config File

```bash
sudo tee /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json << 'EOF'
{
  "metrics": {
    "namespace": "BookStore/EC2",
    "metrics_collected": {
      "mem": {
        "measurement": ["mem_used_percent", "mem_total", "mem_used"],
        "metrics_collection_interval": 60
      },
      "disk": {
        "measurement": ["disk_used_percent", "disk_free"],
        "resources": ["/"],
        "metrics_collection_interval": 60
      },
      "cpu": {
        "measurement": ["cpu_usage_active", "cpu_usage_idle"],
        "totalcpu": true,
        "metrics_collection_interval": 60
      }
    },
    "append_dimensions": {
      "InstanceId": "${aws:InstanceId}"
    }
  },
  "logs": {
    "logs_collected": {
      "files": {
        "collect_list": [
          {
            "file_path": "/var/log/messages",
            "log_group_name": "BookStore/system-logs",
            "log_stream_name": "{instance_id}",
            "retention_in_days": 7
          }
        ]
      }
    }
  }
}
EOF
```

### 3.3 Tạo IAM Role cho EC2

1. Vào **AWS Console → IAM → Roles → Create role**
2. Trusted entity: **AWS service → EC2**
3. Gán policy: **CloudWatchAgentServerPolicy**
4. Tên role: `EC2-CloudWatch-Role`
5. Create role

### 3.4 Gán Role cho EC2 Instance

1. Vào **EC2 → Instances** → chọn instance
2. **Actions → Security → Modify IAM role**
3. Chọn `EC2-CloudWatch-Role` → **Update IAM role**

### 3.5 Khởi động CloudWatch Agent

```bash
sudo /opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl \
  -a fetch-config \
  -m ec2 \
  -c file:/opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json \
  -s

# Kiểm tra status
sudo /opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl -a status
# Kết quả: "status": "running"
```

---

## 4. CloudWatch Alarms

### Tạo SNS Topic cho Notifications

1. Vào **CloudWatch → Alarms → Create alarm**
2. Ở bước Configure actions → **Create new SNS topic**
3. Topic name: `BookStore-Alerts`
4. Email: nhập email
5. Vào email **confirm subscription**

### Alarm 1: CPU cao

| Mục | Giá trị |
|---|---|
| Metric | EC2 → Per-Instance → CPUUtilization |
| Instance | i-0430b9cd20767a4a0 |
| Condition | Greater than **80%** |
| Period | 5 minutes |
| Action | SNS → BookStore-Alerts |
| Alarm name | `BookStore-High-CPU` |

### Alarm 2: RAM cao

| Mục | Giá trị |
|---|---|
| Metric | BookStore/EC2 → mem_used_percent |
| Condition | Greater than **85%** |
| Period | 5 minutes |
| Action | SNS → BookStore-Alerts |
| Alarm name | `BookStore-High-Memory` |

### Alarm 3: Disk đầy

| Mục | Giá trị |
|---|---|
| Metric | BookStore/EC2 → disk_used_percent |
| Condition | Greater than **80%** |
| Period | 5 minutes |
| Action | SNS → BookStore-Alerts |
| Alarm name | `BookStore-High-Disk` |

---

## 5. Security Group cuối cùng

| Type | Port | Source | Mô tả |
|---|---|---|---|
| SSH | 22 | 0.0.0.0/0 | SSH (nên giới hạn IP nhà) |
| HTTP | 80 | 0.0.0.0/0 | Web (Frontend + API) |

> ❌ Đã xóa port 5000 và 1433

---

## 6. Các vấn đề đã gặp và cách giải quyết

| Vấn đề | Nguyên nhân | Cách fix |
|---|---|---|
| Frontend gọi API IP cũ (18.141.56.67) | Docker build cache, chưa rebuild frontend | `docker compose up -d --build frontend` |
| Swagger 404 qua Nginx proxy | Nginx proxy path không khớp | Ẩn Swagger ở production (best practice) |
| Backend crash khi start | DB chưa ready, backend kết nối lỗi | Thêm healthcheck cho DB + `condition: service_healthy` |
| CloudWatch Agent permission denied | Dùng `>` redirect với sudo không hoạt động | Dùng `sudo tee` thay thế |
| SNS email không nhận | Email vào spam hoặc chưa gửi | Check spam + Resend confirmation |

---

## 7. Kiến trúc hệ thống hiện tại

```
┌─────────────────────────────────────────────────┐
│              EC2 Instance                         │
│         (c7i-flex.large, 4GB RAM)                │
│         Elastic IP: 13.228.219.111               │
│                                                  │
│  ┌─────────────────────────────────────────┐     │
│  │         Docker Compose                   │     │
│  │                                          │     │
│  │  ┌──────────────┐                       │     │
│  │  │   Frontend    │                       │     │
│  │  │ (Nginx:80)   │──── Reverse Proxy ──┐ │     │
│  │  │ Angular      │                     │ │     │
│  │  └──────────────┘                     │ │     │
│  │                                       ▼ │     │
│  │  ┌──────────────┐    ┌──────────────┐  │     │
│  │  │   Backend    │◄──▶│   Database   │  │     │
│  │  │ (ASP.NET)    │    │ (SQL Server) │  │     │
│  │  │ expose:10000 │    │ internal     │  │     │
│  │  └──────────────┘    └──────────────┘  │     │
│  │         Docker Internal Network         │     │
│  └─────────────────────────────────────────┘     │
│                                                  │
│  ┌─────────────────────────────────────────┐     │
│  │   CloudWatch Agent                       │     │
│  │   → CPU, RAM, Disk metrics              │     │
│  │   → System logs                          │     │
│  └─────────────────────────────────────────┘     │
└─────────────────────────────────────────────────┘
         │
    Port 80 only
         │
    ┌────▼────────────────────┐
    │      Internet            │
    │   Security Group:        │
    │   - Port 22 (SSH)        │
    │   - Port 80 (HTTP)       │
    └──────────────────────────┘
         │
    ┌────▼────────────────────┐
    │   CloudWatch             │
    │   - Metrics Dashboard    │
    │   - Alarms (CPU/RAM/Disk)│
    │   - SNS Email Alerts     │
    └──────────────────────────┘
```

---

## 8. Tổng kết 3 ngày

| Day | Nội dung |
|---|---|
| **Day 1** | EC2 setup, Docker, Docker Compose, Deploy backend + frontend + DB |
| **Day 2** | Elastic IP, IAM User, ECR, CI/CD GitHub Actions, Linux user `deployer` |
| **Day 3** | Nginx Reverse Proxy, DB Healthcheck, CloudWatch Monitoring + Alarms |

---

## 9. Truy cập ứng dụng

| Service | URL |
|---|---|
| Frontend | http://13.228.219.111 |
| Backend API | http://13.228.219.111/api |
| Swagger | ❌ Ẩn (bảo mật) |
| CloudWatch | AWS Console → CloudWatch |
| CI/CD | GitHub repo → Actions tab |

---

## 10. TODO (Tiếp theo)
- [ ] HTTPS với Let's Encrypt (mua domain hoặc dùng domain miễn phí)
- [ ] Giới hạn port 22 chỉ cho IP nhà
- [ ] Container monitoring script + crontab
- [ ] UptimeRobot (giám sát từ bên ngoài)
- [ ] Tách Database ra Amazon RDS
- [ ] Auto rollback khi deploy lỗi
