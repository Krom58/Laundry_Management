using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Drawing.Drawing2D;
using System.IO;
using OfficeOpenXml;

namespace Laundry_Management.Laundry
{
    public partial class Check_List : Form
    {
        public Check_List()
        {
            InitializeComponent();

            // Wire up checkbox event handlers
            chkCompleted.CheckedChanged += chkCompleted_CheckedChanged;
            chkPending.CheckedChanged += chkPending_CheckedChanged;

            // Add this line to wire up the DateTimePicker ValueChanged event
            dtpCreateDate.ValueChanged += dtpCreateDate_ValueChanged;

            txtSearchId.KeyPress += TxtSearch_KeyPress;
            txtCustomerFilter.KeyPress += TxtSearch_KeyPress;

            // Set default checkbox state
            chkPending.Checked = false;
            chkCompleted.Checked = false;

            // Set date picker to today and checked
            dtpCreateDate.Value = DateTime.Today;
            dtpCreateDate.Checked = true;

            // Load today's orders by default
            LoadOrders(null, null, DateTime.Today);

            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvOrders.DataBindingComplete += DgvOrders_DataBindingComplete;
        }
        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if Enter key was pressed (ASCII code 13)
            if (e.KeyChar == (char)Keys.Enter)
            {
                // Prevent the beep sound
                e.Handled = true;

                // Trigger the search button click event
                btnSearch_Click(sender, EventArgs.Empty);
            }
        }
        private void DgvOrders_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // ปรับชื่อหัวคอลัมน์ให้เป็นภาษาไทย
            if (dgvOrders.Columns["OrderID"] != null)
                dgvOrders.Columns["OrderID"].Visible = false;

            if (dgvOrders.Columns["ReceiptID"] != null)
                dgvOrders.Columns["ReceiptID"].Visible = false;

            if (dgvOrders.Columns["หมายเลขใบรับผ้า"] != null)
                dgvOrders.Columns["หมายเลขใบรับผ้า"].HeaderText = "หมายเลขใบรับผ้า";

            if (dgvOrders.Columns["ชื่อลูกค้า"] != null)
                dgvOrders.Columns["ชื่อลูกค้า"].HeaderText = "ชื่อ-นามสกุล ลูกค้า";

            if (dgvOrders.Columns["เบอร์โทรศัพท์"] != null)
                dgvOrders.Columns["เบอร์โทรศัพท์"].HeaderText = "เบอร์โทร";

            if (dgvOrders.Columns["หมายเลขใบเสร็จ"] != null)
                dgvOrders.Columns["หมายเลขใบเสร็จ"].HeaderText = "หมายเลขใบเสร็จ";

            if (dgvOrders.Columns["วันที่รับผ้า"] != null)
                dgvOrders.Columns["วันที่รับผ้า"].HeaderText = "วันที่รับผ้า";

            if (dgvOrders.Columns["สถานะ"] != null)
                dgvOrders.Columns["สถานะ"].HeaderText = "สถานะการรับผ้า";

            if (dgvOrders.Columns["วันที่ลูกค้ามารับ"] != null)
                dgvOrders.Columns["วันที่ลูกค้ามารับ"].HeaderText = "วันที่ลูกค้ามารับ";
        }
        // New fields to handle canceled order printing
        private bool _printCanceledOrdersMode = false;
        private bool _canceledOrdersPrinted = false;

        // Class to store canceled order information
        private class CanceledOrderInfo
        {
            public int OrderID { get; set; }
            public string CustomOrderId { get; set; }
            public string CustomerName { get; set; }
            public string Phone { get; set; }
            public decimal GrandTotalPrice { get; set; }
            public DateTime OrderDate { get; set; }
            public string CancelReason { get; set; }
            public DateTime CancelledDate { get; set; }
        }
        private List<CanceledOrderInfo> GetCanceledOrdersForDate(DateTime date)
        {
            var canceledOrders = new List<CanceledOrderInfo>();

            // SQL query to get canceled orders with CancelReason and CancelledDate
            string query = @"
        SELECT 
            o.OrderID,
            o.CustomOrderId,
            c.FullName AS CustomerName,
            c.Phone,
            o.GrandTotalPrice,
            o.OrderDate,
            o.CancelReason,
            o.CancelledDate
        FROM OrderHeader o
        LEFT JOIN Customer c ON o.CustomerId = c.CustomerID
        WHERE o.OrderStatus = N'รายการถูกยกเลิก'
        AND o.CancelReason IS NOT NULL
        AND o.CancelledDate IS NOT NULL
        AND CAST(o.CancelledDate AS DATE) = @Date";

            try
            {
                using (SqlConnection conn = DBconfig.GetConnection())
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Date", date);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            canceledOrders.Add(new CanceledOrderInfo
                            {
                                OrderID = Convert.ToInt32(reader["OrderID"]),
                                CustomOrderId = reader["CustomOrderId"].ToString(),
                                CustomerName = reader["CustomerName"] != DBNull.Value ? reader["CustomerName"].ToString() : "",
                                Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : "",
                                GrandTotalPrice = reader["GrandTotalPrice"] != DBNull.Value ? Convert.ToDecimal(reader["GrandTotalPrice"]) : 0,
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                CancelReason = reader["CancelReason"].ToString(),
                                CancelledDate = Convert.ToDateTime(reader["CancelledDate"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't stop the main report
                _printErrorMessage = $"ไม่สามารถดึงข้อมูลรายการที่ยกเลิก: {ex.Message}";
            }

            return canceledOrders;
        }
        private void LoadOrders(string customId = null, string customerName = null, DateTime? createDate = null)
        {
            string query = @"
SELECT 
    o.OrderID, 
    -- ถ้าต้องการให้ช่องหมายเลขใบเสร็จว่างเมื่อยกเลิก ก็ดีแล้วเพราะ r.CustomReceiptId จะเป็น NULL
    r.CustomReceiptId     AS 'หมายเลขใบเสร็จ',
    o.CustomOrderId       AS 'หมายเลขใบรับผ้า', 
    o.CustomerId,
    c.FullName            AS 'ชื่อลูกค้า', 
    c.Phone               AS 'เบอร์โทรศัพท์',
    o.GrandTotalPrice     AS 'ราคารวมใบรับผ้า',
    r.TotalBeforeDiscount AS 'ราคารวมใบเสร็จ',
    r.Discount            AS 'ส่วนลด',
    r.TotalAfterDiscount  AS 'ราคารวมหลังหักส่วนลด',
    o.OrderDate           AS 'วันที่ออกใบรับผ้า',
    o.PickupDate          AS 'วันที่ครบกำหนด',
    r.ReceiptID, 
    r.ReceiptStatus       AS 'สถานะใบเสร็จ',
    r.PaymentMethod       AS 'วิธีการชำระเงิน',
    r.IsPickedUp          AS 'สถานะ', 
    r.CustomerPickupDate  AS 'วันที่ลูกค้ามารับ'
FROM OrderHeader o
LEFT JOIN Customer c 
    ON o.CustomerId = c.CustomerID
LEFT JOIN Receipt r 
    ON o.OrderID = r.OrderID
   AND r.ReceiptStatus <> N'ยกเลิกการพิมพ์'
WHERE 
    -- 1. ไม่เอา OrderStatus = 'รายการถูกยกเลิก'
    (o.OrderStatus <> N'รายการถูกยกเลิก'
    OR o.OrderStatus IS NULL)
";

            var filters = new List<string>();
            var parameters = new List<SqlParameter>();

            // ตรวจสอบ checkbox สถานะการรับผ้า
            if (chkPending.Checked)
            {
                filters.Add("(r.IsPickedUp IS NULL OR r.IsPickedUp <> N'มารับแล้ว')");
            }
            else if (chkCompleted.Checked)
            {
                filters.Add("r.IsPickedUp = N'มารับแล้ว'");
            }

            if (!string.IsNullOrEmpty(customId))
            {
                filters.Add("(o.CustomOrderId LIKE @CustomID OR r.CustomReceiptId LIKE @CustomID)");
                parameters.Add(new SqlParameter("@CustomID", "%" + customId + "%"));
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                filters.Add("c.FullName LIKE @CustomerName");
                parameters.Add(new SqlParameter("@CustomerName", "%" + customerName + "%"));
            }

            if (createDate.HasValue)
            {
                // แก้ไขตรงนี้: ค้นหาเฉพาะจากวันที่ใบรับผ้า (OrderDate) เท่านั้น
                filters.Add("CAST(o.OrderDate AS DATE) = @OrderDate");
                parameters.Add(new SqlParameter("@OrderDate", createDate.Value));
            }

            if (filters.Count > 0)
            {
                query += " AND " + string.Join(" AND ", filters);
            }

            using (SqlConnection conn = DBconfig.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dgvOrders.DataSource = dt;
                if (dgvOrders.Columns["OrderID"] != null)
                    dgvOrders.Columns["OrderID"].Visible = false;

                if (dgvOrders.Columns["ReceiptID"] != null)
                    dgvOrders.Columns["ReceiptID"].Visible = false;

                if (dgvOrders.Columns["CustomerId"] != null)
                    dgvOrders.Columns["CustomerId"].Visible = false;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchId = txtSearchId.Text.Trim();
            string customerName = txtCustomerFilter.Text.Trim();
            DateTime? createDate = dtpCreateDate.Checked ? (DateTime?)dtpCreateDate.Value.Date : null;

            LoadOrders(searchId, customerName, createDate);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearchId.Clear();
            txtCustomerFilter.Clear();
            dtpCreateDate.Checked = true;
            dtpCreateDate.Value = DateTime.Today;
            chkPending.Checked = false;
            chkCompleted.Checked = false;
            LoadOrders(null, null, DateTime.Today);
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chkPending_CheckedChanged(object sender, EventArgs e)
        {
            // ถ้า chkPending ถูกเลือก ให้ยกเลิกการเลือก chkCompleted
            if (chkPending.Checked)
            {
                chkCompleted.Checked = false;
            }

            // ดำเนินการค้นหาอีกครั้ง
            btnSearch_Click(sender, e);
        }

        private void chkCompleted_CheckedChanged(object sender, EventArgs e)
        {
            // ถ้า chkCompleted ถูกเลือก ให้ยกเลิกการเลือก chkPending
            if (chkCompleted.Checked)
            {
                chkPending.Checked = false;
            }

            // ดำเนินการค้นหาอีกครั้ง
            btnSearch_Click(sender, e);
        }
        // Add this method to handle the DateTimePicker ValueChanged event
        private void dtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            // Only trigger search if the DateTimePicker is checked (date is selected)
            if (dtpCreateDate.Checked)
            {
                // Call the existing search functionality
                btnSearch_Click(sender, e);
            }
        }

        private void btnReprintOrder_Click(object sender, EventArgs e)
        {
            // Check if a row is selected
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการพิมพ์ใหม่", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Get OrderID from the selected row
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["OrderID"].Value);
                string customOrderId = dgvOrders.CurrentRow.Cells["หมายเลขใบรับผ้า"].Value?.ToString();
                string customerName = dgvOrders.CurrentRow.Cells["ชื่อลูกค้า"].Value?.ToString();
                string phone = dgvOrders.CurrentRow.Cells["เบอร์โทรศัพท์"].Value?.ToString();

                // Check if order is canceled
                using (SqlConnection conn = DBconfig.GetConnection())
                {
                    conn.Open();
                    string checkOrderStatusQuery = "SELECT OrderStatus FROM OrderHeader WHERE OrderID = @OrderID";
                    using (SqlCommand cmd = new SqlCommand(checkOrderStatusQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderID", orderId);
                        string orderStatus = cmd.ExecuteScalar()?.ToString();

                        if (orderStatus == "รายการถูกยกเลิก")
                        {
                            MessageBox.Show("ไม่สามารถพิมพ์รายการที่ถูกยกเลิกได้", "แจ้งเตือน",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }

                // ตรวจสอบสถานะใบเสร็จว่าเป็น "ยกเลิกการพิมพ์" หรือไม่
                var receiptStatusObj = dgvOrders.CurrentRow.Cells["สถานะใบเสร็จ"].Value;
                if (receiptStatusObj != null && receiptStatusObj != DBNull.Value &&
                    receiptStatusObj.ToString() == "ยกเลิกการพิมพ์")
                {
                    MessageBox.Show("ไม่สามารถพิมพ์ใบรับผ้าที่มีใบเสร็จถูกยกเลิกการพิมพ์ได้", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if required data is available
                if (string.IsNullOrEmpty(customOrderId) || string.IsNullOrEmpty(customerName))
                {
                    MessageBox.Show("ข้อมูลสำหรับการพิมพ์ไม่ครบถ้วน", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Retrieve order items and discount from database - Modified to exclude canceled items
                List<Print_Service.ServiceItem> serviceItems = new List<Print_Service.ServiceItem>();
                decimal discount = 0;

                using (SqlConnection conn = DBconfig.GetConnection())
                {
                    conn.Open();

                    // Get discount from OrderHeader
                    string discountQuery = "SELECT Discount FROM OrderHeader WHERE OrderID = @OrderID";
                    using (SqlCommand discountCmd = new SqlCommand(discountQuery, conn))
                    {
                        discountCmd.Parameters.AddWithValue("@OrderID", orderId);
                        var discountObj = discountCmd.ExecuteScalar();
                        if (discountObj != null && discountObj != DBNull.Value)
                        {
                            discount = Convert.ToDecimal(discountObj);
                        }
                    }

                    // Get order items - Modified to exclude canceled items
                    string itemsQuery = @"
                SELECT ItemName, Quantity, TotalAmount 
                FROM OrderItem 
                WHERE OrderID = @OrderID 
                AND (IsCanceled = 0 OR IsCanceled IS NULL)";  // Add condition to exclude canceled items

                    using (SqlCommand itemsCmd = new SqlCommand(itemsQuery, conn))
                    {
                        itemsCmd.Parameters.AddWithValue("@OrderID", orderId);

                        using (SqlDataReader reader = itemsCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string itemName = reader["ItemName"].ToString();
                                int quantity = Convert.ToInt32(reader["Quantity"]);
                                decimal totalAmount = Convert.ToDecimal(reader["TotalAmount"]);

                                // Calculate price per item
                                decimal price = quantity > 0 ? totalAmount / quantity : 0;

                                serviceItems.Add(new Print_Service.ServiceItem
                                {
                                    Name = itemName,
                                    Quantity = quantity,
                                    Price = price
                                });
                            }
                        }
                    }
                }

                // Check if we have items to print
                if (serviceItems.Count == 0)
                {
                    MessageBox.Show("ไม่พบรายการสินค้าที่สามารถพิมพ์ได้ (รายการอาจถูกยกเลิกทั้งหมด)",
                        "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create confirmation dialog
                using (Form confirmDialog = new Form())
                {
                    confirmDialog.Text = "ยืนยันการพิมพ์ซ้ำ";
                    confirmDialog.Size = new Size(500, 300);
                    confirmDialog.StartPosition = FormStartPosition.CenterParent;
                    confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    confirmDialog.MaximizeBox = false;
                    confirmDialog.MinimizeBox = false;

                    Label lblMessage = new Label();
                    lblMessage.Text = $"ยืนยันการพิมพ์ใบรับผ้าอีกครั้ง\nลูกค้า: {customerName}\nหมายเลขใบรับผ้า: {customOrderId}";
                    lblMessage.Font = new Font("Angsana New", 26, FontStyle.Bold);
                    lblMessage.TextAlign = ContentAlignment.MiddleCenter;
                    lblMessage.Dock = DockStyle.Top;
                    lblMessage.Height = 150;

                    Button btnConfirm = new Button();
                    btnConfirm.Text = "ยืนยัน";
                    btnConfirm.Font = new Font("Angsana New", 24);
                    btnConfirm.Size = new Size(150, 60);
                    btnConfirm.Location = new Point(80, 180);
                    btnConfirm.DialogResult = DialogResult.Yes;

                    Button btnCancel = new Button();
                    btnCancel.Text = "ยกเลิก";
                    btnCancel.Font = new Font("Angsana New", 24);
                    btnCancel.Size = new Size(150, 60);
                    btnCancel.Location = new Point(260, 180);
                    btnCancel.DialogResult = DialogResult.Cancel;

                    confirmDialog.Controls.Add(lblMessage);
                    confirmDialog.Controls.Add(btnConfirm);
                    confirmDialog.Controls.Add(btnCancel);
                    confirmDialog.AcceptButton = btnConfirm;
                    confirmDialog.CancelButton = btnCancel;

                    // Show dialog and wait for response
                    DialogResult result = confirmDialog.ShowDialog();

                    // If not confirmed, cancel the operation
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }

                // Open Print_Service form to print the order
                using (var printForm = new Print_Service(
                    customerName,
                    phone,
                    discount / 100m, // Convert from percentage to decimal
                    customOrderId,
                    serviceItems))
                {
                    printForm.ShowDialog(this);

                    // No need to update any status since we're just reprinting
                    if (printForm.IsPrinted)
                    {
                        MessageBox.Show("พิมพ์ใบรับผ้าใหม่เรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReprintReceipt_Click(object sender, EventArgs e)
        {
            // Check if a row is selected
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการพิมพ์ใบเสร็จใหม่", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Check if the selected row has a receipt
                var receiptIdObj = dgvOrders.CurrentRow.Cells["ReceiptID"].Value;
                if (receiptIdObj == null || receiptIdObj == DBNull.Value)
                {
                    MessageBox.Show("รายการที่เลือกยังไม่มีใบเสร็จ กรุณาออกใบเสร็จก่อน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int receiptId = Convert.ToInt32(receiptIdObj);
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["OrderID"].Value);

                // ตรวจสอบสถานะใบเสร็จว่าเป็น "ยกเลิกการพิมพ์" หรือไม่
                var receiptStatusObj = dgvOrders.CurrentRow.Cells["สถานะใบเสร็จ"].Value;
                if (receiptStatusObj != null && receiptStatusObj != DBNull.Value &&
                    receiptStatusObj.ToString() == "ยกเลิกการพิมพ์")
                {
                    MessageBox.Show("ไม่สามารถพิมพ์ใบเสร็จที่ถูกยกเลิกการพิมพ์ได้", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Create OrderHeaderDto object with required data
                var header = new Find_Service.OrderHeaderDto
                {
                    OrderID = orderId,
                    CustomOrderId = dgvOrders.CurrentRow.Cells["หมายเลขใบรับผ้า"].Value?.ToString(),
                    CustomerName = dgvOrders.CurrentRow.Cells["ชื่อลูกค้า"].Value?.ToString(),
                    Phone = dgvOrders.CurrentRow.Cells["เบอร์โทรศัพท์"].Value?.ToString(),
                    CustomReceiptId = dgvOrders.CurrentRow.Cells["หมายเลขใบเสร็จ"].Value?.ToString()
                };

                // Get additional information from the database
                using (SqlConnection conn = DBconfig.GetConnection())
                {
                    conn.Open();

                    // Get receipt details including discount information and receipt date
                    string receiptQuery = @"
                SELECT 
                    TotalBeforeDiscount, 
                    TotalAfterDiscount, 
                    VAT, 
                    Discount, 
                    PaymentMethod, 
                    ReceiptDate
                FROM Receipt 
                WHERE ReceiptID = @ReceiptID";

                    using (SqlCommand cmd = new SqlCommand(receiptQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ReceiptID", receiptId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                header.GrandTotalPrice = reader["TotalBeforeDiscount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["TotalBeforeDiscount"]) : 0;
                                header.DiscountedTotal = reader["TotalAfterDiscount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["TotalAfterDiscount"]) : 0;
                                header.VatAmount = reader["VAT"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["VAT"]) : 0;

                                // Important: Get the discount value
                                decimal discountAmount = reader["Discount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["Discount"]) : 0;

                                // Calculate discount percentage if needed
                                if (header.GrandTotalPrice > 0)
                                {
                                    header.Discount = Math.Round((discountAmount / header.GrandTotalPrice) * 100, 2);
                                    header.TodayDiscount = discountAmount;
                                    // Setting this to false ensures the discount is treated as an amount rather than percentage
                                    header.IsTodayDiscountPercent = false;
                                }

                                // Set SubTotal
                                header.SubTotal = header.GrandTotalPrice;

                                // Set PaymentMethod
                                if (reader["PaymentMethod"] != DBNull.Value)
                                {
                                    header.PaymentMethod = reader["PaymentMethod"].ToString();
                                }

                                // IMPORTANT: Set the OrderDate to ReceiptDate for printing
                                // This fixes the issue with the receipt date display
                                if (reader["ReceiptDate"] != DBNull.Value)
                                {
                                    header.OrderDate = Convert.ToDateTime(reader["ReceiptDate"]);
                                }
                            }
                            else
                            {
                                MessageBox.Show("ไม่พบข้อมูลใบเสร็จในระบบ", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                    }

                    // Get order discount from OrderHeader as backup if needed
                    string orderQuery = "SELECT Discount FROM OrderHeader WHERE OrderID = @OrderID";
                    using (SqlCommand cmd = new SqlCommand(orderQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderID", orderId);
                        var discountObj = cmd.ExecuteScalar();
                        if (discountObj != null && discountObj != DBNull.Value && header.Discount == 0)
                        {
                            // Only use this as a fallback
                            header.Discount = Convert.ToDecimal(discountObj);
                            header.IsTodayDiscountPercent = true;
                        }
                    }

                    // Get receipt items
                    List<Find_Service.OrderItemDto> items = new List<Find_Service.OrderItemDto>();
                    string itemsQuery = @"
                SELECT 
                    ri.ReceiptItemID, 
                    oi.ItemNumber, 
                    oi.ItemName, 
                    ri.Quantity, 
                    ri.Amount, 
                    ri.IsCanceled
                FROM ReceiptItem ri
                INNER JOIN OrderItem oi ON ri.OrderItemID = oi.OrderItemID
                WHERE ri.ReceiptID = @ReceiptID";

                    using (SqlCommand cmd = new SqlCommand(itemsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ReceiptID", receiptId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new Find_Service.OrderItemDto
                                {
                                    OrderItemID = Convert.ToInt32(reader["ReceiptItemID"]),
                                    ItemNumber = reader["ItemNumber"].ToString(),
                                    ItemName = reader["ItemName"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    TotalAmount = Convert.ToDecimal(reader["Amount"]),
                                    IsCanceled = reader["IsCanceled"] != DBNull.Value && (bool)reader["IsCanceled"]
                                });
                            }
                        }
                    }

                    if (items.Count == 0)
                    {
                        MessageBox.Show("ไม่พบรายการสินค้าในใบเสร็จ", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Create confirmation dialog
                    using (Form confirmDialog = new Form())
                    {
                        confirmDialog.Text = "ยืนยันการพิมพ์ใบเสร็จซ้ำ";
                        confirmDialog.Size = new Size(500, 300);
                        confirmDialog.StartPosition = FormStartPosition.CenterParent;
                        confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                        confirmDialog.MaximizeBox = false;
                        confirmDialog.MinimizeBox = false;

                        Label lblMessage = new Label();
                        lblMessage.Text = $"ยืนยันการพิมพ์ใบเสร็จอีกครั้ง\nลูกค้า: {header.CustomerName}\nเลขที่ใบเสร็จ: {header.CustomReceiptId}";
                        lblMessage.Font = new Font("Angsana New", 26, FontStyle.Bold);
                        lblMessage.TextAlign = ContentAlignment.MiddleCenter;
                        lblMessage.Dock = DockStyle.Top;
                        lblMessage.Height = 150;

                        Button btnConfirm = new Button();
                        btnConfirm.Text = "ยืนยัน";
                        btnConfirm.Font = new Font("Angsana New", 24);
                        btnConfirm.Size = new Size(150, 60);
                        btnConfirm.Location = new Point(80, 180);
                        btnConfirm.DialogResult = DialogResult.Yes;

                        Button btnCancel = new Button();
                        btnCancel.Text = "ยกเลิก";
                        btnCancel.Font = new Font("Angsana New", 24);
                        btnCancel.Size = new Size(150, 60);
                        btnCancel.Location = new Point(260, 180);
                        btnCancel.DialogResult = DialogResult.Cancel;

                        confirmDialog.Controls.Add(lblMessage);
                        confirmDialog.Controls.Add(btnConfirm);
                        confirmDialog.Controls.Add(btnCancel);
                        confirmDialog.AcceptButton = btnConfirm;
                        confirmDialog.CancelButton = btnCancel;

                        // Show dialog and wait for response
                        DialogResult result = confirmDialog.ShowDialog();

                        // If not confirmed, cancel the operation
                        if (result != DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    // Open ReceiptPrintForm to print the receipt without making original+copy
                    // Pass false for printOriginalWithCopy parameter
                    using (var printForm = new ReceiptPrintForm(receiptId, header, items, false))
                    {
                        printForm.ShowDialog(this);

                        // No need to update any status as we're just reprinting
                        if (printForm.IsPrinted)
                        {
                            MessageBox.Show("พิมพ์ใบเสร็จใหม่เรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Define A4 dimensions as constants
        private const int A4_WIDTH_HUNDREDTHS = 827;  // 8.27 inches in hundredths
        private const int A4_HEIGHT_HUNDREDTHS = 1169; // 11.69 inches in hundredths

        private void SetA4PaperSize(PrintDocument printDoc)
        {
            // First try to find the predefined A4 size
            bool foundA4 = false;

            foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
            {
                if (ps.Kind == PaperKind.A4 ||
                    ps.PaperName.ToLower().Contains("a4") ||
                    (Math.Abs(ps.Width - A4_WIDTH_HUNDREDTHS) < 10 && Math.Abs(ps.Height - A4_HEIGHT_HUNDREDTHS) < 10))
                {
                    printDoc.DefaultPageSettings.PaperSize = ps;
                    foundA4 = true;
                    break;
                }
            }

            // If A4 is not found, create a custom size
            if (!foundA4)
            {
                PaperSize customA4 = new PaperSize("A4", A4_WIDTH_HUNDREDTHS, A4_HEIGHT_HUNDREDTHS);
                printDoc.DefaultPageSettings.PaperSize = customA4;
            }

            // Set portrait orientation instead of landscape
            printDoc.DefaultPageSettings.Landscape = false;

            // Set reasonable margins
            printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);

            // Set in printer settings as well
            printDoc.PrinterSettings.DefaultPageSettings.PaperSize = printDoc.DefaultPageSettings.PaperSize;
            printDoc.PrinterSettings.DefaultPageSettings.Landscape = false;
        }

        private void EnsureA4PaperSize(PrintDocument printDoc)
        {
            // Get the current paper size
            PaperSize currentSize = printDoc.DefaultPageSettings.PaperSize;

            // Check if dimensions roughly match A4 (allow small tolerance)
            bool isA4Size = (Math.Abs(currentSize.Width - A4_WIDTH_HUNDREDTHS) < 10 &&
                             Math.Abs(currentSize.Height - A4_HEIGHT_HUNDREDTHS) < 10) ||
                            (Math.Abs(currentSize.Height - A4_WIDTH_HUNDREDTHS) < 10 &&
                             Math.Abs(currentSize.Width - A4_HEIGHT_HUNDREDTHS) < 10);

            if (!isA4Size)
            {
                // Force A4 size if current size doesn't match
                printDoc.DefaultPageSettings.PaperSize = new PaperSize("A4", A4_WIDTH_HUNDREDTHS, A4_HEIGHT_HUNDREDTHS);
            }

            // Ensure orientation is portrait
            printDoc.DefaultPageSettings.Landscape = false;

            // Update printer settings too
            printDoc.PrinterSettings.DefaultPageSettings.PaperSize = printDoc.DefaultPageSettings.PaperSize;
            printDoc.PrinterSettings.DefaultPageSettings.Landscape = false;
        }
        private void btnPrint_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvOrders.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะพิมพ์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check for valid data before printing
                bool hasValidRows = false;
                foreach (DataGridViewRow row in dgvOrders.Rows)
                {
                    if (row.Cells["หมายเลขใบรับผ้า"].Value != null || row.Cells["ชื่อลูกค้า"].Value != null)
                    {
                        hasValidRows = true;
                        break;
                    }
                }

                if (!hasValidRows)
                {
                    MessageBox.Show("ไม่พบข้อมูลที่พร้อมพิมพ์ กรุณาตรวจสอบข้อมูล", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // เตรียมข้อมูลรายการที่ถูกยกเลิก (ถ้ามี)
                List<CanceledOrderInfo> canceledOrders = null;
                if (dtpCreateDate.Checked)
                {
                    canceledOrders = GetCanceledOrdersForDate(dtpCreateDate.Value.Date);
                }

                // Create a PrintDocument object
                PrintDocument printDoc = new PrintDocument();
                printDoc.DocumentName = "รายงานข้อมูล";

                // Store the canceledOrders in a class-level field to access it in the handlers
                _currentCanceledOrders = canceledOrders;

                // Set A4 paper size using our custom method (similar to Print_Service)
                SetA4PaperSize(printDoc);

                // Add handlers for print events
                printDoc.PrintPage += (s, args) =>
                {
                    if (_printCanceledOrdersMode)
                    {
                        PrintCanceledOrdersPage(args, _currentCanceledOrders);
                    }
                    else
                    {
                        PrintPage(s, args);
                    }
                };
                printDoc.EndPrint += PrintDoc_EndPrint;
                printDoc.BeginPrint += (s, args) =>
                {
                    _currentPage = 0;
                    _isPrintSuccessful = true;
                    _printErrorMessage = "";
                    _printCanceledOrdersMode = false; // Start with regular report
                    _canceledOrdersPrinted = false;
                };

                // Flag to track print status
                _isPrintSuccessful = true;
                _printErrorMessage = "";

                // Create a PrintDialog
                using (PrintDialog printDialog = new PrintDialog())
                {
                    printDialog.Document = printDoc;
                    printDialog.UseEXDialog = true;
                    printDialog.AllowSomePages = false;

                    // Show the print dialog
                    if (printDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            // Show print status
                            Cursor = Cursors.WaitCursor;

                            // Double-check paper size is still A4 before printing
                            EnsureA4PaperSize(printDoc);

                            // Reset pagination variables before printing
                            _currentPage = 0;
                            printDoc.Print();
                        }
                        catch (Exception ex)
                        {
                            _isPrintSuccessful = false;
                            _printErrorMessage = ex.Message;
                            MessageBox.Show($"เกิดข้อผิดพลาดในการพิมพ์: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            Cursor = Cursors.Default;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการเตรียมพิมพ์: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private List<CanceledOrderInfo> _currentCanceledOrders;
        private bool _isPrintSuccessful = false;
        private string _printErrorMessage = "";
        private int _currentPage = 0;
        private void PrintDoc_EndPrint(object sender, PrintEventArgs e)
        {
            if (_isPrintSuccessful)
            {
                MessageBox.Show("พิมพ์รายงานข้อมูลเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrEmpty(_printErrorMessage))
            {
                MessageBox.Show($"การพิมพ์ไม่สำเร็จ: {_printErrorMessage}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void DrawTableHeader(Graphics g, Font headerFont, float leftX, float yPosition, float rightX,
    string[] columnNames, out float[] columnWidths, out float headerHeight)
        {
            float availableWidth = rightX - leftX;

            // Optimized column width percentages for better spacing
            float[] columnWidthPercentages = new float[] {
        0.08f, // หมายเลขใบรับผ้า
        0.08f, // หมายเลขใบเสร็จ
        0.12f, // ชื่อลูกค้า
        0.08f, // เบอร์โทรศัพท์
        0.07f, // ราคารวมใบรับผ้า
        0.07f, // ราคารวมใบเสร็จ
        0.06f, // ส่วนลด
        0.09f, // ราคารวมหลังหักส่วนลด
        0.09f, // วันที่ออกใบรับผ้า
        0.09f, // วันที่ต้องมารับผ้า
        0.08f, // สถานะ
        0.09f  // วันที่ลูกค้ามารับ
    };

            // Calculate column widths
            columnWidths = new float[columnWidthPercentages.Length];
            for (int i = 0; i < columnWidthPercentages.Length; i++)
            {
                columnWidths[i] = availableWidth * columnWidthPercentages[i];
            }

            // Slightly increased header height for better text fitting
            headerHeight = headerFont.GetHeight() * 2.7f;
            float currentX = leftX;

            for (int i = 0; i < columnNames.Length; i++)
            {
                RectangleF headerRect = new RectangleF(currentX, yPosition, columnWidths[i], headerHeight);

                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    sf.FormatFlags = StringFormatFlags.LineLimit | StringFormatFlags.NoClip; // Support multi-line and no clipping
                    g.FillRectangle(Brushes.LightGray, headerRect);
                    g.DrawRectangle(Pens.Black, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height);
                    g.DrawString(columnNames[i], headerFont, Brushes.Black, headerRect, sf);
                }

                currentX += columnWidths[i];
            }
        }
        // Override the original PrintPage method to handle the page flow
        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                // Original print page implementation (unchanged)
                // Change to portrait orientation
                e.PageSettings.Landscape = false;
                var printDoc = sender as PrintDocument;
                var canceledOrders = _currentCanceledOrders;
                // Adjust margins to better fit in portrait mode
                float leftMargin = e.MarginBounds.Left - 30;
                float topMargin = e.MarginBounds.Top;
                float rightMargin = e.MarginBounds.Right - 20;
                float availableWidth = rightMargin - leftMargin;
                float availableHeight = e.MarginBounds.Bottom - topMargin;

                // Calculate total valid rows once at the beginning to avoid duplicate calculation
                int totalValidRows = dgvOrders.Rows.Cast<DataGridViewRow>().Count(r =>
                    r.Cells["หมายเลขใบรับผ้า"].Value != null || r.Cells["ชื่อลูกค้า"].Value != null);

                // Pre-calculate totals for all valid rows
                decimal totalOrderAmount = 0m;
                decimal totalReceiptAmount = 0m;
                decimal totalDiscount = 0m;
                decimal totalAfterDiscount = 0m;

                foreach (DataGridViewRow row in dgvOrders.Rows)
                {
                    if (row.Cells["หมายเลขใบรับผ้า"].Value != null || row.Cells["ชื่อลูกค้า"].Value != null)
                    {
                        if (row.Cells["ราคารวมใบรับผ้า"].Value != null && row.Cells["ราคารวมใบรับผ้า"].Value != DBNull.Value)
                        {
                            decimal amount;
                            if (decimal.TryParse(row.Cells["ราคารวมใบรับผ้า"].Value.ToString(), out amount))
                            {
                                totalOrderAmount += amount;
                            }
                        }

                        if (row.Cells["ราคารวมใบเสร็จ"].Value != null && row.Cells["ราคารวมใบเสร็จ"].Value != DBNull.Value)
                        {
                            decimal amount;
                            if (decimal.TryParse(row.Cells["ราคารวมใบเสร็จ"].Value.ToString(), out amount))
                            {
                                totalReceiptAmount += amount;
                            }
                        }

                        if (row.Cells["ส่วนลด"].Value != null && row.Cells["ส่วนลด"].Value != DBNull.Value)
                        {
                            decimal discount;
                            if (decimal.TryParse(row.Cells["ส่วนลด"].Value.ToString(), out discount))
                            {
                                totalDiscount += discount;
                            }
                        }

                        if (row.Cells["ราคารวมหลังหักส่วนลด"].Value != null && row.Cells["ราคารวมหลังหักส่วนลด"].Value != DBNull.Value)
                        {
                            decimal netAmount;
                            if (decimal.TryParse(row.Cells["ราคารวมหลังหักส่วนลด"].Value.ToString(), out netAmount))
                            {
                                totalAfterDiscount += netAmount;
                            }
                        }
                    }
                }

                // Use smaller fonts to fit better in portrait mode
                using (Font titleFont = new Font("Angsana New", 12, FontStyle.Bold))
                using (Font headerFont = new Font("Angsana New", 10, FontStyle.Bold))
                using (Font normalFont = new Font("Angsana New", 10))
                using (Font totalFont = new Font("Angsana New", 10, FontStyle.Bold))
                using (Font smallFont = new Font("Angsana New", 8))
                {
                    float yPosition = topMargin;

                    // Center the title based on the new margins
                    string title = "รายงานข้อมูลการรับผ้า";
                    float titleX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(title, titleFont).Width / 2);
                    e.Graphics.DrawString(title, titleFont, Brushes.Black, titleX, yPosition);
                    yPosition += titleFont.GetHeight();

                    // Draw line across the full width with the new margins
                    e.Graphics.DrawLine(new Pen(Color.Black, 1.5f), leftMargin, yPosition, leftMargin + availableWidth, yPosition);
                    yPosition += 10;

                    // Left-aligned date/time info using the new left margin
                    DateTime today = DateTime.Now;
                    string dateTimeInfo = $"พิมพ์เมื่อ: {today.Day}/{today.Month}/{today.Year} {today.ToString("HH:mm:ss")}";
                    string filterInfo = dtpCreateDate.Checked ? $"วันที่: {dtpCreateDate.Value.ToString("dd/MM/yyyy")}" : "ทุกวันที่";
                    filterInfo += chkPending.Checked ? ", สถานะ: ยังไม่มารับ" :
                                  chkCompleted.Checked ? ", สถานะ: มารับแล้ว" : ", ทุกสถานะ";

                    e.Graphics.DrawString(dateTimeInfo, normalFont, Brushes.Black, leftMargin, yPosition);

                    // Right-aligned filter info based on the new right margin
                    string filterText = $"กรอง: {filterInfo}";
                    float filterX = rightMargin - e.Graphics.MeasureString(filterText, normalFont).Width;
                    e.Graphics.DrawString(filterText, normalFont, Brushes.Black, filterX, yPosition);

                    yPosition += normalFont.GetHeight() * 1.5f;

                    // Adjust row height and spacing for portrait mode
                    float rowHeight = normalFont.GetHeight() * 1.2f;
                    float headerSpace = titleFont.GetHeight() + 10 + normalFont.GetHeight() * 1.5f;
                    float tableHeaderHeight = headerFont.GetHeight() * 2.5f;

                    // Space needed for the title, filter info and table header
                    float fixedHeaderSpace = headerSpace + tableHeaderHeight;

                    // Calculate how many rows can fit on each page
                    // Include space for total row on the last page
                    float totalRowHeight = rowHeight * 1.3f;
                    int rowsPerFirstPage = (int)((availableHeight - fixedHeaderSpace - totalRowHeight) / rowHeight);
                    int rowsPerSubsequentPage = (int)((availableHeight - fixedHeaderSpace - totalRowHeight) / rowHeight);

                    // Ensure at least one row per page (minimum)
                    rowsPerFirstPage = Math.Max(1, rowsPerFirstPage);
                    rowsPerSubsequentPage = Math.Max(1, rowsPerSubsequentPage);

                    // Calculate total pages based on row count
                    int totalPages = 1;
                    if (rowsPerFirstPage < totalValidRows)
                    {
                        totalPages = 1 + (int)Math.Ceiling((double)(totalValidRows - rowsPerFirstPage) / rowsPerSubsequentPage);
                    }

                    // Show continuation marker for pages after the first
                    if (_currentPage > 0)
                    {
                        string continuationText = $"(ต่อ) - หน้า {_currentPage + 1} จาก {totalPages}";
                        float textWidth = e.Graphics.MeasureString(continuationText, headerFont).Width;
                        e.Graphics.DrawString(continuationText, headerFont, Brushes.Black,
                            rightMargin - textWidth, yPosition - normalFont.GetHeight());
                    }

                    // Define column names for the table header - shorter text for portrait mode
                    string[] columnNames = new string[] {
                "ลำดับ",
                "เลขใบรับผ้า",
                "เลขใบเสร็จ",
                "ชื่อลูกค้า",
                "เบอร์โทร",
                "ใบรับผ้า",
                "ราคาก่อนลด",
                "ส่วนลด",
                "ราคาสุทธิ",
                "วันออกใบรับผ้า",
                "วันครบกำหนด",
                "สถานะ",
                "วันที่มารับ"
            };

                    // Adjust column width percentages for better fit including row numbers
                    float[] columnWidthPercentages = new float[] {
                0.04f, // ลำดับ (new column)
                0.08f, // หมายเลขใบรับผ้า
                0.08f, // หมายเลขใบเสร็จ
                0.12f, // ชื่อลูกค้า
                0.07f, // เบอร์โทรศัพท์
                0.07f, // ราคารวมใบรับผ้า
                0.07f, // ราคารวมใบเสร็จ
                0.05f, // ส่วนลด
                0.08f, // ราคารวมหลังหักส่วนลด
                0.08f, // วันที่ออกใบรับผ้า
                0.08f, // วันที่ครบกำหนด
                0.08f, // สถานะ
                0.10f  // วันที่ลูกค้ามารับ
            };

                    // Calculate column widths
                    float[] columnWidths = new float[columnWidthPercentages.Length];
                    for (int i = 0; i < columnWidthPercentages.Length; i++)
                    {
                        columnWidths[i] = availableWidth * columnWidthPercentages[i];
                    }

                    // Draw table header
                    float headerHeight = headerFont.GetHeight() * 2.7f;
                    float headerX = leftMargin;

                    // Draw header cells
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        RectangleF headerRect = new RectangleF(headerX, yPosition, columnWidths[i], headerHeight);

                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            sf.FormatFlags = StringFormatFlags.LineLimit | StringFormatFlags.NoClip;
                            e.Graphics.FillRectangle(Brushes.LightGray, headerRect);
                            e.Graphics.DrawRectangle(Pens.Black, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height);
                            e.Graphics.DrawString(columnNames[i], headerFont, Brushes.Black, headerRect, sf);
                        }

                        headerX += columnWidths[i];
                    }

                    yPosition += headerHeight;

                    // Calculate start and end row for current page
                    int startRow;
                    int endRow;
                    bool isLastPage = false;

                    if (_currentPage == 0)
                    {
                        startRow = 0;
                        endRow = Math.Min(rowsPerFirstPage, dgvOrders.Rows.Count);
                    }
                    else
                    {
                        startRow = rowsPerFirstPage + (_currentPage - 1) * rowsPerSubsequentPage;
                        endRow = Math.Min(startRow + rowsPerSubsequentPage, dgvOrders.Rows.Count);
                    }

                    // Track valid rows for row numbering
                    int validRowCount = 0;

                    // Get the starting row number for this page
                    int rowNumberStart = 0;
                    if (_currentPage > 0)
                    {
                        // Count valid rows on previous pages
                        for (int i = 0; i < startRow; i++)
                        {
                            if (i < dgvOrders.Rows.Count)
                            {
                                DataGridViewRow row = dgvOrders.Rows[i];
                                if (row.Cells["หมายเลขใบรับผ้า"].Value != null || row.Cells["ชื่อลูกค้า"].Value != null)
                                {
                                    rowNumberStart++;
                                }
                            }
                        }
                    }

                    // Draw data rows for this page
                    int validRowsPrinted = 0;
                    for (int i = startRow; i < endRow; i++)
                    {
                        // Skip if we've reached the end of rows
                        if (i >= dgvOrders.Rows.Count)
                            break;

                        DataGridViewRow row = dgvOrders.Rows[i];

                        // Skip rows with no valid data
                        if (row.Cells["หมายเลขใบรับผ้า"].Value == null &&
                            row.Cells["ชื่อลูกค้า"].Value == null)
                        {
                            continue;
                        }

                        // Increment valid row counter (for row numbers)
                        validRowCount++;

                        // Position for this row
                        float rowX = leftMargin;

                        // Draw row number column first
                        RectangleF rowNumberRect = new RectangleF(rowX, yPosition, columnWidths[0], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawRectangle(Pens.Black, rowNumberRect.X, rowNumberRect.Y, rowNumberRect.Width, rowNumberRect.Height);
                            e.Graphics.DrawString((rowNumberStart + validRowCount).ToString(), normalFont, Brushes.Black, rowNumberRect, sf);
                        }
                        rowX += columnWidths[0];

                        // Print each cell in the row
                        for (int j = 1; j < columnNames.Length; j++) // Start from 1 to skip row number column
                        {
                            string cellValue = "";
                            try
                            {
                                // Map column names to data property names
                                string dataProperty = "";
                                switch (j)
                                {
                                    case 1: dataProperty = "หมายเลขใบรับผ้า"; break;
                                    case 2: dataProperty = "หมายเลขใบเสร็จ"; break;
                                    case 3: dataProperty = "ชื่อลูกค้า"; break;
                                    case 4: dataProperty = "เบอร์โทรศัพท์"; break;
                                    case 5: dataProperty = "ราคารวมใบรับผ้า"; break;
                                    case 6: dataProperty = "ราคารวมใบเสร็จ"; break;
                                    case 7: dataProperty = "ส่วนลด"; break;
                                    case 8: dataProperty = "ราคารวมหลังหักส่วนลด"; break;
                                    case 9: dataProperty = "วันที่ออกใบรับผ้า"; break;
                                    case 10: dataProperty = "วันที่ครบกำหนด"; break;
                                    case 11: dataProperty = "สถานะ"; break;
                                    case 12: dataProperty = "วันที่ลูกค้ามารับ"; break;
                                }

                                if (row.Cells[dataProperty].Value != null &&
                                    row.Cells[dataProperty].Value != DBNull.Value)
                                {
                                    // Format values based on column type
                                    if (dataProperty == "ราคารวมใบรับผ้า" ||
                                        dataProperty == "ราคารวมใบเสร็จ" ||
                                        dataProperty == "ส่วนลด" ||
                                        dataProperty == "ราคารวมหลังหักส่วนลด")
                                    {
                                        decimal amount = Convert.ToDecimal(row.Cells[dataProperty].Value);
                                        cellValue = amount.ToString("N2"); // Shorter format
                                    }
                                    else if (dataProperty == "วันที่ออกใบรับผ้า" ||
                                             dataProperty == "วันที่ครบกำหนด" ||
                                             dataProperty == "วันที่ลูกค้ามารับ")
                                    {
                                        if (row.Cells[dataProperty].Value != DBNull.Value)
                                        {
                                            DateTime date = Convert.ToDateTime(row.Cells[dataProperty].Value);
                                            cellValue = date.ToString("dd/MM/yy"); // Shorter date format
                                        }
                                    }
                                    else
                                    {
                                        cellValue = row.Cells[dataProperty].Value.ToString();
                                    }
                                }
                            }
                            catch (InvalidCastException)
                            {
                                cellValue = "-";
                                _printErrorMessage = "มีข้อมูลบางรายการที่ไม่สามารถแสดงได้";
                            }
                            catch (Exception)
                            {
                                cellValue = "?";
                            }

                            // Draw the cell
                            RectangleF cellRect = new RectangleF(rowX, yPosition, columnWidths[j], rowHeight);

                            using (StringFormat sf = new StringFormat())
                            {
                                sf.Alignment = StringAlignment.Center;
                                sf.LineAlignment = StringAlignment.Center;
                                sf.Trimming = StringTrimming.EllipsisCharacter;

                                // Apply special formatting for status and discount
                                if (j == 11 && !string.IsNullOrEmpty(cellValue)) // Status column
                                {
                                    using (SolidBrush statusBrush = new SolidBrush(cellValue == "มารับแล้ว" ? Color.Green : Color.Blue))
                                    {
                                        e.Graphics.DrawString(cellValue, normalFont, statusBrush, cellRect, sf);
                                    }
                                }
                                else if (j == 7 && !string.IsNullOrEmpty(cellValue) && // Discount column
                                         cellValue != "-" && cellValue != "?" &&
                                         row.Cells["ส่วนลด"].Value != null &&
                                         Convert.ToDecimal(row.Cells["ส่วนลด"].Value) > 0)
                                {
                                    using (SolidBrush discountBrush = new SolidBrush(Color.Red))
                                    {
                                        e.Graphics.DrawString(cellValue, normalFont, discountBrush, cellRect, sf);
                                    }
                                }
                                else
                                {
                                    e.Graphics.DrawString(cellValue, normalFont, Brushes.Black, cellRect, sf);
                                }

                                e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            }

                            rowX += columnWidths[j];
                        }

                        yPosition += rowHeight;
                        validRowsPrinted++;
                    }

                    // Determine if this is the last page
                    isLastPage = endRow >= dgvOrders.Rows.Count || startRow + validRowsPrinted >= totalValidRows;

                    // Draw the totals row right after the last data row on the last page
                    if (isLastPage)
                    {
                        // Draw the total summary row immediately after the last data row
                        float summaryRowY = yPosition; // No extra gap - put it right after the last row

                        // Draw a total row with a highlighted background
                        using (SolidBrush totalRowBrush = new SolidBrush(Color.FromArgb(245, 245, 220))) // Beige color
                        {
                            // Draw a full-width rectangle for the total row
                            RectangleF totalRowRect = new RectangleF(leftMargin, summaryRowY, availableWidth, totalRowHeight);
                            e.Graphics.FillRectangle(totalRowBrush, totalRowRect);

                            // Draw a border around the total row with thicker line
                            using (Pen totalRowPen = new Pen(Color.Black, 1.5f))
                            {
                                e.Graphics.DrawRectangle(totalRowPen, totalRowRect.X, totalRowRect.Y, totalRowRect.Width, totalRowRect.Height);
                            }
                        }

                        // Now draw each cell of the total row
                        float totalX = leftMargin;

                        // Draw "รวม" label in first column
                        RectangleF totalLabelRect = new RectangleF(totalX, summaryRowY, columnWidths[0], totalRowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString("รวม", totalFont, Brushes.Black, totalLabelRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, totalLabelRect.X, totalLabelRect.Y, totalLabelRect.Width, totalLabelRect.Height);
                        }
                        totalX += columnWidths[0];

                        // Skip columns 1-4 (keep them empty)
                        for (int j = 1; j <= 4; j++)
                        {
                            RectangleF emptyRect = new RectangleF(totalX, summaryRowY, columnWidths[j], totalRowHeight);
                            e.Graphics.DrawRectangle(Pens.Black, emptyRect.X, emptyRect.Y, emptyRect.Width, emptyRect.Height);
                            totalX += columnWidths[j];
                        }

                        // Draw total order amount in column 5
                        RectangleF totalOrderRect = new RectangleF(totalX, summaryRowY, columnWidths[5], totalRowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString(totalOrderAmount.ToString("N2"), totalFont, Brushes.Black, totalOrderRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, totalOrderRect.X, totalOrderRect.Y, totalOrderRect.Width, totalOrderRect.Height);
                        }
                        totalX += columnWidths[5];

                        // Draw total receipt amount in column 6
                        RectangleF totalReceiptRect = new RectangleF(totalX, summaryRowY, columnWidths[6], totalRowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString(totalReceiptAmount.ToString("N2"), totalFont, Brushes.Black, totalReceiptRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, totalReceiptRect.X, totalReceiptRect.Y, totalReceiptRect.Width, totalReceiptRect.Height);
                        }
                        totalX += columnWidths[6];

                        // Draw total discount in column 7
                        RectangleF totalDiscountRect = new RectangleF(totalX, summaryRowY, columnWidths[7], totalRowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString(totalDiscount.ToString("N2"), totalFont, Brushes.Red, totalDiscountRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, totalDiscountRect.X, totalDiscountRect.Y, totalDiscountRect.Width, totalDiscountRect.Height);
                        }
                        totalX += columnWidths[7];

                        // Draw total after discount in column 8
                        RectangleF totalAfterDiscountRect = new RectangleF(totalX, summaryRowY, columnWidths[8], totalRowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString(totalAfterDiscount.ToString("N2"), totalFont, Brushes.Black, totalAfterDiscountRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, totalAfterDiscountRect.X, totalAfterDiscountRect.Y, totalAfterDiscountRect.Width, totalAfterDiscountRect.Height);
                        }
                        totalX += columnWidths[8];

                        // Draw remaining empty cells
                        for (int j = 9; j < columnNames.Length; j++)
                        {
                            RectangleF emptyRect = new RectangleF(totalX, summaryRowY, columnWidths[j], totalRowHeight);
                            e.Graphics.DrawRectangle(Pens.Black, emptyRect.X, emptyRect.Y, emptyRect.Width, emptyRect.Height);
                            totalX += columnWidths[j];
                        }

                        // Move position past the summary row
                        yPosition = summaryRowY + totalRowHeight + 15;

                        // Add summary text below the total row
                        string summaryText = $"จำนวนรายการทั้งหมด {totalValidRows} รายการ";
                        e.Graphics.DrawString(summaryText, normalFont, Brushes.Black, leftMargin, yPosition);
                        yPosition += normalFont.GetHeight() * 1.5f;
                    }

                    // Add page number at the bottom
                    string pageText = $"หน้า {_currentPage + 1} จาก {totalPages}";

                    e.Graphics.DrawString(pageText, smallFont, Brushes.Black,
                        rightMargin - e.Graphics.MeasureString(pageText, smallFont).Width,
                        e.MarginBounds.Bottom + 10);

                    // Determine if we need to print more pages
                    if (!isLastPage)
                    {
                        _currentPage++;
                        e.HasMorePages = true;
                    }
                    else
                    {
                        // Check if we should print canceled orders after the normal report
                        if (!_canceledOrdersPrinted && dtpCreateDate.Checked &&
                            canceledOrders != null && canceledOrders.Count > 0)
                        {
                            _currentPage = 0;
                            _printCanceledOrdersMode = true;
                            e.HasMorePages = true;
                        }
                        else
                        {
                            _currentPage = 0;
                            _printCanceledOrdersMode = false;
                            e.HasMorePages = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Mark print as failed and store error message
                _isPrintSuccessful = false;
                _printErrorMessage = ex.Message;

                using (Font errorFont = new Font("Angsana New", 14, FontStyle.Bold))
                {
                    string errorMsg = $"เกิดข้อผิดพลาดในการพิมพ์: {ex.Message}";
                    e.Graphics.DrawString(errorMsg, errorFont, Brushes.Red, e.MarginBounds.Left, e.MarginBounds.Top + 100);
                }

                e.HasMorePages = false;
            }
        }
        private void btnExcel_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvOrders.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะส่งออก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check for valid data before exporting
                bool hasValidRows = false;
                foreach (DataGridViewRow row in dgvOrders.Rows)
                {
                    if (row.Cells["หมายเลขใบรับผ้า"].Value != null || row.Cells["ชื่อลูกค้า"].Value != null)
                    {
                        hasValidRows = true;
                        break;
                    }
                }

                if (!hasValidRows)
                {
                    MessageBox.Show("ไม่พบข้อมูลที่พร้อมส่งออก กรุณาตรวจสอบข้อมูล", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create SaveFileDialog
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Files|*.csv|Excel Files|*.xls";
                    sfd.Title = "บันทึกไฟล์ข้อมูล";

                    // Get date string for default filename
                    string dateStr = dtpCreateDate.Checked
                        ? dtpCreateDate.Value.ToString("yyyy-MM-dd")
                        : DateTime.Today.ToString("yyyy-MM-dd");

                    sfd.FileName = $"รายงานข้อมูลการรับผ้า_{dateStr}";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // Show waiting cursor
                        Cursor = Cursors.WaitCursor;

                        // Define columns to export
                        var columnNames = new List<string> {
                            "หมายเลขใบรับผ้า", "หมายเลขใบเสร็จ", "ชื่อลูกค้า", "เบอร์โทรศัพท์",
                            "ราคารวมใบรับผ้า", "ราคารวมใบเสร็จ", "ส่วนลด", "ราคารวมหลังหักส่วนลด",
                            "วันที่ออกใบรับผ้า", "วันที่ครบกำหนด", "สถานะ", "วันที่ลูกค้ามารับ"
                        };

                        // Filter to include only columns that exist in the DataGridView
                        var validColumns = columnNames.Where(col => dgvOrders.Columns[col] != null).ToList();

                        // Create a StringBuilder to build CSV content
                        StringBuilder csv = new StringBuilder();

                        // Add UTF-8 BOM for Thai character support
                        csv.Append("\uFEFF");

                        // Add CSV header row with column names
                        csv.AppendLine(string.Join(",", validColumns.Select(name => $"\"{name}\"")));

                        // Add data rows
                        foreach (DataGridViewRow row in dgvOrders.Rows)
                        {
                            // Skip rows with no data
                            if (row.Cells["หมายเลขใบรับผ้า"].Value == null &&
                                row.Cells["ชื่อลูกค้า"].Value == null)
                            {
                                continue;
                            }

                            var rowValues = new List<string>();

                            foreach (string col in validColumns)
                            {
                                string value = "";

                                if (row.Cells[col].Value != null && row.Cells[col].Value != DBNull.Value)
                                {
                                    // Format based on column type
                                    if (col.Contains("วันที่") && row.Cells[col].Value is DateTime date)
                                    {
                                        value = date.ToString("dd/MM/yyyy");
                                    }
                                    else if ((col == "ราคารวมใบรับผ้า" || col == "ราคารวมใบเสร็จ" || col == "ส่วนลด" || col == "ราคารวมหลังหักส่วนลด") &&
                                           decimal.TryParse(row.Cells[col].Value.ToString(), out decimal num))
                                    {
                                        value = num.ToString("0.00");
                                    }
                                    else
                                    {
                                        value = row.Cells[col].Value.ToString();
                                    }

                                    // เพิ่มโค้ดตรงนี้ - สำหรับคอลัมน์ที่อาจเป็นตัวเลขแต่ต้องการเก็บ 0 นำหน้า
                                    if (col == "เบอร์โทรศัพท์" || col == "หมายเลขใบรับผ้า" || col == "หมายเลขใบเสร็จ")
                                    {
                                        // บังคับให้ Excel ตีความเป็นข้อความ
                                        value = $"=\"{value}\"";
                                    }

                                    // Escape quotes for CSV
                                    value = value.Replace("\"", "\"\"");
                                }

                                // Add quoted value
                                rowValues.Add($"\"{value}\"");
                            }

                            // แก้ไขตรงนี้: เปลี่ยน comma เป็น semicolon
                            csv.AppendLine(string.Join(",", rowValues));
                        }

                        // Create summary information
                        int validRowCount = dgvOrders.Rows.Cast<DataGridViewRow>().Count(r =>
                            r.Cells["หมายเลขใบรับผ้า"].Value != null || r.Cells["ชื่อลูกค้า"].Value != null);

                        csv.AppendLine();
                        csv.AppendLine($"\"จำนวนรายการทั้งหมด {validRowCount} รายการ\"");
                        csv.AppendLine();
                        csv.AppendLine("\"รายงานข้อมูลการรับผ้า\"");
                        csv.AppendLine($"\"พิมพ์เมื่อ: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}\"");

                        // Add filter information
                        string filterInfo = dtpCreateDate.Checked ?
                            $"วันที่: {dtpCreateDate.Value.ToString("dd/MM/yyyy")}" : "ทุกวันที่";
                        filterInfo += chkPending.Checked ? ", สถานะ: ยังไม่มารับ" :
                                     chkCompleted.Checked ? ", สถานะ: มารับแล้ว" : ", ทุกสถานะ";
                        csv.AppendLine($"\"กรอง: {filterInfo}\"");

                        // Save to file
                        File.WriteAllText(sfd.FileName, csv.ToString());

                        Cursor = Cursors.Default;
                        MessageBox.Show("ส่งออกข้อมูลเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show($"เกิดข้อผิดพลาดในการส่งออกข้อมูล: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            // Check if a row is selected
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการยกเลิก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Get OrderID and check if it has a receipt
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["OrderID"].Value);
                var receiptIdObj = dgvOrders.CurrentRow.Cells["ReceiptID"].Value;
                string customOrderId = dgvOrders.CurrentRow.Cells["หมายเลขใบรับผ้า"].Value?.ToString();
                string customerName = dgvOrders.CurrentRow.Cells["ชื่อลูกค้า"].Value?.ToString();

                // Check if order has a receipt (cannot cancel if it has one)
                if (receiptIdObj != null && receiptIdObj != DBNull.Value)
                {
                    MessageBox.Show("ไม่สามารถยกเลิกรายการนี้ได้เนื่องจากมีการออกใบเสร็จแล้ว",
                        "ไม่อนุญาตให้ยกเลิก", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create cancellation reason dialog
                using (Form reasonDialog = new Form())
                {
                    reasonDialog.Text = "ระบุเหตุผลการยกเลิก";
                    reasonDialog.Size = new Size(550, 350);
                    reasonDialog.StartPosition = FormStartPosition.CenterParent;
                    reasonDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    reasonDialog.MaximizeBox = false;
                    reasonDialog.MinimizeBox = false;

                    // Header label
                    Label lblHeader = new Label();
                    lblHeader.Text = $"กรุณาระบุเหตุผลในการยกเลิกรายการ\nลูกค้า: {customerName}\nหมายเลขใบรับผ้า: {customOrderId}";
                    lblHeader.Font = new Font("Angsana New", 22, FontStyle.Bold);
                    lblHeader.TextAlign = ContentAlignment.MiddleCenter;
                    lblHeader.Dock = DockStyle.Top;
                    lblHeader.Height = 120;

                    // Reason label
                    Label lblReason = new Label();
                    lblReason.Text = "เหตุผลการยกเลิก:";
                    lblReason.Font = new Font("Angsana New", 20, FontStyle.Regular);
                    lblReason.Location = new Point(20, 130);
                    lblReason.Size = new Size(150, 40);

                    // Reason textbox
                    TextBox txtReason = new TextBox();
                    txtReason.Font = new Font("Angsana New", 18, FontStyle.Regular);
                    txtReason.Location = new Point(170, 130);
                    txtReason.Size = new Size(350, 40);
                    txtReason.MaxLength = 250; // Limit text length

                    // Error message label
                    Label lblError = new Label();
                    lblError.Text = "";
                    lblError.ForeColor = Color.Red;
                    lblError.Font = new Font("Angsana New", 16, FontStyle.Regular);
                    lblError.Location = new Point(170, 170);
                    lblError.Size = new Size(350, 30);
                    lblError.Visible = false;

                    // Confirm button
                    Button btnConfirm = new Button();
                    btnConfirm.Text = "ยืนยัน";
                    btnConfirm.Font = new Font("Angsana New", 20, FontStyle.Regular);
                    btnConfirm.Size = new Size(150, 60);
                    btnConfirm.Location = new Point(120, 220);
                    btnConfirm.DialogResult = DialogResult.None; // We'll set this manually after validation

                    // Cancel button
                    Button btnCancel = new Button();
                    btnCancel.Text = "ยกเลิก";
                    btnCancel.Font = new Font("Angsana New", 20, FontStyle.Regular);
                    btnCancel.Size = new Size(150, 60);
                    btnCancel.Location = new Point(280, 220);
                    btnCancel.DialogResult = DialogResult.Cancel;

                    // Add controls to form
                    reasonDialog.Controls.Add(lblHeader);
                    reasonDialog.Controls.Add(lblReason);
                    reasonDialog.Controls.Add(txtReason);
                    reasonDialog.Controls.Add(lblError);
                    reasonDialog.Controls.Add(btnConfirm);
                    reasonDialog.Controls.Add(btnCancel);

                    // Define special characters we want to ask about
                    char[] specialChars = { '-', '+', ',' };

                    // KeyPress event handler
                    txtReason.KeyPress += (s, args) =>
                    {
                        // If it's one of the special characters
                        if (specialChars.Contains(args.KeyChar))
                        {
                            // Prevent the default behavior (don't add the character yet)
                            args.Handled = true;

                            // Show confirmation dialog
                            DialogResult result = MessageBox.Show(
                                $"คุณต้องการใช้อักขระพิเศษ '{args.KeyChar}' ในเหตุผลการยกเลิกใช่หรือไม่?",
                                "ยืนยันการใช้อักขระพิเศษ",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                            );

                            // If user confirms, add the character programmatically
                            if (result == DialogResult.Yes)
                            {
                                txtReason.Text = txtReason.Text.Insert(txtReason.SelectionStart, args.KeyChar.ToString());
                                txtReason.SelectionStart = txtReason.SelectionStart + 1;
                            }
                        }
                    };

                    // Validation for empty input and special character confirmation
                    btnConfirm.Click += (s, args) =>
                    {
                        string reason = txtReason.Text.Trim();

                        // Check if reason is empty
                        if (string.IsNullOrEmpty(reason))
                        {
                            lblError.Text = "กรุณาระบุเหตุผลการยกเลิก";
                            lblError.Visible = true;
                            txtReason.Focus();
                            return;
                        }

                        // Check for special characters
                        bool hasSpecialChars = reason.IndexOfAny(specialChars) >= 0;

                        // If special characters exist, confirm once more
                        if (hasSpecialChars)
                        {
                            string specialCharsFound = string.Join(" ", reason.Where(c => specialChars.Contains(c)).Distinct());
                            DialogResult confirmResult = MessageBox.Show(
                                $"เหตุผลของคุณมีอักขระพิเศษ: {specialCharsFound}\nยืนยันที่จะใช้เหตุผลนี้หรือไม่?",
                                "ยืนยันการใช้อักขระพิเศษ",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                            );

                            if (confirmResult != DialogResult.Yes)
                            {
                                return; // User chose not to use special characters
                            }
                        }

                        // Validation passed
                        lblError.Visible = false;
                        reasonDialog.DialogResult = DialogResult.OK;
                        reasonDialog.Close();
                    };

                    // Show dialog and wait for response
                    if (reasonDialog.ShowDialog() != DialogResult.OK)
                    {
                        return; // User canceled
                    }

                    // Get the reason text
                    string cancelReason = txtReason.Text.Trim();

                    // Show final confirmation dialog
                    using (Form confirmDialog = new Form())
                    {
                        confirmDialog.Text = "ยืนยันการยกเลิกรายการ";
                        confirmDialog.Size = new Size(500, 300);
                        confirmDialog.StartPosition = FormStartPosition.CenterParent;
                        confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                        confirmDialog.MaximizeBox = false;
                        confirmDialog.MinimizeBox = false;

                        Label lblMessage = new Label();
                        lblMessage.Text = $"ยืนยันการยกเลิกรายการนี้?\nลูกค้า: {customerName}\nหมายเลขใบรับผ้า: {customOrderId}\nเหตุผล: {cancelReason}";
                        lblMessage.Font = new Font("Angsana New", 22, FontStyle.Bold);
                        lblMessage.TextAlign = ContentAlignment.MiddleCenter;
                        lblMessage.Dock = DockStyle.Top;
                        lblMessage.Height = 180;

                        Button btnConfirmFinal = new Button();
                        btnConfirmFinal.Text = "ยืนยัน";
                        btnConfirmFinal.Font = new Font("Angsana New", 20, FontStyle.Regular);
                        btnConfirmFinal.Size = new Size(150, 60);
                        btnConfirmFinal.Location = new Point(80, 200);
                        btnConfirmFinal.DialogResult = DialogResult.Yes;

                        Button btnCancelFinal = new Button();
                        btnCancelFinal.Text = "ยกเลิก";
                        btnCancelFinal.Font = new Font("Angsana New", 20, FontStyle.Regular);
                        btnCancelFinal.Size = new Size(150, 60);
                        btnCancelFinal.Location = new Point(260, 200);
                        btnCancelFinal.DialogResult = DialogResult.Cancel;

                        confirmDialog.Controls.Add(lblMessage);
                        confirmDialog.Controls.Add(btnConfirmFinal);
                        confirmDialog.Controls.Add(btnCancelFinal);
                        confirmDialog.AcceptButton = btnConfirmFinal;
                        confirmDialog.CancelButton = btnCancelFinal;

                        // Show dialog and wait for response
                        if (confirmDialog.ShowDialog() != DialogResult.Yes)
                        {
                            return; // User canceled
                        }
                    }

                    // Update order status and reason in database
                    using (SqlConnection conn = DBconfig.GetConnection())
                    {
                        conn.Open();
                        using (SqlTransaction transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                // Update OrderHeader status and add cancellation reason
                                string updateOrderQuery = @"UPDATE OrderHeader 
                                                          SET OrderStatus = @Status, 
                                                              CancelReason = @CancelReason,
                                                              CancelledDate = @CancelledDate
                                                          WHERE OrderID = @OrderID";

                                using (SqlCommand cmd = new SqlCommand(updateOrderQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Status", "รายการถูกยกเลิก");
                                    cmd.Parameters.AddWithValue("@CancelReason", cancelReason);
                                    cmd.Parameters.AddWithValue("@CancelledDate", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@OrderID", orderId);
                                    int rowsAffected = cmd.ExecuteNonQuery();

                                    if (rowsAffected <= 0)
                                    {
                                        transaction.Rollback();
                                        MessageBox.Show("ไม่สามารถยกเลิกรายการได้ กรุณาลองใหม่อีกครั้ง", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        return;
                                    }
                                }

                                // Update all related OrderItem records to be canceled
                                string updateItemsQuery = @"
                                    UPDATE OrderItem 
                                    SET IsCanceled = 1, 
                                        CancelReason = @CancelReason 
                                    WHERE OrderID = @OrderID 
                                    AND (IsCanceled = 0 OR IsCanceled IS NULL)";

                                using (SqlCommand cmd = new SqlCommand(updateItemsQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@CancelReason", cancelReason);
                                    cmd.Parameters.AddWithValue("@OrderID", orderId);
                                    cmd.ExecuteNonQuery();
                                }

                                // Commit the transaction
                                transaction.Commit();

                                MessageBox.Show("ยกเลิกรายการเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // Refresh the order list to reflect changes
                                string searchId = txtSearchId.Text.Trim();
                                string customerNameFilter = txtCustomerFilter.Text.Trim();
                                DateTime? createDate = dtpCreateDate.Checked ? (DateTime?)dtpCreateDate.Value.Date : null;
                                LoadOrders(searchId, customerNameFilter, createDate);
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception("เกิดข้อผิดพลาดในการยกเลิกรายการ: " + ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReprintReceiptWithCopy_Click(object sender, EventArgs e)
        {
            // Check if a row is selected
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการพิมพ์สำเนาใบเสร็จ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Check if the selected row has a receipt
                var receiptIdObj = dgvOrders.CurrentRow.Cells["ReceiptID"].Value;
                if (receiptIdObj == null || receiptIdObj == DBNull.Value)
                {
                    MessageBox.Show("รายการที่เลือกยังไม่มีใบเสร็จ กรุณาออกใบเสร็จก่อน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int receiptId = Convert.ToInt32(receiptIdObj);
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["OrderID"].Value);

                // ตรวจสอบสถานะใบเสร็จว่าเป็น "ยกเลิกการพิมพ์" หรือไม่
                var receiptStatusObj = dgvOrders.CurrentRow.Cells["สถานะใบเสร็จ"].Value;
                if (receiptStatusObj != null && receiptStatusObj != DBNull.Value &&
                    receiptStatusObj.ToString() == "ยกเลิกการพิมพ์")
                {
                    MessageBox.Show("ไม่สามารถพิมพ์ใบเสร็จที่ถูกยกเลิกการพิมพ์ได้", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create OrderHeaderDto object with required data
                var header = new Find_Service.OrderHeaderDto
                {
                    OrderID = orderId,
                    CustomOrderId = dgvOrders.CurrentRow.Cells["หมายเลขใบรับผ้า"].Value?.ToString(),
                    CustomerName = dgvOrders.CurrentRow.Cells["ชื่อลูกค้า"].Value?.ToString(),
                    Phone = dgvOrders.CurrentRow.Cells["เบอร์โทรศัพท์"].Value?.ToString(),
                    CustomReceiptId = dgvOrders.CurrentRow.Cells["หมายเลขใบเสร็จ"].Value?.ToString()
                };

                // Get additional information from the database
                using (SqlConnection conn = DBconfig.GetConnection())
                {
                    conn.Open();

                    // Get receipt details including discount information and receipt date
                    string receiptQuery = @"
                SELECT 
                    TotalBeforeDiscount, 
                    TotalAfterDiscount, 
                    VAT, 
                    Discount, 
                    PaymentMethod, 
                    ReceiptDate
                FROM Receipt 
                WHERE ReceiptID = @ReceiptID";

                    using (SqlCommand cmd = new SqlCommand(receiptQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ReceiptID", receiptId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                header.GrandTotalPrice = reader["TotalBeforeDiscount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["TotalBeforeDiscount"]) : 0;
                                header.DiscountedTotal = reader["TotalAfterDiscount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["TotalAfterDiscount"]) : 0;
                                header.VatAmount = reader["VAT"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["VAT"]) : 0;

                                // Important: Get the discount value
                                decimal discountAmount = reader["Discount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["Discount"]) : 0;

                                // Calculate discount percentage if needed
                                if (header.GrandTotalPrice > 0)
                                {
                                    header.Discount = Math.Round((discountAmount / header.GrandTotalPrice) * 100, 2);
                                    header.TodayDiscount = discountAmount;
                                    // Setting this to false ensures the discount is treated as an amount rather than percentage
                                    header.IsTodayDiscountPercent = false;
                                }

                                // Set SubTotal
                                header.SubTotal = header.GrandTotalPrice;

                                // Set PaymentMethod
                                if (reader["PaymentMethod"] != DBNull.Value)
                                {
                                    header.PaymentMethod = reader["PaymentMethod"].ToString();
                                }

                                // IMPORTANT: Set the OrderDate to ReceiptDate for printing
                                // This fixes the issue with the receipt date display
                                if (reader["ReceiptDate"] != DBNull.Value)
                                {
                                    header.OrderDate = Convert.ToDateTime(reader["ReceiptDate"]);
                                }
                            }
                            else
                            {
                                MessageBox.Show("ไม่พบข้อมูลใบเสร็จในระบบ", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                    }

                    // Get order discount from OrderHeader as backup if needed
                    string orderQuery = "SELECT Discount FROM OrderHeader WHERE OrderID = @OrderID";
                    using (SqlCommand cmd = new SqlCommand(orderQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderID", orderId);
                        var discountObj = cmd.ExecuteScalar();
                        if (discountObj != null && discountObj != DBNull.Value && header.Discount == 0)
                        {
                            // Only use this as a fallback
                            header.Discount = Convert.ToDecimal(discountObj);
                            header.IsTodayDiscountPercent = true;
                        }
                    }

                    // Get receipt items
                    List<Find_Service.OrderItemDto> items = new List<Find_Service.OrderItemDto>();
                    string itemsQuery = @"
                SELECT 
                    ri.ReceiptItemID, 
                    oi.ItemNumber, 
                    oi.ItemName, 
                    ri.Quantity, 
                    ri.Amount, 
                    ri.IsCanceled
                FROM ReceiptItem ri
                INNER JOIN OrderItem oi ON ri.OrderItemID = oi.OrderItemID
                WHERE ri.ReceiptID = @ReceiptID";

                    using (SqlCommand cmd = new SqlCommand(itemsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ReceiptID", receiptId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new Find_Service.OrderItemDto
                                {
                                    OrderItemID = Convert.ToInt32(reader["ReceiptItemID"]),
                                    ItemNumber = reader["ItemNumber"].ToString(),
                                    ItemName = reader["ItemName"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    TotalAmount = Convert.ToDecimal(reader["Amount"]),
                                    IsCanceled = reader["IsCanceled"] != DBNull.Value && (bool)reader["IsCanceled"]
                                });
                            }
                        }
                    }

                    if (items.Count == 0)
                    {
                        MessageBox.Show("ไม่พบรายการสินค้าในใบเสร็จ", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Create confirmation dialog
                    using (Form confirmDialog = new Form())
                    {
                        confirmDialog.Text = "ยืนยันการพิมพ์สำเนาใบเสร็จ";
                        confirmDialog.Size = new Size(500, 300);
                        confirmDialog.StartPosition = FormStartPosition.CenterParent;
                        confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                        confirmDialog.MaximizeBox = false;
                        confirmDialog.MinimizeBox = false;

                        Label lblMessage = new Label();
                        lblMessage.Text = $"ยืนยันการพิมพ์สำเนาใบเสร็จ\nลูกค้า: {header.CustomerName}\nเลขที่ใบเสร็จ: {header.CustomReceiptId}";
                        lblMessage.Font = new Font("Angsana New", 26, FontStyle.Bold);
                        lblMessage.TextAlign = ContentAlignment.MiddleCenter;
                        lblMessage.Dock = DockStyle.Top;
                        lblMessage.Height = 150;

                        Button btnConfirm = new Button();
                        btnConfirm.Text = "ยืนยัน";
                        btnConfirm.Font = new Font("Angsana New", 24);
                        btnConfirm.Size = new Size(150, 60);
                        btnConfirm.Location = new Point(80, 180);
                        btnConfirm.DialogResult = DialogResult.Yes;

                        Button btnCancel = new Button();
                        btnCancel.Text = "ยกเลิก";
                        btnCancel.Font = new Font("Angsana New", 24);
                        btnCancel.Size = new Size(150, 60);
                        btnCancel.Location = new Point(260, 180);
                        btnCancel.DialogResult = DialogResult.Cancel;

                        confirmDialog.Controls.Add(lblMessage);
                        confirmDialog.Controls.Add(btnConfirm);
                        confirmDialog.Controls.Add(btnCancel);
                        confirmDialog.AcceptButton = btnConfirm;
                        confirmDialog.CancelButton = btnCancel;

                        // Show dialog and wait for response
                        DialogResult result = confirmDialog.ShowDialog();

                        // If not confirmed, cancel the operation
                        if (result != DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    // Create a custom ReceiptPrintForm but modify its internal state
                    using (var printForm = new ReceiptPrintForm(receiptId, header, items, false))
                    {
                        // Set it to print as "สำเนา" instead of "ต้นฉบับ"
                        printForm.PrintAsCopy = true;

                        printForm.ShowDialog(this);

                        // No need to update any status as we're just reprinting
                        if (printForm.IsPrinted)
                        {
                            MessageBox.Show("พิมพ์สำเนาใบเสร็จเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void PrintCanceledOrdersPage(PrintPageEventArgs e, List<CanceledOrderInfo> canceledOrders)
        {
            // Mark that we've handled printing canceled orders
            _canceledOrdersPrinted = true;

            try
            {
                if (canceledOrders == null || canceledOrders.Count == 0)
                {
                    e.HasMorePages = false;
                    return;
                }

                // Set up margins and available space
                float leftMargin = e.MarginBounds.Left - 30;
                float topMargin = e.MarginBounds.Top;
                float rightMargin = e.MarginBounds.Right - 20;
                float availableWidth = rightMargin - leftMargin;
                float availableHeight = e.MarginBounds.Bottom - topMargin;

                using (Font titleFont = new Font("Angsana New", 14, FontStyle.Bold))
                using (Font headerFont = new Font("Angsana New", 11, FontStyle.Bold))
                using (Font normalFont = new Font("Angsana New", 10))
                using (Font boldFont = new Font("Angsana New", 10, FontStyle.Bold))
                using (Font smallFont = new Font("Angsana New", 8))
                {
                    float yPosition = topMargin;

                    // Draw title for canceled orders page
                    string title = "รายงานรายการที่ถูกยกเลิกในวันที่ " + dtpCreateDate.Value.ToString("dd/MM/yyyy");
                    float titleX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(title, titleFont).Width / 2);
                    e.Graphics.DrawString(title, titleFont, Brushes.Black, titleX, yPosition);
                    yPosition += titleFont.GetHeight() * 1.2f;

                    // Draw separating line
                    e.Graphics.DrawLine(new Pen(Color.Black, 1.5f), leftMargin, yPosition, leftMargin + availableWidth, yPosition);
                    yPosition += 15;

                    // Draw info text about canceled orders
                    string infoText = $"รายการที่ถูกยกเลิกทั้งหมด: {canceledOrders.Count} รายการ";
                    e.Graphics.DrawString(infoText, boldFont, Brushes.Black, leftMargin, yPosition);
                    yPosition += boldFont.GetHeight() * 1.5f;

                    // Define column widths for the canceled orders table
                    // Adjust these percentages to fit your data
                    float[] columnWidths = new float[]
                    {
                0.05f, // ลำดับ
                0.10f, // หมายเลขใบรับผ้า
                0.20f, // ชื่อลูกค้า
                0.10f, // เบอร์โทร
                0.15f, // ราคารวม
                0.15f, // วันที่สร้างรายการ
                0.25f  // เหตุผลการยกเลิก
                    };

                    // Calculate actual widths in pixels
                    float[] colWidths = new float[columnWidths.Length];
                    for (int i = 0; i < columnWidths.Length; i++)
                    {
                        colWidths[i] = availableWidth * columnWidths[i];
                    }

                    // Draw table headers for canceled orders
                    string[] columnHeaders = new string[]
                    {
                "ลำดับ",
                "เลขใบรับผ้า",
                "ชื่อลูกค้า",
                "เบอร์โทร",
                "ราคารวม",
                "วันที่สร้างรายการ",
                "เหตุผลการยกเลิก"
                    };

                    float headerHeight = headerFont.GetHeight() * 1.5f;
                    float headerX = leftMargin;

                    // Draw header row
                    for (int i = 0; i < columnHeaders.Length; i++)
                    {
                        RectangleF headerRect = new RectangleF(headerX, yPosition, colWidths[i], headerHeight);

                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;

                            // Fill with light gray background
                            e.Graphics.FillRectangle(Brushes.LightGray, headerRect);
                            e.Graphics.DrawRectangle(Pens.Black, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height);
                            e.Graphics.DrawString(columnHeaders[i], headerFont, Brushes.Black, headerRect, sf);
                        }

                        headerX += colWidths[i];
                    }

                    yPosition += headerHeight;

                    // Calculate rows per page
                    float rowHeight = normalFont.GetHeight() * 1.5f;
                    int rowsPerPage = (int)((availableHeight - (yPosition - topMargin) - 30) / rowHeight);
                    rowsPerPage = Math.Max(1, rowsPerPage); // At least one row

                    // Calculate start and end indexes for this page
                    int startIndex = _currentPage * rowsPerPage;
                    int endIndex = Math.Min(startIndex + rowsPerPage, canceledOrders.Count);

                    // Calculate total pages for canceled orders
                    int totalPages = (int)Math.Ceiling((double)canceledOrders.Count / rowsPerPage);

                    // Draw data rows
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        CanceledOrderInfo order = canceledOrders[i];
                        float rowX = leftMargin;

                        // Draw row number
                        RectangleF cellRect = new RectangleF(rowX, yPosition, colWidths[0], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            e.Graphics.DrawString((i + 1).ToString(), normalFont, Brushes.Black, cellRect, sf);
                        }
                        rowX += colWidths[0];

                        // Draw order ID
                        cellRect = new RectangleF(rowX, yPosition, colWidths[1], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            e.Graphics.DrawString(order.CustomOrderId, normalFont, Brushes.Black, cellRect, sf);
                        }
                        rowX += colWidths[1];

                        // Draw customer name
                        cellRect = new RectangleF(rowX, yPosition, colWidths[2], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            e.Graphics.DrawString(order.CustomerName, normalFont, Brushes.Black, cellRect, sf);
                        }
                        rowX += colWidths[2];

                        // Draw phone
                        cellRect = new RectangleF(rowX, yPosition, colWidths[3], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            e.Graphics.DrawString(order.Phone, normalFont, Brushes.Black, cellRect, sf);
                        }
                        rowX += colWidths[3];

                        // Draw total price
                        cellRect = new RectangleF(rowX, yPosition, colWidths[4], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            e.Graphics.DrawString(order.GrandTotalPrice.ToString("N2"), normalFont, Brushes.Black, cellRect, sf);
                        }
                        rowX += colWidths[4];

                        // Draw order date
                        cellRect = new RectangleF(rowX, yPosition, colWidths[5], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            e.Graphics.DrawString(order.OrderDate.ToString("dd/MM/yy HH:mm"), normalFont, Brushes.Black, cellRect, sf);
                        }
                        rowX += colWidths[5];

                        // Draw cancel reason
                        cellRect = new RectangleF(rowX, yPosition, colWidths[6], rowHeight);
                        using (StringFormat sf = new StringFormat
                        {
                            // Change this line from 'Alignment = StringAlignment.Left' to:
                            Alignment = StringAlignment.Near, // StringAlignment.Near is equivalent to left alignment
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter
                        })
                        {
                            sf.FormatFlags = StringFormatFlags.NoWrap;
                            e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);

                            // Adjust padding for reason text
                            cellRect.Inflate(-5, 0);
                            e.Graphics.DrawString(order.CancelReason, normalFont, Brushes.Black, cellRect, sf);
                        }

                        yPosition += rowHeight;
                    }

                    // Add page number at the bottom
                    string pageText = $"หน้า {_currentPage + 1} จาก {totalPages}";

                    e.Graphics.DrawString(pageText, smallFont, Brushes.Black,
                        rightMargin - e.Graphics.MeasureString(pageText, smallFont).Width,
                        e.MarginBounds.Bottom + 10);

                    // Check if we need more pages for canceled orders
                    if (endIndex < canceledOrders.Count)
                    {
                        _currentPage++;
                        e.HasMorePages = true;
                    }
                    else
                    {
                        _currentPage = 0;
                        e.HasMorePages = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _printErrorMessage = ex.Message;
                using (Font errorFont = new Font("Angsana New", 14, FontStyle.Bold))
                {
                    string errorMsg = $"เกิดข้อผิดพลาดในการพิมพ์รายการที่ถูกยกเลิก: {ex.Message}";
                    e.Graphics.DrawString(errorMsg, errorFont, Brushes.Red, e.MarginBounds.Left, e.MarginBounds.Top + 100);
                }
                e.HasMorePages = false;
            }
        }
    }
}
