using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laundry_Management.Laundry
{
    public partial class Setting_Id : Form
    {
        // ลบตัวแปร _cs ออก

        public Setting_Id()
        {
            InitializeComponent();
            LoadSettings();
        }
        private void LoadSettings()
        {
            try
            {
                using (var conn = DBconfig.GetConnection())
                {
                    conn.Open();

                    // First, check if the AppSettings table exists
                    bool tableExists = false;
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppSettings'", conn))
                    {
                        tableExists = (int)cmd.ExecuteScalar() > 0;
                    }

                    // If table doesn't exist, create it
                    if (!tableExists)
                    {
                        using (var cmd = new SqlCommand(
                            @"CREATE TABLE AppSettings (
                                    SettingKey NVARCHAR(50) PRIMARY KEY,
                                    SettingValue NVARCHAR(255) NULL
                                )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Get settings values
                    using (var cmd = new SqlCommand(
                        "SELECT SettingKey, SettingValue FROM AppSettings WHERE SettingKey IN ('NextOrderId', 'NextReceiptId')", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            bool foundOrderId = false;
                            bool foundReceiptId = false;

                            while (reader.Read())
                            {
                                string key = reader["SettingKey"].ToString();
                                string value = reader["SettingValue"].ToString();

                                if (key == "NextOrderId")
                                {
                                    txtNextOrderId.Text = value;
                                    foundOrderId = true;
                                }
                                else if (key == "NextReceiptId")
                                {
                                    txtNextReceiptId.Text = value;
                                    foundReceiptId = true;
                                }
                            }

                            // If settings weren't found, set default values
                            if (!foundOrderId)
                                txtNextOrderId.Text = "1";
                            if (!foundReceiptId)
                                txtNextReceiptId.Text = "1";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดในการโหลดการตั้งค่า: " + ex.Message,
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Set default values in case of error
                txtNextOrderId.Text = "1";
                txtNextReceiptId.Text = "1";
            }
        }
        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(txtNextOrderId.Text) ||
                    string.IsNullOrWhiteSpace(txtNextReceiptId.Text))
                {
                    MessageBox.Show("กรุณากรอกเลขเริ่มต้นทั้งหมด", "ข้อมูลไม่ครบถ้วน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate numeric values
                if (!int.TryParse(txtNextOrderId.Text, out int orderIdValue) || orderIdValue < 1)
                {
                    MessageBox.Show("กรุณากรอกเลข Order เริ่มต้นเป็นตัวเลขที่มากกว่า 0 และกรอกแต่ตัวเลขเท่านั้น", "ข้อมูลไม่ถูกต้อง",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNextOrderId.Focus();
                    return;
                }

                if (!int.TryParse(txtNextReceiptId.Text, out int receiptIdValue) || receiptIdValue < 1)
                {
                    MessageBox.Show("กรุณากรอกเลข Receipt เริ่มต้นเป็นตัวเลขที่มากกว่า 0 และกรอกแต่ตัวเลขเท่านั้น", "ข้อมูลไม่ถูกต้อง",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNextReceiptId.Focus();
                    return;
                }

                // Save settings
                using (var conn = DBconfig.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update or insert NextOrderId
                            using (var cmd = new SqlCommand(
                                @"IF EXISTS (SELECT 1 FROM AppSettings WHERE SettingKey = 'NextOrderId')
                                        UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'NextOrderId'
                                      ELSE
                                        INSERT INTO AppSettings (SettingKey, SettingValue) VALUES ('NextOrderId', @value)",
                                conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@value", txtNextOrderId.Text.Trim());
                                cmd.ExecuteNonQuery();
                            }

                            // Update or insert NextReceiptId
                            using (var cmd = new SqlCommand(
                                @"IF EXISTS (SELECT 1 FROM AppSettings WHERE SettingKey = 'NextReceiptId')
                                        UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'NextReceiptId'
                                      ELSE
                                        INSERT INTO AppSettings (SettingKey, SettingValue) VALUES ('NextReceiptId', @value)",
                                conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@value", txtNextReceiptId.Text.Trim());
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            MessageBox.Show("บันทึกการตั้งค่าเรียบร้อยแล้ว", "สำเร็จ",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดในการบันทึกการตั้งค่า: " + ex.Message,
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
