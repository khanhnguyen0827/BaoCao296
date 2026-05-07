using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Linq;

namespace BAOCAO_369.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        public async Task<(int successCount, int errorCount, List<string> errors)> ImportDataAsync(string fileContent)
        {
            int successCount = 0;
            int errorCount = 0;
            List<string> errors = new List<string>();

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1. Cập nhật dữ liệu cũ disable = 1
                try
                {
                    string disableSql = "UPDATE BAOCAO.BC_VP_QD_296_BIEU_3 SET DISABLE = 1";
                    using (OracleCommand disableCmd = new OracleCommand(disableSql, conn))
                    {
                        await disableCmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi khi disable dữ liệu cũ: {ex.Message}");
                    errorCount++;
                }

                // 2. Phân tích nội dung file. Có thể là dạng lệnh SQL (;) hoặc dạng CSV có header
                var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(l => l.Trim())
                                       .Where(l => !string.IsNullOrEmpty(l))
                                       .ToList();

                if (lines.Count > 0 && lines[0].StartsWith("ID,ID_DV,COT_1"))
                {
                    // Dạng CSV
                    for (int i = 1; i < lines.Count; i++) // Bỏ qua dòng header
                    {
                        string line = lines[i];
                        string[] parts = line.Split(',');
                        if (parts.Length >= 18)
                        {
                            try
                            {
                                string insertSql = @"
                                    INSERT INTO BAOCAO.BC_VP_QD_296_BIEU_3 
                                    (ID_DV, COT_1, COT_2, COT_3, COT_4, COT_5, COT_6, COT_7, COT_8, COT_9, COT_10, COT_11, COT_12, COT_13, COT_14, DISABLE, NGAY_TAO) 
                                    VALUES 
                                    (:idDv, :c1, :c2, :c3, :c4, :c5, :c6, :c7, :c8, :c9, :c10, :c11, :c12, :c13, :c14, :disable, :ngayTao)";
                                
                                using (OracleCommand cmd = new OracleCommand(insertSql, conn))
                                {
                                    cmd.Parameters.Add(new OracleParameter("idDv", ParseDecimal(parts[1])));
                                    cmd.Parameters.Add(new OracleParameter("c1", ParseDecimal(parts[2])));
                                    cmd.Parameters.Add(new OracleParameter("c2", ParseDecimal(parts[3])));
                                    cmd.Parameters.Add(new OracleParameter("c3", ParseDecimal(parts[4])));
                                    cmd.Parameters.Add(new OracleParameter("c4", ParseDecimal(parts[5])));
                                    cmd.Parameters.Add(new OracleParameter("c5", ParseDecimal(parts[6])));
                                    cmd.Parameters.Add(new OracleParameter("c6", ParseDecimal(parts[7])));
                                    cmd.Parameters.Add(new OracleParameter("c7", ParseDecimal(parts[8])));
                                    cmd.Parameters.Add(new OracleParameter("c8", ParseDecimal(parts[9])));
                                    cmd.Parameters.Add(new OracleParameter("c9", ParseDecimal(parts[10])));
                                    cmd.Parameters.Add(new OracleParameter("c10", ParseDecimal(parts[11])));
                                    cmd.Parameters.Add(new OracleParameter("c11", ParseDecimal(parts[12])));
                                    cmd.Parameters.Add(new OracleParameter("c12", ParseDecimal(parts[13])));
                                    cmd.Parameters.Add(new OracleParameter("c13", ParseDecimal(parts[14])));
                                    cmd.Parameters.Add(new OracleParameter("c14", ParseDecimal(parts[15])));
                                    cmd.Parameters.Add(new OracleParameter("disable", ParseDecimal(parts[16])));
                                    
                                    var parsedDate = ParseDate(parts[17]);
                                    if (parsedDate.HasValue) {
                                        cmd.Parameters.Add(new OracleParameter("ngayTao", OracleDbType.Date) { Value = parsedDate.Value });
                                    } else {
                                        cmd.Parameters.Add(new OracleParameter("ngayTao", DBNull.Value));
                                    }
                                    
                                    await cmd.ExecuteNonQueryAsync();
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Lỗi dòng {i+1}: {ex.Message}");
                                errorCount++;
                            }
                        }
                    }
                }
                else
                {
                    // Dạng kịch bản SQL thuần túy tách bằng dấu ;
                    var statements = fileContent.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => s.Trim())
                                               .Where(s => !string.IsNullOrEmpty(s))
                                               .ToList();

                    foreach (var stmt in statements)
                    {
                        try
                        {
                            using (OracleCommand cmd = new OracleCommand(stmt, conn))
                            {
                                await cmd.ExecuteNonQueryAsync();
                                successCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Lỗi lệnh: {stmt.Substring(0, Math.Min(stmt.Length, 30))}... Chi tiết: {ex.Message}");
                            errorCount++;
                        }
                    }
                }
            }

            return (successCount, errorCount, errors);
        }

        private decimal? ParseDecimal(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            if (decimal.TryParse(val, out decimal result)) return result;
            return null;
        }

        private DateTime? ParseDate(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            if (DateTime.TryParseExact(val, "dd-MMM-yy", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out DateTime dt))
                return dt;
            if (DateTime.TryParse(val, out DateTime dt2))
                return dt2;
            return null;
        }
    }
}
