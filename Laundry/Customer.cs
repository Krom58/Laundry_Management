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

namespace Laundry_Management
{
    public partial class Customer : Form
    {
        public Customer()
        {
            InitializeComponent();

            txtFullName.KeyPress += TxtSearch_KeyPress;
            txtPhone.KeyPress += TxtSearch_KeyPress;

            LoadCustomerData();
        }
        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ตรวจสอบว่ากด Enter หรือไม่ (รหัส ASCII 13)
            if (e.KeyChar == (char)Keys.Enter)
            {
                // ป้องกันเสียง beep
                e.Handled = true;

                // เรียกฟังก์ชัน Search เหมือนกดปุ่มค้นหา
                Search_Click(sender, EventArgs.Empty);
            }
        }
        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string discountText = txtDiscount.Text.Trim();
            int? discount = null;
            string note = txtNote.Text.Trim();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("กรุณากรอกชื่อ-นามสกุล และเบอร์โทร");
                return;
            }

            if (!string.IsNullOrEmpty(discountText))
            {
                if (int.TryParse(discountText, out int d))
                    discount = d;
                else
                {
                    MessageBox.Show("กรุณากรอกส่วนลดเป็นตัวเลขจำนวนเต็ม");
                    return;
                }
            }

            string query = "INSERT INTO Customer (FullName, Phone, Discount, Note) VALUES (@FullName, @Phone, @Discount, @Note)";

            using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Phone", phone);
                command.Parameters.AddWithValue("@Discount", (object)discount ?? DBNull.Value);
                command.Parameters.AddWithValue("@Note", note);

                connection.Open();
                command.ExecuteNonQuery();
            }

            MessageBox.Show("บันทึกข้อมูลลูกค้าเรียบร้อยแล้ว");
            // ล้างค่า TextBox ทั้งหมด
            txtFullName.Text = "";
            txtPhone.Text = "";
            txtDiscount.Text = "";
            txtNote.Text = "";
            LoadCustomerData();
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการเลือกเซลล์หรือไม่
            if (dataGridView1.CurrentCell == null)
            {
                MessageBox.Show("กรุณาเลือกข้อมูลที่ต้องการแก้ไข");
                return;
            }

            // ดึงแถวที่เซลล์ปัจจุบันอยู่
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            DataGridViewRow row = dataGridView1.Rows[rowIndex];

            // ดึงค่า CustomerID จากแถวนั้น
            var customerIdObj = row.Cells["CustomerID"].Value;

            if (customerIdObj == null)
            {
                MessageBox.Show("ไม่พบข้อมูลรหัสลูกค้า");
                return;
            }
            int customerId = Convert.ToInt32(customerIdObj);

            // ดึงข้อมูลลูกค้าจากแถว
            string fullName = row.Cells["FullName"].Value?.ToString();
            string phone = row.Cells["Phone"].Value?.ToString();
            string discountStr = row.Cells["Discount"].Value?.ToString();
            int? discount = null;
            string note = row.Cells["Note"].Value?.ToString();
            if (int.TryParse(discountStr, out int d))
                discount = d;

            // เปิดฟอร์มแก้ไขข้อมูล
            using (var modifyForm = new ModifyCustomer(customerId, fullName, phone, discount, note))
            {
                if (modifyForm.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomerData();
                }
            }
        }
        private void LoadCustomerData()
        {
            string query = "SELECT CustomerID, FullName, Phone, Discount, Note FROM Customer";

            using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dataGridView1.DataSource = dt;
            }
            dataGridView1.Columns["FullName"].HeaderText = "ชื่อ-นามสกุล";
            dataGridView1.Columns["Phone"].HeaderText = "เบอร์โทร";
            dataGridView1.Columns["Discount"].HeaderText = "ส่วนลด";
            dataGridView1.Columns["Note"].HeaderText = "หมายเหตุ";
            if (dataGridView1.Columns["CustomerID"] != null)
            {
                dataGridView1.Columns["CustomerID"].Visible = false;
            }
        }

        private void Customer_Load(object sender, EventArgs e)
        {
            // ตั้งค่า AutoSizeColumnsMode และ AutoSizeRowsMode
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            LoadCustomerData();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการเลือกเซลล์หรือไม่
            if (dataGridView1.CurrentCell == null)
            {
                MessageBox.Show("กรุณาเลือกข้อมูลที่ต้องการลบ");
                return;
            }

            // ดึงแถวที่เซลล์ปัจจุบันอยู่
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            DataGridViewRow row = dataGridView1.Rows[rowIndex];

            // ดึงค่า CustomerID จากแถวนั้น
            var customerIdObj = row.Cells["CustomerID"].Value;

            if (customerIdObj == null)
            {
                MessageBox.Show("ไม่พบข้อมูลรหัสลูกค้า");
                return;
            }

            int customerId = Convert.ToInt32(customerIdObj);
            string customerName = row.Cells["FullName"].Value?.ToString() ?? "ไม่ระบุชื่อ";

            // สร้าง Custom Confirmation Dialog ขนาดใหญ่พร้อมตัวอักษรขนาดใหญ่
            Form confirmDialog = new Form();
            confirmDialog.Text = "ยืนยันการลบข้อมูล";
            confirmDialog.StartPosition = FormStartPosition.CenterParent;
            confirmDialog.Size = new Size(900, 600);
            confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            confirmDialog.MaximizeBox = false;
            confirmDialog.MinimizeBox = false;

            // ข้อความแจ้งเตือน
            Label confirmLabel = new Label();
            confirmLabel.Text = $"คุณต้องการลบข้อมูลลูกค้า\n\"{customerName}\"\nใช่หรือไม่?";
            confirmLabel.Font = new Font("Angsana New", 28, FontStyle.Bold);
            confirmLabel.AutoSize = true;
            confirmLabel.Location = new Point(confirmDialog.Width - 600, confirmDialog.Height - 450);
            confirmLabel.TextAlign = ContentAlignment.MiddleCenter;
            confirmLabel.Padding = new Padding(0, 30, 0, 30);

            // ปุ่มยืนยัน
            Button yesButton = new Button();
            yesButton.Text = "ใช่ ลบข้อมูล";
            yesButton.Font = new Font("Angsana New", 22, FontStyle.Bold);
            yesButton.DialogResult = DialogResult.Yes;
            yesButton.Size = new Size(180, 70);
            yesButton.Location = new Point(confirmDialog.Width - 400, confirmDialog.Height - 150);
            yesButton.BackColor = Color.FromArgb(192, 0, 0); // สีแดง
            yesButton.ForeColor = Color.White;

            // ปุ่มยกเลิก
            Button noButton = new Button();
            noButton.Text = "ไม่ ยกเลิก";
            noButton.Font = new Font("Angsana New", 22, FontStyle.Regular);
            noButton.DialogResult = DialogResult.No;
            noButton.Size = new Size(180, 70);
            noButton.Location = new Point(confirmDialog.Width - 200, confirmDialog.Height - 150);

            // เพิ่มองค์ประกอบทั้งหมดลงในฟอร์ม
            confirmDialog.Controls.Add(confirmLabel);
            confirmDialog.Controls.Add(yesButton);
            confirmDialog.Controls.Add(noButton);

            // แสดงฟอร์มและรอการตอบกลับ
            DialogResult confirmResult = confirmDialog.ShowDialog(this);

            if (confirmResult == DialogResult.Yes)
            {
                string query = "DELETE FROM Customer WHERE CustomerID = @CustomerID";

                using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customerId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("ลบข้อมูลลูกค้าเรียบร้อยแล้ว", "สำเร็จ",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCustomerData();
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();

            // Build the SQL query with parameters
            string query = "SELECT CustomerID, FullName, Phone, Discount, Note FROM Customer WHERE 1=1";
            if (!string.IsNullOrEmpty(fullName))
            {
                query += " AND FullName LIKE @FullName";
            }
            if (!string.IsNullOrEmpty(phone))
            {
                query += " AND Phone LIKE @Phone";
            }

            using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(fullName))
                {
                    command.Parameters.AddWithValue("@FullName", "%" + fullName + "%");
                }
                if (!string.IsNullOrEmpty(phone))
                {
                    command.Parameters.AddWithValue("@Phone", "%" + phone + "%");
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt;
                }
            }
        }
    }
}
