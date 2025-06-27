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
            ItemName.KeyPress += TxtSearch_KeyPress;
            ItemNumber.KeyPress += TxtSearch_KeyPress;
        }
        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ตรวจสอบว่ากด Enter หรือไม่ (รหัส ASCII 13)
            if (e.KeyChar == (char)Keys.Enter)
            {
                // ป้องกันเสียง beep
                e.Handled = true;

                // เรียกฟังก์ชันค้นหาเหมือนกับการกดปุ่ม
                Search_Click(sender, EventArgs.Empty);
            }
        }
        private void Search_Click(object sender, EventArgs e)
        {
            string itemName = ItemName.Text.Trim();
            string itemNumber = ItemNumber.Text.Trim();
            string gender = Gender.SelectedItem?.ToString() ?? "";
            string serviceType = ServiceType.SelectedItem?.ToString() ?? "";

            // สร้าง query โดยใช้ parameter เพื่อป้องกัน SQL Injection
            string query = "SELECT ServiceID, ItemNumber, ItemName, ServiceType, Price, Gender FROM LaundryService WHERE IsCancelled = N'ใช้งาน'";
            if (!string.IsNullOrEmpty(itemName))
                query += " AND ItemName LIKE @itemName";
            if (!string.IsNullOrEmpty(itemNumber))
                query += " AND ItemNumber LIKE @itemNumber";
            if (!string.IsNullOrEmpty(gender))
                query += " AND Gender = @gender";
            if (!string.IsNullOrEmpty(serviceType))
                query += " AND ServiceType = @serviceType";

            using (SqlConnection connection = DBconfig.GetConnection())
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
                        if (dataGridView1.Columns["ServiceID"] != null)
                        {
                            dataGridView1.Columns["ServiceID"].Visible = false;
                        }
                    }
                }
            }
        }

        private void Service_Load(object sender, EventArgs e)
        {
            dgvItems.CellValueChanged += DgvItems_CellValueChanged;
            dgvItems.RowsAdded += DgvItems_RowsChanged;
            dgvItems.RowsRemoved += DgvItems_RowsChanged;

            // ตั้งค่า AutoSizeColumnsMode และ AutoSizeRowsMode
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Enable text wrapping for all cells
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvItems.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // ตั้งค่า SelectionMode เป็น CellSelect เพื่อให้สามารถเลือกเซลล์เดียวได้
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvItems.SelectionMode = DataGridViewSelectionMode.CellSelect;

            // ป้องกันการพิมพ์ใน ComboBox
            ServiceType.DropDownStyle = ComboBoxStyle.DropDownList;
            Gender.DropDownStyle = ComboBoxStyle.DropDownList;

            // เพิ่มข้อมูลใน ComboBox ServiceType
            ServiceType.Items.Add("");
            ServiceType.Items.Add("ซักแห้ง (Dry Cleaning Service)");
            ServiceType.Items.Add("ซักน้ำ (Laundry Service)");

            // เพิ่มข้อมูลใน ComboBox Gender
            Gender.Items.Add("");
            Gender.Items.Add("สุภาพบุรุษ (Gentleman)");
            Gender.Items.Add("สุภาพสตรี (Ladies)");

            LoadAllData();
            LoadSelectedItems();
            ClearSelectedItems();

            // Initialize the total
            UpdateTotalFromDataGridView();
        }
        private void LoadAllData()
        {
            string query = "SELECT ItemNumber, ItemName, ServiceType, Price, Gender FROM LaundryService WHERE IsCancelled = N'ใช้งาน'";

            using (SqlConnection connection = DBconfig.GetConnection())
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
            string query = "SELECT ItemNumber, ItemName, Quantity, TotalAmount FROM SelectedItems";

            using (SqlConnection connection = DBconfig.GetConnection())
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dgvItems.DataSource = dt;
            }
            dgvItems.Columns["ItemNumber"].HeaderText = "หมายเลขรายการ";
            dgvItems.Columns["ItemName"].HeaderText = "ชื่อ-รายการ";
            dgvItems.Columns["Quantity"].HeaderText = "จำนวนชิ้น";
            dgvItems.Columns["TotalAmount"].HeaderText = "จำนวนเงิน";
        }


        private void btnSaveSelected_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการเลือกเซลล์หรือไม่
            if (dataGridView1.CurrentCell == null)
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการ");
                return;
            }

            // ดึงแถวที่เซลล์ปัจจุบันอยู่
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            DataGridViewRow row = dataGridView1.Rows[rowIndex];

            // ดึงข้อมูลจากแถวนั้น
            var priceValue = row.Cells["Price"].Value?.ToString();
            var itemNumber = row.Cells["ItemNumber"].Value?.ToString();
            var itemName = row.Cells["ItemName"].Value?.ToString();

            if (string.IsNullOrEmpty(priceValue))
            {
                MessageBox.Show("ไม่พบข้อมูลราคา");
                return;
            }

            string checkQuery = "SELECT COUNT(*) FROM SelectedItems WHERE ItemNumber = @itemNumber";
            using (SqlConnection connection = DBconfig.GetConnection())
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

            // ตรวจสอบว่าเป็นรายการรหัส A00 หรือไม่
            if (itemNumber == "A00")
            {
                // ถ้าเป็นรหัส A00 ให้บันทึกลงฐานข้อมูลทันทีโดยกำหนดจำนวนเป็น 1 ชิ้น
                int quantity = 1;
                decimal totalAmount = unitPrice * quantity;

                // บันทึกข้อมูลลงฐานข้อมูล
                string insertQuery = "INSERT INTO SelectedItems (ItemNumber, ItemName, Quantity, TotalAmount) VALUES (@itemNumber, @itemName, @quantity, @totalAmount)";
                using (SqlConnection connection = DBconfig.GetConnection())
                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@itemNumber", itemNumber);
                    command.Parameters.AddWithValue("@itemName", itemName);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@totalAmount", totalAmount);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                // โหลดข้อมูลใหม่
                LoadSelectedItems();
            }
            else
            {
                // สำหรับรายการอื่นๆ ที่ไม่ใช่ A00 ให้ทำงานตามปกติ
                var itemForm = new Item(unitPrice, itemNumber, itemName, 1); // Default quantity set to 1
                var result = itemForm.ShowDialog();

                // Refresh dataGridView2 after adding a new item
                if (result == DialogResult.OK)
                {
                    LoadSelectedItems();
                }
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการเลือกเซลล์หรือไม่
            if (dgvItems.CurrentCell == null)
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการลบ");
                return;
            }

            // ดึงแถวที่เซลล์ปัจจุบันอยู่
            int rowIndex = dgvItems.CurrentCell.RowIndex;
            DataGridViewRow row = dgvItems.Rows[rowIndex];

            // ดึงค่า ItemNumber จากแถวนั้น
            var itemNumber = row.Cells["ItemNumber"].Value?.ToString();

            if (string.IsNullOrEmpty(itemNumber))
            {
                MessageBox.Show("ไม่พบรหัสสินค้า");
                return;
            }

            var confirmResult = MessageBox.Show("คุณต้องการลบรายการนี้หรือไม่?", "ยืนยันการลบ", MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                string query = "DELETE FROM SelectedItems WHERE ItemNumber = @itemNumber";

                using (SqlConnection connection = DBconfig.GetConnection())
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
            // ตรวจสอบว่ามีการเลือกเซลล์หรือไม่
            if (dgvItems.CurrentCell == null)
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการแก้ไข");
                return;
            }

            // ดึงแถวที่เซลล์ปัจจุบันอยู่
            int rowIndex = dgvItems.CurrentCell.RowIndex;
            DataGridViewRow row = dgvItems.Rows[rowIndex];

            // ดึงข้อมูลจากแถวนั้น
            var itemNumber = row.Cells["ItemNumber"].Value?.ToString();
            var itemName = row.Cells["ItemName"].Value?.ToString();
            var quantityValue = row.Cells["Quantity"].Value?.ToString();
            var totalAmountValue = row.Cells["TotalAmount"].Value?.ToString();

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

            // เพิ่มเงื่อนไขสำหรับรหัสผ้า A00 เพื่อให้แก้ไขราคาได้
            if (itemNumber == "A00")
            {
                // แสดง dialog สำหรับการแก้ไขราคา
                using (var priceForm = new Form())
                {
                    priceForm.Text = "แก้ไขราคา";
                    priceForm.Size = new Size(400, 200);
                    priceForm.StartPosition = FormStartPosition.CenterParent;
                    priceForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    priceForm.MaximizeBox = false;
                    priceForm.MinimizeBox = false;

                    Label lblPrice = new Label();
                    lblPrice.Text = "ราคาต่อชิ้น:";
                    lblPrice.Font = new Font("Angsana New", 20, FontStyle.Regular);
                    lblPrice.Location = new Point(20, 20);
                    lblPrice.Size = new Size(150, 40);

                    TextBox txtPrice = new TextBox();
                    txtPrice.Text = unitPrice.ToString("0.00");
                    txtPrice.Font = new Font("Angsana New", 20, FontStyle.Regular);
                    txtPrice.Location = new Point(170, 20);
                    txtPrice.Size = new Size(180, 40);

                    Button btnConfirm = new Button();
                    btnConfirm.Text = "ตกลง";
                    btnConfirm.Font = new Font("Angsana New", 18, FontStyle.Regular);
                    btnConfirm.Location = new Point(80, 80);
                    btnConfirm.Size = new Size(100, 50);
                    btnConfirm.DialogResult = DialogResult.OK;

                    Button btnCancel = new Button();
                    btnCancel.Text = "ยกเลิก";
                    btnCancel.Font = new Font("Angsana New", 18, FontStyle.Regular);
                    btnCancel.Location = new Point(210, 80);
                    btnCancel.Size = new Size(100, 50);
                    btnCancel.DialogResult = DialogResult.Cancel;

                    priceForm.Controls.Add(lblPrice);
                    priceForm.Controls.Add(txtPrice);
                    priceForm.Controls.Add(btnConfirm);
                    priceForm.Controls.Add(btnCancel);
                    priceForm.AcceptButton = btnConfirm;
                    priceForm.CancelButton = btnCancel;

                    if (priceForm.ShowDialog() == DialogResult.OK)
                    {
                        if (decimal.TryParse(txtPrice.Text, out decimal newPrice) && newPrice > 0)
                        {
                            // คำนวณราคารวมใหม่
                            decimal newTotalAmount = newPrice * quantity;

                            // อัพเดทราคาในฐานข้อมูลโดยตรง
                            string updateQuery = "UPDATE SelectedItems SET TotalAmount = @totalAmount WHERE ItemNumber = @itemNumber";
                            using (SqlConnection connection = DBconfig.GetConnection())
                            using (SqlCommand command = new SqlCommand(updateQuery, connection))
                            {
                                command.Parameters.AddWithValue("@itemNumber", itemNumber);
                                command.Parameters.AddWithValue("@totalAmount", newTotalAmount);
                                connection.Open();
                                command.ExecuteNonQuery();
                            }

                            // โหลดข้อมูลใหม่
                            LoadSelectedItems();
                            return;
                        }
                        else
                        {
                            MessageBox.Show("กรุณาระบุราคาที่ถูกต้อง");
                            return;
                        }
                    }
                    else
                    {
                        // ผู้ใช้ยกเลิกการแก้ไขราคา
                        return;
                    }
                }
            }

            // ดำเนินการปกติสำหรับรหัสอื่นๆ
            var result = itemForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                LoadSelectedItems(); // แค่ refresh grid ไม่ต้อง update DB ที่นี่
            }
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
            ClearSelectedItems();
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

                int? customerId = selectForm.SelectedCustomerId; // Get CustomerId
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

                // ดึงค่า OrderId ที่ผู้ใช้ตั้งค่าไว้จาก AppSettingsManager
                string customOrderId = AppSettingsManager.GetNextOrderId();
                bool orderSaved = false;
                int orderId = 0;

                try
                {
                    // 1) Save Header → get orderId
                    orderId = SaveHeaderToDatabase(
                        customerId, customerName, phone, discount,
                        grandTotal, discountedTotal,
                        orderDate, pickupDate, customOrderId);

                    if (orderId == 0)
                    {
                        MessageBox.Show("บันทึกหัวคำสั่งซื้อไม่สำเร็จ");
                        return;
                    }

                    orderSaved = true;

                    // เตรียมข้อมูลสำหรับพิมพ์
                    var serviceItems = selectedItems
                        .Select(i => new Print_Service.ServiceItem
                        {
                            Name = i.ItemName,
                            Quantity = i.Quantity,
                            Price = i.TotalAmount / i.Quantity
                        })
                        .ToList();

                    // 2) แสดงฟอร์มพิมพ์ โดยส่ง customOrderId แทน orderId.ToString()
                    using (var printForm = new Print_Service(
                        customerName,
                        phone,
                        discount / 100,
                        customOrderId, // ใช้ OrderId ที่ผู้ใช้ตั้งค่าไว้แทน
                        serviceItems))
                    {
                        printForm.ShowDialog(this);

                        if (!printForm.IsPrinted)
                        {
                            // ถ้าพิมพ์ไม่สำเร็จ → อัพเดทสถานะเป็น "รายการถูกยกเลิก" แทนที่จะลบ
                            UpdateOrderStatus(orderId, "รายการถูกยกเลิก");

                            // คืนค่า OrderId กลับไปใช้เดิม (ไม่เพิ่มค่าใหม่)
                            // โดยต้องลด OrderId ในฐานข้อมูลลง 1 ค่า เพื่อใช้ซ้ำในครั้งถัดไป
                            string currentId = AppSettingsManager.GetSetting("NextOrderId");
                            int nextId;
                            if (int.TryParse(currentId, out nextId) && nextId > 1)
                            {
                                // ตั้งค่า NextOrderId ให้กลับไปเป็นค่าเดิม (ลดลง 1)
                                AppSettingsManager.UpdateSetting("NextOrderId", (nextId - 1).ToString());
                            }

                            MessageBox.Show("ยกเลิกการพิมพ์ และบันทึกออร์เดอร์เป็นสถานะ 'รายการถูกยกเลิก'");
                            return;
                        }
                    }

                    // 3) ถ้าพิมพ์สำเร็จ → Save Items และอัพเดทสถานะเป็น "รอดำเนินการ"
                    SaveItemsToDatabase(orderId, selectedItems);
                    UpdateOrderStatus(orderId, "ดำเนินการสำเร็จ");

                    // 4) เคลียร์ตะกร้า และแจ้งผล
                    ClearSelectedItems();
                    MessageBox.Show("พิมพ์และบันทึกสำเร็จ");
                }
                catch (Exception ex)
                {
                    // ถ้าเกิดข้อผิดพลาดระหว่างการทำงาน
                    if (orderSaved && orderId > 0)
                    {
                        // ถ้ามีการบันทึก Header แล้ว ให้อัพเดทสถานะเป็น "รายการถูกยกเลิก"
                        UpdateOrderStatus(orderId, "รายการถูกยกเลิก");

                        // คืนค่า OrderId กลับไปใช้เดิม
                        string currentId = AppSettingsManager.GetSetting("NextOrderId");
                        int nextId;
                        if (int.TryParse(currentId, out nextId) && nextId > 1)
                        {
                            // ตั้งค่า NextOrderId ให้กลับไปเป็นค่าเดิม (ลดลง 1)
                            AppSettingsManager.UpdateSetting("NextOrderId", (nextId - 1).ToString());
                        }
                    }

                    MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // เพิ่มเมธอดนี้ในคลาส Service
        private void ClearSelectedItems()
        {
            string query = "DELETE FROM SelectedItems";
            using (SqlConnection connection = DBconfig.GetConnection())
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

            foreach (DataGridViewRow row in dgvItems.Rows)
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
    int? customerId, string customerName, string phone, decimal discount,
    decimal grandTotal, decimal discountedTotal,
    DateTime orderDate, DateTime pickupDate, string customOrderId)
        {
            int orderId = 0;
            using (SqlConnection conn = DBconfig.GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
        INSERT INTO OrderHeader
          (CustomerId, Discount, GrandTotalPrice, DiscountedTotal, OrderDate, PickupDate, CustomOrderId, OrderStatus)
        OUTPUT INSERTED.OrderID
        VALUES
          (@custId, @disc, @grand, @discTot, @odt, @pdt, @customId, @status);", conn))
                {
                    if (customerId.HasValue)
                        cmd.Parameters.AddWithValue("@custId", customerId.Value);
                    else
                        cmd.Parameters.AddWithValue("@custId", DBNull.Value);

                    cmd.Parameters.AddWithValue("@disc", discount);
                    cmd.Parameters.AddWithValue("@grand", grandTotal);
                    cmd.Parameters.AddWithValue("@discTot", discountedTotal);
                    cmd.Parameters.AddWithValue("@odt", orderDate);
                    cmd.Parameters.AddWithValue("@pdt", pickupDate);
                    cmd.Parameters.AddWithValue("@customId", customOrderId);
                    cmd.Parameters.AddWithValue("@status", "ดำเนินการสำเร็จ"); // Default status

                    object result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out orderId))
                    {
                        // Successfully retrieved orderId
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
            using (SqlConnection conn = DBconfig.GetConnection())
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
            using (SqlConnection conn = DBconfig.GetConnection())
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
        private void UpdateOrderStatus(int orderId, string status)
        {
            using (SqlConnection conn = DBconfig.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE OrderHeader SET OrderStatus = @status WHERE OrderID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", orderId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void UpdateTotalFromDataGridView()
        {
            decimal total = 0;

            // Check if dgvItems has data
            if (dgvItems.Rows.Count > 0)
            {
                // Loop through all rows in dgvItems
                foreach (DataGridViewRow row in dgvItems.Rows)
                {
                    // Skip new rows (the empty row at the end)
                    if (row.IsNewRow) continue;

                    // Check if TotalAmount column exists and has a value
                    if (row.Cells["TotalAmount"].Value != null &&
                        row.Cells["TotalAmount"].Value != DBNull.Value)
                    {
                        // Add to total
                        total += Convert.ToDecimal(row.Cells["TotalAmount"].Value);
                    }
                }
            }

            // Update the lblTotal with the calculated total
            lblTotal.Text = total.ToString("N2") + " บาท";
        }
        private void DgvItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Update the total when a cell value changes
            UpdateTotalFromDataGridView();
        }

        private void DgvItems_RowsChanged(object sender, EventArgs e)
        {
            // Update the total when rows are added or removed
            UpdateTotalFromDataGridView();
        }
    }
}
