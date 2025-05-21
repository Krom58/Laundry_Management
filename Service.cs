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
    public partial class Service : Form
    {
        public Service()
        {
            InitializeComponent();
        }

        private void Search_Click(object sender, EventArgs e)
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string itemName = ItemName.Text.Trim();
            string itemNumber = ItemNumber.Text.Trim();
            string gender = Gender.SelectedItem?.ToString() ?? "";
            string serviceType = ServiceType.SelectedItem?.ToString() ?? "";

            // สร้าง query โดยใช้ parameter เพื่อป้องกัน SQL Injection
            string query = "SELECT ServiceID, ServiceType, ItemNumber, ItemName, Price, Gender FROM LaundryService WHERE 1=1";
            if (!string.IsNullOrEmpty(itemName))
                query += " AND ItemName LIKE @itemName";
            if (!string.IsNullOrEmpty(itemNumber))
                query += " AND ItemNumber LIKE @itemNumber";
            if (!string.IsNullOrEmpty(gender))
                query += " AND Gender = @gender";
            if (!string.IsNullOrEmpty(serviceType))
                query += " AND ServiceType = @serviceType";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(itemName))
                        command.Parameters.AddWithValue("@itemName", "%" + itemName + "%");
                    if (!string.IsNullOrEmpty(itemNumber))
                        command.Parameters.AddWithValue("@itemNumber", "%" + itemNumber + "%");
                    if (!string.IsNullOrEmpty(gender))
                        command.Parameters.AddWithValue("@gender", gender);
                    if (!string.IsNullOrEmpty(serviceType))
                        command.Parameters.AddWithValue("@serviceType", serviceType);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        dataGridView1.DataSource = dataTable;
                    }
                }
            }
        }

        private void Service_Load(object sender, EventArgs e)
        {
            // ตั้งค่า AutoSizeColumnsMode และ AutoSizeRowsMode
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView2.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Enable text wrapping for all cells
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView2.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // ป้องกันการพิมพ์ใน ComboBox
            ServiceType.DropDownStyle = ComboBoxStyle.DropDownList;
            Gender.DropDownStyle = ComboBoxStyle.DropDownList;

            // เพิ่มข้อมูลใน ComboBox ServiceType
            ServiceType.Items.Add("ซักแห้ง (Dry Cleaning Service)");
            ServiceType.Items.Add("ซักน้ำ (Laundry Service)");

            // เพิ่มข้อมูลใน ComboBox Gender
            Gender.Items.Add("สุภาพบุรุษ (Gentleman)");
            Gender.Items.Add("สุภาพสตรี (Ladies)");

            LoadAllData();
            LoadSelectedItems();
        }
        private void LoadAllData()
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "SELECT ItemNumber, ItemName, ServiceType, Price, Gender FROM LaundryService";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridView1.DataSource = dataTable;
                }
            }
        }
        private void LoadSelectedItems()
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "SELECT ItemNumber, ItemName, Quantity, TotalAmount FROM SelectedItems";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dataGridView2.DataSource = dt;
            }
        }


        private void btnSaveSelected_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการ");
                return;
            }

            var selectedRow = dataGridView1.SelectedRows[0];
            var priceValue = selectedRow.Cells["Price"].Value?.ToString();
            var itemNumber = selectedRow.Cells["ItemNumber"].Value?.ToString();
            var itemName = selectedRow.Cells["ItemName"].Value?.ToString();

            if (string.IsNullOrEmpty(priceValue))
            {
                MessageBox.Show("ไม่พบข้อมูลราคา");
                return;
            }

            // เช็คว่ามี ItemNumber นี้ใน SelectedItems แล้วหรือยัง
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string checkQuery = "SELECT COUNT(*) FROM SelectedItems WHERE ItemNumber = @itemNumber";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(checkQuery, connection))
            {
                command.Parameters.AddWithValue("@itemNumber", itemNumber);
                connection.Open();
                int count = (int)command.ExecuteScalar();
                if (count > 0)
                {
                    MessageBox.Show("รายการนี้ถูกเลือกไว้แล้ว");
                    return;
                }
            }

            decimal unitPrice = 0;
            decimal.TryParse(priceValue, out unitPrice);

            var itemForm = new Item(unitPrice, itemNumber, itemName, 1); // Default quantity set to 1
            var result = itemForm.ShowDialog();

            // Refresh dataGridView2 after adding a new item
            if (result == DialogResult.OK)
            {
                LoadSelectedItems();
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการลบ");
                return;
            }

            var selectedRow = dataGridView2.SelectedRows[0];
            var itemNumber = selectedRow.Cells["ItemNumber"].Value?.ToString();

            if (string.IsNullOrEmpty(itemNumber))
            {
                MessageBox.Show("ไม่พบรหัสสินค้า");
                return;
            }

            var confirmResult = MessageBox.Show("คุณต้องการลบรายการนี้หรือไม่?", "ยืนยันการลบ", MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
                string query = "DELETE FROM SelectedItems WHERE ItemNumber = @itemNumber";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@itemNumber", itemNumber);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                LoadSelectedItems(); // Refresh the grid
            }
        }

        private void btnFix_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการแก้ไข");
                return;
            }

            var selectedRow = dataGridView2.SelectedRows[0];
            var itemNumber = selectedRow.Cells["ItemNumber"].Value?.ToString();
            var itemName = selectedRow.Cells["ItemName"].Value?.ToString();
            var quantityValue = selectedRow.Cells["Quantity"].Value?.ToString();
            var totalAmountValue = selectedRow.Cells["TotalAmount"].Value?.ToString();

            if (string.IsNullOrEmpty(itemNumber) || string.IsNullOrEmpty(itemName) ||
                string.IsNullOrEmpty(quantityValue) || string.IsNullOrEmpty(totalAmountValue))
            {
                MessageBox.Show("ข้อมูลไม่ครบถ้วน");
                return;
            }

            int quantity = 0;
            decimal totalAmount = 0;
            decimal.TryParse(totalAmountValue, out totalAmount);
            int.TryParse(quantityValue, out quantity);

            if (quantity == 0)
            {
                MessageBox.Show("จำนวนไม่ถูกต้อง");
                return;
            }

            decimal unitPrice = totalAmount / quantity;

            // ส่งข้อมูลเดิมไปให้ Item ฟอร์ม
            var itemForm = new Item(unitPrice, itemNumber, itemName, quantity);
            itemForm.IsEditMode = true; // เพิ่ม property นี้ใน Item.cs

            var result = itemForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                LoadSelectedItems(); // แค่ refresh grid ไม่ต้อง update DB ที่นี่
            }
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
