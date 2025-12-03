using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laundry_Management.Laundry
{
    public partial class Pickup_List : Form
    {
        private bool _isInitializing = true;
        private bool _isPrintSuccessful = false;
        private string _printErrorMessage = "";
        private int _currentPage = 0;
        
        public Pickup_List()
        {
            InitializeComponent();

            // เพิ่ม event handlers สำหรับการกด Enter ในช่องค้นหา
            txtOrderId.KeyPress += TxtSearch_KeyPress;
            txtCustomerFilter.KeyPress += TxtSearch_KeyPress;

            // Wire up event handlers
            chkNotPickup.CheckedChanged += chkNotPickup_CheckedChanged;
            chkPickedup.CheckedChanged += chkPickedup_CheckedChanged;

            // Register event handlers for both date pickers
            dtpCreateDate.ValueChanged += DtpCreateDate_ValueChanged;
            dtpCreateDateEnd.ValueChanged += DtpCreateDate_ValueChanged;

            // เพิ่ม event handlers สำหรับปุ่มพิมพ์และ Excel
            btnPrint.Click += btnPrint_Click;
            btnExcel.Click += btnExcel_Click;

            // กำหนดรูปแบบวันที่และค่าเริ่มต้น
            dtpCreateDate.Format = DateTimePickerFormat.Custom;
            dtpCreateDate.CustomFormat = "dd/MM/yyyy";
            dtpCreateDateEnd.Format = DateTimePickerFormat.Custom;
            dtpCreateDateEnd.CustomFormat = "dd/MM/yyyy";
            
            // กำหนดค่าเริ่มต้นของวันที่
            dtpCreateDate.Value = DateTime.Today;
            dtpCreateDateEnd.Value = DateTime.Today;

            // Initialize the form
            LoadPickupOrders();
            chkNotPickup.Checked = true;
            chkPickedup.Checked = false;
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // ตั้งค่าปุ่ม Search เป็น AcceptButton ของฟอร์ม
            this.AcceptButton = btnSearch;
            
            // เสร็จสิ้นการ initialize
            _isInitializing = false;
        }
        
        private void DtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            // ข้ามการทำงานถ้ากำลัง initialize
            if (_isInitializing) return;
            
            // ตรวจสอบว่าวันที่เริ่มต้นไม่มากกว่าวันที่สิ้นสุด
            if (dtpCreateDate.Value > dtpCreateDateEnd.Value)
            {
                MessageBox.Show("วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpCreateDate.Value = dtpCreateDateEnd.Value;
                return;
            }
            
            // เรียกค้นหาอัตโนมัติเมื่อเปลี่ยนวันที่
            btnSearch_Click(sender, EventArgs.Empty);
        }
        
        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ตรวจสอบว่ากด Enter หรือไม่ (รหัส ASCII 13)
            if (e.KeyChar == (char)Keys.Enter)
            {
                // ป้องกันเสียง beep
                e.Handled = true;

                // เรียกฟังก์ชันค้นหาเหมือนกับการกดปุ่ม
                btnSearch_Click(sender, EventArgs.Empty);
            }
        }
        private void LoadPickupOrders()
        {
            string query = @"
                        SELECT 
                            o.OrderID, 
                            o.CustomOrderId as 'หมายเลขใบรับผ้า', 
                            o.CustomerId,
                            c.FullName as 'ชื่อลูกค้า', 
                            c.Phone as 'เบอร์โทรศัพท์',
                            r.ReceiptID,
                            r.CustomReceiptId as 'หมายเลขใบเสร็จ',
                            o.OrderDate as 'วันที่ส่งผ้า',
                            o.PickupDate as 'วันที่ครบกำหนด',
                            r.IsPickedUp as 'สถานะ', 
                            r.CustomerPickupDate as 'วันที่มารับ'
                        FROM OrderHeader o
                        LEFT JOIN Customer c ON o.CustomerId = c.CustomerID
                        INNER JOIN Receipt r ON o.OrderID = r.OrderID
                        WHERE (r.IsPickedUp IS NULL OR r.IsPickedUp <> N'มารับแล้ว')
                        AND CAST(o.OrderDate AS DATE) = @TodayDate
                        AND o.OrderStatus = N'ออกใบเสร็จแล้ว' AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                        ORDER BY o.OrderDate ASC, r.ReceiptID ASC
                    ";

            using (SqlConnection conn = Laundry_Management.Laundry.DBconfig.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TodayDate", DateTime.Today);

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
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string orderId = txtOrderId.Text.Trim();
            string customerFilter = txtCustomerFilter.Text.Trim();
            
            // ใช้วันที่จาก DateTimePicker ตลอดเวลา
            DateTime startDate = dtpCreateDate.Value.Date;
            DateTime endDate = dtpCreateDateEnd.Value.Date;

            var query = @"
                        SELECT 
                            o.OrderID,
                            o.CustomOrderId as 'หมายเลขใบรับผ้า', 
                            o.CustomerId,
                            c.FullName as 'ชื่อลูกค้า', 
                            c.Phone as 'เบอร์โทรศัพท์', 
                            r.ReceiptID,
                            r.CustomReceiptId as 'หมายเลขใบเสร็จ',
                            o.OrderDate as 'วันที่ส่งผ้า',
                            o.PickupDate as 'วันที่ครบกำหนด',
                            r.IsPickedUp as 'สถานะ', 
                            r.CustomerPickupDate as 'วันที่มารับ'
                        FROM OrderHeader o
                        LEFT JOIN Customer c ON o.CustomerId = c.CustomerID
                        INNER JOIN Receipt r ON o.OrderID = r.OrderID
                        WHERE 1=1
                        AND o.OrderStatus = N'ออกใบเสร็จแล้ว' AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                    ";

            var filters = new List<string>();
            var parameters = new List<SqlParameter>();
            string orderByClause = "";

            // ตรวจสอบ checkbox สถานะการรับผ้า และเลือกคอลัมน์วันที่ที่เหมาะสม
            if (chkNotPickup.Checked)
            {
                filters.Add("(r.IsPickedUp IS NULL OR r.IsPickedUp <> N'มารับแล้ว')");
                // ค้นหาตามวันที่ส่งผ้า (OrderDate) - นี่คือจุดที่แก้ไข
                filters.Add("CAST(o.OrderDate AS DATE) BETWEEN @StartDate AND @EndDate");
                // เรียงตามวันที่ส่งผ้า
                orderByClause = " ORDER BY o.OrderDate ASC, r.ReceiptID ASC";
            }
            else if (chkPickedup.Checked)
            {
                filters.Add("r.IsPickedUp = N'มารับแล้ว'");
                // ค้นหาตามวันที่มารับผ้า (CustomerPickupDate)
                filters.Add("CAST(r.CustomerPickupDate AS DATE) BETWEEN @StartDate AND @EndDate");
                // เรียงตามวันที่มารับผ้า
                orderByClause = " ORDER BY r.CustomerPickupDate ASC, r.ReceiptID ASC";
            }
            
            parameters.Add(new SqlParameter("@StartDate", startDate));
            parameters.Add(new SqlParameter("@EndDate", endDate));

            if (!string.IsNullOrEmpty(orderId))
            {
                // ถ้าผู้ใช้ป้อนข้อมูลในรูปแบบ OR-xxx/yyy
                if (orderId.StartsWith("OR-") && orderId.Contains("/"))
                {
                    string[] parts = orderId.Replace("OR-", "").Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int orderIdNum) && int.TryParse(parts[1], out int receiptIdNum))
                    {
                        filters.Add("(o.OrderID = @OrderID AND r.ReceiptID = @ReceiptID)");
                        parameters.Add(new SqlParameter("@OrderID", orderIdNum));
                        parameters.Add(new SqlParameter("@ReceiptID", receiptIdNum));
                    }
                    else
                    {
                        // ถ้ารูปแบบไม่ถูกต้อง ให้ค้นหาแบบ LIKE
                        filters.Add("(o.CustomOrderId LIKE @CustomID OR r.CustomReceiptId LIKE @CustomID)");
                        parameters.Add(new SqlParameter("@CustomID", "%" + orderId + "%"));
                    }
                }
                else
                {
                    // ถ้าไม่ได้ป้อนในรูปแบบ OR-xxx/yyy ให้ค้นหาแบบ LIKE
                    filters.Add("(o.CustomOrderId LIKE @CustomID OR r.CustomReceiptId LIKE @CustomID)");
                    parameters.Add(new SqlParameter("@CustomID", "%" + orderId + "%"));
                }
            }

            if (!string.IsNullOrEmpty(customerFilter))
            {
                filters.Add("c.FullName LIKE @CustomerName");
                parameters.Add(new SqlParameter("@CustomerName", "%" + customerFilter + "%"));
            }

            if (filters.Count > 0)
            {
                query += " AND " + string.Join(" AND ", filters);
            }
            
            // เพิ่ม ORDER BY clause
            query += orderByClause;

            using (SqlConnection conn = Laundry_Management.Laundry.DBconfig.GetConnection())
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

        private void chkNotPickup_CheckedChanged(object sender, EventArgs e)
        {
            // ป้องกันการยกเลิกทั้งสองรายการ
            if (!chkNotPickup.Checked && !chkPickedup.Checked)
            {
                // ถ้าพยายามยกเลิก chkNotPickup และ chkPickedup ไม่ได้ถูกเลือก ให้กลับไปเลือก chkNotPickup
                chkNotPickup.Checked = true;
                return;
            }

            // ถ้า chkNotPickup ถูกเลือก ให้ยกเลิกการเลือก chkPickedup
            if (chkNotPickup.Checked)
            {
                chkPickedup.Checked = false;
            }

            // ดำเนินการค้นหาอีกครั้ง
            btnSearch_Click(sender, e);
        }

        private void chkPickedup_CheckedChanged(object sender, EventArgs e)
        {
            // ป้องกันการยกเลิกทั้งสองรายการ
            if (!chkPickedup.Checked && !chkNotPickup.Checked)
            {
                // ถ้าพยายามยกเลิก chkPickedup และ chkNotPickup ไม่ได้ถูกเลือก ให้กลับไปเลือก chkPickedup
                chkPickedup.Checked = true;
                return;
            }

            // ถ้า chkPickedup ถูกเลือก ให้ยกเลิกการเลือก chkNotPickup
            if (chkPickedup.Checked)
            {
                chkNotPickup.Checked = false;
            }

            // ดำเนินการค้นหาอีกครั้ง
            btnSearch_Click(sender, e);
        }

        private void Customer_Pickup_Check_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการบันทึกการรับผ้า", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // ดึง OrderID และ ReceiptID โดยตรงจาก DataGridView (ซึ่งเรามี column เหล่านี้แต่อาจซ่อนไว้)
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["OrderID"].Value);
                int receiptId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["ReceiptID"].Value);

                // ตรวจสอบสถานะ IsPickedUp ก่อน
                var isPickedUpObj = dgvOrders.CurrentRow.Cells["สถานะ"].Value;
                if (isPickedUpObj != null && isPickedUpObj.ToString() == "มารับแล้ว")
                {
                    MessageBox.Show("ไม่สามารถบันทึกซ้ำได้", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ดึงข้อมูลที่จำเป็นสำหรับการแสดงผลในกล่องยืนยัน
                string receiptCustomId = dgvOrders.CurrentRow.Cells["หมายเลขใบเสร็จ"].Value?.ToString() ?? "";
                string customerName = dgvOrders.CurrentRow.Cells["ชื่อลูกค้า"].Value?.ToString() ?? "ลูกค้า";

                // สร้างกล่องข้อความยืนยันด้วยตัวอักษรขนาดใหญ่
                using (Form confirmDialog = new Form())
                {
                    confirmDialog.Text = "ยืนยันการรับผ้า";
                    confirmDialog.Size = new Size(500, 300);
                    confirmDialog.StartPosition = FormStartPosition.CenterParent;
                    confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    confirmDialog.MaximizeBox = false;
                    confirmDialog.MinimizeBox = false;

                    Label lblMessage = new Label();
                    lblMessage.Text = $"ยืนยันการรับผ้าของ\n{customerName}\nหมายเลขใบเสร็จ {receiptCustomId}";
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

                    // แสดงกล่องข้อความและรอการตอบกลับ
                    DialogResult result = confirmDialog.ShowDialog();

                    // ถ้าไม่ได้กดยืนยัน ให้ยกเลิกการทำงาน
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }

                // ดำเนินการบันทึกข้อมูล - ใช้ receiptId ที่ดึงมาโดยตรงแทนการหาใหม่
                string updateQuery = @"
                        UPDATE Receipt
                        SET IsPickedUp = N'มารับแล้ว', CustomerPickupDate = @PickupDate
                        WHERE ReceiptID = @ReceiptID
                    ";

                using (SqlConnection conn = Laundry_Management.Laundry.DBconfig.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@PickupDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ReceiptID", receiptId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("บันทึกการรับผ้าเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // เปลี่ยน checkbox เป็น "มารับแล้ว"
                            chkPickedup.Checked = true;
                            // รีเฟรชข้อมูล
                            btnSearch_Click(sender, e);
                        }
                        else
                        {
                            MessageBox.Show("ไม่สามารถบันทึกข้อมูลได้", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            // ดีบักเพิ่มเติม - แสดงข้อมูลที่จะอัปเดต
                            MessageBox.Show($"Debug Info: ReceiptID = {receiptId}", "Debug", MessageBoxButtons.OK);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // เพิ่มการดักจับข้อผิดพลาด
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการยกเลิกการรับผ้า", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // ดึง OrderID และ ReceiptID โดยตรงจาก DataGridView
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["OrderID"].Value);
                int receiptId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["ReceiptID"].Value);

                // ตรวจสอบสถานะ IsPickedUp ก่อน
                var isPickedUpObj = dgvOrders.CurrentRow.Cells["สถานะ"].Value;
                if (isPickedUpObj == null || isPickedUpObj.ToString() != "มารับแล้ว")
                {
                    MessageBox.Show("ไม่สามารถยกเลิกได้ เนื่องจากยังไม่มีการบันทึกการรับผ้า", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ดึงข้อมูลที่จำเป็นสำหรับการแสดงผล
                string receiptCustomId = dgvOrders.CurrentRow.Cells["หมายเลขใบเสร็จ"].Value?.ToString() ?? "";
                string customerName = dgvOrders.CurrentRow.Cells["ชื่อลูกค้า"].Value?.ToString() ?? "ลูกค้า";

                // สร้างกล่องข้อความยืนยัน
                using (Form confirmDialog = new Form())
                {
                    confirmDialog.Text = "ยืนยันการยกเลิกการรับผ้า";
                    confirmDialog.Size = new Size(550, 300);
                    confirmDialog.StartPosition = FormStartPosition.CenterParent;
                    confirmDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    confirmDialog.MaximizeBox = false;
                    confirmDialog.MinimizeBox = false;

                    Label lblMessage = new Label();
                    lblMessage.Text = $"ยืนยันการยกเลิกการรับผ้าของ\n{customerName}\nหมายเลขใบเสร็จ {receiptCustomId}";
                    lblMessage.Font = new Font("Angsana New", 26, FontStyle.Bold);
                    lblMessage.TextAlign = ContentAlignment.MiddleCenter;
                    lblMessage.Dock = DockStyle.Top;
                    lblMessage.Height = 150;

                    Button btnConfirm = new Button();
                    btnConfirm.Text = "ยืนยัน";
                    btnConfirm.Font = new Font("Angsana New", 24);
                    btnConfirm.Size = new Size(150, 60);
                    btnConfirm.Location = new Point(100, 180);
                    btnConfirm.DialogResult = DialogResult.Yes;

                    Button btnCancel = new Button();
                    btnCancel.Text = "ยกเลิก";
                    btnCancel.Font = new Font("Angsana New", 24);
                    btnCancel.Size = new Size(150, 60);
                    btnCancel.Location = new Point(300, 180);
                    btnCancel.DialogResult = DialogResult.Cancel;

                    confirmDialog.Controls.Add(lblMessage);
                    confirmDialog.Controls.Add(btnConfirm);
                    confirmDialog.Controls.Add(btnCancel);
                    confirmDialog.AcceptButton = btnConfirm;
                    confirmDialog.CancelButton = btnCancel;

                    // แสดงกล่องข้อความและรอการตอบกลับ
                    DialogResult result = confirmDialog.ShowDialog();

                    // ถ้าไม่ได้กดยืนยัน ให้ยกเลิกการทำงาน
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }

                // อัปเดตข้อมูลในฐานข้อมูล - ใช้ receiptId โดยตรง
                string updateQuery = @"
                        UPDATE Receipt
                        SET IsPickedUp = N'ยังไม่มารับ', CustomerPickupDate = NULL
                        WHERE ReceiptID = @ReceiptID
                    ";

                using (SqlConnection conn = Laundry_Management.Laundry.DBconfig.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ReceiptID", receiptId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("ยกเลิกการรับผ้าเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // เปลี่ยน checkbox เป็น "ยังไม่มารับ"
                            chkNotPickup.Checked = true;
                            btnSearch_Click(sender, e);
                        }
                        else
                        {
                            MessageBox.Show("ไม่สามารถยกเลิกข้อมูลได้", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            // ดีบักเพิ่มเติม - แสดงข้อมูลที่จะอัปเดต
                            MessageBox.Show($"Debug Info: ReceiptID = {receiptId}", "Debug", MessageBoxButtons.OK);
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
                printDoc.DocumentName = "รายงานการมารับผ้า";

                // กำหนดขนาดกระดาษเป็น A4
                SetA4PaperSize(printDoc);

                // Add handlers for print events
                printDoc.PrintPage += PrintPage;
                printDoc.EndPrint += PrintDoc_EndPrint;
                printDoc.BeginPrint += (s, args) =>
                {
                    _currentPage = 0;
                    _isPrintSuccessful = true;
                    _printErrorMessage = "";
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

        private void SetA4PaperSize(PrintDocument printDoc)
        {
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

            if (!foundA4)
            {
                PaperSize a4Size = new PaperSize("A4", 827, 1169);
                printDoc.DefaultPageSettings.PaperSize = a4Size;
            }

            printDoc.DefaultPageSettings.Landscape = false;
            printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
        }

        private void EnsureA4PaperSize(PrintDocument printDoc)
        {
            PaperSize currentSize = printDoc.DefaultPageSettings.PaperSize;
            bool isA4Size = (Math.Abs(currentSize.Width - 827) < 10 && Math.Abs(currentSize.Height - 1169) < 10) ||
                            (Math.Abs(currentSize.Height - 827) < 10 && Math.Abs(currentSize.Width - 1169) < 10);

            if (!isA4Size)
            {
                printDoc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            }

            printDoc.DefaultPageSettings.Landscape = false;
        }

        private void PrintDoc_EndPrint(object sender, PrintEventArgs e)
        {
            if (_isPrintSuccessful)
            {
                MessageBox.Show("พิมพ์รายงานเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                e.PageSettings.Landscape = false;

                float leftMargin = e.MarginBounds.Left;
                float topMargin = e.MarginBounds.Top;
                float rightMargin = e.MarginBounds.Right;
                float bottomMargin = e.MarginBounds.Bottom;
                float availableWidth = rightMargin - leftMargin;
                float availableHeight = bottomMargin - topMargin;

                using (Font titleFont = new Font("Angsana New", 16, FontStyle.Bold))
                using (Font headerFont = new Font("Angsana New", 11, FontStyle.Bold))
                using (Font normalFont = new Font("Angsana New", 10))
                using (Font smallFont = new Font("Angsana New", 9))
                {
                    float yPosition = topMargin;

                    // Title
                    string title = "รายงานการมารับผ้า";
                    float titleX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(title, titleFont).Width / 2);
                    e.Graphics.DrawString(title, titleFont, Brushes.Black, titleX, yPosition);
                    yPosition += titleFont.GetHeight();

                    // Draw line
                    e.Graphics.DrawLine(new Pen(Color.Black, 1.5f), leftMargin, yPosition, leftMargin + availableWidth, yPosition);
                    yPosition += 10;

                    // Date range info
                    string dateRangeInfo = $"ช่วงวันที่: {dtpCreateDate.Value.ToString("dd/MM/yyyy")} ถึง {dtpCreateDateEnd.Value.ToString("dd/MM/yyyy")}";

                    // Print date/time
                    DateTime today = DateTime.Now;
                    string dateTimeInfo = $"พิมพ์เมื่อ: {today.ToString("dd/MM/yyyy HH:mm:ss")}";
                    e.Graphics.DrawString(dateTimeInfo, normalFont, Brushes.Black, leftMargin, yPosition);

                    // Status filter info
                    string statusInfo = chkNotPickup.Checked ? "สถานะ: ยังไม่มารับ" :
                                       chkPickedup.Checked ? "สถานะ: มารับแล้ว" : "ทุกสถานะ";
                    float statusX = rightMargin - e.Graphics.MeasureString(statusInfo, normalFont).Width;
                    e.Graphics.DrawString(statusInfo, normalFont, Brushes.Black, statusX, yPosition);
                    yPosition += normalFont.GetHeight() * 1.5f;

                    // Column definitions
                    string[] columnNames = new string[] {
                        "ลำดับ",
                        "หมายเลข\nใบรับผ้า",
                        "หมายเลข\nใบเสร็จ",
                        "ชื่อลูกค้า",
                        "เบอร์โทร",
                        "วันที่\nส่งผ้า",
                        "วันที่\nครบกำหนด",
                        "สถานะ",
                        "วันที่\nมารับ"
                    };

                    float[] columnWidthPercentages = new float[] {
                        0.06f, // ลำดับ
                        0.12f, // หมายเลขใบรับผ้า
                        0.12f, // หมายเลขใบเสร็จ
                        0.16f, // ชื่อลูกค้า
                        0.12f, // เบอร์โทร
                        0.12f, // วันที่ส่งผ้า
                        0.12f, // วันที่ครบกำหนด
                        0.10f, // สถานะ
                        0.12f  // วันที่มารับ
                    };

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
                            sf.FormatFlags = StringFormatFlags.LineLimit | StringFormatFlags.NoClip;

                            e.Graphics.FillRectangle(Brushes.LightGray, headerRect);
                            e.Graphics.DrawRectangle(Pens.Black, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height);
                            e.Graphics.DrawString(columnNames[i], headerFont, Brushes.Black, headerRect, sf);
                        }
                        currentX += columnWidths[i];
                    }
                    yPosition += headerHeight;

                    // Calculate rows per page
                    float rowHeight = normalFont.GetHeight() * 1.2f;
                    int rowsPerPage = (int)((availableHeight - (yPosition - topMargin) - 40) / rowHeight);

                    // Calculate row range for current page
                    int startRow = _currentPage * rowsPerPage;
                    int endRow = Math.Min(startRow + rowsPerPage, dgvOrders.Rows.Count);

                    // Print data rows
                    int sequenceNumber = startRow + 1;
                    for (int i = startRow; i < endRow; i++)
                    {
                        DataGridViewRow row = dgvOrders.Rows[i];

                        if (row.Cells["หมายเลขใบรับผ้า"].Value == null)
                            continue;

                        currentX = leftMargin;

                        // Sequence number
                        RectangleF seqRect = new RectangleF(currentX, yPosition, columnWidths[0], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(sequenceNumber.ToString(), normalFont, Brushes.Black, seqRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, seqRect.X, seqRect.Y, seqRect.Width, seqRect.Height);
                        }
                        currentX += columnWidths[0];

                        // Data columns
                        string[] dataColumns = new string[] {
                            "หมายเลขใบรับผ้า",
                            "หมายเลขใบเสร็จ",
                            "ชื่อลูกค้า",
                            "เบอร์โทรศัพท์",
                            "วันที่ส่งผ้า",
                            "วันที่ครบกำหนด",
                            "สถานะ",
                            "วันที่มารับ"
                        };

                        for (int j = 0; j < dataColumns.Length; j++)
                        {
                            string cellValue = "";
                            try
                            {
                                if (row.Cells[dataColumns[j]].Value != null && row.Cells[dataColumns[j]].Value != DBNull.Value)
                                {
                                    if (dataColumns[j].Contains("วันที่"))
                                    {
                                        DateTime date = Convert.ToDateTime(row.Cells[dataColumns[j]].Value);
                                        cellValue = date.ToString("dd/MM/yy");
                                    }
                                    else
                                    {
                                        cellValue = row.Cells[dataColumns[j]].Value.ToString();
                                    }
                                }
                            }
                            catch
                            {
                                cellValue = "-";
                            }

                            RectangleF cellRect = new RectangleF(currentX, yPosition, columnWidths[j + 1], rowHeight);
                            using (StringFormat sf = new StringFormat())
                            {
                                sf.Alignment = StringAlignment.Center;
                                sf.LineAlignment = StringAlignment.Center;
                                sf.Trimming = StringTrimming.EllipsisCharacter;

                                if (dataColumns[j] == "สถานะ" && !string.IsNullOrEmpty(cellValue))
                                {
                                    using (SolidBrush statusBrush = new SolidBrush(cellValue == "มารับแล้ว" ? Color.Green : Color.Blue))
                                    {
                                        e.Graphics.DrawString(cellValue, normalFont, statusBrush, cellRect, sf);
                                    }
                                }
                                else
                                {
                                    e.Graphics.DrawString(cellValue, normalFont, Brushes.Black, cellRect, sf);
                                }

                                e.Graphics.DrawRectangle(Pens.Black, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);
                            }
                            currentX += columnWidths[j + 1];
                        }

                        yPosition += rowHeight;
                        sequenceNumber++;
                    }

                    // Page number
                    int totalValidRows = dgvOrders.Rows.Cast<DataGridViewRow>().Count(r =>
                        r.Cells["หมายเลขใบรับผ้า"].Value != null);
                    int totalPages = (int)Math.Ceiling((double)totalValidRows / rowsPerPage);
                    if (totalPages == 0) totalPages = 1;

                    string pageText = $"หน้า {_currentPage + 1} จาก {totalPages}";
                    e.Graphics.DrawString(pageText, smallFont, Brushes.Black,
                        rightMargin - e.Graphics.MeasureString(pageText, smallFont).Width, bottomMargin);

                    // Check if more pages needed
                    if (endRow < dgvOrders.Rows.Count)
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

                // Check for valid data
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

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Files|*.csv|Excel Files|*.xls";
                    sfd.Title = "บันทึกไฟล์ข้อมูล";

                    string dateStr = dtpCreateDate.Value.ToString("yyyy-MM-dd") + "_ถึง_" + dtpCreateDateEnd.Value.ToString("yyyy-MM-dd");
                    sfd.FileName = $"รายงานการมารับผ้า_{dateStr}";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        Cursor = Cursors.WaitCursor;

                        var columnNames = new List<string> {
                            "หมายเลขใบรับผ้า", "หมายเลขใบเสร็จ", "ชื่อลูกค้า", "เบอร์โทรศัพท์",
                            "วันที่ส่งผ้า", "วันที่ครบกำหนด", "สถานะ", "วันที่มารับ"
                        };

                        StringBuilder csv = new StringBuilder();
                        csv.Append("\uFEFF"); // UTF-8 BOM

                        // Header row
                        csv.AppendLine(string.Join(",", columnNames.Select(name => $"\"{name}\"")));

                        // Data rows
                        int validRowCount = 0;
                        foreach (DataGridViewRow row in dgvOrders.Rows)
                        {
                            if (row.Cells["หมายเลขใบรับผ้า"].Value == null)
                                continue;

                            validRowCount++;
                            var rowValues = new List<string>();

                            foreach (string col in columnNames)
                            {
                                string value = "";
                                if (row.Cells[col].Value != null && row.Cells[col].Value != DBNull.Value)
                                {
                                    if (col.Contains("วันที่") && row.Cells[col].Value is DateTime date)
                                    {
                                        value = date.ToString("dd/MM/yyyy HH:mm");
                                    }
                                    else
                                    {
                                        value = row.Cells[col].Value.ToString();
                                    }

                                    if (col == "เบอร์โทรศัพท์" || col == "หมายเลขใบรับผ้า" || col == "หมายเลขใบเสร็จ")
                                    {
                                        value = $"=\"{value}\"";
                                    }

                                    value = value.Replace("\"", "\"\"");
                                }
                                rowValues.Add($"\"{value}\"");
                            }
                            csv.AppendLine(string.Join(",", rowValues));
                        }

                        // Summary
                        csv.AppendLine();
                        csv.AppendLine($"\"จำนวนรายการทั้งหมด {validRowCount} รายการ\"");
                        csv.AppendLine();
                        csv.AppendLine("\"รายงานการมารับผ้า\"");
                        csv.AppendLine($"\"พิมพ์เมื่อ: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}\"");

                        string filterInfo = "ช่วงวันที่: " + dtpCreateDate.Value.ToString("dd/MM/yyyy") + " ถึง " + dtpCreateDateEnd.Value.ToString("dd/MM/yyyy");
                        filterInfo += chkNotPickup.Checked ? ", สถานะ: ยังไม่มารับ" :
                                     chkPickedup.Checked ? ", สถานะ: มารับแล้ว" : ", ทุกสถานะ";
                        csv.AppendLine($"\"กรอง: {filterInfo}\"");

                        System.IO.File.WriteAllText(sfd.FileName, csv.ToString());

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
