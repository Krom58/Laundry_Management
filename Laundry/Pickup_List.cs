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

namespace Laundry_Management.Laundry
{
    public partial class Pickup_List : Form
    {
        private bool _isInitializing = true;
        
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
                        AND CAST(r.ReceiptDate AS DATE) = @TodayDate
                        AND o.OrderStatus = N'ออกใบเสร็จแล้ว' AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
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

            // ตรวจสอบ checkbox สถานะการรับผ้า
            if (chkNotPickup.Checked)
            {
                filters.Add("(r.IsPickedUp IS NULL OR r.IsPickedUp <> N'มารับแล้ว')");
            }
            else if (chkPickedup.Checked)
            {
                filters.Add("r.IsPickedUp = N'มารับแล้ว'");
            }

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

            // เพิ่มเงื่อนไขการค้นหาวันที่เป็นช่วง
            filters.Add("CAST(r.ReceiptDate AS DATE) BETWEEN @StartDate AND @EndDate");
            parameters.Add(new SqlParameter("@StartDate", startDate));
            parameters.Add(new SqlParameter("@EndDate", endDate));

            if (filters.Count > 0)
            {
                query += " AND " + string.Join(" AND ", filters);
            }

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
    }
}
