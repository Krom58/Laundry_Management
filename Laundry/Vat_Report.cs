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
using System.IO;
using System.Drawing.Printing;

namespace Laundry_Management.Laundry
{
    public partial class Vat_Report : Form
    {
        private DataTable reportData;
        private DataTable summaryData;
        private int _currentPage = 0;
        public Vat_Report()
        {
            InitializeComponent();
            InitializeDataGridView();

            // กำหนดค่าเริ่มต้นของ DateTimePicker
            dtpCreateDateFirst.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpCreateDateLast.Value = DateTime.Now;

            // เปลี่ยนรูปแบบให้แสดงวันที่แบบเต็ม
            dtpCreateDateFirst.Format = DateTimePickerFormat.Custom;
            dtpCreateDateFirst.CustomFormat = "dd/MM/yyyy";
            dtpCreateDateLast.Format = DateTimePickerFormat.Custom;
            dtpCreateDateLast.CustomFormat = "dd/MM/yyyy";

            // Register date change event handlers
            dtpCreateDateFirst.ValueChanged += DtpCreateDate_ValueChanged;
            dtpCreateDateLast.ValueChanged += DtpCreateDate_ValueChanged;

            // Load report data when form loads
            this.Load += Vat_Report_Load;
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Vat_Report_Load(object sender, EventArgs e)
        {
            LoadReportData();
        }
        private void InitializeDataGridView()
        {
            // Configure the DataGridView
            dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvReport.AllowUserToAddRows = false;
            dgvReport.AllowUserToDeleteRows = false;
            dgvReport.AllowUserToOrderColumns = true;
            dgvReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReport.MultiSelect = false;
            dgvReport.ReadOnly = true;

            // Enhanced settings for Thai text display
            dgvReport.DefaultCellStyle.Font = new Font("Angsana New", 22);
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Angsana New", 22, FontStyle.Bold);

            // Enable text wrapping for columns that may contain lengthy text
            dgvReport.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvReport.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // Increase row height for better Thai text display
            dgvReport.RowTemplate.Height = 40;

            // Allow columns to be adjusted by users if needed
            dgvReport.AllowUserToResizeColumns = true;

            // Set clean selection style
            dgvReport.DefaultCellStyle.SelectionBackColor = Color.FromArgb(205, 230, 247);
            dgvReport.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        // Event handler for both date pickers
        private void DtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            // ตรวจสอบว่าวันที่เริ่มต้นไม่มากกว่าวันที่สิ้นสุด
            if (dtpCreateDateFirst.Value > dtpCreateDateLast.Value)
            {
                MessageBox.Show("วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpCreateDateFirst.Value = dtpCreateDateLast.Value;
                return;
            }

            // Prevent recursive loading when both date pickers trigger events in close succession
            // Use BeginInvoke to delay the execution slightly, allowing both date changes to complete
            this.BeginInvoke(new MethodInvoker(() =>
            {
                LoadReportData();
            }));
        }

        private void LoadReportData()
        {
            try
            {
                this.Text = $"รายงานภาษีขาย - {dtpCreateDateFirst.Value:dd/MM/yyyy} ถึง {dtpCreateDateLast.Value:dd/MM/yyyy}";

                using (SqlConnection connection = DBconfig.GetConnection())
                {
                    reportData = new DataTable();
                    reportData.Columns.Add("วันที่", typeof(DateTime));
                    reportData.Columns.Add("เลขที่ใบเสร็จ", typeof(string));
                    reportData.Columns.Add("ชื่อลูกค้า", typeof(string));
                    reportData.Columns.Add("เลขประจำตัวIDลูกค้า", typeof(string));
                    reportData.Columns.Add("รายละเอียดการชำระเงิน", typeof(string));
                    reportData.Columns.Add("ยอดก่อนรวมภาษี", typeof(decimal));
                    reportData.Columns.Add("ภาษี 7%", typeof(decimal));
                    reportData.Columns.Add("ยอดรวม", typeof(decimal));

                    connection.Open();
                    string query = @"
                SELECT 
                    r.ReceiptDate AS 'วันที่',
                    r.CustomReceiptId AS 'เลขที่ใบเสร็จ',
                    ISNULL(o.CustomerID, '') AS 'เลขประจำตัวIDลูกค้า',
                    ISNULL(c.FullName, '') AS 'ชื่อลูกค้า',
                    r.PaymentMethod AS 'รายละเอียดการชำระเงิน',
                    ISNULL(r.TotalAfterDiscount - r.VAT, 0) AS 'ยอดก่อนรวมภาษี',
                    ISNULL(r.VAT, 0) AS 'ภาษี 7%',
                    ISNULL(r.TotalAfterDiscount, 0) AS 'ยอดรวม'
                FROM Receipt r
                LEFT JOIN [OrderHeader] o ON r.OrderID = o.OrderID
                LEFT JOIN Customer c ON o.CustomerID = c.CustomerID
                WHERE r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว' 
                AND r.ReceiptDate BETWEEN @StartDate AND @EndDate
                ORDER BY r.ReceiptDate, r.ReceiptID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        DateTime startDateTime = dtpCreateDateFirst.Value.Date;
                        DateTime endDateTime = dtpCreateDateLast.Value.Date.AddDays(1).AddSeconds(-1);

                        command.Parameters.AddWithValue("@StartDate", startDateTime);
                        command.Parameters.AddWithValue("@EndDate", endDateTime);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DataRow row = reportData.NewRow();
                                row["วันที่"] = reader["วันที่"];
                                row["เลขที่ใบเสร็จ"] = reader["เลขที่ใบเสร็จ"].ToString();
                                row["ชื่อลูกค้า"] = reader["ชื่อลูกค้า"].ToString();
                                row["เลขประจำตัวIDลูกค้า"] = reader["เลขประจำตัวIDลูกค้า"].ToString();
                                row["รายละเอียดการชำระเงิน"] = reader["รายละเอียดการชำระเงิน"].ToString();
                                row["ยอดก่อนรวมภาษี"] = Convert.ToDecimal(reader["ยอดก่อนรวมภาษี"]);
                                row["ภาษี 7%"] = Convert.ToDecimal(reader["ภาษี 7%"]);
                                row["ยอดรวม"] = Convert.ToDecimal(reader["ยอดรวม"]);
                                reportData.Rows.Add(row);
                            }
                        }
                    }

                    string summaryQuery = @"
        SELECT 
            SUM(ISNULL(r.TotalAfterDiscount, 0)) AS 'ยอดรวม',
            0 AS 'Non Vat'
        FROM Receipt r
        WHERE r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว' 
        AND r.ReceiptDate BETWEEN @StartDate AND @EndDate";

                    using (SqlCommand command = new SqlCommand(summaryQuery, connection))
                    {
                        // กำหนดเวลาให้ครอบคลุมทั้งวัน
                        DateTime startDateTime = dtpCreateDateFirst.Value.Date; // เริ่มต้นที่ 00:00:00
                        DateTime endDateTime = dtpCreateDateLast.Value.Date.AddDays(1).AddSeconds(-1); // สิ้นสุดที่ 23:59:59

                        command.Parameters.AddWithValue("@StartDate", startDateTime);
                        command.Parameters.AddWithValue("@EndDate", endDateTime);

                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        summaryData = new DataTable();
                        adapter.Fill(summaryData);

                        // คำนวณ VAT และยอดก่อนภาษีจากยอดรวม
                        if (summaryData.Rows.Count > 0)
                        {
                            decimal totalAmount = Convert.ToDecimal(summaryData.Rows[0]["ยอดรวม"]);
                            decimal vatAmount = Math.Round(totalAmount / 107m * 7m, 2); // คำนวณ VAT จากยอดรวมและปัดเป็น 2 ตำแหน่ง
                            decimal vatableAmount = totalAmount - vatAmount; // คำนวณยอดก่อนภาษี

                            // เพิ่มคอลัมน์หากไม่มีอยู่แล้ว
                            if (!summaryData.Columns.Contains("ภาษี 7%"))
                                summaryData.Columns.Add("ภาษี 7%", typeof(decimal));
                            if (!summaryData.Columns.Contains("ยอดก่อนรวมภาษี"))
                                summaryData.Columns.Add("ยอดก่อนรวมภาษี", typeof(decimal));

                            // กำหนดค่า
                            summaryData.Rows[0]["ภาษี 7%"] = vatAmount;
                            summaryData.Rows[0]["ยอดก่อนรวมภาษี"] = vatableAmount;
                        }
                    }
                }

                DisplayReportData();

                if (reportData != null && reportData.Rows.Count > 0)
                {
                    this.Text = $"{this.Text} - จำนวน {reportData.Rows.Count} รายการ";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดในการโหลดข้อมูล: " + ex.Message, "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayReportData()
        {
            if (reportData == null || reportData.Rows.Count == 0)
            {
                dgvReport.DataSource = null;
                MessageBox.Show("ไม่พบข้อมูลสำหรับช่วงวันที่ที่เลือก", "ข้อมูล", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // สร้าง DataTable ใหม่เพื่อแสดงผล
            DataTable displayData = reportData.Copy();

            // คำนวณผลรวมสำหรับยอดรวมเท่านั้น
            decimal sumTotal = 0;
            foreach (DataRow row in reportData.Rows)
            {
                sumTotal += row.Field<decimal>("ยอดรวม");
            }

            // คำนวณ VAT ตามสูตร ยอดรวม / 107 * 7
            decimal sumVat = Math.Round(sumTotal / 107m * 7m, 2);
            // คำนวณยอดก่อนรวมภาษีจากยอดรวมและภาษี
            decimal sumVatable = sumTotal - sumVat;

            // เพิ่มแถวสรุป
            DataRow summaryRow = displayData.NewRow();
            summaryRow["วันที่"] = DBNull.Value;
            summaryRow["เลขที่ใบเสร็จ"] = "";
            summaryRow["ชื่อลูกค้า"] = "รวมทั้งสิ้น";
            summaryRow["เลขประจำตัวIDลูกค้า"] = "";
            summaryRow["รายละเอียดการชำระเงิน"] = "";
            summaryRow["ยอดก่อนรวมภาษี"] = sumVatable;
            summaryRow["ภาษี 7%"] = sumVat;
            summaryRow["ยอดรวม"] = sumTotal;
            displayData.Rows.Add(summaryRow);

            dgvReport.DataSource = displayData;

            // Format the numeric columns - ปรับปรุงการจัดรูปแบบของคอลัมน์ตัวเลข
            foreach (DataGridViewColumn column in dgvReport.Columns)
            {
                if (column.Name == "Non Vat" || column.Name == "ยอดก่อนรวมภาษี" ||
                    column.Name == "ภาษี 7%" || column.Name == "ยอดรวม")
                {
                    column.DefaultCellStyle.Format = "N2";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    // เพิ่ม padding ด้านขวาให้กับคอลัมน์ตัวเลข เพื่อให้ตัวเลขไม่ชิดขอบมากเกินไป
                    column.DefaultCellStyle.Padding = new Padding(0, 0, 30, 0); // เพิ่มจาก 10 เป็น 30
                }
                else if (column.Name == "วันที่")
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
            }

            // Highlight the summary row
            if (dgvReport.Rows.Count > 0)
            {
                int lastRowIndex = dgvReport.Rows.Count - 1;
                dgvReport.Rows[lastRowIndex].DefaultCellStyle.BackColor = Color.LightGray;
                dgvReport.Rows[lastRowIndex].DefaultCellStyle.Font = new Font(dgvReport.Font, FontStyle.Bold);
            }

            // ปรับขนาดคอลัมน์ให้เหมาะสมกับข้อมูล
            dgvReport.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            // กำหนดความกว้างคอลัมน์แบบเฉพาะเจาะจง
            dgvReport.Columns["ชื่อลูกค้า"].MinimumWidth = 200;
            dgvReport.Columns["รายละเอียดการชำระเงิน"].MinimumWidth = 150;

            // กำหนดความกว้างคงที่สำหรับคอลัมน์ตัวเลขเพื่อให้มีพื้นที่เพียงพอและสวยงาม
            if (dgvReport.Columns["ยอดก่อนรวมภาษี"] != null)
            {
                dgvReport.Columns["ยอดก่อนรวมภาษี"].MinimumWidth = 120;
                dgvReport.Columns["ยอดก่อนรวมภาษี"].Width = 120;
            }

            if (dgvReport.Columns["ภาษี 7%"] != null)
            {
                dgvReport.Columns["ภาษี 7%"].MinimumWidth = 100;
                dgvReport.Columns["ภาษี 7%"].Width = 100;
            }

            if (dgvReport.Columns["ยอดรวม"] != null)
            {
                // Increase width to better accommodate the numeric values
                dgvReport.Columns["ยอดรวม"].MinimumWidth = 150;
                dgvReport.Columns["ยอดรวม"].Width = 150;

                // Set AutoSizeMode to None to prevent auto-resizing
                dgvReport.Columns["ยอดรวม"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                // Format with thousand separators and ensure right alignment
                dgvReport.Columns["ยอดรวม"].DefaultCellStyle.Format = "N2";
                dgvReport.Columns["ยอดรวม"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                // Add more padding to ensure numbers aren't too close to the edge
                dgvReport.Columns["ยอดรวม"].DefaultCellStyle.Padding = new Padding(0, 0, 35, 0); // เพิ่มจาก 25 เป็น 35
            }
        }

        private void btnExcel_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReport.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะส่งออก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check for valid data before exporting (skip the summary row)
                bool hasValidRows = false;
                for (int i = 0; i < dgvReport.Rows.Count - 1; i++) // Skip the last row which is the summary
                {
                    DataGridViewRow row = dgvReport.Rows[i];
                    if (row.Cells["เลขที่ใบเสร็จ"].Value != null && !string.IsNullOrEmpty(row.Cells["เลขที่ใบเสร็จ"].Value.ToString()))
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
                    sfd.Title = "บันทึกไฟล์รายงานภาษีขาย";

                    // Get date range string for default filename
                    string dateRangeStr = $"{dtpCreateDateFirst.Value.ToString("yyyy-MM-dd")}_ถึง_{dtpCreateDateLast.Value.ToString("yyyy-MM-dd")}";
                    sfd.FileName = $"รายงานภาษีขาย_{dateRangeStr}";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // Show waiting cursor
                        Cursor = Cursors.WaitCursor;

                        // Define columns to export with Thai headers
                        var columnNames = new List<string> {
                            "วันที่",
                            "เลขที่ใบเสร็จ",
                            "ชื่อลูกค้า",
                            "เลขประจำตัวIDลูกค้า",
                            "รายละเอียดการชำระเงิน",
                            "ยอดก่อนรวมภาษี",
                            "ภาษี 7%",
                            "ยอดรวม"
                        };

                        var columnProperties = new List<string> {
                            "วันที่",
                            "เลขที่ใบเสร็จ",
                            "ชื่อลูกค้า",
                            "เลขประจำตัวIDลูกค้า",
                            "รายละเอียดการชำระเงิน",
                            "ยอดก่อนรวมภาษี",
                            "ภาษี 7%",
                            "ยอดรวม"
                        };

                        // Create a StringBuilder to build CSV content
                        StringBuilder csv = new StringBuilder();

                        // Add UTF-8 BOM for Thai character support
                        csv.Append("\uFEFF");

                        // Add CSV header row with column names
                        csv.AppendLine(string.Join(",", columnNames.Select(name => $"\"{name}\"")));

                        decimal totalAmount = 0;
                        int validRowCount = 0;

                        // Export all rows except the summary row at the end
                        for (int i = 0; i < dgvReport.Rows.Count - 1; i++) // Skip the last row (summary)
                        {
                            DataGridViewRow row = dgvReport.Rows[i];

                            validRowCount++;
                            var rowValues = new List<string>();

                            for (int j = 0; j < columnProperties.Count; j++)
                            {
                                string cellValue = "";
                                var cell = row.Cells[columnProperties[j]];

                                if (cell.Value != null && cell.Value != DBNull.Value)
                                {
                                    // Format based on column type
                                    if (columnProperties[j] == "วันที่" && cell.Value is DateTime date)
                                    {
                                        cellValue = date.ToString("dd/MM/yyyy");
                                    }
                                    else if (columnProperties[j] == "ยอดรวม")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalAmount += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else
                                    {
                                        cellValue = cell.Value.ToString();
                                    }

                                    // Preserve leading zeros for ID fields by forcing Excel to treat them as text
                                    if (columnProperties[j] == "เลขที่ใบเสร็จ" || columnProperties[j] == "เลขประจำตัวIDลูกค้า")
                                    {
                                        cellValue = $"=\"{cellValue}\"";
                                    }

                                    // Escape quotes for CSV
                                    cellValue = cellValue.Replace("\"", "\"\"");
                                }

                                // Add quoted value
                                rowValues.Add($"\"{cellValue}\"");
                            }

                            csv.AppendLine(string.Join(",", rowValues));
                        }
                        decimal totalVat = Math.Round(totalAmount / 107m * 7m, 2);
                        decimal totalVatableAmt = totalAmount - totalVat;
                        // Add summary information
                        csv.AppendLine();
                        csv.AppendLine($"\"สรุปรายงานภาษีขาย\"");
                        csv.AppendLine($"\"ช่วงวันที่: {dtpCreateDateFirst.Value.ToString("dd/MM/yyyy")} ถึง {dtpCreateDateLast.Value.ToString("dd/MM/yyyy")}\"");
                        csv.AppendLine($"\"จำนวนรายการทั้งหมด: {validRowCount} รายการ\"");
                        csv.AppendLine($"\"ยอดรวมก่อนภาษี: {totalVatableAmt.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"ภาษีมูลค่าเพิ่ม 7%: {totalVat.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"ยอดรวมทั้งสิ้น: {totalAmount.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"ออกรายงานเมื่อ: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}\"");
                        csv.AppendLine();
                        csv.AppendLine($"\"บริษัท เอเชียโฮเต็ล จำกัด (มหาชน)\"");
                        csv.AppendLine($"\"296 ถนนพญาไท แขวงถนนเพชรบุรี เขตราชเทวี กรุงเทพฯ 10400\"");
                        csv.AppendLine($"\"เลขประจำตัวผู้เสียภาษี 0107535000346 (สำนักงานใหญ่)\"");

                        // Save to file
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

        private void btnPrint_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReport.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะพิมพ์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Reset pagination tracking variable before printing
                _currentPage = 0;

                PrintDocument printDocument = new PrintDocument();
                printDocument.PrintPage += PrintDocument_PrintPage;

                // เพิ่ม event handler สำหรับการพิมพ์เสร็จสิ้น
                printDocument.EndPrint += PrintDocument_EndPrint;

                // Create a PrintDialog to let the user choose printer settings
                using (PrintDialog printDialog = new PrintDialog())
                {
                    printDialog.Document = printDocument;
                    printDialog.AllowSelection = false;
                    printDialog.AllowSomePages = false;

                    // If user clicks OK in the print dialog, print directly
                    if (printDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            // Show waiting cursor during printing
                            Cursor = Cursors.WaitCursor;

                            printDocument.Print();

                            // ย้าย code ส่วนนี้ไปยัง EndPrint event
                            // การแจ้งเตือนจะถูกแสดงหลังจากพิมพ์เสร็จสิ้นแล้ว
                            // MessageBox.Show("พิมพ์รายงานเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            Cursor = Cursors.Default;
                            MessageBox.Show($"เกิดข้อผิดพลาดในการพิมพ์: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการเตรียมพิมพ์: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void PrintDocument_EndPrint(object sender, PrintEventArgs e)
        {
            // เปลี่ยนเคอร์เซอร์กลับเป็นค่าเริ่มต้น
            Cursor = Cursors.Default;

            // แสดงข้อความแจ้งเตือนหลังจากพิมพ์เสร็จสิ้น
            // ใช้ Invoke เพื่อให้แน่ใจว่ากล่องข้อความจะแสดงบนเธรดหลัก UI
            this.Invoke((MethodInvoker)delegate {
                MessageBox.Show("พิมพ์รายงานเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Define fonts
            Font titleFont = new Font("Angsana New", 16, FontStyle.Bold);
            Font headerFont = new Font("Angsana New", 12);
            Font dataFont = new Font("Angsana New", 12);
            Font companyFont = new Font("Angsana New", 12);

            // Page margins
            int leftMargin = 40;
            int topMargin = 40;
            int currentY = topMargin;

            // Page title
            string title = "รายงานภาษีขาย";
            string dateRange = $"{dtpCreateDateFirst.Value.ToString("dd/MM/yyyy")} ถึง {dtpCreateDateLast.Value.ToString("dd/MM/yyyy")}";

            // Draw report title (centered)
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            e.Graphics.DrawString(title, titleFont, Brushes.Black, e.PageBounds.Width / 2, currentY, centerFormat);
            currentY += titleFont.Height;
            e.Graphics.DrawString(dateRange, headerFont, Brushes.Black, e.PageBounds.Width / 2, currentY, centerFormat);
            currentY += headerFont.Height + 10;

            // Company information (left aligned)
            string[] companyInfo = {
                    "ชื่อผู้ประกอบการ     :     บริษัท เอเชียโฮเต็ล จำกัด (มหาชน)",
                    "ชื่อสถานประกอบการ  :     บริษัท เอเชียโฮเต็ล จำกัด (มหาชน)",
                    "ที่อยู่สถานประกอบการ :     296 ถนนพญาไท แขวงถนนเพชรบุรี เขตราชเทวี กรุงเทพฯ 10400"
                };

            foreach (string info in companyInfo)
            {
                e.Graphics.DrawString(info, companyFont, Brushes.Black, leftMargin, currentY);
                currentY += companyFont.Height + 5;
            }

            // Table dimensions calculation - update columnWidths array to match the 8 columns in your report
            int[] columnWidths = { 60, 80, 120, 100, 100, 100, 80, 120 }; // Adjusted for 8 columns with wider ยอดรวม column
            int tableWidth = columnWidths.Sum();
            int tableRightEdge = leftMargin + tableWidth;

            // Registration number (right aligned to match the table edge)
            string registrationNumber = "เลขประจำตัวผู้เสียภาษี 0107535000346";
            string headquartersLabel = "สำนักงานใหญ่";

            // Measure text for right alignment
            SizeF regNumSize = e.Graphics.MeasureString(registrationNumber, companyFont);

            // Position registration number to align with the right edge of the table
            float regNumX = tableRightEdge - regNumSize.Width;

            // Draw registration number right-aligned to the table edge
            e.Graphics.DrawString(registrationNumber, companyFont, Brushes.Black,
                regNumX, topMargin + titleFont.Height + headerFont.Height + 10);

            // Calculate position for checkbox to maintain alignment with the right edge of the table
            float checkboxX = tableRightEdge - 15 - e.Graphics.MeasureString(headquartersLabel, companyFont).Width;

            // Draw checkbox for headquarters with adjusted position
            Rectangle checkboxRect = new Rectangle(
                (int)checkboxX,
                topMargin + titleFont.Height + headerFont.Height + companyFont.Height + 15,
                15, 15);
            e.Graphics.DrawRectangle(Pens.Black, checkboxRect);

            // Draw headquarters label next to checkbox
            e.Graphics.DrawString(headquartersLabel, companyFont, Brushes.Black,
                checkboxRect.Right + 5, checkboxRect.Top);

            // Space before the table
            currentY += 20;

            // Table header - using the same columnWidths calculated earlier
            string[] headers = {
    "วันที่",
    "เลขที่ใบเสร็จ",
    "ชื่อลูกค้า",
    "เลขประจำตัวIDลูกค้า",
    "รายละเอียดการชำระเงิน",
    "ยอดก่อนรวมภาษี",
    "ภาษี 7%",
    "ยอดรวม"
};

            // Increase the header height to accommodate two lines
            int headerHeight = (int)(headerFont.Height * 2.5f); // Increased from previous value

            // Draw table header background and border
            Rectangle tableHeaderRect = new Rectangle(leftMargin, currentY,
                tableWidth, headerHeight);
            e.Graphics.FillRectangle(Brushes.LightGray, tableHeaderRect);
            e.Graphics.DrawRectangle(Pens.Black, tableHeaderRect);

            int columnX = leftMargin;

            // Draw column headers
            for (int i = 0; i < headers.Length; i++)
            {
                // Draw the column header text
                Rectangle cellRect = new Rectangle(columnX, currentY, columnWidths[i], headerHeight);
                // Modified code to right-align the "ยอดก่อนรวมภาษี" column (column 5) in PrintDocument_PrintPage method
                StringFormat cellFormat = new StringFormat
                {
                    Alignment = (i >= 5 && i <= 7) ? StringAlignment.Far : StringAlignment.Center, // Include column 5 in right alignment
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoClip
                };

                e.Graphics.DrawString(headers[i], headerFont, Brushes.Black,
                    new RectangleF(cellRect.X + 2, cellRect.Y, cellRect.Width - 4, cellRect.Height),
                    cellFormat);

                // Draw vertical line between columns
                if (i < headers.Length - 1)
                {
                    e.Graphics.DrawLine(Pens.Black, columnX + columnWidths[i], currentY,
                        columnX + columnWidths[i], currentY + headerHeight);
                }

                columnX += columnWidths[i];
            }

            currentY += headerHeight;

            // Calculate rows per page based on available space
            int rowHeight = dataFont.Height + 6;
            int availableHeight = e.MarginBounds.Height - (currentY - topMargin) - 50; // Reserve space for page number
            int rowsPerPage = availableHeight / rowHeight;

            // Calculate start and end row for current page
            int startRow = _currentPage * rowsPerPage;
            int endRow = Math.Min(startRow + rowsPerPage, dgvReport.Rows.Count);

            // Print data rows for current page
            for (int rowIndex = startRow; rowIndex < endRow; rowIndex++)
            {
                bool isSummaryRow = (rowIndex == dgvReport.Rows.Count - 1);
                Font rowFont = isSummaryRow ? headerFont : dataFont;
                int currentRowHeight = rowFont.Height + 6;

                // Draw row background for summary row
                if (isSummaryRow)
                {
                    Rectangle summaryRect = new Rectangle(leftMargin, currentY, tableWidth, currentRowHeight);
                    e.Graphics.FillRectangle(Brushes.LightGray, summaryRect);
                }

                // Draw table row
                columnX = leftMargin;
                for (int col = 0; col < dgvReport.Columns.Count; col++)
                {
                    string value = dgvReport.Rows[rowIndex].Cells[col].Value?.ToString() ?? "";

                    // Format date values
                    if (col == 0 && !isSummaryRow && DateTime.TryParse(value, out DateTime date))
                    {
                        value = date.ToString("dd/MM/yy");
                    }

                    // Define cell rectangle
                    Rectangle cellRect = new Rectangle(columnX, currentY, columnWidths[col], currentRowHeight);

                    // Set alignment based on column
                    StringFormat cellFormat = new StringFormat
                    {
                        Alignment = (col >= 5 && col <= 8) ? StringAlignment.Far : StringAlignment.Near,
                        LineAlignment = StringAlignment.Center
                    };

                    // Draw cell value
                    e.Graphics.DrawString(value, rowFont, Brushes.Black,
                        new RectangleF(cellRect.X + 2, cellRect.Y, cellRect.Width - 4, cellRect.Height),
                        cellFormat);

                    // Draw cell borders
                    e.Graphics.DrawRectangle(Pens.Black, cellRect);

                    columnX += columnWidths[col];
                }

                currentY += currentRowHeight;
            }

            // Check if we have more pages to print
            if (endRow < dgvReport.Rows.Count)
            {
                _currentPage++;
                e.HasMorePages = true;
            }
            else
            {
                // No more pages - reset for next print job
                _currentPage = 0;
                e.HasMorePages = false;
            }

            // Draw the page number at the bottom - fix to show correct page numbers
            int totalPages = (int)Math.Ceiling((double)dgvReport.Rows.Count / rowsPerPage);

            // The current page is 1-based for display (while _currentPage is 0-based for calculations)
            // On the last page, we've already reset _currentPage to 0, so handle this special case
            int displayPageNumber = e.HasMorePages ? _currentPage : totalPages;
            string pageText = $"หน้า {displayPageNumber} จาก {totalPages}";

            SizeF pageTextSize = e.Graphics.MeasureString(pageText, dataFont);
            e.Graphics.DrawString(pageText, dataFont, Brushes.Black,
                e.PageBounds.Width / 2 - pageTextSize.Width / 2,
                e.PageBounds.Height - 30);
        }
    }
}
