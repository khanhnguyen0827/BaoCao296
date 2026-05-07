using System;
using System.ComponentModel.DataAnnotations;

namespace BAOCAO_369.Models
{
    // Model mô phỏng lại cấu trúc bảng QUANTRI.DM_NHAN_VIEN
    public class NhanVienModel
    {
        public decimal ID_NV { get; set; }
        public string USERNAME { get; set; }
        public string FIRSTNAME { get; set; }
        public string LASTNAME { get; set; }
        public string EMAIL { get; set; }
        public decimal? ID_DV { get; set; }
        public string PASSWORD { get; set; }
        public string SALTPASSWORD { get; set; }
        public int DISABLE { get; set; }
    }

    // Model dữ liệu chứa thông tin người dùng gõ vào form Login
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Bắt buộc nhập Mật khẩu")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
        
        // Cờ lưu lỗi
        public string? ErrorMessage { get; set; }
    }
}
