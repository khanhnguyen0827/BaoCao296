using Microsoft.AspNetCore.Authentication.Cookies;

// Load file .env nếu có để bảo vệ dữ liệu nhạy cảm
DotNetEnv.Env.Load();

// Khởi tạo một WebApplicationBuilder bằng tham số dòng lệnh truyền vào hệ thống
var builder = WebApplication.CreateBuilder(args);

// --- ĐĂNG KÝ CÁC DỊCH VỤ VÀO DI CONTAINER NỘI BỘ ---
// Đăng ký và đưa Controller (kiến trúc MVC) vào DI Container
builder.Services.AddControllersWithViews();
// Đăng ký dịch vụ BaoCaoService ở chế độ Scoped (tái tạo object Service riêng biệt cho mỗi Request truy cập web của 1 người dùng)
builder.Services.AddScoped<BAOCAO_369.Services.BaoCaoService>();
// Đăng ký dịch vụ ExcelExportService đảm nhận khâu xuất báo cáo
builder.Services.AddScoped<BAOCAO_369.Services.ExcelExportService>();
// Đăng ký dịch vụ DatabaseService đảm nhận import SQL
builder.Services.AddScoped<BAOCAO_369.Services.DatabaseService>();
// Đăng ký dịch vụ EmailService
builder.Services.AddScoped<BAOCAO_369.Services.EmailService>();

// Đăng ký dịch vụ AuthService (Xác thực đăng nhập)
// builder.Services.AddScoped<BAOCAO_369.Services.AuthService>();

// Authentication removed

// Build (xây dựng) cấu hình thành đối tượng ứng dụng Web Application
var app = builder.Build();

// --- CẤU HÌNH PIPELINE ĐIỀU HƯỚNG CÁC YÊU CẦU ĐẦU VÀO (HTTP REQUEST PIPELINE) ---
// Kiểm tra nếu ứng dụng không hoạt động trong môi trường lập trình viên (.IsDevelopment)
if (!app.Environment.IsDevelopment())
{
    // Cấu hình bật trang thông báo chung khi mã code xảy ra exception chưa dự phòng (Sẽ điều hướng sang route Controller Home, Action Error)
    app.UseExceptionHandler("/Home/Error");
    // Áp dụng chuẩn bảo mật HTTP Strict Transport Security (HSTS) - Gợi ý trình duyệt thiết lập HTTPS bắt buộc trong 30 ngày (Định dạng sản xuất chuyên nghiệp)
    app.UseHsts();
}

// Luôn chuyển các request đường dẫn "http" thẳng sang hệ "https"
app.UseHttpsRedirection();
// Cho phép hệ điều hành (Kestrel) nắm phương thức Routing tự động phân giải Controller
app.UseRouting();

// Authentication middleware removed
// Authorization middleware removed

// Tối ưu phục vụ việc gọi các tệp tĩnh (Asset: image, js, css) nhanh chóng (Hỗ trợ ASP.NET modern .NET 8/9+)
app.MapStaticAssets();

// Thiết lập điều hướng tuyến đường (Routes) của Controller
// Cú pháp mặc định: Nếu không gõ URL gì cả thì vào thẳng tên miền /Home/Index
app.MapControllerRoute(
    name: "default",                         // Tên Route Rule
    pattern: "{controller=Home}/{action=Index}/{id?}") // Mẫu Map có thể mở rộng (Dấu hỏi chấm là tham số ID optional)
    .WithStaticAssets();                     // Gộp middleware cache Asset

// Bắt đầu Start Server nội bộ và Lắng nghe cổng web Port được giao
app.Run();
