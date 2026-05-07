// Import thư viện System cơ sở
using System;
// Import thư viện Collections.Generic để sử dụng cấu trúc dữ liệu List
using System.Collections.Generic;

// Định nghĩa không gian tên Models chứa các đối tượng dữ liệu
namespace BAOCAO_369.Models
{
    // ==========================================
    // Model DonVi biểu diễn dữ liệu của một Đơn Vị Điện Lực
    // ==========================================
    public class DonVi
    {
        // Mã ID nội bộ của đơn vị (kiểu số thập phân lấy từ Oracle DB)
        public decimal ID_DV { get; set; }
        // Mã string viết tắt của đơn vị (Ví dụ: PA, PB...)
        public string MA_DV { get; set; }
        // Tên hiển thị đầy đủ của đơn vị điện lực
        public string TEN_DV { get; set; }
        // Email của đơn vị
        public string EMAIL { get; set; }
    }

    // ==========================================
    // Model BaoCaoItem biểu diễn 1 dòng dữ liệu báo cáo trên bảng Biểu Cẩu
    // Chứa các chỉ số, giá trị từ cột 3 đến cột 15
    // ==========================================
    public class BaoCaoItem
    {
        // Số thứ tự hiển thị của bản ghi
        public decimal TT { get; set; }
        // Khóa ngoại liên kết với ID Đơn Vị
        public decimal ID_DV { get; set; }
        // Tên đơn vị (dùng để hiển thị lên lưới dữ liệu)
        public string TEN_DV { get; set; }
        // Thời điểm cập nhật cuối cùng (hiển thị Last Updated)
        public DateTime? NgayCapNhat { get; set; }
        
        // Các trường dữ liệu tương ứng phục vụ nghiệp vụ báo cáo
        public decimal COT_3 { get; set; } // Dữ liệu Cột 3
        public decimal COT_4 { get; set; } // Dữ liệu Cột 4
        public decimal COT_5 { get; set; } // Dữ liệu Cột 5
        public decimal COT_6 { get; set; } // Dữ liệu Cột 6
        public decimal COT_7 { get; set; } // Dữ liệu Cột 7
        public decimal COT_8 { get; set; } // Dữ liệu Cột 8
        public decimal COT_9 { get; set; } // Dữ liệu Cột 9
        public decimal COT_10 { get; set; } // Dữ liệu Cột 10
        public decimal COT_11 { get; set; } // Dữ liệu Cột 11
        public decimal COT_12 { get; set; } // Dữ liệu Cột 12
        public decimal COT_13 { get; set; } // Dữ liệu Cột 13
        public decimal COT_14 { get; set; } // Dữ liệu Cột 14
        
        // Cột 15: Tỷ lệ phần trăm tính theo Công thức (13)/(14) * 100
        // Thuộc tính này chỉ có getter (get-only) để tự động tính toán từ các cột khác
        public decimal COT_15 
        { 
            get 
            { 
                // Tránh trường hợp chia cho 0 (divide by zero)
                if (COT_14 == 0) return 0;
                // Tính phép chia, nhân hệ số 100 để ra phần trăm, và làm tròn 2 chữ số thập phân
                return Math.Round((COT_13 / COT_14) * 100, 2); 
            } 
        }
    }

    // ==========================================
    // Model BaoCaoViewModel (Data Transfer Object) dùng để đóng gói nhiều loại dữ liệu 
    // và đẩy sang giao diện (View) để Render
    // ==========================================
    public class BaoCaoViewModel
    {
        // Danh sách các bản ghi báo cáo để rải ra lưới bảng
        // Khởi tạo sẵn một List trống để tránh lỗi NullReferenceException
        public List<BaoCaoItem> Items { get; set; } = new List<BaoCaoItem>();
        
        // Danh sách các đơn vị để đổ dữ liệu vào Dropdownlist (ComboxBox)
        public List<DonVi> DonVis { get; set; } = new List<DonVi>();
        
        // Lưu trữ lựa chọn hiện tại ID đơn vị người dùng đang xem
        public decimal? IdDv { get; set; }
        
        // Ngày bắt đầu truy vấn
        public DateTime FromDate { get; set; }
        // Ngày kết thúc (hoặc ngày liệu mới nhất dùng hiển thị trên UI)
        public DateTime ToDate { get; set; }
    }
}
