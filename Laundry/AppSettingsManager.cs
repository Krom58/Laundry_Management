using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Laundry_Management.Laundry
{
    internal class AppSettingsManager
    {
        // สำหรับ Order ใช้ปี พ.ศ.
        public static string GetNextOrderId()
        {
            return GetNextIdWithYearPrefix("NextOrderId", "NextOrderYear", true); // true = ใช้ปี พ.ศ.
        }

        // สำหรับ Receipt ใช้ปี ค.ศ.
        public static string GetNextReceiptId()
        {
            return GetNextIdWithYearPrefix("NextReceiptId", "NextReceiptYear", false); // false = ใช้ปี ค.ศ.
        }

        private static string GetNextIdWithYearPrefix(string idKey, string yearKey, bool useBuddhistYear)
        {
            try
            {
                using (var conn = DBconfig.GetConnection())
                {
                    conn.Open();
                    EnsureTableExists(conn);

                    // คำนวณปีปัจจุบัน
                    int currentYear;
                    if (useBuddhistYear)
                    {
                        // ปี พ.ศ. = ค.ศ. + 543
                        currentYear = DateTime.Now.Year + 543;
                    }
                    else
                    {
                        // ปี ค.ศ.
                        currentYear = DateTime.Now.Year;
                    }
                    
                    int lastTwoDigits = currentYear % 100; // เอา 2 ตัวท้าย เช่น 2568 -> 68, 2025 -> 25

                    // อ่านปีที่เก็บไว้
                    string savedYear = GetSetting(yearKey, "0");
                    int.TryParse(savedYear, out int storedYear);

                    string nextId;
                    int runningNumber;

                    // ตรวจสอบว่าปีเปลี่ยนหรือไม่
                    if (storedYear != lastTwoDigits)
                    {
                        // ปีเปลี่ยน: รีเซ็ตเลขวิ่งเป็น 1
                        runningNumber = 1;
                        UpdateSetting(idKey, "2"); // เซ็ตค่าถัดไปเป็น 2
                        UpdateSetting(yearKey, lastTwoDigits.ToString()); // บันทึกปีใหม่
                    }
                    else
                    {
                        // ปีเดิม: ใช้เลขวิ่งต่อเนื่อง
                        nextId = GetSetting(idKey, "1");
                        if (!int.TryParse(nextId, out runningNumber))
                        {
                            runningNumber = 1;
                        }
                        UpdateSetting(idKey, (runningNumber + 1).ToString());
                    }

                    // สร้าง ID รูปแบบ YYNNNN (2 ตัวปี + 4 ตัวเลขวิ่ง)
                    return $"{lastTwoDigits:D2}{runningNumber:D4}";
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"เกิดข้อผิดพลาดในการดึง {idKey}: {ex.Message}",
                    "ข้อผิดพลาด", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                
                // กรณีเกิดข้อผิดพลาด ส่งค่าเริ่มต้น
                int year = useBuddhistYear ? (DateTime.Now.Year + 543) % 100 : DateTime.Now.Year % 100;
                return $"{year:D2}0001";
            }
        }

        public static string GetSetting(string key, string defaultValue = "")
        {
            try
            {
                using (var conn = DBconfig.GetConnection())
                {
                    conn.Open();
                    EnsureTableExists(conn);

                    using (var cmd = new SqlCommand(
                        "SELECT SettingValue FROM AppSettings WHERE SettingKey = @key", conn))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                            return result.ToString();

                        using (var insertCmd = new SqlCommand(
                            "INSERT INTO AppSettings (SettingKey, SettingValue) VALUES (@key, @value)", conn))
                        {
                            insertCmd.Parameters.AddWithValue("@key", key);
                            insertCmd.Parameters.AddWithValue("@value", defaultValue);
                            insertCmd.ExecuteNonQuery();
                        }

                        return defaultValue;
                    }
                }
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static bool UpdateSetting(string key, string value)
        {
            try
            {
                using (var conn = DBconfig.GetConnection())
                {
                    conn.Open();
                    EnsureTableExists(conn);

                    using (var cmd = new SqlCommand(
                        @"IF EXISTS (SELECT 1 FROM AppSettings WHERE SettingKey = @key)
                                UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = @key
                              ELSE
                                INSERT INTO AppSettings (SettingKey, SettingValue) VALUES (@key, @value)",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@value", value);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("เกิดข้อผิดพลาดในการบันทึกการตั้งค่า: " + ex.Message,
                    "ข้อผิดพลาด", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        private static void EnsureTableExists(SqlConnection conn)
        {
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppSettings'", conn))
            {
                bool tableExists = (int)cmd.ExecuteScalar() > 0;

                if (!tableExists)
                {
                    using (var createCmd = new SqlCommand(
                        @"CREATE TABLE AppSettings (
                                SettingKey NVARCHAR(50) PRIMARY KEY,
                                SettingValue NVARCHAR(255) NULL
                            )", conn))
                    {
                        createCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
