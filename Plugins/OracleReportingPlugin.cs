using Microsoft.SemanticKernel;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel;
using System.Data;

namespace BAOCAO_369.Plugins
{
    public class OracleReportingPlugin
    {
        private readonly string _connectionString;

        public OracleReportingPlugin(IConfiguration configuration)
        {
            // Lấy Connection String từ appsettings.json. Lưu ý Host Docker nếu dính lỗi
            _connectionString = configuration.GetConnectionString("OracleDb") ?? "User Id=system;Password=DB_BAOCAO2026;Data Source=localhost:1521/XEPDB1";
        }

        [KernelFunction]
        [Description("Chạy một câu lệnh SQL SELECT trên database Oracle để lấy dữ liệu báo cáo.")]
        public async Task<string> ExecuteQuery(
            [Description("Câu lệnh SQL hoàn chỉnh (chỉ dùng SELECT).")] string sql)
        {
            try
            {
                // Rào chắn bảo vệ an ninh nhẹ: chặn các cú pháp cập nhật/xoá (Nên quy hoạch User DB bằng quyền READONLY sẽ chắc chắn hơn)
                if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return "Database Error: Chỉ được phép chạy câu lệnh truy xuất SELECT.";
                }

                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var dt = new DataTable();
                dt.Load(reader);

                // Giới hạn Token Window của LLM: Rất quan trọng khi đối diện Table hàng ngàn Rows
                if (dt.Rows.Count > 100)
                {
                    var partialDt = dt.Clone();
                    for (int i = 0; i < 100; i++)
                    {
                        partialDt.ImportRow(dt.Rows[i]);
                    }
                    return System.Text.Json.JsonSerializer.Serialize(partialDt) + "\n[System: Phản hồi quá dài, hệ thống đã cắt bớt chỉ lấy 100 dòng đầu. Hãy yêu cầu chi tiết hơn và dùng các hàm như SUM, GROUP BY trong SQL.]";
                }

                // Chuyển kết quả thành dạng JSON để AI dễ đọc và tiêu hóa logic
                return System.Text.Json.JsonSerializer.Serialize(dt);
            }
            catch (Exception ex)
            {
                // Nếu SQL sai logic máy AI viết sai hàm nội tại, Oracle sẽ lật Exception, nhả Exception ra để máy AI đọc và Tự Sửa Lỗi code (Self-correction)
                return $"Oracle Error: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Lấy cấu trúc (schema) của các bảng trong database để biết các cột dữ liệu trước khi soạn SQL.")]
        public string GetTableSchema([Description("Tên Bảng (Ví dụ: DM_DON_VI, SALESS)")] string tableName)
        {
            // Tốt nhất là truy vấn Data Dictionary của Oracle (ALL_TAB_COLS), ở đây trả về tĩnh theo yêu cầu của bạn:
            var tn = tableName.ToUpper().Trim();
            
            if (tn == "SALES" || tn == "DOANH_THU") 
                return "Table: SALES, Columns: ID (Number), AMOUNT (Number), SALEDATE (Date), REGION (Varchar2)";
                
            if (tn == "DM_NHAN_VIEN") 
                return "Table: DM_NHAN_VIEN, Columns: ID_NV (Number) PRIMARY KEY, TEN_NV (Varchar2(200)), CHUC_VU (Varchar2(100)), ID_DV (Number) FOREIGN KEY";
                
            if (tn == "DM_DON_VI") 
                return "Table: DM_DON_VI, Columns: ID_DV (Number) PRIMARY KEY, TEN_DV (Varchar2(250)), MA_DV (Varchar2(50))";

            if (tn == "DOANH_THU_THANG") 
                return "Table: DOANH_THU_THANG, Columns: THANG (Number), NAM (Number), TONG_DOANH_THU (Number), ID_DV (Number)";
                
            return $"Không tìm thấy cấu trúc bảng cho '{tableName}'. Bạn có thể xem bảng DM_DON_VI, QUAN_LY, SALES.";
        }
    }
}
