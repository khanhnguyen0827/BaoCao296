using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using BAOCAO_369.Models;

namespace BAOCAO_369.Services
{
    // Dịch vụ thực thi đối chiếu/kết nối DB phục vụ việc Đăng nhập
    public class AuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        // Lấy thông tin chứng chỉ của nhân viên thông qua Username và verify Pass
        public async Task<NhanVienModel?> AuthenticateUser(string username, string inputPassword)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();
                
                // Tìm bằng USERNAME (không phân biệt hoa thường) & chưa bị khóa
                string sql = @"
                    SELECT ID_NV, USERNAME, PASSWORD, SALTPASSWORD, FIRSTNAME, LASTNAME, ID_DV, EMAIL 
                    FROM QUANTRI.DM_NHAN_VIEN 
                    WHERE LOWER(USERNAME) = LOWER(:usr) AND NVL(DISABLE, 0) = 0";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("usr", username));

                    using (OracleDataReader reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var nv = new NhanVienModel
                            {
                                ID_NV = Convert.ToDecimal(reader["ID_NV"]),
                                USERNAME = reader["USERNAME"].ToString() ?? "",
                                PASSWORD = reader["PASSWORD"]?.ToString() ?? "",
                                SALTPASSWORD = reader["SALTPASSWORD"]?.ToString() ?? "",
                                FIRSTNAME = reader["FIRSTNAME"]?.ToString() ?? "",
                                LASTNAME = reader["LASTNAME"]?.ToString() ?? "",
                                EMAIL = reader["EMAIL"]?.ToString() ?? "",
                                ID_DV = reader["ID_DV"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["ID_DV"])
                            };

                            // Xác thực khóa Pass
                            if (VerifyPassword(inputPassword, nv.PASSWORD, nv.SALTPASSWORD))
                            {
                                return nv;
                            }
                        }
                    }
                }
            }
            return null; // Không trùng khớp
        }

        // Kiểm tra Hash so sánh các kịch bản password thường thấy ở hệ thống truyền thống
        private bool VerifyPassword(string inputPlain, string dbHash, string dbSalt)
        {
            if (string.IsNullOrEmpty(dbHash)) return false;

            // 1. Nếu lưu kiểu text thô (Plain text)
            if (inputPlain == dbHash) return true;

            // 2. MD5 cơ bản (mật khẩu)
            string hashSimple = CalculateMD5(inputPlain);
            if (string.Equals(hashSimple, dbHash, StringComparison.OrdinalIgnoreCase)) return true;

            if (!string.IsNullOrEmpty(dbSalt))
            {
                // 3. MD5(Salt + Pass)
                string hashWithSaltFront = CalculateMD5(dbSalt + inputPlain);
                if (string.Equals(hashWithSaltFront, dbHash, StringComparison.OrdinalIgnoreCase)) return true;

                // 4. MD5(Pass + Salt)
                string hashWithSaltBack = CalculateMD5(inputPlain + dbSalt);
                if (string.Equals(hashWithSaltBack, dbHash, StringComparison.OrdinalIgnoreCase)) return true;
                
                // 5. Thêm kịch bản Hash SHA256 (phòng trường hợp DB dùng SHA256)
                string sha256Simple = CalculateSHA256(inputPlain);
                if (string.Equals(sha256Simple, dbHash, StringComparison.OrdinalIgnoreCase)) return true;
                
                string sha256SaltBack = CalculateSHA256(inputPlain + dbSalt);
                if (string.Equals(sha256SaltBack, dbHash, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else
            {
                // Cũng thử check xem mã hóa SHA256 ko salt
                string sha256Simple = CalculateSHA256(inputPlain);
                if (string.Equals(sha256Simple, dbHash, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false; // Sai hoàn toàn
        }

        private string CalculateMD5(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach(var b in hashBytes) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
        
        private string CalculateSHA256(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes) builder.Append(b.ToString("X2"));
                return builder.ToString();
            }
        }
    }
}
