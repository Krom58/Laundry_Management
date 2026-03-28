using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Laundry_Management
{
    public partial class Modify_Type_Service : Form
    {
        public int ServiceID { get; set; } // ใช้สำหรับอ้างอิง record ที่จะแก้ไข
        private string _serviceTypeToSelect;
        private string _genderToSelect;
        public Modify_Type_Service(string itemName, string serviceType, string gender, string price, string itemNumber, int serviceId)
        {
            InitializeComponent();
            ItemName.Text = itemName;
            _serviceTypeToSelect = serviceType;
            _genderToSelect = gender;
            Price.Text = price;
            ItemNumber.Text = itemNumber;
            ServiceID = serviceId;
        }

        public Modify_Type_Service()
        {
            InitializeComponent();
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // สร้าง MessageBox สำหรับการยืนยันด้วยตัวอักษรขนาดใหญ่
            Form confirmDialog = new Form();
            confirmDialog.Text = "ยืนยันการบันทึก";
            confirmDialog.StartPosition = FormStartPosition.CenterParent;
            confirmDialog.Size = new Size(500, 300);
            confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            confirmDialog.MaximizeBox = false;
            confirmDialog.MinimizeBox = false;

            // สร้างข้อความยืนยัน
            Label confirmText = new Label();
            confirmText.Text = "คุณต้องการบันทึกการเปลี่ยนแปลงใช่หรือไม่?";
            confirmText.Font = new Font("Angsana New", 28, FontStyle.Bold);
            confirmText.AutoSize = true;
            confirmText.TextAlign = ContentAlignment.MiddleCenter;
            confirmText.Dock = DockStyle.Top;
            confirmText.Padding = new Padding(0, 30, 0, 30);

            // สร้างปุ่มยืนยัน
            Button confirmButton = new Button();
            confirmButton.Text = "ยืนยัน";
            confirmButton.Font = new Font("Angsana New", 22, FontStyle.Regular);
            confirmButton.DialogResult = DialogResult.Yes;
            confirmButton.Size = new Size(150, 60);
            confirmButton.Location = new Point(confirmDialog.Width - 320, confirmDialog.Height - 100);

            // สร้างปุ่มยกเลิก
            Button cancelButton = new Button();
            cancelButton.Text = "ยกเลิก";
            cancelButton.Font = new Font("Angsana New", 22, FontStyle.Regular);
            cancelButton.DialogResult = DialogResult.No;
            cancelButton.Size = new Size(150, 60);
            cancelButton.Location = new Point(confirmDialog.Width - 160, confirmDialog.Height - 100);

            // เพิ่มองค์ประกอบต่างๆ ลงในฟอร์ม
            confirmDialog.Controls.Add(confirmText);
            confirmDialog.Controls.Add(confirmButton);
            confirmDialog.Controls.Add(cancelButton);

            // แสดงกล่องข้อความและรอผลการยืนยัน
            DialogResult result = confirmDialog.ShowDialog(this);

            // ดำเนินการต่อเมื่อผู้ใช้ยืนยัน
            if (result == DialogResult.Yes)
            {
                using (SqlConnection conn = Laundry_Management.Laundry.DBconfig.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    if (chkIsCancelled.Checked)
                    {
                        // สร้างรายการใหม่ก่อน
                        string insertQuery = "INSERT INTO LaundryService (ServiceType, ItemName, Price, Gender, ItemNumber) VALUES (@ServiceType, @ItemName, @Price, @Gender, @ItemNumber)";
                        using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@ServiceType", ServiceType.Text);
                            command.Parameters.AddWithValue("@ItemName", ItemName.Text);
                            command.Parameters.AddWithValue("@Price", Price.Text);
                            command.Parameters.AddWithValue("@Gender", Gender.Text);
                            command.Parameters.AddWithValue("@ItemNumber", ItemNumber.Text);
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        // จากนั้นอัพเดทรายการเดิมเป็น "ไม่ใช้งาน"
                        cmd.CommandText = @"UPDATE LaundryService 
                            SET IsCancelled = N'ไม่ใช้งาน', CancelledDate = @CancelledDate
                            WHERE ServiceID = @ServiceID";
                        cmd.Parameters.AddWithValue("@CancelledDate", dtpCancelledDate.Value);
                    }
                    else if (chkCancel.Checked)
                    {
                        // เฉพาะอัพเดทสถานะเป็น "ไม่ใช้งาน" โดยไม่สร้างรายการใหม่
                        cmd.CommandText = @"UPDATE LaundryService 
                            SET IsCancelled = N'ไม่ใช้งาน', CancelledDate = @CancelledDate
                            WHERE ServiceID = @ServiceID";
                        cmd.Parameters.AddWithValue("@CancelledDate", dtpCancelledDate.Value);
                    }
                    else
                    {
                        // อัพเดทข้อมูลปกติและตั้งสถานะเป็น "ใช้งาน"
                        cmd.CommandText = @"UPDATE LaundryService 
                            SET ServiceType = @ServiceType, ItemName = @ItemName, Price = @Price, Gender = @Gender, ItemNumber = @ItemNumber, 
                                IsCancelled = N'ใช้งาน', CancelledDate = NULL
                            WHERE ServiceID = @ServiceID";
                    }

                    cmd.Parameters.AddWithValue("@ServiceType", ServiceType.Text);
                    cmd.Parameters.AddWithValue("@ItemName", ItemName.Text);
                    cmd.Parameters.AddWithValue("@Price", Price.Text);
                    cmd.Parameters.AddWithValue("@Gender", Gender.Text);
                    cmd.Parameters.AddWithValue("@ItemNumber", ItemNumber.Text);
                    cmd.Parameters.AddWithValue("@ServiceID", ServiceID);

                    cmd.ExecuteNonQuery();
                }

                // แสดงข้อความบันทึกสำเร็จ
                MessageBox.Show("บันทึกข้อมูลเรียบร้อยแล้ว", "สำเร็จ",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            // หากผู้ใช้กดยกเลิก จะไม่มีการดำเนินการใดๆ
        }

        private void Modify_Type_Service_Load(object sender, EventArgs e)
        {
            ServiceType.DropDownStyle = ComboBoxStyle.DropDownList;
            Gender.DropDownStyle = ComboBoxStyle.DropDownList;

            // เพิ่มข้อมูลใน ComboBox ServiceType
            ServiceType.Items.Add("ซักแห้ง (Dry Cleaning Service)");
            ServiceType.Items.Add("ซักน้ำ (Laundry Service)");

            // เพิ่มข้อมูลใน ComboBox Gender
            Gender.Items.Add("สุภาพบุรุษ (Gentleman)");
            Gender.Items.Add("สุภาพสตรี (Ladies)");

            // เลือกค่าตามที่ต้องการ
            if (!string.IsNullOrEmpty(_serviceTypeToSelect) && ServiceType.Items.Contains(_serviceTypeToSelect))
                ServiceType.SelectedItem = _serviceTypeToSelect;
            else if (ServiceType.Items.Count > 0)
                ServiceType.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(_genderToSelect) && Gender.Items.Contains(_genderToSelect))
                Gender.SelectedItem = _genderToSelect;
            else if (Gender.Items.Count > 0)
                Gender.SelectedIndex = 0;
            chkIsCancelled.CheckedChanged += ChkStatus_CheckedChanged;
            chkCancel.CheckedChanged += ChkStatus_CheckedChanged;
        }
        private void ChkStatus_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox currentCheckBox = (CheckBox)sender;

            // ถ้า checkbox ที่เพิ่งถูกคลิกถูก checked
            if (currentCheckBox.Checked)
            {
                // ถ้าเป็น chkIsCancelled ให้ยกเลิกการเลือก chkCancel
                if (currentCheckBox == chkIsCancelled)
                    chkCancel.Checked = false;
                // ถ้าเป็น chkCancel ให้ยกเลิกการเลือก chkIsCancelled
                else if (currentCheckBox == chkCancel)
                    chkIsCancelled.Checked = false;
            }
        }
    }
}
