using Laundry_Management.Laundry;
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
            string query = "SELECT ItemNumber, ItemName, ServiceType, Price, Gender FROM LaundryService WHERE IsCancelled = N'ยังไม่ยกเลิก'";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridView1.DataSource = dataTable;
                }
                dataGridView1.Columns["ItemNumber"].HeaderText = "หมายเลขรายการ";
                dataGridView1.Columns["ItemName"].HeaderText = "ชื่อ-รายการ";
                dataGridView1.Columns["ServiceType"].HeaderText = "ประเภทการซัก";
                dataGridView1.Columns["Price"].HeaderText = "ราคา";
                dataGridView1.Columns["Gender"].HeaderText = "เพศ";
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
            dataGridView2.Columns["ItemNumber"].HeaderText = "หมายเลขรายการ";
            dataGridView2.Columns["ItemName"].HeaderText = "ชื่อ-รายการ";
            dataGridView2.Columns["Quantity"].HeaderText = "จำนวนชิ้น";
            dataGridView2.Columns["TotalAmount"].HeaderText = "จำนวนเงิน";
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

        private void btnEnd_Click(object sender, EventArgs e)
        {
            var selectedItems = GetSelectedItems();
            if (!selectedItems.Any())
            {
                MessageBox.Show("กรุณาเลือกรายการก่อนบันทึก");
                return;
            }

            // เลือกลูกค้า
            using (var selectForm = new Select_Customer(selectedItems))
            {
                if (selectForm.ShowDialog(this) != DialogResult.OK)
                    return;

                string customerName = selectForm.SelectedCustomerName;
                string phone = selectForm.SelectedPhone;
                decimal discount = selectForm.SelectedDiscount;

                // คำนวณยอด
                decimal grandTotal = selectedItems.Sum(i => i.TotalAmount);
                DateTime orderDate = DateTime.Now;
                DateTime pickupDate = orderDate.AddDays(2);
                decimal discountedTotal = discount > 0
                    ? grandTotal - (grandTotal * (discount / 100m))
                    : grandTotal;

                // 1) Save Header → get orderId
                int orderId = SaveHeaderToDatabase(
                    customerName, phone, discount,
                    grandTotal, discountedTotal,
                    orderDate, pickupDate);

                if (orderId == 0)
                {
                    MessageBox.Show("บันทึกหัวคำสั่งซื้อไม่สำเร็จ");
                    return;
                }

                // เตรียมข้อมูลสำหรับพิมพ์
                var serviceItems = selectedItems
                    .Select(i => new Print_Service.ServiceItem
                    {
                        Name = i.ItemName,
                        Quantity = i.Quantity,
                        Price = i.TotalAmount / i.Quantity
                    })
                    .ToList();

                // 2) แสดงฟอร์มพิมพ์
                using (var printForm = new Print_Service(
                    customerName,
                    phone,
                    discount / 100m,
                    orderId.ToString(),
                    serviceItems))
                {
                    printForm.ShowDialog(this);

                    if (!printForm.IsPrinted)
                    {
                        // ยกเลิกการพิมพ์ → ลบ Header คืนแล้วจบ
                        DeleteOrderHeader(orderId);
                        return;
                    }
                }

                // 3) ถ้าพิมพ์สำเร็จ → Save Items
                SaveItemsToDatabase(orderId, selectedItems);

                // 4) เคลียร์ตะกร้า และแจ้งผล
                ClearSelectedItems();
                MessageBox.Show("พิมพ์และบันทึกสำเร็จ");
            }
        }
        // เพิ่มเมธอดนี้ในคลาส Service
        private void ClearSelectedItems()
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "DELETE FROM SelectedItems";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadSelectedItems(); // Refresh the grid if needed
        }
        private List<Item> GetSelectedItems()
        {
            var selectedItems = new List<Item>();

            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Cells["ItemNumber"].Value != null &&
                    row.Cells["ItemName"].Value != null &&
                    row.Cells["Quantity"].Value != null &&
                    row.Cells["TotalAmount"].Value != null)
                {
                    var item = new Item
                    {
                        ItemNumber = row.Cells["ItemNumber"].Value.ToString(),
                        ItemName = row.Cells["ItemName"].Value.ToString(),
                        Quantity = int.Parse(row.Cells["Quantity"].Value.ToString()),
                        TotalAmount = decimal.Parse(row.Cells["TotalAmount"].Value.ToString())
                    };
                    selectedItems.Add(item);
                }
            }

            return selectedItems;
        }
        private int SaveHeaderToDatabase(
    string customerName, string phone, decimal discount,
    decimal grandTotal, decimal discountedTotal,
    DateTime orderDate, DateTime pickupDate)
        {
            int orderId = 0;
            string cs = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO OrderHeader
  (CustomerName, Phone, Discount, GrandTotalPrice, DiscountedTotal, OrderDate, PickupDate)
OUTPUT INSERTED.OrderID
VALUES
  (@cust,@phone,@disc,@grand,@discTot,@odt,@pdt);", conn))
                {
                    cmd.Parameters.AddWithValue("@cust", customerName);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@disc", discount);
                    cmd.Parameters.AddWithValue("@grand", grandTotal);
                    cmd.Parameters.AddWithValue("@discTot", discountedTotal);
                    cmd.Parameters.AddWithValue("@odt", orderDate);
                    cmd.Parameters.AddWithValue("@pdt", pickupDate);

                    object result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out orderId))
                    {
                        // got orderId
                    }
                    else
                    {
                        orderId = 0;
                    }
                }
            }
            return orderId;
        }
        private void SaveItemsToDatabase(int orderId, List<Item> items)
        {
            string cs = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                foreach (Item i in items)
                {
                    using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO OrderItem
  (OrderID, ItemNumber, ItemName, Quantity, TotalAmount)
VALUES
  (@oid,@num,@name,@qty,@amt);", conn))
                    {
                        cmd.Parameters.AddWithValue("@oid", orderId);
                        cmd.Parameters.AddWithValue("@num", i.ItemNumber);
                        cmd.Parameters.AddWithValue("@name", i.ItemName);
                        cmd.Parameters.AddWithValue("@qty", i.Quantity);
                        cmd.Parameters.AddWithValue("@amt", i.TotalAmount);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // 3) ลบ Header ถ้าพิมพ์ไม่สำเร็จ
        private void DeleteOrderHeader(int orderId)
        {
            string cs = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM OrderHeader WHERE OrderID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
