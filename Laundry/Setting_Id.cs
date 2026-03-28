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

        public Setting_Id()
        {
            InitializeComponent();
            
            // เพิ่ม Load event
            this.Load += Setting_Id_Load;
        }

        private void Setting_Id_Load(object sender, EventArgs e)
        {
            // สร้าง Label หลังจากฟอร์มโหลดเสร็จแล้ว
            CreatePreviewLabels();
            
            // โหลดค่าจากฐานข้อมูล
            LoadSettings();
            
            // เพิ่ม event สำหรับการเปลี่ยนค่าใน TextBox
            txtNextOrderId.TextChanged += TxtNextOrderId_TextChanged;
            txtNextReceiptId.TextChanged += TxtNextReceiptId_TextChanged;
        }

        private void CreatePreviewLabels()
        {
            // สร้าง Label แสดงตัวอย่าง Order ID
            lblOrderIdPreview = new Label();
            lblOrderIdPreview.Font = new Font("Angsana New", 18, FontStyle.Bold);
            lblOrderIdPreview.ForeColor = Color.Blue;
            lblOrderIdPreview.AutoSize = true;
            lblOrderIdPreview.Location = new Point(txtNextOrderId.Right + 20, txtNextOrderId.Top + 10);
            lblOrderIdPreview.Text = "";
            this.Controls.Add(lblOrderIdPreview);

            // สร้าง Label แสดงตัวอย่าง Receipt ID
            lblReceiptIdPreview = new Label();
            lblReceiptIdPreview.Font = new Font("Angsana New", 18, FontStyle.Bold);
            lblReceiptIdPreview.ForeColor = Color.Blue;
            lblReceiptIdPreview.AutoSize = true;
            lblReceiptIdPreview.Location = new Point(txtNextReceiptId.Right + 20, txtNextReceiptId.Top + 10);
            lblReceiptIdPreview.Text = "";
            this.Controls.Add(lblReceiptIdPreview);
        }

        private void TxtNextOrderId_TextChanged(object sender, EventArgs e)
        {
            UpdateOrderIdPreview();
        }

        private void TxtNextReceiptId_TextChanged(object sender, EventArgs e)
        {
            UpdateReceiptIdPreview();
        }

        private void UpdateOrderIdPreview()
        {
            if (lblOrderIdPreview == null) return; // ป้องกัน null

            if (int.TryParse(txtNextOrderId.Text, out int runningNumber) && runningNumber > 0 && runningNumber <= 9999)
            {
                // คำนวณปี พ.ศ. 2 ตัวท้าย
                int buddhistYear = DateTime.Now.Year + 543;
                int yearPrefix = buddhistYear % 100;

                // แสดงตัวอย่าง
                string preview = $"{yearPrefix:D2}{runningNumber:D4}";
                lblOrderIdPreview.Text = $"→ ตัวอย่าง: {preview}";
                lblOrderIdPreview.ForeColor = Color.Blue;
            }
            else if (!string.IsNullOrEmpty(txtNextOrderId.Text))
            {
                lblOrderIdPreview.Text = "⚠ ต้องเป็นตัวเลข 1-9999";
                lblOrderIdPreview.ForeColor = Color.Red;
            }
            else
            {
                lblOrderIdPreview.Text = "";
            }
        }

        private void UpdateReceiptIdPreview()
        {
            if (lblReceiptIdPreview == null) return; // ป้องกัน null

            if (int.TryParse(txtNextReceiptId.Text, out int runningNumber) && runningNumber > 0 && runningNumber <= 9999)
            {
                // คำนวณปี ค.ศ. 2 ตัวท้าย
                int christianYear = DateTime.Now.Year;
                int yearPrefix = christianYear % 100;

                // แสดงตัวอย่าง
                string preview = $"{yearPrefix:D2}{runningNumber:D4}";
                lblReceiptIdPreview.Text = $"→ ตัวอย่าง: {preview}";
                lblReceiptIdPreview.ForeColor = Color.Blue;
            }
            else if (!string.IsNullOrEmpty(txtNextReceiptId.Text))
            {
                lblReceiptIdPreview.Text = "⚠ ต้องเป็นตัวเลข 1-9999";
                lblReceiptIdPreview.ForeColor = Color.Red;
            }
            else
            {
                lblReceiptIdPreview.Text = "";
            }
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

                    // Get settings values (เฉพาะเลขวิ่ง)
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

                    // อัพเดทตัวอย่างหลังจากโหลดเสร็จ
                    UpdateOrderIdPreview();
                    UpdateReceiptIdPreview();
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

                // Validate numeric values (เลขวิ่ง 1-9999)
                if (!int.TryParse(txtNextOrderId.Text, out int orderIdValue) || orderIdValue < 1 || orderIdValue > 9999)
                {
                    MessageBox.Show("กรุณากรอกเลข Order เริ่มต้นเป็นตัวเลข 1-9999 เท่านั้น", "ข้อมูลไม่ถูกต้อง",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNextOrderId.Focus();
                    return;
                }

                if (!int.TryParse(txtNextReceiptId.Text, out int receiptIdValue) || receiptIdValue < 1 || receiptIdValue > 9999)
                {
                    MessageBox.Show("กรุณากรอกเลข Receipt เริ่มต้นเป็นตัวเลข 1-9999 เท่านั้น", "ข้อมูลไม่ถูกต้อง",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNextReceiptId.Focus();
                    return;
                }

                // แสดงตัวอย่าง ID ที่จะใช้
                int buddhistYear = DateTime.Now.Year + 543;
                int christianYear = DateTime.Now.Year;
                string orderIdExample = $"{buddhistYear % 100:D2}{orderIdValue:D4}";
                string receiptIdExample = $"{christianYear % 100:D2}{receiptIdValue:D4}";

                var confirmResult = MessageBox.Show(
                    $"ต้องการบันทึกการตั้งค่าหรือไม่?\n\n" +
                    $"Order ID ถัดไป: {orderIdExample} (ปี พ.ศ. {buddhistYear})\n" +
                    $"Receipt ID ถัดไป: {receiptIdExample} (ปี ค.ศ. {christianYear})",
                    "ยืนยันการบันทึก",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult != DialogResult.Yes)
                    return;

                // Save settings
                using (var conn = DBconfig.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update or insert NextOrderId (เก็บเฉพาะเลขวิ่ง)
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

                            // Update or insert NextReceiptId (เก็บเฉพาะเลขวิ่ง)
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

                            // บันทึกปีปัจจุบัน (สำหรับตรวจสอบการเปลี่ยนปี)
                            using (var cmd = new SqlCommand(
                                @"IF EXISTS (SELECT 1 FROM AppSettings WHERE SettingKey = 'NextOrderYear')
                                            UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'NextOrderYear'
                                          ELSE
                                            INSERT INTO AppSettings (SettingKey, SettingValue) VALUES ('NextOrderYear', @value)",
                                conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@value", (buddhistYear % 100).ToString());
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new SqlCommand(
                                @"IF EXISTS (SELECT 1 FROM AppSettings WHERE SettingKey = 'NextReceiptYear')
                                            UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'NextReceiptYear'
                                          ELSE
                                            INSERT INTO AppSettings (SettingKey, SettingValue) VALUES ('NextReceiptYear', @value)",
                                conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@value", (christianYear % 100).ToString());
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
