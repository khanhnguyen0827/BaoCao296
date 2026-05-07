# Hệ Thống Báo Cáo Thống Kê (Biểu số 3 - QĐ 296/QĐ-EVN)

Dự án phát triển ứng dụng Web giúp tự động hoá việc trích xuất báo cáo thống kê, xuất file Excel và gửi email báo cáo tự động cho các đơn vị. Hệ thống được xây dựng trên nền tảng ASP.NET Core MVC (C#) và sử dụng Oracle Database.

## 🚀 Các tính năng chính

- **Thống kê Báo cáo Biểu số 3**: Tự động tổng hợp dữ liệu hồ sơ, công việc theo các trạng thái (đang thực hiện, đã kết thúc, trả lại, v.v.) theo QĐ 296/QĐ-EVN.
- **Xuất Báo cáo Excel**: Hỗ trợ xuất dữ liệu báo cáo ra file Excel tổng hợp cho toàn bộ các đơn vị.
- **Gửi Email tự động**: Tích hợp tính năng gửi báo cáo thống kê qua Email tự động đến các phòng ban, đơn vị liên quan.
- **Tích hợp AI**: Hỗ trợ xử lý, phân tích thông minh sử dụng Semantic Kernel (OpenAI).

## 🛠️ Công nghệ sử dụng

- **Framework**: .NET 10 SDK, ASP.NET Core MVC
- **Database**: Oracle Database (`Oracle.ManagedDataAccess.Core`)
- **Excel Export**: ClosedXML
- **AI Integration**: Microsoft Semantic Kernel, OpenAI
- **Frontend**: Bootstrap, jQuery, Vanilla CSS

## ⚙️ Hướng dẫn Cài đặt & Cấu hình

### 1. Yêu cầu hệ thống
- Cài đặt [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (hoặc phiên bản tương thích).
- Cài đặt Oracle Database (ví dụ: Oracle Database 21c/23c Express Edition).
- Đã cài đặt Git.

### 2. Tải mã nguồn & Cài đặt thư viện
Clone dự án về máy tính của bạn:
```bash
git clone https://github.com/khanhnguyen0827/BaoCao296.git
cd BaoCao296
```
Khôi phục các thư viện NuGet:
```bash
dotnet restore
```

### 3. Cấu hình Cơ sở dữ liệu (Oracle)
- Tạo một User/Schema trong Oracle Database.
- Chạy script SQL (ví dụ: `2.SCRIP_THONGKE_BIEU_3.sql`) hoặc sử dụng tính năng Import SQL có sẵn trong hệ thống (`Views/Database/ImportSql.cshtml`) để tạo các bảng, view, thủ tục cần thiết.

### 4. Cấu hình Biến môi trường (.env)
Dự án sử dụng thư viện `DotNetEnv` để đọc các cấu hình bảo mật từ file `.env`.
Tạo một file `.env` ở thư mục gốc của dự án (cùng cấp với thư mục `Controllers`, `Program.cs`) với định dạng sau:

```env
# Database Configuration
ConnectionStrings__OracleDb=User Id=YOUR_USER;Password=YOUR_PASSWORD;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=YOUR_SERVICE)))

# SMTP Configuration (Gửi Email)
SmtpSettings__SenderEmail=your-email@gmail.com
SmtpSettings__Username=your-email@gmail.com
SmtpSettings__Password=your-app-password

# OpenAI Configuration (Tính năng AI)
AI__OpenAI__ApiKey=YOUR_OPENAI_API_KEY
```

*Lưu ý: Không bao giờ đẩy (commit) file `.env` chứa mật khẩu/API Key thật của bạn lên GitHub. File này đã được thêm vào `.gitignore`.*

### 5. Khởi chạy ứng dụng
Mở terminal/cmd tại thư mục chứa file `.csproj` và chạy lệnh:
```bash
dotnet run
```
Mở trình duyệt và truy cập vào địa chỉ: `http://localhost:5206` (Hoặc port hiển thị trong terminal của bạn).

## 🗂 Cấu trúc thư mục

- `Controllers/`: Chứa logic điều hướng của ứng dụng.
- `Services/`: Chứa logic xử lý nghiệp vụ (`BaoCaoService`, `EmailService`, `ExcelExportService`, `BrainService`, v.v.)
- `Views/`: Chứa giao diện hiển thị HTML (Razor views).
- `Models/`: Chứa các định nghĩa Class, Entity dữ liệu.
- `wwwroot/`: Chứa các tài nguyên tĩnh như CSS, JS, hình ảnh, các thư viện Frontend.

## 📄 Bản quyền
Dự án được xây dựng phục vụ cho nghiệp vụ quản lý báo cáo nội bộ.
