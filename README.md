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

## ⚙️ Hướng dẫn Cài đặt & Cấu hình Chi Tiết

### Bước 1: Yêu cầu hệ thống cần chuẩn bị
Để chạy được dự án, máy tính của bạn cần cài đặt sẵn các phần mềm sau:
- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** hoặc mới hơn.
- **IDE/Editor**: Khuyến nghị dùng **Visual Studio 2022** hoặc **Visual Studio Code** (cài sẵn C# Dev Kit).
- **Oracle Database**: Cài đặt Oracle Database (ví dụ bản 21c/23c Express Edition) hoặc dùng Oracle Cloud/Server nội bộ.
- **Git** để clone mã nguồn.

### Bước 2: Tải mã nguồn và Cài đặt thư viện
Mở Terminal/Command Prompt và chạy các lệnh sau:
1. Clone dự án về máy tính:
   ```bash
   git clone https://github.com/khanhnguyen0827/BaoCao296.git
   cd BaoCao296
   ```
2. Khôi phục (restore) các thư viện NuGet mà dự án sử dụng:
   ```bash
   dotnet restore
   ```

### Bước 3: Cài đặt và Cấu hình Cơ sở dữ liệu (Oracle)
1. **Tạo User (Schema)**: 
   Mở công cụ quản lý Oracle (như SQL Developer hoặc SQL*Plus), đăng nhập bằng tài khoản `sys` hoặc `system` và tạo một user mới:
   ```sql
   CREATE USER DB_BAOCAO IDENTIFIED BY mat_khau_cua_ban;
   GRANT CONNECT, RESOURCE, DBA TO DB_BAOCAO;
   ```
2. **Khởi tạo dữ liệu**:
   - Sử dụng tool SQL Developer, kết nối vào user vừa tạo (`DB_BAOCAO`).
   - Mở và chạy toàn bộ nội dung file script thống kê biểu mẫu (ví dụ: `2.SCRIP_THONGKE_BIEU_3.sql`) để tạo các `TABLE`, `VIEW`, và chèn dữ liệu mẫu.
   - *Cách 2*: Nếu ứng dụng đã chạy được một phần, bạn có thể truy cập `/Database/ImportSql` trên giao diện web để upload file script SQL lên.

### Bước 4: Thiết lập Biến môi trường (.env)
Hệ thống sử dụng file `.env` để bảo mật thông tin nhạy cảm (không lưu trực tiếp trong mã code hay file JSON công khai). 

1. Tại thư mục gốc của dự án (cùng cấp với thư mục `Controllers`, file `Program.cs`), **tạo một file mới** và đặt tên là `.env`.
2. Copy đoạn nội dung sau vào file `.env` và thay đổi các giá trị cho phù hợp với máy của bạn:

```env
# 1. Cấu hình Database Oracle
# Thay đổi User Id, Password, HOST, PORT, SERVICE_NAME tương ứng với CSDL của bạn
ConnectionStrings__OracleDb=User Id=DB_BAOCAO;Password=mat_khau_cua_ban;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)))

# 2. Cấu hình gửi Email (Sử dụng Gmail)
# SenderEmail và Username thường giống nhau. Password là "Mật khẩu ứng dụng" (App Password) sinh ra từ tài khoản Google.
SmtpSettings__SenderEmail=email-cua-ban@gmail.com
SmtpSettings__Username=email-cua-ban@gmail.com
SmtpSettings__Password=mat_khau_ung_dung_gmail

# 3. Cấu hình OpenAI (Tùy chọn nếu dùng tính năng Brain/AI)
AI__OpenAI__ApiKey=sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxx
```
*(**Lưu ý**: Để lấy `SmtpSettings__Password` của Gmail, bạn cần bật Xác minh 2 bước trong tài khoản Google, sau đó vào phần **App passwords** (Mật khẩu ứng dụng) để tạo một mật khẩu 16 chữ số.)*

### Bước 5: Build và Khởi chạy ứng dụng
**Cách 1: Sử dụng Terminal / Command Line**
Mở terminal tại thư mục dự án và chạy:
```bash
dotnet build
dotnet run
```
Sau khi terminal báo `Now listening on: http://localhost:5206` (hoặc cổng tương tự), hãy mở trình duyệt và truy cập vào đường dẫn đó.

**Cách 2: Sử dụng Visual Studio 2022**
1. Nhấp đúp vào file `BAOCAO_369.csproj` để mở dự án bằng Visual Studio.
2. Đợi Visual Studio load xong các thư viện.
3. Bấm nút **▶ IIS Express** (hoặc nút Start theo tên project) ở thanh menu phía trên, hoặc nhấn phím `F5` để chạy dự án ở chế độ Debug.
Trình duyệt sẽ tự động bật lên hiển thị trang chủ của hệ thống.

## 🗂 Cấu trúc thư mục

- `Controllers/`: Chứa logic điều hướng của ứng dụng.
- `Services/`: Chứa logic xử lý nghiệp vụ (`BaoCaoService`, `EmailService`, `ExcelExportService`, `BrainService`, v.v.)
- `Views/`: Chứa giao diện hiển thị HTML (Razor views).
- `Models/`: Chứa các định nghĩa Class, Entity dữ liệu.
- `wwwroot/`: Chứa các tài nguyên tĩnh như CSS, JS, hình ảnh, các thư viện Frontend.

## 📄 Bản quyền
Dự án được xây dựng phục vụ cho nghiệp vụ quản lý báo cáo nội bộ.
