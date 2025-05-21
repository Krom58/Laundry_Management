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
            LoadCustomerData();
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

            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "INSERT INTO Customer (FullName, Phone, Discount) VALUES (@FullName, @Phone, @Discount)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Phone", phone);
                command.Parameters.AddWithValue("@Discount", (object)discount ?? DBNull.Value);

                connection.Open();
                command.ExecuteNonQuery();
            }

            MessageBox.Show("บันทึกข้อมูลลูกค้าเรียบร้อยแล้ว");
            // ล้างค่า TextBox ทั้งหมด
            txtFullName.Text = "";
            txtPhone.Text = "";
            txtDiscount.Text = "";
            LoadCustomerData();
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการแก้ไข");
                return;
            }

            var selectedRow = dataGridView1.SelectedRows[0];
            var customerIdObj = selectedRow.Cells["CustomerID"].Value;
            if (customerIdObj == null)
            {
                MessageBox.Show("ไม่พบข้อมูลรหัสลูกค้า");
                return;
            }
            int customerId = Convert.ToInt32(customerIdObj);

            string fullName = selectedRow.Cells["FullName"].Value?.ToString();
            string phone = selectedRow.Cells["Phone"].Value?.ToString();
            string discountStr = selectedRow.Cells["Discount"].Value?.ToString();
            int? discount = null;
            if (int.TryParse(discountStr, out int d))
                discount = d;

            using (var modifyForm = new ModifyCustomer(customerId, fullName, phone, discount))
            {
                if (modifyForm.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomerData();
                }
            }
        }
        private void LoadCustomerData()
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "SELECT CustomerID, FullName, Phone, Discount FROM Customer";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dataGridView1.DataSource = dt;
            }
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
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการลบ");
                return;
            }

            // ดึงค่า CustomerID จากแถวที่เลือก (แม้คอลัมน์จะถูกซ่อน แต่ยังเข้าถึงได้)
            var selectedRow = dataGridView1.SelectedRows[0];
            var customerIdObj = selectedRow.Cells["CustomerID"].Value;

            if (customerIdObj == null)
            {
                MessageBox.Show("ไม่พบข้อมูลรหัสลูกค้า");
                return;
            }

            int customerId = Convert.ToInt32(customerIdObj);

            var confirmResult = MessageBox.Show("คุณต้องการลบข้อมูลลูกค้ารายนี้หรือไม่?", "ยืนยันการลบ", MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
                string query = "DELETE FROM Customer WHERE CustomerID = @CustomerID";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customerId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("ลบข้อมูลลูกค้าเรียบร้อยแล้ว");
                LoadCustomerData();
            }
        }
    }
}
