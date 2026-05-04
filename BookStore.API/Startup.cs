using System;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Identity;
using BookStore.Infrastructure.Persistence;
using BookStore.Infrastructure.Repositories;
using BookStore.Infrastructure.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using BookStore.Application.Configurations;
using BookStore.API.Middleware;
using BookStore.API.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

using BookStore.Application.VnpayProvider.Extensions;

namespace BookStore.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddControllersWithViews();
            services.AddMemoryCache();
            services.AddHttpContextAccessor();
            services.AddDbContext<BookStoreDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")
                ));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            })
            .AddEntityFrameworkStores<BookStoreDbContext>()
            .AddDefaultTokenProviders();
            services.AddScoped<IAuthService, AuthRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
            services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IShippingAddressRepository, ShippingAddressRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ProductService>();
            services.AddScoped<OrderService>();
            services.AddScoped<FlashSaleService>();
            services.AddScoped<InvoiceService>();
            services.AddScoped<ExcelExportService>();
            services.AddScoped<CustomerService>();
            services.AddScoped<InventoryService>();
            services.AddScoped<ReviewService>();
            services.AddScoped<SuppliersService>();
            services.AddScoped<SubCategoriesService>();
            services.AddScoped<CategoriesService>();
            services.AddScoped<FavoriteService>();
            services.AddScoped<CartService>();
            services.AddScoped<ShippingAddressService>();
            services.AddScoped<AuthService>();
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<PricingService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IFileService, FileService>();
            var key = System.Text.Encoding.UTF8.GetBytes(Configuration["JWT:Secret"] ?? "Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_123");
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie("Cookies", options =>
            {
                options.Cookie.Name = "BookStore.Admin.Cookie";
                options.LoginPath = "/api/auth/login";
                options.AccessDeniedPath = "/api/auth/forbidden";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
                options.Cookie.Path = "/";
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Configuration["JWT:ValidAudience"],
                    ValidIssuer = Configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key)
                };
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular",
                    builder => builder
                        .WithOrigins(
                            "http://localhost:4200",
                            "http://localhost:53214",
                            "https://book-store-giao-dien.vercel.app",
                            "https://book-store-giao-dien-iixqkx84y-duykhanh42s-projects.vercel.app"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "BookStore.API", Version = "v1" });
            });

            // SignalR & Payment Configuration
            services.AddSignalR();
            services.AddHttpClient();
            services.Configure<ZaloPayConfig>(Configuration.GetSection(ZaloPayConfig.ConfigName));
            services.AddHttpClient<IZaloPayService, ZaloPayService>();
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("forgot-password", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(10);
                    opt.PermitLimit = 3;
                    opt.QueueLimit = 0;
                });
                options.RejectionStatusCode = 429;
            });
            services.Configure<PayOSConfig>(Configuration.GetSection("PayOS"));
            services.AddScoped<IPayOSService, PayOSService>();
            
            services.Configure<VnPayConfig>(Configuration.GetSection(VnPayConfig.ConfigName));
            
            services.AddVnpayClient(options =>
            {
                options.TmnCode = Configuration["VnPay:TmnCode"];
                options.HashSecret = Configuration["VnPay:HashSecret"];
                options.BaseUrl = Configuration["VnPay:BaseUrl"];
                options.CallbackUrl = Configuration["VnPay:ReturnUrl"];
            });

            services.AddScoped<IVnPayService, VnPayService>();

            services.AddHostedService<OrderCleanupService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookStore.API v1"));
            QuestPDF.Settings.License = LicenseType.Community;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
               

            }

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("AllowAngular");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<BookStore.API.Hubs.NotificationHub>("/notificationHub");
                endpoints.MapAreaControllerRoute(
        name: "admin_default",
        areaName: "Admin",
        pattern: "Admin/{controller=Home}/{action=Index}/{id?}"
    );
                endpoints.MapFallbackToFile("index.html");

            });
        }
    }
}
