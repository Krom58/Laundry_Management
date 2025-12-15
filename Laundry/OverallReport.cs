using OfficeOpenXml.FormulaParsing.Excel.Functions.Finance;
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
    public partial class OverallReport : Form
    {
        // ตัวแปรสำหรับ Pagination (แสดงข้อมูลบนหน้าจอ)
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalRecords = 0;
        private int _totalPages = 0;
        private DataTable _allData;

        // ตัวแปรสำหรับ Printing (เปลี่ยนชื่อเพื่อไม่ให้ซ้ำ)
        private bool _isPrintSuccessful = false;
        private string _printErrorMessage = "";
        private int _printCurrentPage = 0;
        public OverallReport()
        {
            InitializeComponent();

            // เพิ่ม event handlers สำหรับการกด Enter ในช่องค้นหา
            txtCustomerFilter.KeyPress += TxtSearch_KeyPress;
            textBox1.KeyPress += TxtSearch_KeyPress;

            // กำหนดค่าเริ่มต้นของ DateTimePicker
            dtpCreateDate.Value = DateTime.Today.AddMonths(-1);
            dtpCreateDateEnd.Value = DateTime.Today;

            // เปลี่ยนรูปแบบให้แสดงวันที่แบบเต็ม
            dtpCreateDate.Format = DateTimePickerFormat.Custom;
            dtpCreateDate.CustomFormat = "dd/MM/yyyy";
            dtpCreateDateEnd.Format = DateTimePickerFormat.Custom;
            dtpCreateDateEnd.CustomFormat = "dd/MM/yyyy";

            // Register event handlers
            this.Load += OverallReport_Load;
            dtpCreateDate.ValueChanged += DateFilter_ValueChanged;
            dtpCreateDateEnd.ValueChanged += DateFilter_ValueChanged;

            // เพิ่ม event handler สำหรับ checkbox
            chkSimple.CheckedChanged += ChkReportMode_CheckedChanged;
            chkDetail.CheckedChanged += ChkReportMode_CheckedChanged;

            // ตั้งค่าเริ่มต้นให้ chkSimple ถูกเลือก
            chkSimple.Checked = true;
            btnFirstPage.Click += btnFirstPage_Click;
            btnPreviousPage.Click += btnPreviousPage_Click;
            btnNextPage.Click += btnNextPage_Click;
            btnLastPage.Click += btnLastPage_Click;
        }

        private void ChkReportMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox changedCheckbox = sender as CheckBox;

            if (changedCheckbox == null)
                return;

            if (changedCheckbox.Checked)
            {
                if (changedCheckbox == chkSimple)
                    chkDetail.Checked = false;
                else if (changedCheckbox == chkDetail)
                    chkSimple.Checked = false;

                // เพิ่มบรรทัดนี้
                _currentPage = 1;
                LoadReportData();
            }
            else
            {
                if (!chkSimple.Checked && !chkDetail.Checked)
                    changedCheckbox.Checked = true;
            }
        }

        private void DateFilter_ValueChanged(object sender, EventArgs e)
        {
            if (dtpCreateDate.Value > dtpCreateDateEnd.Value)
            {
                MessageBox.Show("วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (sender == dtpCreateDate)
                    dtpCreateDate.Value = dtpCreateDateEnd.Value;
                else
                    dtpCreateDateEnd.Value = dtpCreateDate.Value;

                return;
            }

            // เพิ่มบรรทัดนี้
            _currentPage = 1;
            LoadReportData();
        }

        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ตรวจสอบว่ากด Enter หรือไม่ (รหัส ASCII 13)
            if (e.KeyChar == (char)Keys.Enter)
            {
                // ป้องกันเสียง beep
                e.Handled = true;

                // เรียกฟังก์ชันค้นหา
                btnSearch_Click(sender, EventArgs.Empty);
            }
        }

        private void OverallReport_Load(object sender, EventArgs e)
        {
            InitializeDataGridView();
            LoadReportData();
        }

        private void InitializeDataGridView()
        {
            // Configure the DataGridView
            dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvCustomers.AllowUserToAddRows = false;
            dgvCustomers.AllowUserToDeleteRows = false;
            dgvCustomers.AllowUserToOrderColumns = true;
            dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCustomers.MultiSelect = false;
            dgvCustomers.ReadOnly = true;

            // Enhanced settings for Thai text display
            dgvCustomers.DefaultCellStyle.Font = new Font("Angsana New", 22);
            dgvCustomers.ColumnHeadersDefaultCellStyle.Font = new Font("Angsana New", 22, FontStyle.Bold);

            // Enable text wrapping
            dgvCustomers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvCustomers.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // Increase row height
            dgvCustomers.RowTemplate.Height = 40;

            // Allow columns to be adjusted by users
            dgvCustomers.AllowUserToResizeColumns = true;

            // Set clean selection style
            dgvCustomers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(205, 230, 247);
            dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private void LoadReportData()
        {
            Cursor = Cursors.WaitCursor;

            try
            {
                using (SqlConnection connection = DBconfig.GetConnection())
                {
                    connection.Open();

                    DateTime startDate = dtpCreateDate.Value.Date;
                    DateTime endDate = dtpCreateDateEnd.Value.Date.AddDays(1).AddSeconds(-1);

                    DataTable reportData;

                    if (chkSimple.Checked)
                    {
                        reportData = LoadSimpleReport(connection, startDate, endDate);
                    }
                    else if (chkDetail.Checked)
                    {
                        reportData = LoadDetailReport(connection, startDate, endDate);
                    }
                    else
                    {
                        reportData = new DataTable();
                    }

                    // เพิ่มส่วนนี้
                    _allData = reportData.Copy();
                    _totalRecords = reportData.Rows.Count;
                    _totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                    if (_totalPages == 0) _totalPages = 1;

                    DisplayReportData(reportData);
                    UpdatePaginationButtons(); // เพิ่มบรรทัดนี้
                }

                string modeText = chkSimple.Checked ? "(แบบสรุป)" : "(แบบรายละเอียด)";
                this.Text = $"รายงานการใช้งานของลูกค้า {modeText} - {dtpCreateDate.Value:dd/MM/yyyy} ถึง {dtpCreateDateEnd.Value:dd/MM/yyyy}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดข้อมูล: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private DataTable LoadSimpleReport(SqlConnection connection, DateTime startDate, DateTime endDate)
        {
            // Query เดิมของคุณ
            string query = @"
                SELECT
                    c.CustomerID,
                    c.FullName AS 'ชื่อลูกค้า',
                    c.Phone AS 'เบอร์โทร',
                    ISNULL(oc.OrderCount, 0) AS 'รวมการใช้บริการทั้งหมด',
                    ISNULL(uc.UnpaidAmount, 0) AS 'มาใช้บริการ(ยังไม่ได้ชำระเงิน)',
                    ISNULL(pc.PaidAmount, 0) AS 'มาใช้บริการ(ชำระเงินแล้ว)',
                    ISNULL(uc.UnpaidAmount, 0) + ISNULL(pc.PaidAmount, 0) AS 'ยอดรวมทั้งหมด'
                FROM Customer c
                LEFT JOIN (
                    SELECT o.CustomerId, COUNT(DISTINCT o.OrderID) AS OrderCount
                    FROM OrderHeader o
                    WHERE COALESCE(o.OrderDate, o.PickupDate) BETWEEN @StartDate AND @EndDate
                      AND (o.OrderStatus = N'ดำเนินการสำเร็จ' OR o.OrderStatus = N'ออกใบเสร็จแล้ว')
                    GROUP BY o.CustomerId
                ) oc ON c.CustomerID = oc.CustomerId
                LEFT JOIN (
                    SELECT o.CustomerId, SUM(o.GrandTotalPrice) AS UnpaidAmount
                    FROM OrderHeader o
                    WHERE COALESCE(o.OrderDate, o.PickupDate) BETWEEN @StartDate AND @EndDate
                      AND o.OrderStatus = N'ดำเนินการสำเร็จ'
                    GROUP BY o.CustomerId
                ) uc ON c.CustomerID = uc.CustomerId
                LEFT JOIN (
                    SELECT oh.CustomerId, SUM(r.TotalAfterDiscount) AS PaidAmount
                    FROM Receipt r
                    INNER JOIN OrderHeader oh ON r.OrderID = oh.OrderID
                    WHERE r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                      AND r.ReceiptDate BETWEEN @StartDate AND @EndDate
                    GROUP BY oh.CustomerId
                ) pc ON c.CustomerID = pc.CustomerId
                WHERE (
                    oc.OrderCount IS NOT NULL AND oc.OrderCount > 0
                ) OR (
                    uc.UnpaidAmount IS NOT NULL AND uc.UnpaidAmount > 0
                ) OR (
                    pc.PaidAmount IS NOT NULL AND pc.PaidAmount > 0
                )
                ORDER BY c.FullName;
            ";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable reportData = new DataTable();
                adapter.Fill(reportData);

                return reportData;
            }
        }

        private DataTable LoadDetailReport(SqlConnection connection, DateTime startDate, DateTime endDate)
        {
            // Query แบบรายละเอียดที่แยกตามชื่อรายการและนับจำนวนครั้ง
            string query = @"
        WITH ServiceTypeData AS (
            SELECT 
                c.CustomerID,
                c.FullName,
                c.Phone,
                oi.ItemName,
                -- รวมการใช้บริการทั้งหมด (นับจำนวน OrderItem ที่ไม่ถูกยกเลิก)
                COUNT(CASE 
                    WHEN oh.OrderStatus = N'ดำเนินการสำเร็จ' OR oh.OrderStatus = N'ออกใบเสร็จแล้ว' 
                    THEN oi.OrderItemID 
                END) AS TotalServiceCount,
                -- จำนวนครั้งที่ยังไม่ได้ชำระเงิน
                COUNT(CASE 
                    WHEN oh.OrderStatus = N'ดำเนินการสำเร็จ' 
                    THEN oi.OrderItemID 
                END) AS UnpaidCount,
                -- จำนวนครั้งที่ชำระเงินแล้ว
                COUNT(CASE 
                    WHEN r.ReceiptID IS NOT NULL AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                    THEN oi.OrderItemID 
                END) AS PaidCount,
                -- ยอดรวมที่ยังไม่ได้ชำระเงิน (OrderStatus = 'ดำเนินการสำเร็จ')
                ISNULL(SUM(CASE 
                    WHEN oh.OrderStatus = N'ดำเนินการสำเร็จ' 
                    THEN oi.TotalAmount 
                    ELSE 0 
                END), 0) AS UnpaidAmount,
                -- ยอดรวมที่ชำระเงินแล้ว (มี Receipt และ ReceiptStatus = 'พิมพ์เรียบร้อยแล้ว')
                ISNULL(SUM(CASE 
                    WHEN r.ReceiptID IS NOT NULL AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                    THEN oi.TotalAmount 
                    ELSE 0 
                END), 0) AS PaidAmount
            FROM Customer c
            INNER JOIN OrderHeader oh ON c.CustomerID = oh.CustomerId
            INNER JOIN OrderItem oi ON oh.OrderID = oi.OrderID
            LEFT JOIN Receipt r ON oh.OrderID = r.OrderID 
                AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                AND r.ReceiptDate BETWEEN @StartDate AND @EndDate
            WHERE COALESCE(oh.OrderDate, oh.PickupDate) BETWEEN @StartDate AND @EndDate
              AND (oh.OrderStatus = N'ดำเนินการสำเร็จ' OR oh.OrderStatus = N'ออกใบเสร็จแล้ว')
              AND oi.IsCanceled = 0
            GROUP BY c.CustomerID, c.FullName, c.Phone, oi.ItemName
        )
        SELECT 
            CustomerID,
            FullName AS 'ชื่อลูกค้า',
            Phone AS 'เบอร์โทร',
            ItemName AS 'ชื่อรายการ',
            TotalServiceCount AS 'รวมการใช้บริการทั้งหมด',
            UnpaidCount AS 'จำนวนครั้งยังไม่จ่าย',
            PaidCount AS 'จำนวนครั้งจ่ายแล้ว',
            UnpaidAmount AS 'ยอดยังไม่ชำระ',
            PaidAmount AS 'ยอดชำระแล้ว',
            (UnpaidAmount + PaidAmount) AS 'ยอดรวม'
        FROM ServiceTypeData
        WHERE TotalServiceCount > 0 OR UnpaidAmount > 0 OR PaidAmount > 0
        ORDER BY FullName, ItemName;
    ";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable reportData = new DataTable();
                adapter.Fill(reportData);

                return reportData;
            }
        }

        private void DisplayReportData(DataTable data)
        {
            if (data == null || data.Rows.Count == 0)
            {
                dgvCustomers.DataSource = null;
                MessageBox.Show("ไม่พบข้อมูลสำหรับช่วงวันที่ที่เลือก", "ข้อมูล",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // สร้าง DataTable สำหรับแสดงข้อมูลในหน้าปัจจุบัน
            DataTable displayData = data.Clone();

            // คำนวณ index เริ่มต้นและสิ้นสุด
            int startIndex = (_currentPage - 1) * _pageSize;
            int endIndex = Math.Min(startIndex + _pageSize, data.Rows.Count);

            // คัดลอกข้อมูลสำหรับหน้าปัจจุบัน
            for (int i = startIndex; i < endIndex; i++)
            {
                displayData.ImportRow(data.Rows[i]);
            }

            // คำนวณ totals จากข้อมูลที่แสดงในหน้าปัจจุบัน
            decimal grandTotal = 0m;
            decimal totalUnpaidAmount = 0m;
            decimal totalPaidAmount = 0m;
            int totalOrders = 0;

            foreach (DataRow row in displayData.Rows)
            {
                decimal amt = 0m;
                decimal unpaid = 0m;
                decimal paid = 0m;
                int orders = 0;

                if (row.Table.Columns.Contains("ยอดรวมทั้งหมด") && row["ยอดรวมทั้งหมด"] != DBNull.Value)
                    decimal.TryParse(row["ยอดรวมทั้งหมด"].ToString(), out amt);

                if (row.Table.Columns.Contains("มาใช้บริการ(ยังไม่ได้ชำระเงิน)") && row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"] != DBNull.Value)
                    decimal.TryParse(row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].ToString(), out unpaid);

                if (row.Table.Columns.Contains("มาใช้บริการ(ชำระเงินแล้ว)") && row["มาใช้บริการ(ชำระเงินแล้ว)"] != DBNull.Value)
                    decimal.TryParse(row["มาใช้บริการ(ชำระเงินแล้ว)"].ToString(), out paid);

                if (row.Table.Columns.Contains("รวมการใช้บริการทั้งหมด") && row["รวมการใช้บริการทั้งหมด"] != DBNull.Value)
                    int.TryParse(row["รวมการใช้บริการทั้งหมด"].ToString(), out orders);

                grandTotal += amt;
                totalUnpaidAmount += unpaid;
                totalPaidAmount += paid;
                totalOrders += orders;
            }

            // เพิ่มคอลัมน์ IsSummary
            if (!displayData.Columns.Contains("IsSummary"))
                displayData.Columns.Add("IsSummary", typeof(bool));

            foreach (DataRow r in displayData.Rows)
                r["IsSummary"] = false;

            // สร้าง summary row แสดงว่าเป็นหน้าไหน
            DataRow summaryRow = displayData.NewRow();
            if (displayData.Columns.Contains("CustomerID")) summaryRow["CustomerID"] = DBNull.Value;
            if (displayData.Columns.Contains("ชื่อลูกค้า")) summaryRow["ชื่อลูกค้า"] = $"รวมหน้านี้ (แสดง {displayData.Rows.Count} รายการ  )";
            if (displayData.Columns.Contains("เบอร์โทร")) summaryRow["เบอร์โทร"] = "";
            if (displayData.Columns.Contains("รวมการใช้บริการทั้งหมด")) summaryRow["รวมการใช้บริการทั้งหมด"] = totalOrders;
            if (displayData.Columns.Contains("มาใช้บริการ(ยังไม่ได้ชำระเงิน)")) summaryRow["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"] = totalUnpaidAmount;
            if (displayData.Columns.Contains("มาใช้บริการ(ชำระเงินแล้ว)")) summaryRow["มาใช้บริการ(ชำระเงินแล้ว)"] = totalPaidAmount;
            if (displayData.Columns.Contains("ยอดรวมทั้งหมด")) summaryRow["ยอดรวมทั้งหมด"] = grandTotal;
            summaryRow["IsSummary"] = true;
            displayData.Rows.Add(summaryRow);

            dgvCustomers.DataSource = displayData;

            // ตั้งค่าคอลัมน์ (โค้ดเดิมยังคงเหมือนเดิม)
            if (dgvCustomers.Columns["IsSummary"] != null)
                dgvCustomers.Columns["IsSummary"].Visible = false;

            if (dgvCustomers.Columns["CustomerID"] != null)
                dgvCustomers.Columns["CustomerID"].Visible = false;

            int displayIndex = 0;
            if (dgvCustomers.Columns["ชื่อลูกค้า"] != null)
            {
                dgvCustomers.Columns["ชื่อลูกค้า"].DisplayIndex = displayIndex++;
                dgvCustomers.Columns["ชื่อลูกค้า"].MinimumWidth = 250;
            }

            if (dgvCustomers.Columns["เบอร์โทร"] != null)
            {
                dgvCustomers.Columns["เบอร์โทร"].DisplayIndex = displayIndex++;
                dgvCustomers.Columns["เบอร์โทร"].MinimumWidth = 150;
            }

            if (dgvCustomers.Columns["รวมการใช้บริการทั้งหมด"] != null)
            {
                dgvCustomers.Columns["รวมการใช้บริการทั้งหมด"].DisplayIndex = displayIndex++;
                dgvCustomers.Columns["รวมการใช้บริการทั้งหมด"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvCustomers.Columns["รวมการใช้บริการทั้งหมด"].DefaultCellStyle.Format = "N0";
                dgvCustomers.Columns["รวมการใช้บริการทั้งหมด"].MinimumWidth = 200;
            }

            if (dgvCustomers.Columns["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"] != null)
            {
                dgvCustomers.Columns["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].DisplayIndex = displayIndex++;
                dgvCustomers.Columns["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvCustomers.Columns["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].DefaultCellStyle.Format = "N2";
                dgvCustomers.Columns["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].DefaultCellStyle.Padding = new Padding(0, 0, 30, 0);
                dgvCustomers.Columns["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].MinimumWidth = 180;
            }

            if (dgvCustomers.Columns["มาใช้บริการ(ชำระเงินแล้ว)"] != null)
            {
                dgvCustomers.Columns["มาใช้บริการ(ชำระเงินแล้ว)"].DisplayIndex = displayIndex++;
                dgvCustomers.Columns["มาใช้บริการ(ชำระเงินแล้ว)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvCustomers.Columns["มาใช้บริการ(ชำระเงินแล้ว)"].DefaultCellStyle.Format = "N2";
                dgvCustomers.Columns["มาใช้บริการ(ชำระเงินแล้ว)"].DefaultCellStyle.Padding = new Padding(0, 0, 30, 0);
                dgvCustomers.Columns["มาใช้บริการ(ชำระเงินแล้ว)"].MinimumWidth = 180;
            }

            if (dgvCustomers.Columns["ยอดรวมทั้งหมด"] != null)
            {
                dgvCustomers.Columns["ยอดรวมทั้งหมด"].DisplayIndex = displayIndex++;
                dgvCustomers.Columns["ยอดรวมทั้งหมด"].DefaultCellStyle.Format = "N2";
                dgvCustomers.Columns["ยอดรวมทั้งหมด"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvCustomers.Columns["ยอดรวมทั้งหมด"].DefaultCellStyle.Padding = new Padding(0, 0, 30, 0);
                dgvCustomers.Columns["ยอดรวมทั้งหมด"].MinimumWidth = 150;
            }

            dgvCustomers.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            if (dgvCustomers.Rows.Count > 0)
            {
                int lastIndex = dgvCustomers.Rows.Count - 1;
                var lastRow = dgvCustomers.Rows[lastIndex];
                lastRow.DefaultCellStyle.BackColor = Color.LightGray;
                lastRow.DefaultCellStyle.Font = new Font(dgvCustomers.Font, FontStyle.Bold);
                lastRow.DefaultCellStyle.ForeColor = Color.Black;
            }

            // **แก้ไขส่วนนี้: คำนวณยอดรวมทั้งหมดจาก _allData แทน data**
            decimal allGrandTotal = 0m;
            decimal allTotalUnpaidAmount = 0m;
            decimal allTotalPaidAmount = 0m;
            int allTotalOrders = 0;

            // ใช้ _allData แทน data เพื่อให้แน่ใจว่าคำนวณจากข้อมูลทั้งหมด
            DataTable sourceData = _allData ?? data;

            foreach (DataRow row in sourceData.Rows)
            {
                decimal amt = 0m;
                decimal unpaid = 0m;
                decimal paid = 0m;
                int orders = 0;

                if (row.Table.Columns.Contains("ยอดรวมทั้งหมด") && row["ยอดรวมทั้งหมด"] != DBNull.Value)
                    decimal.TryParse(row["ยอดรวมทั้งหมด"].ToString(), out amt);

                if (row.Table.Columns.Contains("มาใช้บริการ(ยังไม่ได้ชำระเงิน)") && row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"] != DBNull.Value)
                    decimal.TryParse(row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].ToString(), out unpaid);

                if (row.Table.Columns.Contains("มาใช้บริการ(ชำระเงินแล้ว)") && row["มาใช้บริการ(ชำระเงินแล้ว)"] != DBNull.Value)
                    decimal.TryParse(row["มาใช้บริการ(ชำระเงินแล้ว)"].ToString(), out paid);

                if (row.Table.Columns.Contains("รวมการใช้บริการทั้งหมด") && row["รวมการใช้บริการทั้งหมด"] != DBNull.Value)
                    int.TryParse(row["รวมการใช้บริการทั้งหมด"].ToString(), out orders);

                allGrandTotal += amt;
                allTotalUnpaidAmount += unpaid;
                allTotalPaidAmount += paid;
                allTotalOrders += orders;
            }

            // **แก้ไขส่วนนี้: ตั้งค่า Title ใหม่ทั้งหมดแทนการ append**
            string modeText = chkSimple.Checked ? "(แบบสรุป)" : "(แบบรายละเอียด)";
            this.Text = $"รายงานการใช้งานของลูกค้า {modeText} - {dtpCreateDate.Value:dd/MM/yyyy} ถึง {dtpCreateDateEnd.Value:dd/MM/yyyy} - จำนวน {sourceData.Rows.Count} ลูกค้า | ใช้บริการ {allTotalOrders} ครั้ง | ยังไม่ชำระ {allTotalUnpaidAmount:N2} บาท | ชำระแล้ว {allTotalPaidAmount:N2} บาท | ยอดรวมทั้งหมด {allGrandTotal:N2} บาท";
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvCustomers.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะพิมพ์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check for valid data before printing
                bool hasValidRows = false;
                foreach (DataGridViewRow row in dgvCustomers.Rows)
                {
                    // Check if not summary row
                    if (dgvCustomers.Columns["IsSummary"] != null &&
                        row.Cells["IsSummary"].Value != null &&
                        row.Cells["IsSummary"].Value is bool &&
                        (bool)row.Cells["IsSummary"].Value == false)
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
                printDoc.DocumentName = "รายงานการใช้งานของลูกค้า";

                // Set A4 paper size
                SetA4PaperSize(printDoc);

                // Add handlers for print events
                printDoc.PrintPage += PrintPage;
                printDoc.EndPrint += PrintDoc_EndPrint;
                printDoc.BeginPrint += (s, args) =>
                {
                    _printCurrentPage = 0;
                    _isPrintSuccessful = true;
                    _printErrorMessage = "";
                };

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
                            // Show waiting cursor
                            Cursor = Cursors.WaitCursor;

                            // Double-check paper size is still A4 before printing
                            EnsureA4PaperSize(printDoc);

                            // Reset pagination variables before printing
                            _printCurrentPage = 0;
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
            // First try to find the predefined A4 size
            bool foundA4 = false;

            foreach (System.Drawing.Printing.PaperSize ps in printDoc.PrinterSettings.PaperSizes)
            {
                if (ps.Kind == System.Drawing.Printing.PaperKind.A4)
                {
                    printDoc.DefaultPageSettings.PaperSize = ps;
                    foundA4 = true;
                    break;
                }
            }

            // If A4 is not found, create a custom size
            if (!foundA4)
            {
                System.Drawing.Printing.PaperSize customA4 = new System.Drawing.Printing.PaperSize("A4", 827, 1169);
                printDoc.DefaultPageSettings.PaperSize = customA4;
            }

            // Set portrait orientation
            printDoc.DefaultPageSettings.Landscape = false;

            // Set reasonable margins
            printDoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(40, 40, 40, 40);

            // Set in printer settings as well
            printDoc.PrinterSettings.DefaultPageSettings.PaperSize = printDoc.DefaultPageSettings.PaperSize;
        }

        private void EnsureA4PaperSize(PrintDocument printDoc)
        {
            // Get the current paper size
            System.Drawing.Printing.PaperSize currentSize = printDoc.DefaultPageSettings.PaperSize;

            // Check if dimensions roughly match A4 (allow small tolerance)
            bool isA4Size = (Math.Abs(currentSize.Width - 827) < 10 && Math.Abs(currentSize.Height - 1169) < 10) ||
                            (Math.Abs(currentSize.Height - 827) < 10 && Math.Abs(currentSize.Width - 1169) < 10);

            if (!isA4Size)
            {
                // Force A4 size if current size doesn't match
                printDoc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1169);
            }

            // Ensure orientation is portrait
            printDoc.DefaultPageSettings.Landscape = false;

            // Update printer settings too — assign the PaperSize (not the whole PageSettings)
            printDoc.PrinterSettings.DefaultPageSettings.PaperSize = printDoc.DefaultPageSettings.PaperSize;
            printDoc.PrinterSettings.DefaultPageSettings.Landscape = printDoc.DefaultPageSettings.Landscape;
        }

        private void PrintDoc_EndPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (_isPrintSuccessful)
            {
                MessageBox.Show("พิมพ์รายงานการใช้งานของลูกค้าเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrEmpty(_printErrorMessage))
            {
                MessageBox.Show($"การพิมพ์ไม่สำเร็จ: {_printErrorMessage}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
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
                using (Font boldFont = new Font("Angsana New", 11, FontStyle.Bold))
                using (Font smallFont = new Font("Angsana New", 9))
                {
                    float yPosition = topMargin;

                    // Title
                    string title = "รายงานการใช้งานของลูกค้า";
                    float titleX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(title, titleFont).Width / 2);
                    e.Graphics.DrawString(title, titleFont, Brushes.Black, titleX, yPosition);
                    yPosition += titleFont.GetHeight();

                    e.Graphics.DrawLine(new Pen(Color.Black, 1.5f), leftMargin, yPosition, leftMargin + availableWidth, yPosition);
                    yPosition += 10;

                    // Date range
                    string dateRangeInfo = $"ช่วงวันที่: {dtpCreateDate.Value:dd/MM/yyyy} ถึง {dtpCreateDateEnd.Value:dd/MM/yyyy}";
                    float dateRangeX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(dateRangeInfo, normalFont).Width / 2);
                    e.Graphics.DrawString(dateRangeInfo, normalFont, Brushes.Black, dateRangeX, yPosition);
                    yPosition += normalFont.GetHeight() * 1.5f;

                    // Print date
                    DateTime today = DateTime.Now;
                    string dateTimeInfo = $"พิมพ์เมื่อ: {today:dd/MM/yyyy HH:mm:ss}";
                    e.Graphics.DrawString(dateTimeInfo, normalFont, Brushes.Black, leftMargin, yPosition);
                    yPosition += normalFont.GetHeight() * 1.5f;

                    // Column definitions
                    string[] columnNames = new string[] {
                "ลำดับ",
                "ชื่อลูกค้า",
                "เบอร์โทร",
                "รวมการใช้บริการ\nทั้งหมด",
                "มาใช้บริการ\n(ยังไม่ได้ชำระเงิน)",
                "มาใช้บริการ\n(ชำระเงินแล้ว)",
                "ยอดรวมทั้งหมด"
            };

                    float[] columnWidthPercentages = new float[] {
                0.06f, 0.25f, 0.15f, 0.13f, 0.13f, 0.13f, 0.15f
            };

                    float[] columnWidths = new float[columnWidthPercentages.Length];
                    for (int i = 0; i < columnWidthPercentages.Length; i++)
                    {
                        columnWidths[i] = availableWidth * columnWidthPercentages[i];
                    }

                    // Draw header
                    float headerHeight = headerFont.GetHeight() * 2.7f;
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
                    int rowsPerPage = (int)((availableHeight - (yPosition - topMargin) - 40 - rowHeight) / rowHeight);

                    // **แก้ไขส่วนนี้: ใช้ _allData แทน dgvCustomers**
                    int totalDataRows = _allData != null ? _allData.Rows.Count : 0;
                    int startRow = _printCurrentPage * rowsPerPage;
                    int endRow = Math.Min(startRow + rowsPerPage, totalDataRows);
                    bool isLastPage = endRow >= totalDataRows;

                    // Calculate totals from _allData
                    decimal totalAmount = 0;
                    decimal totalUnpaid = 0;
                    decimal totalPaid = 0;
                    int totalOrders = 0;

                    if (_allData != null)
                    {
                        foreach (DataRow row in _allData.Rows)
                        {
                            decimal amt = 0;
                            decimal unpaid = 0;
                            decimal paid = 0;
                            int orders = 0;

                            if (_allData.Columns.Contains("ยอดรวมทั้งหมด") && row["ยอดรวมทั้งหมด"] != DBNull.Value)
                                decimal.TryParse(row["ยอดรวมทั้งหมด"].ToString(), out amt);

                            if (_allData.Columns.Contains("มาใช้บริการ(ยังไม่ได้ชำระเงิน)") && row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"] != DBNull.Value)
                                decimal.TryParse(row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].ToString(), out unpaid);

                            if (_allData.Columns.Contains("มาใช้บริการ(ชำระเงินแล้ว)") && row["มาใช้บริการ(ชำระเงินแล้ว)"] != DBNull.Value)
                                decimal.TryParse(row["มาใช้บริการ(ชำระเงินแล้ว)"].ToString(), out paid);

                            if (_allData.Columns.Contains("รวมการใช้บริการทั้งหมด") && row["รวมการใช้บริการทั้งหมด"] != DBNull.Value)
                                int.TryParse(row["รวมการใช้บริการทั้งหมด"].ToString(), out orders);

                            totalAmount += amt;
                            totalUnpaid += unpaid;
                            totalPaid += paid;
                            totalOrders += orders;
                        }
                    }

                    // Print data rows from _allData
                    int sequenceNumber = startRow + 1;

                    for (int i = startRow; i < endRow; i++)
                    {
                        if (_allData == null) break;

                        DataRow row = _allData.Rows[i];
                        currentX = leftMargin;

                        // ลำดับ
                        RectangleF seqRect = new RectangleF(currentX, yPosition, columnWidths[0], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(sequenceNumber.ToString(), normalFont, Brushes.Black, seqRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, seqRect.X, seqRect.Y, seqRect.Width, seqRect.Height);
                        }
                        currentX += columnWidths[0];

                        // ชื่อลูกค้า
                        string customerName = row["ชื่อลูกค้า"]?.ToString() ?? "";
                        RectangleF nameRect = new RectangleF(currentX, yPosition, columnWidths[1], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                        {
                            e.Graphics.DrawString(customerName, normalFont, Brushes.Black,
                                new RectangleF(nameRect.X + 5, nameRect.Y, nameRect.Width - 10, nameRect.Height), sf);
                            e.Graphics.DrawRectangle(Pens.Black, nameRect.X, nameRect.Y, nameRect.Width, nameRect.Height);
                        }
                        currentX += columnWidths[1];

                        // เบอร์โทร
                        string phone = row["เบอร์โทร"]?.ToString() ?? "";
                        RectangleF phoneRect = new RectangleF(currentX, yPosition, columnWidths[2], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(phone, normalFont, Brushes.Black, phoneRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, phoneRect.X, phoneRect.Y, phoneRect.Width, phoneRect.Height);
                        }
                        currentX += columnWidths[2];

                        // รวมการใช้บริการทั้งหมด
                        string totalOrdersStr = row["รวมการใช้บริการทั้งหมด"]?.ToString() ?? "0";
                        RectangleF totalOrdersRect = new RectangleF(currentX, yPosition, columnWidths[3], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(totalOrdersStr, normalFont, Brushes.Black, totalOrdersRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, totalOrdersRect.X, totalOrdersRect.Y, totalOrdersRect.Width, totalOrdersRect.Height);
                        }
                        currentX += columnWidths[3];

                        // มาใช้บริการ(ยังไม่ได้ชำระเงิน)
                        decimal unpaidAmount = 0;
                        if (row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"] != null && row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"] != DBNull.Value)
                            decimal.TryParse(row["มาใช้บริการ(ยังไม่ได้ชำระเงิน)"].ToString(), out unpaidAmount);
                        RectangleF unpaidRect = new RectangleF(currentX, yPosition, columnWidths[4], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(unpaidAmount.ToString("N2"), normalFont, Brushes.Black,
                                new RectangleF(unpaidRect.X, unpaidRect.Y, unpaidRect.Width - 5, unpaidRect.Height), sf);
                            e.Graphics.DrawRectangle(Pens.Black, unpaidRect.X, unpaidRect.Y, unpaidRect.Width, unpaidRect.Height);
                        }
                        currentX += columnWidths[4];

                        // มาใช้บริการ(ชำระเงินแล้ว)
                        decimal paidAmount = 0;
                        if (row["มาใช้บริการ(ชำระเงินแล้ว)"] != null && row["มาใช้บริการ(ชำระเงินแล้ว)"] != DBNull.Value)
                            decimal.TryParse(row["มาใช้บริการ(ชำระเงินแล้ว)"].ToString(), out paidAmount);
                        RectangleF paidRect = new RectangleF(currentX, yPosition, columnWidths[5], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(paidAmount.ToString("N2"), normalFont, Brushes.Black,
                                new RectangleF(paidRect.X, paidRect.Y, paidRect.Width - 5, paidRect.Height), sf);
                            e.Graphics.DrawRectangle(Pens.Black, paidRect.X, paidRect.Y, paidRect.Width, paidRect.Height);
                        }
                        currentX += columnWidths[5];

                        // ยอดรวมทั้งหมด
                        decimal amount = 0;
                        if (row["ยอดรวมทั้งหมด"] != null && row["ยอดรวมทั้งหมด"] != DBNull.Value)
                            decimal.TryParse(row["ยอดรวมทั้งหมด"].ToString(), out amount);
                        RectangleF amountRect = new RectangleF(currentX, yPosition, columnWidths[6], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawString(amount.ToString("N2"), normalFont, Brushes.Black,
                                new RectangleF(amountRect.X, amountRect.Y, amountRect.Width - 5, amountRect.Height), sf);
                            e.Graphics.DrawRectangle(Pens.Black, amountRect.X, amountRect.Y, amountRect.Width, amountRect.Height);
                        }

                        yPosition += rowHeight;
                        sequenceNumber++;
                    }

                    // Print summary on last page
                    if (isLastPage)
                    {
                        currentX = leftMargin;

                        RectangleF summaryRowRect = new RectangleF(leftMargin, yPosition, availableWidth, rowHeight);
                        e.Graphics.FillRectangle(Brushes.LightGray, summaryRowRect);

                        RectangleF totalLabelRect = new RectangleF(currentX, yPosition,
                            columnWidths[0] + columnWidths[1] + columnWidths[2], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, totalLabelRect.X, totalLabelRect.Y, totalLabelRect.Width, totalLabelRect.Height);
                            e.Graphics.DrawString("รวมทั้งหมด", boldFont, Brushes.Black, totalLabelRect, sf);
                        }
                        currentX += columnWidths[0] + columnWidths[1] + columnWidths[2];

                        RectangleF totalOrdersRect = new RectangleF(currentX, yPosition, columnWidths[3], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, totalOrdersRect.X, totalOrdersRect.Y, totalOrdersRect.Width, totalOrdersRect.Height);
                            e.Graphics.DrawString(totalOrders.ToString(), boldFont, Brushes.Black, totalOrdersRect, sf);
                        }
                        currentX += columnWidths[3];

                        RectangleF unpaidRect = new RectangleF(currentX, yPosition, columnWidths[4], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, unpaidRect.X, unpaidRect.Y, unpaidRect.Width, unpaidRect.Height);
                            e.Graphics.DrawString(totalUnpaid.ToString("N2"), boldFont, Brushes.Black,
                                new RectangleF(unpaidRect.X, unpaidRect.Y, unpaidRect.Width - 5, unpaidRect.Height), sf);
                        }
                        currentX += columnWidths[4];

                        RectangleF paidRect = new RectangleF(currentX, yPosition, columnWidths[5], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, paidRect.X, paidRect.Y, paidRect.Width, paidRect.Height);
                            e.Graphics.DrawString(totalPaid.ToString("N2"), boldFont, Brushes.Black,
                                new RectangleF(paidRect.X, paidRect.Y, paidRect.Width - 5, paidRect.Height), sf);
                        }
                        currentX += columnWidths[5];

                        RectangleF totalAmountRect = new RectangleF(currentX, yPosition, columnWidths[6], rowHeight);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.DrawRectangle(Pens.Black, totalAmountRect.X, totalAmountRect.Y, totalAmountRect.Width, totalAmountRect.Height);
                            e.Graphics.DrawString(totalAmount.ToString("N2"), boldFont, Brushes.Black,
                                new RectangleF(totalAmountRect.X, totalAmountRect.Y, totalAmountRect.Width - 5, totalAmountRect.Height), sf);
                        }

                        yPosition += rowHeight + 15;
                        string summaryText = $"จำนวนลูกค้าทั้งหมด {totalDataRows} ราย | " +
                                           $"ใช้บริการ {totalOrders} ครั้ง | " +
                                           $"ยังไม่ชำระ {totalUnpaid:N2} บาท | " +
                                           $"ชำระแล้ว {totalPaid:N2} บาท | " +
                                           $"ยอดรวมทั้งหมด {totalAmount:N2} บาท";
                        e.Graphics.DrawString(summaryText, normalFont, Brushes.Black, leftMargin, yPosition);
                    }

                    // Page number
                    int totalPages = (int)Math.Ceiling((double)totalDataRows / rowsPerPage);
                    if (totalPages == 0) totalPages = 1;

                    string pageText = $"หน้า {_printCurrentPage + 1} จาก {totalPages}";
                    e.Graphics.DrawString(pageText, smallFont, Brushes.Black,
                        rightMargin - e.Graphics.MeasureString(pageText, smallFont).Width, bottomMargin);

                    // Check if more pages needed
                    if (endRow < totalDataRows)
                    {
                        _printCurrentPage++;
                        e.HasMorePages = true;
                    }
                    else
                    {
                        _printCurrentPage = 0;
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
                if (dgvCustomers.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะส่งออก", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Files|*.csv|Excel Files|*.xls";
                    sfd.Title = "บันทึกไฟล์รายงานการใช้งานของลูกค้า";

                    string dateRangeStr = $"{dtpCreateDate.Value:yyyy-MM-dd}_ถึง_{dtpCreateDateEnd.Value:yyyy-MM-dd}";
                    sfd.FileName = $"รายงานการใช้งานของลูกค้า_{dateRangeStr}";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        Cursor = Cursors.WaitCursor;

                        StringBuilder csv = new StringBuilder();

                        // Add UTF-8 BOM for Thai character support
                        csv.Append("\uFEFF");

                        // Add header row
                        List<string> headers = new List<string>();
                        List<DataGridViewColumn> visibleColumns = new List<DataGridViewColumn>();
                        // Use column DisplayIndex order for consistent export
                        foreach (DataGridViewColumn column in dgvCustomers.Columns.Cast<DataGridViewColumn>().OrderBy(c => c.DisplayIndex))
                        {
                            if (column.Visible)
                            {
                                headers.Add($"\"{column.HeaderText}\"");
                                visibleColumns.Add(column);
                            }
                        }
                        csv.AppendLine(string.Join(",", headers));

                        // Add data rows
                        decimal grandTotal = 0;
                        decimal totalUnpaidAmount = 0;
                        decimal totalPaidAmount = 0;
                        int totalOrdersCount = 0;

                        foreach (DataGridViewRow row in dgvCustomers.Rows)
                        {
                            // Skip summary row when accumulating and writing individual rows
                            if (dgvCustomers.Columns["IsSummary"] != null &&
                                row.Cells["IsSummary"].Value != null &&
                                row.Cells["IsSummary"].Value is bool &&
                                (bool)row.Cells["IsSummary"].Value == true)
                            {
                                // We'll write summary later
                                continue;
                            }

                            List<string> cells = new List<string>();
                            foreach (DataGridViewColumn column in visibleColumns)
                            {
                                string value = row.Cells[column.Index].Value?.ToString() ?? "";

                                // **แก้ไข: รักษาเลข 0 หน้าสำหรับเบอร์โทรศัพท์**
                                if (column.HeaderText == "เบอร์โทร")
                                {
                                    // ใช้ Tab เพื่อบังคับให้ Excel แสดงเป็นข้อความ
                                    cells.Add($"\"{value}\t\"");
                                }
                                // รวมยอดเงิน
                                else if (column.HeaderText == "ยอดรวมทั้งหมด" && decimal.TryParse(value, out decimal amount))
                                {
                                    grandTotal += amount;
                                    value = amount.ToString("N2");
                                    value = value.Replace("\"", "\"\"");
                                    cells.Add($"\"{value}\"");
                                }
                                else if (column.HeaderText == "มาใช้บริการ(ยังไม่ได้ชำระเงิน)" && decimal.TryParse(value, out decimal unpaid))
                                {
                                    totalUnpaidAmount += unpaid;
                                    value = unpaid.ToString("N2");
                                    value = value.Replace("\"", "\"\"");
                                    cells.Add($"\"{value}\"");
                                }
                                else if (column.HeaderText == "มาใช้บริการ(ชำระเงินแล้ว)" && decimal.TryParse(value, out decimal paid))
                                {
                                    totalPaidAmount += paid;
                                    value = paid.ToString("N2");
                                    value = value.Replace("\"", "\"\"");
                                    cells.Add($"\"{value}\"");
                                }
                                else if (column.HeaderText == "รวมการใช้บริการทั้งหมด" && int.TryParse(value, out int orders))
                                {
                                    totalOrdersCount += orders;
                                    value = value.Replace("\"", "\"\"");
                                    cells.Add($"\"{value}\"");
                                }
                                else
                                {
                                    value = value.Replace("\"", "\"\"");
                                    cells.Add($"\"{value}\"");
                                }
                            }
                            csv.AppendLine(string.Join(",", cells));
                        }

                        // Add summary row (build according to visibleColumns order)
                        csv.AppendLine();
                        List<string> summaryCells = new List<string>();
                        foreach (var column in visibleColumns)
                        {
                            string header = column.HeaderText;
                            if (header == "ชื่อลูกค้า")
                                summaryCells.Add("\"รวมทั้งหมด\"");
                            else if (header == "เบอร์โทร")
                                summaryCells.Add("\"\"");
                            else if (header == "รวมการใช้บริการทั้งหมด")
                                summaryCells.Add($"\"{totalOrdersCount}\"");
                            else if (header == "มาใช้บริการ(ยังไม่ได้ชำระเงิน)")
                                summaryCells.Add($"\"{totalUnpaidAmount:N2}\"");
                            else if (header == "มาใช้บริการ(ชำระเงินแล้ว)")
                                summaryCells.Add($"\"{totalPaidAmount:N2}\"");
                            else if (header == "ยอดรวมทั้งหมด")
                                summaryCells.Add($"\"{grandTotal:N2}\"");
                            else
                                summaryCells.Add("\"\"");
                        }
                        csv.AppendLine(string.Join(",", summaryCells));

                        // Add summary information
                        csv.AppendLine();
                        csv.AppendLine($"\"รายงานการใช้งานของลูกค้า\"");
                        csv.AppendLine($"\"ช่วงวันที่: {dtpCreateDate.Value:dd/MM/yyyy} ถึง {dtpCreateDateEnd.Value:dd/MM/yyyy}\"");
                        int customerCount = dgvCustomers.Rows.Count - (dgvCustomers.Columns.Contains("IsSummary") ? 1 : 0);
                        csv.AppendLine($"\"จำนวนลูกค้าทั้งหมด: {customerCount} ราย\"");
                        csv.AppendLine($"\"จำนวนครั้งในการใช้บริการทั้งหมด: {totalOrdersCount} ครั้ง\"");
                        csv.AppendLine($"\"ยอดรวมยังไม่ชำระ: {totalUnpaidAmount:N2} บาท\"");
                        csv.AppendLine($"\"ยอดรวมชำระแล้ว: {totalPaidAmount:N2} บาท\"");
                        csv.AppendLine($"\"ยอดรวมทั้งหมด: {grandTotal:N2} บาท\"");
                        csv.AppendLine($"\"ออกรายงานเมื่อ: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\"");

                        System.IO.File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8);

                        Cursor = Cursors.Default;
                        MessageBox.Show("ส่งออกข้อมูลเรียบร้อยแล้ว", "สำเร็จ",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show($"เกิดข้อผิดพลาดในการส่งออกข้อมูล: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            try
            {
                string customerName = txtCustomerFilter.Text.Trim();
                string phone = textBox1.Text.Trim();

                using (SqlConnection connection = DBconfig.GetConnection())
                {
                    connection.Open();

                    DateTime startDate = dtpCreateDate.Value.Date;
                    DateTime endDate = dtpCreateDateEnd.Value.Date.AddDays(1).AddSeconds(-1);

                    DataTable reportData;

                    if (chkSimple.Checked)
                    {
                        reportData = SearchSimpleReport(connection, startDate, endDate, customerName, phone);
                    }
                    else if (chkDetail.Checked)
                    {
                        reportData = SearchDetailReport(connection, startDate, endDate, customerName, phone);
                    }
                    else
                    {
                        reportData = new DataTable();
                    }

                    // เพิ่มส่วนนี้ (เหมือนกับใน LoadReportData)
                    _currentPage = 1;  // รีเซ็ตกลับไปหน้าแรก
                    _allData = reportData.Copy();  // เก็บข้อมูลที่ค้นหาได้
                    _totalRecords = reportData.Rows.Count;
                    _totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                    if (_totalPages == 0) _totalPages = 1;

                    DisplayReportData(reportData);
                    UpdatePaginationButtons();  // เพิ่มบรรทัดนี้
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการค้นหา: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private DataTable SearchSimpleReport(SqlConnection connection, DateTime startDate, DateTime endDate,
            string customerName, string phone)
        {
            string query = @"
        SELECT
            c.CustomerID,
            c.FullName AS 'ชื่อลูกค้า',
            c.Phone AS 'เบอร์โทร',
            ISNULL(oc.OrderCount, 0) AS 'รวมการใช้บริการทั้งหมด',
            ISNULL(uc.UnpaidAmount, 0) AS 'มาใช้บริการ(ยังไม่ได้ชำระเงิน)',
            ISNULL(pc.PaidAmount, 0) AS 'มาใช้บริการ(ชำระเงินแล้ว)',
            ISNULL(uc.UnpaidAmount, 0) + ISNULL(pc.PaidAmount, 0) AS 'ยอดรวมทั้งหมด'
        FROM Customer c
        LEFT JOIN (
            SELECT o.CustomerId, COUNT(DISTINCT o.OrderID) AS OrderCount
            FROM OrderHeader o
            WHERE COALESCE(o.OrderDate, o.PickupDate) BETWEEN @StartDate AND @EndDate
              AND (o.OrderStatus = N'ดำเนินการสำเร็จ' OR o.OrderStatus = N'ออกใบเสร็จแล้ว')
            GROUP BY o.CustomerId
        ) oc ON c.CustomerID = oc.CustomerId
        LEFT JOIN (
            SELECT o.CustomerId, SUM(o.GrandTotalPrice) AS UnpaidAmount
            FROM OrderHeader o
            WHERE COALESCE(o.OrderDate, o.PickupDate) BETWEEN @StartDate AND @EndDate
              AND o.OrderStatus = N'ดำเนินการสำเร็จ'
            GROUP BY o.CustomerId
        ) uc ON c.CustomerID = uc.CustomerId
        LEFT JOIN (
            SELECT oh.CustomerId, SUM(r.TotalAfterDiscount) AS PaidAmount
            FROM Receipt r
            INNER JOIN OrderHeader oh ON r.OrderID = oh.OrderID
            WHERE r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
              AND r.ReceiptDate BETWEEN @StartDate AND @EndDate
            GROUP BY oh.CustomerId
        ) pc ON c.CustomerID = pc.CustomerId
        WHERE 1=1
    ";

            // เพิ่มเงื่อนไขการค้นหา
            if (!string.IsNullOrEmpty(customerName))
                query += " AND c.FullName LIKE @CustomerName";

            if (!string.IsNullOrEmpty(phone))
                query += " AND c.Phone LIKE @Phone";

            // กรองเฉพาะลูกค้าที่มีข้อมูล
            query += @"
        AND (
            (oc.OrderCount IS NOT NULL AND oc.OrderCount > 0)
            OR (uc.UnpaidAmount IS NOT NULL AND uc.UnpaidAmount > 0)
            OR (pc.PaidAmount IS NOT NULL AND pc.PaidAmount > 0)
        )
        ORDER BY c.FullName;
    ";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                if (!string.IsNullOrEmpty(customerName))
                    command.Parameters.AddWithValue("@CustomerName", "%" + customerName + "%");
                if (!string.IsNullOrEmpty(phone))
                    command.Parameters.AddWithValue("@Phone", "%" + phone + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable reportData = new DataTable();
                adapter.Fill(reportData);

                return reportData;
            }
        }

        private DataTable SearchDetailReport(SqlConnection connection, DateTime startDate, DateTime endDate,
            string customerName, string phone)
        {
            string query = @"
        WITH ServiceTypeData AS (
            SELECT 
                c.CustomerID,
                c.FullName,
                c.Phone,
                oi.ItemName,
                -- รวมการใช้บริการทั้งหมด (นับจำนวน OrderItem ที่ไม่ถูกยกเลิก)
                COUNT(CASE 
                    WHEN oh.OrderStatus = N'ดำเนินการสำเร็จ' OR oh.OrderStatus = N'ออกใบเสร็จแล้ว' 
                    THEN oi.OrderItemID 
                END) AS TotalServiceCount,
                -- จำนวนครั้งที่ยังไม่ได้ชำระเงิน
                COUNT(CASE 
                    WHEN oh.OrderStatus = N'ดำเนินการสำเร็จ' 
                    THEN oi.OrderItemID 
                END) AS UnpaidCount,
                -- จำนวนครั้งที่ชำระเงินแล้ว
                COUNT(CASE 
                    WHEN r.ReceiptID IS NOT NULL AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                    THEN oi.OrderItemID 
                END) AS PaidCount,
                -- ยอดรวมที่ยังไม่ได้ชำระเงิน (OrderStatus = 'ดำเนินการสำเร็จ')
                ISNULL(SUM(CASE 
                    WHEN oh.OrderStatus = N'ดำเนินการสำเร็จ' 
                    THEN oi.TotalAmount 
                    ELSE 0 
                END), 0) AS UnpaidAmount,
                -- ยอดรวมที่ชำระเงินแล้ว (มี Receipt และ ReceiptStatus = 'พิมพ์เรียบร้อยแล้ว')
                ISNULL(SUM(CASE 
                    WHEN r.ReceiptID IS NOT NULL AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                    THEN oi.TotalAmount 
                    ELSE 0 
                END), 0) AS PaidAmount
            FROM Customer c
            INNER JOIN OrderHeader oh ON c.CustomerID = oh.CustomerId
            INNER JOIN OrderItem oi ON oh.OrderID = oi.OrderID
            LEFT JOIN Receipt r ON oh.OrderID = r.OrderID 
                AND r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                AND r.ReceiptDate BETWEEN @StartDate AND @EndDate
            WHERE COALESCE(oh.OrderDate, oh.PickupDate) BETWEEN @StartDate AND @EndDate
              AND (oh.OrderStatus = N'ดำเนินการสำเร็จ' OR oh.OrderStatus = N'ออกใบเสร็จแล้ว')
              AND oi.IsCanceled = 0
    ";

            // เพิ่มเงื่อนไขการค้นหา
            if (!string.IsNullOrEmpty(customerName))
                query += " AND c.FullName LIKE @CustomerName";

            if (!string.IsNullOrEmpty(phone))
                query += " AND c.Phone LIKE @Phone";

            query += @"
            GROUP BY c.CustomerID, c.FullName, c.Phone, oi.ItemName
        )
        SELECT 
            CustomerID,
            FullName AS 'ชื่อลูกค้า',
            Phone AS 'เบอร์โทร',
            ItemName AS 'ชื่อรายการ',
            TotalServiceCount AS 'รวมการใช้บริการทั้งหมด',
            UnpaidCount AS 'จำนวนครั้งยังไม่จ่าย',
            PaidCount AS 'จำนวนครั้งจ่ายแล้ว',
            UnpaidAmount AS 'ยอดยังไม่ชำระ',
            PaidAmount AS 'ยอดชำระแล้ว',
            (UnpaidAmount + PaidAmount) AS 'ยอดรวม'
        FROM ServiceTypeData
        WHERE TotalServiceCount > 0 OR UnpaidAmount > 0 OR PaidAmount > 0
        ORDER BY FullName, ItemName;
    ";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                if (!string.IsNullOrEmpty(customerName))
                    command.Parameters.AddWithValue("@CustomerName", "%" + customerName + "%");
                if (!string.IsNullOrEmpty(phone))
                    command.Parameters.AddWithValue("@Phone", "%" + phone + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable reportData = new DataTable();
                adapter.Fill(reportData);

                return reportData;
            }
        }
        private void UpdatePaginationButtons()
        {
            btnFirstPage.Enabled = _currentPage > 1;
            btnPreviousPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
            btnLastPage.Enabled = _currentPage < _totalPages;
        }
        private void btnFirstPage_Click(object sender, EventArgs e)
        {
            _currentPage = 1;
            DisplayReportData(_allData);
            UpdatePaginationButtons();
        }

        private void btnPreviousPage_Click(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                DisplayReportData(_allData);
                UpdatePaginationButtons();
            }
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                DisplayReportData(_allData);
                UpdatePaginationButtons();
            }
        }

        private void btnLastPage_Click(object sender, EventArgs e)
        {
            _currentPage = _totalPages;
            DisplayReportData(_allData);
            UpdatePaginationButtons();
        }
    }
}
