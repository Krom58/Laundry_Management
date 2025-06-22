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

        private void LoadOrders(string customId = null, string customerName = null, DateTime? createDate = null)
        {
            string query = @"
                            SELECT 
                                o.OrderID, 
                                r.CustomReceiptId as 'หมายเลขใบเสร็จ',
                                o.CustomOrderId as 'หมายเลขใบรับผ้า', 
                                o.CustomerName as 'ชื่อลูกค้า', 
                                o.Phone as 'เบอร์โทรศัพท์',
                                r.TotalBeforeDiscount as 'ราคารวม',
                                r.Discount as 'ส่วนลด',
                                r.TotalAfterDiscount as 'ราคารวมหลังหักส่วนลด',
                                o.OrderDate as 'วันที่ออกใบรับผ้า',
                                o.PickupDate as 'วันที่ต้องมารับผ้า',
                                r.ReceiptID, 
                                r.ReceiptStatus as 'สถานะใบเสร็จ',
                                r.PaymentMethod as 'วิธีการชำระเงิน',
                                r.IsPickedUp as 'สถานะ', 
                                r.CustomerPickupDate as 'วันที่ลูกค้ามารับ'
                            FROM OrderHeader o
                            LEFT JOIN Receipt r ON o.OrderID = r.OrderID
                            WHERE 1=1
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
                filters.Add("o.CustomerName LIKE @CustomerName");
                parameters.Add(new SqlParameter("@CustomerName", "%" + customerName + "%"));
            }

            if (createDate.HasValue)
            {
                filters.Add("(CAST(r.ReceiptDate AS DATE) = @ReceiptDate OR (r.ReceiptDate IS NULL AND CAST(o.OrderDate AS DATE) = @ReceiptDate))");
                parameters.Add(new SqlParameter("@ReceiptDate", createDate.Value));
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
            chkPending.Checked = true;
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

                // Check if required data is available
                if (string.IsNullOrEmpty(customOrderId) || string.IsNullOrEmpty(customerName))
                {
                    MessageBox.Show("ข้อมูลสำหรับการพิมพ์ไม่ครบถ้วน", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Retrieve order items and discount from database
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

                    // Get order items
                    string itemsQuery = @"
                        SELECT ItemName, Quantity, TotalAmount 
                        FROM OrderItem 
                        WHERE OrderID = @OrderID";
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
                    MessageBox.Show("ไม่พบรายการสินค้าสำหรับการพิมพ์", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            oi.IsCanceled
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

                    // Open ReceiptPrintForm to print the receipt
                    using (var printForm = new ReceiptPrintForm(receiptId, header, items))
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

                // Create a PrintDocument object
                PrintDocument printDoc = new PrintDocument();
                printDoc.DocumentName = "รายงานข้อมูล";

                // กำหนดขนาดกระดาษเป็น A4
                bool foundA4 = false;
                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
                {
                    if (ps.Kind == PaperKind.A4)
                    {
                        printDoc.DefaultPageSettings.PaperSize = ps;
                        foundA4 = true;
                        break;
                    }
                }

                // If A4 is not found, create a custom A4 paper size (210mm x 297mm)
                if (!foundA4)
                {
                    // A4 in hundredths of an inch (8.27" x 11.69")
                    PaperSize a4Size = new PaperSize("A4", 827, 1169);
                    printDoc.DefaultPageSettings.PaperSize = a4Size;
                }

                // Set landscape orientation for better fit of the table
                printDoc.DefaultPageSettings.Landscape = true;

                // Set reasonable margins (10mm = ~40 hundredths of an inch)
                printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);

                // Add handlers for print events
                printDoc.PrintPage += PrintPage;
                printDoc.EndPrint += PrintDoc_EndPrint;

                // Flag to track print status
                _isPrintSuccessful = true;
                _printErrorMessage = "";

                // Create a PrintDialog
                using (PrintDialog printDialog = new PrintDialog())
                {
                    printDialog.Document = printDoc;
                    printDialog.UseEXDialog = true;

                    // Show the print dialog
                    if (printDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            // Show print status
                            Cursor = Cursors.WaitCursor;

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

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                e.PageSettings.Landscape = true;
                // Set page orientation to landscape
                // Note: This should ideally be set before printing, but we'll ensure the layout works well in landscape

                // Page settings
                float leftMargin = e.MarginBounds.Left;
                float topMargin = e.MarginBounds.Top;
                float rightMargin = e.MarginBounds.Right;
                float bottomMargin = e.MarginBounds.Bottom;
                float availableWidth = rightMargin - leftMargin;
                float availableHeight = bottomMargin - topMargin;

                // ปรับขนาดฟอนต์ให้เล็กลง
                using (Font titleFont = new Font("Angsana New", 14, FontStyle.Bold))
                using (Font headerFont = new Font("Angsana New", 10, FontStyle.Bold))
                using (Font normalFont = new Font("Angsana New", 10))
                using (Font smallFont = new Font("Angsana New", 9))
                {
                    // Draw the page header
                    float yPosition = topMargin;

                    // Title centered at the top
                    string title = "รายงานข้อมูลการรับผ้า";
                    float titleX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(title, titleFont).Width / 2);
                    e.Graphics.DrawString(title, titleFont, Brushes.Black, titleX, yPosition);
                    yPosition += titleFont.GetHeight();

                    // Draw underline below title
                    e.Graphics.DrawLine(new Pen(Color.Black, 1.5f), leftMargin, yPosition, leftMargin + availableWidth, yPosition);
                    yPosition += 10;

                    // Date and filters information line
                    DateTime today = DateTime.Now;
                    string dateTimeInfo = $"พิมพ์เมื่อ: {today.Day}/{today.Month}/{today.Year} {today.ToString("HH:mm:ss")}";

                    // Get current search criteria for the header
                    string filterInfo = dtpCreateDate.Checked ? $"วันที่: {dtpCreateDate.Value.ToString("dd/MM/yyyy")}" : "ทุกวันที่";
                    filterInfo += chkPending.Checked ? ", สถานะ: ยังไม่มารับ" :
                                  chkCompleted.Checked ? ", สถานะ: มารับแล้ว" : ", ทุกสถานะ";

                    // Print date info on left and filter info on right
                    e.Graphics.DrawString(dateTimeInfo, normalFont, Brushes.Black, leftMargin, yPosition);

                    string filterText = $"กรอง: {filterInfo}";
                    float filterX = rightMargin - e.Graphics.MeasureString(filterText, normalFont).Width;
                    e.Graphics.DrawString(filterText, normalFont, Brushes.Black, filterX, yPosition);

                    yPosition += normalFont.GetHeight() * 1.5f;

                    // Define only the essential columns we want to display based on the image
                    string[] columnNames = new string[] {
    "หมายเลข\nใบรับผ้า",
    "หมายเลข\nใบเสร็จ",
    "ชื่อลูกค้า",
    "เบอร์โทรศัพท์",
    "ราคารวม",
    "ส่วนลด",
    "ราคารวม\nหลังหักส่วนลด",
    "วันที่ออก\nใบรับผ้า",
    "วันที่ต้อง\nมารับผ้า",
    "สถานะ",
    "วันที่ลูกค้า\nมารับ"
};

                    string[] columnDataProperties = new string[] {
                "หมายเลขใบรับผ้า",
                "หมายเลขใบเสร็จ",
                "ชื่อลูกค้า",
                "เบอร์โทรศัพท์",
                "ราคารวม",
                "ส่วนลด",
                "ราคารวมหลังหักส่วนลด",
                "วันที่ออกใบรับผ้า",
                "วันที่ต้องมารับผ้า",
                "สถานะ",
                "วันที่ลูกค้ามารับ"
            };

                    // Column width percentages - adjust these to match the desired widths
                    float[] columnWidthPercentages = new float[] {
                0.09f, // หมายเลขใบรับผ้า
                0.09f, // หมายเลขใบเสร็จ
                0.13f, // ชื่อลูกค้า
                0.09f, // เบอร์โทรศัพท์
                0.08f, // ราคารวม
                0.08f, // ส่วนลด
                0.10f, // ราคารวมหลังหักส่วนลด
                0.09f, // วันที่ออกใบรับผ้า
                0.09f, // วันที่ต้องมารับผ้า
                0.08f, // สถานะ
                0.08f  // วันที่ลูกค้ามารับ
            };

                    // Calculate column widths
                    float[] columnWidths = new float[columnWidthPercentages.Length];
                    for (int i = 0; i < columnWidthPercentages.Length; i++)
                    {
                        columnWidths[i] = availableWidth * columnWidthPercentages[i];
                    }

                    // Draw table header
                    float headerHeight = headerFont.GetHeight() * 2.5f;
                    float currentX = leftMargin;

                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        RectangleF headerRect = new RectangleF(currentX, yPosition, columnWidths[i], headerHeight);

                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            sf.FormatFlags = StringFormatFlags.LineLimit; // รองรับหลายบรรทัด
                            e.Graphics.FillRectangle(Brushes.LightGray, headerRect);
                            e.Graphics.DrawRectangle(Pens.Black, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height);
                            e.Graphics.DrawString(columnNames[i], headerFont, Brushes.Black, headerRect, sf);
                        }

                        currentX += columnWidths[i];
                    }
                    yPosition += headerHeight;

                    // Calculate how many rows can fit on a page
                    float rowHeight = normalFont.GetHeight() * 1.2f;
                    int rowsPerPage = (int)((availableHeight - (yPosition - topMargin) - 40) / rowHeight);

                    // Calculate the range of rows to print for the current page
                    int startRow = _currentPage * rowsPerPage;
                    int endRow = Math.Min(startRow + rowsPerPage, dgvOrders.Rows.Count);

                    // Print data rows
                    int validRowsPrinted = 0;
                    for (int i = startRow; i < endRow; i++)
                    {
                        DataGridViewRow row = dgvOrders.Rows[i];

                        // Skip rows with no data
                        if (row.Cells["หมายเลขใบรับผ้า"].Value == null &&
                            row.Cells["ชื่อลูกค้า"].Value == null)
                        {
                            continue;
                        }

                        currentX = leftMargin;

                        for (int j = 0; j < columnDataProperties.Length; j++)
                        {
                            string cellValue = "";
                            try
                            {
                                if (row.Cells[columnDataProperties[j]].Value != null &&
                                    row.Cells[columnDataProperties[j]].Value != DBNull.Value)
                                {
                                    // Format value based on column type
                                    if (columnDataProperties[j] == "ราคารวม" ||
                                        columnDataProperties[j] == "ส่วนลด" ||
                                        columnDataProperties[j] == "ราคารวมหลังหักส่วนลด")
                                    {
                                        decimal amount = Convert.ToDecimal(row.Cells[columnDataProperties[j]].Value);
                                        cellValue = amount.ToString("N2");
                                    }
                                    else if (columnDataProperties[j] == "วันที่ออกใบรับผ้า" ||
                                             columnDataProperties[j] == "วันที่ต้องมารับผ้า" ||
                                             columnDataProperties[j] == "วันที่ลูกค้ามารับ")
                                    {
                                        if (row.Cells[columnDataProperties[j]].Value != DBNull.Value)
                                        {
                                            DateTime date = Convert.ToDateTime(row.Cells[columnDataProperties[j]].Value);
                                            cellValue = date.ToString("dd/MM/yyyy");
                                        }
                                    }
                                    else
                                    {
                                        cellValue = row.Cells[columnDataProperties[j]].Value.ToString();
                                    }
                                }
                            }
                            catch (InvalidCastException)
                            {
                                // Handle the case where DBNull can't be cast to other types
                                cellValue = "-";
                                _printErrorMessage = "มีข้อมูลบางรายการที่ไม่สามารถแสดงได้";
                            }
                            catch (Exception)
                            {
                                cellValue = "?";
                            }

                            RectangleF cellRect = new RectangleF(currentX, yPosition, columnWidths[j], rowHeight);

                            using (StringFormat sf = new StringFormat())
                            {
                                sf.Alignment = StringAlignment.Center;
                                sf.LineAlignment = StringAlignment.Center;
                                sf.Trimming = StringTrimming.EllipsisCharacter;

                                // Apply special formatting for status
                                if (columnDataProperties[j] == "สถานะ" && !string.IsNullOrEmpty(cellValue))
                                {
                                    using (SolidBrush statusBrush = new SolidBrush(cellValue == "มารับแล้ว" ? Color.Green : Color.Blue))
                                    {
                                        e.Graphics.DrawString(cellValue, normalFont, statusBrush, cellRect, sf);
                                    }
                                }
                                else if (columnDataProperties[j] == "ส่วนลด" && !string.IsNullOrEmpty(cellValue) &&
                                         cellValue != "-" && cellValue != "?" && Convert.ToDecimal(row.Cells[columnDataProperties[j]].Value) > 0)
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

                            currentX += columnWidths[j];
                        }

                        yPosition += rowHeight;
                        validRowsPrinted++;
                    }

                    // Add summary at the bottom if this is the last page
                    bool isLastPage = endRow >= dgvOrders.Rows.Count ||
                                      endRow >= dgvOrders.Rows.Cast<DataGridViewRow>().Count(r =>
                                        r.Cells["หมายเลขใบรับผ้า"].Value != null || r.Cells["ชื่อลูกค้า"].Value != null);

                    if (isLastPage)
                    {
                        yPosition += 15;

                        // Count valid rows
                        int validRowCount = dgvOrders.Rows.Cast<DataGridViewRow>().Count(r =>
                            r.Cells["หมายเลขใบรับผ้า"].Value != null || r.Cells["ชื่อลูกค้า"].Value != null);

                        string summaryText = $"จำนวนรายการทั้งหมด {validRowCount} รายการ";
                        e.Graphics.DrawString(summaryText, normalFont, Brushes.Black, leftMargin, yPosition);
                    }

                    // Add page number at the bottom right
                    int totalValidRows = dgvOrders.Rows.Cast<DataGridViewRow>().Count(r =>
                        r.Cells["หมายเลขใบรับผ้า"].Value != null || r.Cells["ชื่อลูกค้า"].Value != null);

                    int totalPages = (int)Math.Ceiling((double)totalValidRows / rowsPerPage);
                    if (totalPages == 0) totalPages = 1; // Ensure at least one page

                    string pageText = $"หน้า {_currentPage + 1} จาก {totalPages}";

                    e.Graphics.DrawString(pageText, smallFont, Brushes.Black,
                        rightMargin - e.Graphics.MeasureString(pageText, smallFont).Width,
                        bottomMargin + 10);

                    // Check if more pages are needed
                    if (endRow < dgvOrders.Rows.Count &&
                        endRow < dgvOrders.Rows.Cast<DataGridViewRow>().Count(r =>
                            r.Cells["หมายเลขใบรับผ้า"].Value != null || r.Cells["ชื่อลูกค้า"].Value != null))
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
                // Mark print as failed and store error message
                _isPrintSuccessful = false;
                _printErrorMessage = ex.Message;

                // Show error report on the page
                using (Font errorFont = new Font("Angsana New", 14, FontStyle.Bold))
                {
                    string errorMsg = $"เกิดข้อผิดพลาดในการพิมพ์: {ex.Message}";
                    e.Graphics.DrawString(errorMsg, errorFont, Brushes.Red, e.MarginBounds.Left, e.MarginBounds.Top + 100);
                }

                // No more pages after error
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
                            "ราคารวม", "ส่วนลด", "ราคารวมหลังหักส่วนลด",
                            "วันที่ออกใบรับผ้า", "วันที่ต้องมารับผ้า", "สถานะ", "วันที่ลูกค้ามารับ"
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
                                    else if ((col == "ราคารวม" || col == "ส่วนลด" || col == "ราคารวมหลังหักส่วนลด") &&
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
    }
}
