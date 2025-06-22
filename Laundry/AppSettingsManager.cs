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
        // Removed _cs, now using DBconfig.GetConnection()

        public static string GetNextOrderId()
        {
            string nextId = GetSetting("NextOrderId", "1");

            int numericId;
            if (int.TryParse(nextId, out numericId))
            {
                if (!UpdateSetting("NextOrderId", (numericId + 1).ToString()))
                {
                    System.Windows.Forms.MessageBox.Show("ไม่สามารถเพิ่มค่า Order ID ได้ กรุณาตรวจสอบการตั้งค่า",
                        "คำเตือน", System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }

            return nextId;
        }

        public static string GetNextReceiptId()
        {
            string nextId = GetSetting("NextReceiptId", "1");

            int numericId;
            if (int.TryParse(nextId, out numericId))
            {
                if (!UpdateSetting("NextReceiptId", (numericId + 1).ToString()))
                {
                    System.Windows.Forms.MessageBox.Show("ไม่สามารถเพิ่มค่า Receipt ID ได้ กรุณาตรวจสอบการตั้งค่า",
                        "คำเตือน", System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }

            return nextId;
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
