
# Report Day 2: CI/CD Pipeline cho BookStore trên AWS EC2

## Thông tin dự án
- **Repository:** https://github.com/DUYKHANH42/BookStore_ASP_NC.git
- **Server:** AWS EC2 - Amazon Linux 2023 (c7i-flex.large)
- **Elastic IP:** 13.228.219.111
- **CI/CD Tool:** GitHub Actions
- **Container Registry:** Amazon ECR
- **Ngày thực hiện:** 28/08/2026

---

## 1. Tổng quan CI/CD Flow

```
Developer push code lên GitHub (branch develop)
       │
       ▼
GitHub Actions tự động trigger
       │
       ▼
Build Docker images (Backend + Frontend)
       │
       ▼
Push images lên Amazon ECR
       │
       ▼
SSH vào EC2 (user: deployer)
       │
       ▼
Pull code mới + docker compose up
       │
       ▼
App tự động cập nhật! ✅
```

---

## 2. Gán Elastic IP (IP cố định)

### Vấn đề
- Mỗi lần stop/start EC2 instance, Public IP thay đổi
- CI/CD cần IP cố định để SSH vào server

### Các bước thực hiện
1. Vào **AWS Console → EC2 → Elastic IPs**
2. Click **Allocate Elastic IP address** → Allocate
3. Chọn Elastic IP → **Actions → Associate Elastic IP address**
4. Chọn instance → Associate

### Kết quả
| Mục | Giá trị |
|---|---|
| Elastic IP | 13.228.219.111 |
| Allocation ID | eipalloc-04c483ce4c5952ab8 |
| Association ID | eipassoc-0609300c613134388 |
| Instance ID | i-0430b9cd20767a4a0 |
| Public DNS | ec2-13-228-219-111.ap-southeast-1.compute.amazonaws.com |

### Chi phí Elastic IP
| Trạng thái | Chi phí |
|---|---|
| Instance đang chạy | ✅ Miễn phí |
| Instance đã stop | ❌ ~$0.005/giờ (~$3.6/tháng) |

---

## 3. Tạo IAM User cho CI/CD

### Các bước thực hiện
1. Vào **AWS Console → IAM → Users → Create user**
2. User name: `github-actions-deployer`
3. Tạo custom policy `ECR-Push-Policy` với quyền:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:BatchGetImage",
        "ecr:PutImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload",
        "ecr:CreateRepository",
        "ecr:DescribeRepositories"
      ],
      "Resource": "*"
    }
  ]
}
```

4. Gán policy cho user
5. Tạo **Access Key** (Security credentials → Create access key → Third-party service)

---

## 4. Tạo ECR Repositories

### Các bước thực hiện
1. Vào **AWS Console → ECR (Elastic Container Registry)**
2. Tạo repository: `bookstore-api`
3. Tạo repository: `bookstore-frontend`
4. Region: `ap-southeast-1`

---

## 5. Tạo Linux User `deployer` trên EC2

### Tại sao cần user riêng?
- Không dùng `ec2-user` (admin) cho CI/CD → best practice bảo mật
- User `deployer` chỉ có quyền docker + git pull, không có sudo

### Các lệnh thực hiện

```bash
# Tạo user
sudo useradd -m -s /bin/bash deployer

# Thêm vào group docker
sudo usermod -aG docker deployer

# Clone dự án cho deployer
sudo -u deployer git clone https://github.com/DUYKHANH42/BookStore_ASP_NC.git /home/deployer/BookStore_ASP_NC
```

### Phân quyền
| User | Vai trò | Quyền |
|---|---|---|
| `ec2-user` | Admin, SSH thủ công | Toàn quyền sudo |
| `deployer` | CI/CD only | Chỉ docker + git pull |

---

## 6. Tạo SSH Key cho CI/CD (user deployer)

### SSH Key để GitHub Actions SSH vào EC2

```bash
# Tạo SSH key dạng RSA PEM (tương thích tốt nhất)
sudo -u deployer ssh-keygen -t rsa -b 4096 -m PEM -f /home/deployer/.ssh/github-actions-new -N ""

# Thêm public key vào authorized_keys
sudo bash -c 'cat /home/deployer/.ssh/github-actions-new.pub > /home/deployer/.ssh/authorized_keys'
sudo chmod 700 /home/deployer/.ssh
sudo chmod 600 /home/deployer/.ssh/authorized_keys
sudo chown deployer:deployer /home/deployer/.ssh/authorized_keys

# Lấy private key → encode base64 để lưu vào GitHub Secret
sudo cat /home/deployer/.ssh/github-actions-new | base64 -w 0
```

### SSH Key để deployer pull code từ GitHub

```bash
# Tạo SSH key cho GitHub
sudo -u deployer ssh-keygen -t ed25519 -f /home/deployer/.ssh/github -N ""

# Cấu hình SSH config
sudo -u deployer bash -c 'cat > /home/deployer/.ssh/config << EOF
Host github.com
  IdentityFile ~/.ssh/github
  IdentitiesOnly yes
EOF'

# Thêm github.com vào known_hosts
sudo -u deployer bash -c 'ssh-keyscan -t ed25519 github.com >> /home/deployer/.ssh/known_hosts'

# Đổi remote URL sang SSH
sudo -u deployer git -C /home/deployer/BookStore_ASP_NC remote set-url origin git@github.com:DUYKHANH42/BookStore_ASP_NC.git

# Lấy public key → thêm vào GitHub SSH keys
sudo cat /home/deployer/.ssh/github.pub
```

### Test kết nối

```bash
# Test SSH vào server bằng deployer
sudo -u deployer ssh -i /home/deployer/.ssh/github-actions-new -o StrictHostKeyChecking=no deployer@13.228.219.111

# Test GitHub auth cho deployer
sudo -u deployer ssh -T git@github.com
# Kết quả: Hi DUYKHANH42! You've successfully authenticated
```

---

## 7. Cấu hình GitHub Secrets

Vào **GitHub repo → Settings → Secrets and variables → Actions → New repository secret**

| Secret Name | Value | Mô tả |
|---|---|---|
| `EC2_SSH_KEY_B64` | Private key (base64 encoded) | SSH key để connect EC2 |
| `EC2_HOST` | `13.228.219.111` | Elastic IP của EC2 |
| `EC2_USER` | `deployer` | Linux user cho CI/CD |
| `AWS_ACCESS_KEY_ID` | Access Key từ IAM user | Để login ECR |
| `AWS_SECRET_ACCESS_KEY` | Secret Key từ IAM user | Để login ECR |
| `AWS_REGION` | `ap-southeast-1` | AWS Region |
| `AWS_ACCOUNT_ID` | Account ID (12 số) | Để login ECR |

---

## 8. GitHub Actions Workflow

File: `.github/workflows/deploy.yml`

```yaml
name: Build & Deploy BookStore

on:
  push:
    branches:
      - develop

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ secrets.AWS_REGION }}

      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build & Push Backend image
        env:
          ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
        run: |
          docker build -t $ECR_REGISTRY/bookstore-api:latest -t $ECR_REGISTRY/bookstore-api:${{ github.sha }} .
          docker push $ECR_REGISTRY/bookstore-api:latest
          docker push $ECR_REGISTRY/bookstore-api:${{ github.sha }}

      - name: Build & Push Frontend image
        env:
          ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
        run: |
          docker build -t $ECR_REGISTRY/bookstore-frontend:latest -t $ECR_REGISTRY/bookstore-frontend:${{ github.sha }} ./BookStore_GiaoDien
          docker push $ECR_REGISTRY/bookstore-frontend:latest
          docker push $ECR_REGISTRY/bookstore-frontend:${{ github.sha }}

      - name: Deploy to EC2
        env:
          SSH_KEY_B64: ${{ secrets.EC2_SSH_KEY_B64 }}
          EC2_HOST: ${{ secrets.EC2_HOST }}
          EC2_USER: ${{ secrets.EC2_USER }}
        run: |
          echo "$SSH_KEY_B64" | base64 -d > /tmp/ssh_key
          chmod 600 /tmp/ssh_key
          
          ssh -o StrictHostKeyChecking=no -i /tmp/ssh_key $EC2_USER@$EC2_HOST << 'ENDSSH'
            cd BookStore_ASP_NC
            git pull origin develop
            docker compose up -d --build
            docker image prune -f
          ENDSSH
          
          rm -f /tmp/ssh_key
```

---

## 9. Cấu hình SSH Key cho ec2-user (push code)

User `ec2-user` cũng cần SSH key để push code lên GitHub:

```bash
# Tạo SSH key
ssh-keygen -t ed25519 -f ~/.ssh/github -N ""

# Cấu hình SSH
cat >> ~/.ssh/config << 'EOF'
Host github.com
  IdentityFile ~/.ssh/github
  IdentitiesOnly yes
EOF

# Thêm github.com vào known_hosts
ssh-keyscan -t ed25519 github.com >> ~/.ssh/known_hosts

# Đổi remote URL sang SSH
git remote set-url origin git@github.com:DUYKHANH42/BookStore_ASP_NC.git

# Lấy public key → thêm vào GitHub SSH keys
cat ~/.ssh/github.pub

# Test
ssh -T git@github.com
# Kết quả: Hi DUYKHANH42! You've successfully authenticated
```

---

## 10. Cập nhật Frontend Environment

Đổi API URL trỏ về Elastic IP:

File: `BookStore_GiaoDien/src/environments/environment.ts`

```typescript
export const environment = {
  apiUrl: 'http://13.228.219.111:5000/api',
  uploadUrl: 'http://13.228.219.111:5000/uploads',
  production: true
};
```

---

## 11. Trigger CI/CD

```bash
cd ~/BookStore_ASP_NC
echo "" >> README.md
git add .
git commit -m "trigger CI/CD"
git push origin develop
```

Kiểm tra: Vào **GitHub repo → tab Actions** → xem workflow chạy pass ✅

---

## 12. Các vấn đề đã gặp và cách giải quyết

| Vấn đề | Nguyên nhân | Cách fix |
|---|---|---|
| `ssh: no key found` | appleboy/ssh-action không parse được OPENSSH key | Đổi sang SSH trực tiếp + base64 encode key |
| `Permission denied (publickey)` | Public key không khớp trong authorized_keys | Ghi đè authorized_keys bằng đúng public key |
| `Password authentication not supported` | GitHub không hỗ trợ password cho git | Dùng SSH key + đổi remote URL sang `git@github.com` |
| `deployer is not in sudoers file` | Chạy sudo từ shell deployer | Thoát về ec2-user rồi dùng `sudo -u deployer` |
| `Connection timed out` | Security Group port 22 giới hạn IP | Mở port 22 cho 0.0.0.0/0 |
| IP thay đổi khi stop/start | EC2 dùng dynamic public IP | Gán Elastic IP (13.228.219.111) |

---

## 13. Kiến trúc CI/CD hoàn chỉnh

```
┌─────────────────────────────────────────────────────────┐
│                     GitHub                                │
│  ┌──────────────┐    ┌──────────────────────────────┐    │
│  │  Source Code  │───▶│  GitHub Actions Workflow      │    │
│  │  (develop)    │    │  1. Build Docker images       │    │
│  └──────────────┘    │  2. Push to ECR               │    │
│                       │  3. SSH deploy to EC2         │    │
│                       └──────────────────────────────┘    │
└──────────────────────────────┬────────────────────────────┘
                               │
                     ┌─────────▼─────────┐
                     │   Amazon ECR       │
                     │  bookstore-api     │
                     │  bookstore-frontend│
                     └─────────┬─────────┘
                               │
                     ┌─────────▼─────────┐
                     │   EC2 Instance     │
                     │   (deployer user)  │
                     │                    │
                     │  ┌──────────────┐  │
                     │  │Docker Compose│  │
                     │  │ Frontend :80 │  │
                     │  │ Backend:5000 │  │
                     │  │ DB :1433     │  │
                     │  └──────────────┘  │
                     └────────────────────┘
```

---

## 14. Tổng kết SSH Keys

| Key | Thuộc user | Lưu ở đâu | Mục đích |
|---|---|---|---|
| `/home/deployer/.ssh/github-actions-new` | deployer | GitHub Secret (EC2_SSH_KEY_B64) | GitHub Actions SSH vào EC2 |
| `/home/deployer/.ssh/github` | deployer | GitHub SSH keys | deployer pull code từ GitHub |
| `~/.ssh/github` | ec2-user | GitHub SSH keys | ec2-user push code lên GitHub |

---

## 15. Lệnh hữu ích

```bash
# Xem workflow trên GitHub
# https://github.com/DUYKHANH42/BookStore_ASP_NC/actions

# Trigger CI/CD thủ công (push commit trống)
git commit --allow-empty -m "trigger CI/CD"
git push origin develop

# Xem logs container sau khi deploy
docker compose ps
docker compose logs -f

# Test SSH deployer
sudo -u deployer ssh -T git@github.com
sudo -u deployer ssh -i /home/deployer/.ssh/github-actions-new -o StrictHostKeyChecking=no deployer@13.228.219.111
```

---

## 16. Lưu ý bảo mật

- ✅ User `deployer` không có quyền sudo — chỉ deploy
- ✅ SSH key riêng cho CI/CD, không dùng key admin
- ✅ IAM user chỉ có quyền ECR, không có full access
- ✅ Private key encode base64 lưu trong GitHub Secrets (encrypted)
- ⚠️ Port 22 đang mở 0.0.0.0/0 — sau này nên giới hạn lại IP
- ⚠️ Nên rotate SSH keys và Access Keys định kỳ

---

## 17. TODO (Tiếp theo)
- [ ] HTTPS với Let's Encrypt + Nginx reverse proxy
- [ ] Giới hạn port 22 chỉ cho GitHub Actions IP ranges
- [ ] Thêm bước test tự động trong CI/CD (unit test, integration test)
- [ ] Notification khi deploy thành công/thất bại (Slack/Discord)
- [ ] Monitoring & Logging (CloudWatch)
- [ ] Auto rollback khi deploy lỗi
