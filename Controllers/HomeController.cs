// Import không gian tên System.Diagnostics để sử dụng lớp Activity (hữu ích trong việc gỡ lỗi và trace các request)
using System.Diagnostics;
// Import ASP.NET Core MVC để sử dụng lớp Controller và các IActionResult
using Microsoft.AspNetCore.Mvc;
// Import các models được sử dụng trong project, điển hình là ErrorViewModel
using BAOCAO_369.Models;

// Định nghĩa không gian tên của Controllers sử dụng cú pháp mới (file-scoped namespace)
namespace BAOCAO_369.Controllers;

// Kế thừa từ class Controller của MVC framework để tạo HomeController (xử lý trang chủ)
public class HomeController : Controller
{
    // Action trả về thẻ hiển thị Trang chủ (Index)
    public IActionResult Index()
    {
        // Trả về View tương ứng (thường là Views/Home/Index.cshtml)
        return View();
    }

    // Action trả về thẻ hiển thị Trang Chính sách bảo mật (Privacy)
    public IActionResult Privacy()
    {
        // Trả về View tương ứng (Views/Home/Privacy.cshtml)
        return View();
    }

    // Annotation [ResponseCache] thiết lập để trình duyệt không cache lại nội dung của trang báo lỗi này
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // Action Error xử lý và hiển thị trang báo lỗi khi ứng dụng gặp sự cố
    public IActionResult Error()
    {
        // Khởi tạo một ErrorViewModel với RequestId là ID của Activity hiện tại hoặc ID để truy vết HTTP Context
        // Sau đó gửi Model này sang cho màn hình View (Views/Shared/Error.cshtml) để hiển thị mã lỗi cho người dùng
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
