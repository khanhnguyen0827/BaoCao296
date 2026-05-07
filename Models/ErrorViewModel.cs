// Gói các Model trong namespace mặc định
namespace BAOCAO_369.Models;

// ==========================================
// Lớp ErrorViewModel dùng chuyên biệt cho việc hiển thị màn hình báo lỗi hệ thống
// ==========================================
public class ErrorViewModel
{
    // Cung cấp mã yêu cầu (Request ID) giúp hỗ trợ truy vết lỗi trong log
    public string? RequestId { get; set; }

    // Dùng biểu thức lambda cơ bản sinh ra 1 thuộc tính (Property) dạng boolean (true/false)
    // Sẽ trả về true nếu thuộc tính RequestId đang có giá trị không rỗng hoặc NULL
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
