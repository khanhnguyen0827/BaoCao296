// Import thư viện để sử dụng List (danh sách)
using System.Collections.Generic;
// Import thư viện hỗ trợ tương tác với Data (Dữ liệu)
using System.Data;
// Import quản trị cấp cấu hình ứng dụng (như appsettings.json)
using Microsoft.Extensions.Configuration;
// Import thư viện Client chính thức của Oracle hỗ trợ giao tiếp DB
using Oracle.ManagedDataAccess.Client;
// Import Models để sử dụng các kiểu dữ liệu tự định nghĩa
using BAOCAO_369.Models;
// Import thư viện System cơ bản
using System;

// Định nghĩa không gian tên Service cho thao tác nghiệp vụ liên quan đến Báo Cáo
namespace BAOCAO_369.Services
{
    // ===============================================
    // Lớp Dịch Vụ thao tác, truy vấn hệ thống Oracle DataBase
    // ===============================================
    public class BaoCaoService
    {
        // Khai báo biến lưu giữ ConnectionString đến DB
        private readonly string _connectionString;

        // Constructor nhận cấu hình từ Dependency Injection của ASP.NET
        public BaoCaoService(IConfiguration configuration)
        {
            // Lấy chuỗi kết nối từ file config với Key là 'OracleDb'
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        // --- Hàm truy vấn lấy danh sách Đơn Vị ---
        public List<DonVi> GetDonVis()
        {
            // Khởi tạo một List trống để lưu kết quả trả về
            var list = new List<DonVi>();
            // Sử dụng "using" để kết nối đến Oracle - tự động gọi Dispose() / Đóng DB lại sau khi kết thúc block
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                // Mở luồng kết nối tới server DB
                conn.Open();
                // Câu SQL truy vấn ID, Mã, Tên của Đơn vị với điều kiện chưa bị Disable và có ID Cha là 342 (Đơn Vị Tổng) HOẶC chính là Đơn Vị Tổng 342, được sắp xếp bằng cột STT
                string sql = "SELECT ID_DV, MA_DV, TEN_DV, EMAIL FROM QUANTRI.DM_DON_VI WHERE DISABLE = 0 AND (ID_DV_CHA = 342 OR ID_DV = 342) ORDER BY STT, TEN_DV";
                // Tạo một OracleCommand để chuẩn bị thực thi câu lệnh SQL với luồng (conn) đã mở
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                // ExecuteReader sẽ thực thi câu lệnh SQL select và trả về 1 Data Reader cho phép đọc theo luồng qua từng Row dữ liệu
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    // Lặp qua từng dòng của Reader trả về
                    while (reader.Read())
                    {
                        // Parse (Chuyển Data) sang kiểu phù hợp của Model DonVi và nạp nó vào List
                        list.Add(new DonVi { 
                            ID_DV = Convert.ToDecimal(reader["ID_DV"]), 
                            TEN_DV = reader["TEN_DV"]?.ToString(),
                            EMAIL = reader["EMAIL"]?.ToString() 
                        });
                    }
                }
            }
            // Kết thúc block using, Connection tự đóng và trả List ra cho caller
            return list;
        }

        // --- Hàm truy vấn lấy Danh sách Dữ Liệu Báo Cáo (kèm logic xem báo cáo mới nhất) ---
        public List<BaoCaoItem> GetBaoCaoItems(DateTime fromDate, DateTime toDate, decimal? idDv = null)
        {
            // Khởi tạo List để chứa dòng item trả về
            var items = new List<BaoCaoItem>();

            // Khởi tạo luồng OracleConnection nội bộ
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                // Mở kết nối database
                conn.Open();
                // Biểu thức CTE (Common Table Expression - bảng tạm LatestBC)
                // Phục vụ cho việc chọn ra Bản ghi báo cáo MỚI NHẤT (dựa trên ROW_NUMBER() tạo STT giật lùi NGAY_TAO DESC của từng ID_DV)
                // trong hệ thống của table BAOCAO.BC_VP_QD_296_BIEU_3 với điều kiện ngày <= toDate và không Disable
                string sql = @"
                    WITH LatestBC AS (
                        SELECT * FROM (
                            SELECT b.*, ROW_NUMBER() OVER (PARTITION BY ID_DV ORDER BY NGAY_TAO DESC) as rn
                            FROM BAOCAO.BC_VP_QD_296_BIEU_3 b
                            WHERE TRUNC(b.NGAY_TAO) <= TRUNC(:toDate) AND NVL(b.DISABLE, 0) = 0
                        ) WHERE rn = 1
                    )
                    -- Phần main SELECT trả về từ bảng Đơn Vị (LEFT JOIN sang CTE LatestBC) 
                    -- Sử dụng ROW_NUMBER để đánh số thứ tự ảo theo cột STT DB
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY d.STT, d.ID_DV) as TT,
                        d.ID_DV,
                        d.TEN_DV,
                        b.NGAY_TAO,
                        b.COT_3, b.COT_4, b.COT_5, 
                        b.COT_6, b.COT_7, b.COT_8, b.COT_9, b.COT_10, 
                        b.COT_11, b.COT_12, b.COT_13, b.COT_14
                    FROM QUANTRI.DM_DON_VI d
                    LEFT JOIN LatestBC b ON d.ID_DV = b.ID_DV
                    WHERE d.DISABLE = 0
                    AND (d.ID_DV_CHA = 342 OR d.ID_DV = 342)";

                // Filter động (Dynamic Query) bằng cách cộng chuỗi (nếu CÓ truyền idDv cụ thể, chứ không phải tìm ALL)
                if (idDv.HasValue && idDv > 0)
                {
                    sql += " AND d.ID_DV = :idDv";
                }

                // Sắp xếp đầu ra của bảng bằng ORDER BY (như vậy ROW_NUMBER sẽ trùng khớp trật tự)
                sql += " ORDER BY d.STT, d.ID_DV";

                // Khởi tạo Command với câu lệnh SQL vừa chuẩn bị
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    // Dùng Parameter (OracleParameter) thay vì cộng biến trực tiếp để Tránh trường hợp bị SQL Injection và xử lý type an toàn
                    cmd.Parameters.Add(new OracleParameter("toDate", toDate)); // Biến Ràng Bộc (Bind Variable) cho `:toDate`
                    // Nếu điều kiện filter có thì add tiếp Bind variable
                    if (idDv.HasValue && idDv > 0)
                    {
                        cmd.Parameters.Add(new OracleParameter("idDv", idDv.Value));
                    }

                    // Thực thi và trả đối tượng DataReader
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        // Chạy lặp vào từng Record/Row Data nhận được
                        while (reader.Read())
                        {
                            // Nhúng Data thành một C# Object (BaoCaoItem) 
                            // Có áp dụng kiểm tra DBNull.Value do lệnh Join có thể sinh ra NULL data nếu đơn vị chưa nhập điểm báo cáo nào
                            items.Add(new BaoCaoItem
                            {
                                TT = Convert.ToDecimal(reader["TT"]), // Lấy biến TT
                                ID_DV = Convert.ToDecimal(reader["ID_DV"]), // ID Đơn Vị
                                TEN_DV = reader["TEN_DV"]?.ToString() ?? "", // Lấy Tên Đơn vị, fallback = "" phòng hờ ngoại lệ
                                NgayCapNhat = reader["NGAY_TAO"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NGAY_TAO"]),
                                // Các cột 3 - 14 tiếp nhận data hoặc trả về 0 tránh bị Exception convert nếu giá trị là NULL từ OracleDB
                                COT_3 = reader["COT_3"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_3"]),
                                COT_4 = reader["COT_4"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_4"]),
                                COT_5 = reader["COT_5"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_5"]),
                                COT_6 = reader["COT_6"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_6"]),
                                COT_7 = reader["COT_7"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_7"]),
                                COT_8 = reader["COT_8"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_8"]),
                                COT_9 = reader["COT_9"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_9"]),
                                COT_10 = reader["COT_10"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_10"]),
                                COT_11 = reader["COT_11"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_11"]),
                                COT_12 = reader["COT_12"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_12"]),
                                COT_13 = reader["COT_13"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_13"]),
                                COT_14 = reader["COT_14"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_14"])
                            });
                        }
                    }
                }
            }

            // Lọc bỏ các đơn vị có tất cả các cột dữ liệu bằng 0 (nếu không filter đích danh 1 đơn vị)
            if (!idDv.HasValue || idDv <= 0)
            {
                items = items.Where(x => 
                    x.COT_3 != 0 || x.COT_4 != 0 || x.COT_5 != 0 || 
                    x.COT_6 != 0 || x.COT_7 != 0 || x.COT_8 != 0 || 
                    x.COT_9 != 0 || x.COT_10 != 0 || x.COT_11 != 0 || 
                    x.COT_12 != 0 || x.COT_13 != 0 || x.COT_14 != 0
                ).ToList();

                // Cập nhật lại số thứ tự TT sau khi đã lọc
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].TT = i + 1;
                }
            }

            // Trả List kết quả đã lấy lên cho cấp Controller
            return items;
        }

        // --- Hàm lấy toàn bộ lịch sử (cả DISABLE) của một đơn vị ---
        public List<BaoCaoItem> GetLichSuDonVi(decimal idDv)
        {
            var items = new List<BaoCaoItem>();
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT 
                        d.ID_DV,
                        d.TEN_DV,
                        b.NGAY_TAO,
                        b.COT_3, b.COT_4, b.COT_5, 
                        b.COT_6, b.COT_7, b.COT_8, b.COT_9, b.COT_10, 
                        b.COT_11, b.COT_12, b.COT_13, b.COT_14,
                        b.DISABLE
                    FROM BAOCAO.BC_VP_QD_296_BIEU_3 b
                    LEFT JOIN QUANTRI.DM_DON_VI d ON b.ID_DV = d.ID_DV
                    WHERE b.ID_DV = :idDv
                    ORDER BY b.NGAY_TAO DESC";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("idDv", idDv));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        int stt = 1;
                        while (reader.Read())
                        {
                            var item = new BaoCaoItem
                            {
                                TT = stt++,
                                ID_DV = Convert.ToDecimal(reader["ID_DV"]),
                                TEN_DV = reader["TEN_DV"]?.ToString() ?? "",
                                NgayCapNhat = reader["NGAY_TAO"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NGAY_TAO"]),
                                COT_3 = reader["COT_3"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_3"]),
                                COT_4 = reader["COT_4"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_4"]),
                                COT_5 = reader["COT_5"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_5"]),
                                COT_6 = reader["COT_6"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_6"]),
                                COT_7 = reader["COT_7"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_7"]),
                                COT_8 = reader["COT_8"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_8"]),
                                COT_9 = reader["COT_9"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_9"]),
                                COT_10 = reader["COT_10"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_10"]),
                                COT_11 = reader["COT_11"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_11"]),
                                COT_12 = reader["COT_12"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_12"]),
                                COT_13 = reader["COT_13"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_13"]),
                                COT_14 = reader["COT_14"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["COT_14"])
                            };
                            
                            // Ghi chú trạng thái vào Disable để View hiển thị (có thể mượn 1 property hoặc tạo mới, 
                            // nhưng trong model chưa có prop Disable. Sẽ tạo thêm prop Disable trong model hoặc pass ViewBag.
                            // Để an toàn, tạm truyền qua viewbag hoặc ko cần vì sẽ liệt kê tất cả theo ngày.
                            items.Add(item);
                        }
                    }
                }
            }
            return items;
        }

    }
}
