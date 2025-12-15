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
        private List<PickupOrderDto> _allData = new List<PickupOrderDto>();
        private int _currentPageIndex = 0;
        private const int _pageSize = 25;
        private int _totalPages = 0;
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

        // DTO สำหรับเก็บข้อมูลรายการรับผ้า
        public class PickupOrderDto
        {
            public int OrderID { get; set; }
            public string CustomOrderId { get; set; }
            public int CustomerId { get; set; }
            public string CustomerName { get; set; }
            public string Phone { get; set; }
            public int ReceiptID { get; set; }
            public string CustomReceiptId { get; set; }
            public DateTime OrderDate { get; set; }
            public DateTime PickupDate { get; set; }
            public string IsPickedUp { get; set; }
            public DateTime? CustomerPickupDate { get; set; }
            public string OrderStatus { get; set; }
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
                    o.CustomOrderId, 
                    o.CustomerId,
                    c.FullName, 
                    c.Phone,
                    r.ReceiptID,
                    r.CustomReceiptId,
                    o.OrderDate,
                    o.PickupDate,
                    r.IsPickedUp, 
                    r.CustomerPickupDate,
                    o.OrderStatus
                FROM OrderHeader o
                LEFT JOIN Customer c ON o.CustomerId = c.CustomerID
                INNER JOIN Receipt r ON o.OrderID = r.OrderID
                WHERE (r.IsPickedUp IS NULL OR r.IsPickedUp <> N'มารับแล้ว')
                AND CAST(o.OrderDate AS DATE) = @TodayDate
                AND o.OrderStatus = N'ออกใบเสร็จแล้ว' AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                ORDER BY o.OrderDate ASC, r.ReceiptID ASC
            ";

            var pickupOrders = new List<PickupOrderDto>();

            using (SqlConnection conn = Laundry_Management.Laundry.DBconfig.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TodayDate", DateTime.Today);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pickupOrders.Add(new PickupOrderDto
                            {
                                OrderID = reader.GetInt32(0),
                                CustomOrderId = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                CustomerId = reader.GetInt32(2),
                                CustomerName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                ReceiptID = reader.GetInt32(5),
                                CustomReceiptId = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                OrderDate = reader.GetDateTime(7),
                                PickupDate = reader.GetDateTime(8),
                                IsPickedUp = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                CustomerPickupDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                                OrderStatus = reader.IsDBNull(11) ? "" : reader.GetString(11)
                            });
                        }
                    }
                }
            }

            // เก็บข้อมูลทั้งหมดไว้
            _allData = pickupOrders;

            // คำนวณจำนวนหน้าทั้งหมด
            _totalPages = (int)Math.Ceiling((double)_allData.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;

            // แสดงข้อมูลหน้าปัจจุบัน
            DisplayCurrentPage();

            // อัปเดตสถานะปุ่ม
            UpdatePaginationButtons();
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

            // แก้ไข query ให้ไม่ใช้ alias ภาษาไทย
            var query = @"
                SELECT 
                    o.OrderID,
                    o.CustomOrderId,
                    o.CustomerId,
                    c.FullName,
                    c.Phone,
                    r.ReceiptID,
                    r.CustomReceiptId,
                    o.OrderDate,
                    o.PickupDate,
                    r.IsPickedUp,
                    r.CustomerPickupDate,
                    o.OrderStatus
                FROM OrderHeader o
                LEFT JOIN Customer c ON o.CustomerId = c.CustomerID
                LEFT JOIN Receipt r ON o.OrderID = r.OrderID AND r.ReceiptStatus <> N'ยกเลิกการพิมพ์'
                WHERE 1=1
            ";

            var filters = new List<string>();
            var parameters = new List<SqlParameter>();
            string orderByClause = "";

            // ตรวจสอบ checkbox สถานะการรับผ้า
            if (chkNotPickup.Checked)
            {
                // เปลี่ยนเป็น INNER JOIN เพราะต้องมีใบเสร็จ
                query = query.Replace("LEFT JOIN Receipt r", "INNER JOIN Receipt r");

                filters.Add(@"(
        (o.OrderStatus = N'ดำเนินการสำเร็จ') 
        OR 
        (o.OrderStatus = N'ออกใบเสร็จแล้ว' 
         AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
         AND (r.IsPickedUp IS NULL OR r.IsPickedUp <> N'มารับแล้ว'))
    )");

                filters.Add("CAST(o.OrderDate AS DATE) BETWEEN @StartDate AND @EndDate");
                orderByClause = " ORDER BY o.OrderDate ASC, ISNULL(r.ReceiptID, 0) ASC";
            }
            else if (chkPickedup.Checked)
            {
                // เปลี่ยนเป็น INNER JOIN เพราะต้องมีใบเสร็จ
                query = query.Replace("LEFT JOIN Receipt r", "INNER JOIN Receipt r");

                filters.Add("o.OrderStatus = N'ออกใบเสร็จแล้ว'");
                filters.Add("r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'");
                filters.Add("r.IsPickedUp = N'มารับแล้ว'");
                filters.Add("CAST(r.CustomerPickupDate AS DATE) BETWEEN @StartDate AND @EndDate");
                orderByClause = " ORDER BY r.CustomerPickupDate ASC, r.ReceiptID ASC";
            }

            parameters.Add(new SqlParameter("@StartDate", startDate));
            parameters.Add(new SqlParameter("@EndDate", endDate));

            if (!string.IsNullOrEmpty(orderId))
            {
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
                        filters.Add("(o.CustomOrderId LIKE @CustomID OR r.CustomReceiptId LIKE @CustomID)");
                        parameters.Add(new SqlParameter("@CustomID", "%" + orderId + "%"));
                    }
                }
                else
                {
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

            query += orderByClause;

            using (SqlConnection conn = Laundry_Management.Laundry.DBconfig.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                conn.Open();

                var pickupOrders = new List<PickupOrderDto>();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // ตรวจสอบว่า ReceiptID เป็น NULL หรือไม่ ถ้าเป็น NULL ให้ข้าม
                        if (reader.IsDBNull(5)) // ReceiptID อยู่ที่ index 5
                            continue;

                        pickupOrders.Add(new PickupOrderDto
                        {
                            OrderID = reader.GetInt32(0),
                            CustomOrderId = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            CustomerId = reader.GetInt32(2),
                            CustomerName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            ReceiptID = reader.GetInt32(5), // ตอนนี้ปลอดภัยแล้วเพราะตรวจสอบแล้ว
                            CustomReceiptId = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            OrderDate = reader.GetDateTime(7),
                            PickupDate = reader.GetDateTime(8),
                            IsPickedUp = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            CustomerPickupDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                            OrderStatus = reader.IsDBNull(11) ? "" : reader.GetString(11)
                        });
                    }
                }

                // เก็บข้อมูลทั้งหมดไว้
                _allData = pickupOrders;

                // คำนวณจำนวนหน้าทั้งหมด
                _currentPageIndex = 0;
                _totalPages = (int)Math.Ceiling((double)_allData.Count / _pageSize);
                if (_totalPages == 0) _totalPages = 1;

                // แสดงข้อมูลหน้าปัจจุบัน
                DisplayCurrentPage();

                // อัปเดตสถานะปุ่ม
                UpdatePaginationButtons();
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
                // เปลี่ยนจากการตรวจสอบ dgvOrders.Rows เป็น _allData
                if (_allData == null || _allData.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะพิมพ์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                    // Calculate row range for current page จาก _allData
                    int startRow = _currentPage * rowsPerPage;
                    int endRow = Math.Min(startRow + rowsPerPage, _allData.Count);

                    // Print data rows จาก _allData
                    int sequenceNumber = startRow + 1;
                    for (int i = startRow; i < endRow; i++)
                    {
                        PickupOrderDto dataRow = _allData[i];

                        currentX = leftMargin;

                        // Sequence number
                        RectangleF seqRect = new RectangleF(currentX, yPosition, columnWidths[0], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(sequenceNumber.ToString(), normalFont, Brushes.Black, seqRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, seqRect.X, seqRect.Y, seqRect.Width, seqRect.Height);
                        }
                        currentX += columnWidths[0];

                        // หมายเลขใบรับผ้า
                        RectangleF orderIdRect = new RectangleF(currentX, yPosition, columnWidths[1], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                        {
                            e.Graphics.DrawString(dataRow.CustomOrderId ?? "", normalFont, Brushes.Black, orderIdRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, orderIdRect.X, orderIdRect.Y, orderIdRect.Width, orderIdRect.Height);
                        }
                        currentX += columnWidths[1];

                        // หมายเลขใบเสร็จ
                        RectangleF receiptIdRect = new RectangleF(currentX, yPosition, columnWidths[2], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                        {
                            e.Graphics.DrawString(dataRow.CustomReceiptId ?? "", normalFont, Brushes.Black, receiptIdRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, receiptIdRect.X, receiptIdRect.Y, receiptIdRect.Width, receiptIdRect.Height);
                        }
                        currentX += columnWidths[2];

                        // ชื่อลูกค้า
                        RectangleF nameRect = new RectangleF(currentX, yPosition, columnWidths[3], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                        {
                            e.Graphics.DrawString(dataRow.CustomerName ?? "", normalFont, Brushes.Black, nameRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, nameRect.X, nameRect.Y, nameRect.Width, nameRect.Height);
                        }
                        currentX += columnWidths[3];

                        // เบอร์โทร
                        RectangleF phoneRect = new RectangleF(currentX, yPosition, columnWidths[4], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                        {
                            e.Graphics.DrawString(dataRow.Phone ?? "", normalFont, Brushes.Black, phoneRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, phoneRect.X, phoneRect.Y, phoneRect.Width, phoneRect.Height);
                        }
                        currentX += columnWidths[4];

                        // วันที่ส่งผ้า
                        RectangleF orderDateRect = new RectangleF(currentX, yPosition, columnWidths[5], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            string orderDateStr = dataRow.OrderDate != DateTime.MinValue ? dataRow.OrderDate.ToString("dd/MM/yy") : "-";
                            e.Graphics.DrawString(orderDateStr, normalFont, Brushes.Black, orderDateRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, orderDateRect.X, orderDateRect.Y, orderDateRect.Width, orderDateRect.Height);
                        }
                        currentX += columnWidths[5];

                        // วันที่ครบกำหนด
                        RectangleF pickupDateRect = new RectangleF(currentX, yPosition, columnWidths[6], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            string pickupDateStr = dataRow.PickupDate != DateTime.MinValue ? dataRow.PickupDate.ToString("dd/MM/yy") : "-";
                            e.Graphics.DrawString(pickupDateStr, normalFont, Brushes.Black, pickupDateRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, pickupDateRect.X, pickupDateRect.Y, pickupDateRect.Width, pickupDateRect.Height);
                        }
                        currentX += columnWidths[6];

                        // สถานะ
                        RectangleF statusRect = new RectangleF(currentX, yPosition, columnWidths[7], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            string statusValue = dataRow.IsPickedUp ?? "";
                            Brush statusBrush = statusValue == "มารับแล้ว" ? Brushes.Green : Brushes.Blue;
                            e.Graphics.DrawString(statusValue, normalFont, statusBrush, statusRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, statusRect.X, statusRect.Y, statusRect.Width, statusRect.Height);
                        }
                        currentX += columnWidths[7];

                        // วันที่มารับ
                        RectangleF customerPickupRect = new RectangleF(currentX, yPosition, columnWidths[8], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            string pickupStr = dataRow.CustomerPickupDate.HasValue ? dataRow.CustomerPickupDate.Value.ToString("dd/MM/yy") : "-";
                            e.Graphics.DrawString(pickupStr, normalFont, Brushes.Black, customerPickupRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, customerPickupRect.X, customerPickupRect.Y, customerPickupRect.Width, customerPickupRect.Height);
                        }

                        yPosition += rowHeight;
                        sequenceNumber++;
                    }

                    // Page number - ใช้ _allData.Count
                    int totalPages = (int)Math.Ceiling((double)_allData.Count / rowsPerPage);
                    if (totalPages == 0) totalPages = 1;

                    string pageText = $"หน้า {_currentPage + 1} จาก {totalPages}";
                    e.Graphics.DrawString(pageText, smallFont, Brushes.Black,
                        rightMargin - e.Graphics.MeasureString(pageText, smallFont).Width, bottomMargin);

                    // Check if more pages needed
                    if (endRow < _allData.Count)
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
                // เปลี่ยนจากการตรวจสอบ dgvOrders.Rows เป็น _allData
                if (_allData == null || _allData.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะส่งออก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    "ลำดับ", "หมายเลขใบรับผ้า", "หมายเลขใบเสร็จ", "ชื่อลูกค้า", "เบอร์โทรศัพท์",
                    "วันที่ส่งผ้า", "วันที่ครบกำหนด", "สถานะ", "วันที่มารับ"
                };

                        StringBuilder csv = new StringBuilder();
                        csv.Append("\uFEFF"); // UTF-8 BOM

                        // Header row
                        csv.AppendLine(string.Join(",", columnNames.Select(name => $"\"{name}\"")));

                        // Data rows จาก _allData
                        for (int i = 0; i < _allData.Count; i++)
                        {
                            var dataRow = _allData[i];
                            var rowValues = new List<string>();

                            // ลำดับ
                            rowValues.Add($"\"{i + 1}\"");

                            // หมายเลขใบรับผ้า
                            rowValues.Add($"=\"{dataRow.CustomOrderId ?? ""}\"");

                            // หมายเลขใบเสร็จ
                            rowValues.Add($"=\"{dataRow.CustomReceiptId ?? ""}\"");

                            // ชื่อลูกค้า
                            string customerName = (dataRow.CustomerName ?? "").Replace("\"", "\"\"");
                            rowValues.Add($"\"{customerName}\"");

                            // เบอร์โทรศัพท์
                            rowValues.Add($"=\"{dataRow.Phone ?? ""}\"");

                            // วันที่ส่งผ้า
                            string orderDate = dataRow.OrderDate != DateTime.MinValue
                                ? dataRow.OrderDate.ToString("dd/MM/yyyy HH:mm")
                                : "";
                            rowValues.Add($"\"{orderDate}\"");

                            // วันที่ครบกำหนด
                            string pickupDate = dataRow.PickupDate != DateTime.MinValue
                                ? dataRow.PickupDate.ToString("dd/MM/yyyy HH:mm")
                                : "";
                            rowValues.Add($"\"{pickupDate}\"");

                            // สถานะ
                            string status = (dataRow.IsPickedUp ?? "").Replace("\"", "\"\"");
                            rowValues.Add($"\"{status}\"");

                            // วันที่มารับ
                            string customerPickupDate = dataRow.CustomerPickupDate.HasValue
                                ? dataRow.CustomerPickupDate.Value.ToString("dd/MM/yyyy HH:mm")
                                : "";
                            rowValues.Add($"\"{customerPickupDate}\"");

                            csv.AppendLine(string.Join(",", rowValues));
                        }

                        // Summary
                        csv.AppendLine();
                        csv.AppendLine($"\"จำนวนรายการทั้งหมด {_allData.Count} รายการ\"");
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
        private void DisplayCurrentPage()
        {
            if (_allData == null || _allData.Count == 0)
            {
                dgvOrders.DataSource = null;
                return;
            }

            // ดึงข้อมูลเฉพาะหน้าปัจจุบัน
            var pageData = _allData
                .Skip(_currentPageIndex * _pageSize)
                .Take(_pageSize)
                .Select(x => new
                {
                    OrderID = x.OrderID,
                    หมายเลขใบรับผ้า = x.CustomOrderId,
                    CustomerId = x.CustomerId,
                    ชื่อลูกค้า = x.CustomerName,
                    เบอร์โทรศัพท์ = x.Phone,
                    ReceiptID = x.ReceiptID,
                    หมายเลขใบเสร็จ = x.CustomReceiptId,
                    วันที่ส่งผ้า = x.OrderDate,
                    วันที่ครบกำหนด = x.PickupDate,
                    สถานะ = x.IsPickedUp,
                    วันที่มารับ = x.CustomerPickupDate,
                    OrderStatus = x.OrderStatus
                })
                .ToList();

            // ผูกข้อมูลกับ DataGridView
            dgvOrders.DataSource = pageData;

            // ซ่อนคอลัมน์ที่ไม่จำเป็น
            if (dgvOrders.Columns["OrderID"] != null)
                dgvOrders.Columns["OrderID"].Visible = false;
            if (dgvOrders.Columns["ReceiptID"] != null)
                dgvOrders.Columns["ReceiptID"].Visible = false;
            if (dgvOrders.Columns["CustomerId"] != null)
                dgvOrders.Columns["CustomerId"].Visible = false;
            if (dgvOrders.Columns["OrderStatus"] != null)
                dgvOrders.Columns["OrderStatus"].Visible = false;

            // อัปเดตข้อความแสดงหน้า
            int startRecord = (_currentPageIndex * _pageSize) + 1;
            int endRecord = Math.Min((_currentPageIndex + 1) * _pageSize, _allData.Count);
            this.Text = $"รายการมารับผ้า - จำนวน {_allData.Count} รายการ (แสดง {startRecord}-{endRecord})";
        }

        private void UpdatePaginationButtons()
        {
            // เปิด/ปิดปุ่มตามหน้าปัจจุบัน
            btnFirstPage.Enabled = _currentPageIndex > 0;
            btnPreviousPage.Enabled = _currentPageIndex > 0;
            btnNextPage.Enabled = _currentPageIndex < _totalPages - 1;
            btnLastPage.Enabled = _currentPageIndex < _totalPages - 1;
        }

        private void NavigateToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _totalPages)
                return;

            _currentPageIndex = pageIndex;
            DisplayCurrentPage();
            UpdatePaginationButtons();
        }
        private void btnFirstPage_Click(object sender, EventArgs e)
        {
            NavigateToPage(0);
        }

        private void btnPreviousPage_Click(object sender, EventArgs e)
        {
            NavigateToPage(_currentPageIndex - 1);
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            NavigateToPage(_currentPageIndex + 1);
        }

        private void btnLastPage_Click(object sender, EventArgs e)
        {
            NavigateToPage(_totalPages - 1);
        }
    }
}
