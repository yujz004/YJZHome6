using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using VisionSort_Client.Models;

namespace VisionSort_Client.Services
{
    public class SqlDataService : IDataService
    {
        // 连接字符串（告诉程序怎么找到数据库）
        // 注意：如果你的数据库名不同，请修改 "Database=VisionSortDB"
        private readonly string _connectionString =
            "Server=localhost;Database=VisionSortDB;Trusted_Connection=True;";

        public void SaveRecord(InspectionRecord record)
        {
            string sql = @"
                INSERT INTO InspectionRecords 
                (ProductCode, Result, DefectType, Confidence, DefectX, DefectY, DefectW, DefectH, ImagePath, InspTime)
                VALUES 
                (@ProductCode, @Result, @DefectType, @Confidence, @DefectX, @DefectY, @DefectW, @DefectH, @ImagePath, @InspTime)
            ";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                // 把 C# 对象里的数据填充到 SQL 语句中（防止注入攻击）
                cmd.Parameters.AddWithValue("@ProductCode", record.ProductCode);
                cmd.Parameters.AddWithValue("@Result", record.Result);
                cmd.Parameters.AddWithValue("@DefectType", (object)record.DefectType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Confidence", record.Confidence);
                cmd.Parameters.AddWithValue("@DefectX", (object)record.DefectX ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DefectY", (object)record.DefectY ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DefectW", (object)record.DefectW ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DefectH", (object)record.DefectH ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ImagePath", (object)record.ImageSavePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@InspTime", record.InspTime);

                conn.Open();
                cmd.ExecuteNonQuery(); // 执行插入
            }
        }

        public async Task<List<InspectionRecord>> GetAllRecordsAsync()
        {
            var records = new List<InspectionRecord>();
            string sql = "SELECT * FROM InspectionRecords ORDER BY InspTime DESC"; // 最新在前

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var record = new InspectionRecord
                        {
                            RecordID = reader.GetInt32(reader.GetOrdinal("RecordID")),
                            ProductCode = reader.GetString(reader.GetOrdinal("ProductCode")),
                            Result = reader.GetString(reader.GetOrdinal("Result")),
                            DefectType = reader.IsDBNull(reader.GetOrdinal("DefectType")) ? null : reader.GetString(reader.GetOrdinal("DefectType")),
                            Confidence = reader.GetDecimal(reader.GetOrdinal("Confidence")),
                            DefectX = reader.IsDBNull(reader.GetOrdinal("DefectX")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DefectX")),
                            DefectY = reader.IsDBNull(reader.GetOrdinal("DefectY")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DefectY")),
                            DefectW = reader.IsDBNull(reader.GetOrdinal("DefectW")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DefectW")),
                            DefectH = reader.IsDBNull(reader.GetOrdinal("DefectH")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DefectH")),
                            ImageSavePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                            InspTime = reader.GetDateTime(reader.GetOrdinal("InspTime"))
                        };
                        records.Add(record);
                    }
                }
            }
            return records;
        }

    }
}
