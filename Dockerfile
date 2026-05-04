# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file và tất cả .csproj để restore dependencies trước (tận dụng Docker cache)
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

# Render yêu cầu ứng dụng lắng nghe trên cổng 10000 (mặc định của Render)
# hoặc đọc từ biến môi trường PORT do Render cung cấp
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "BookStore.API.dll"]
