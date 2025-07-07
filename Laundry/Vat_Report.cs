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
                // อัพเดทชื่อฟอร์มเพื่อแสดงวันที่กำลังดูรายงาน
                this.Text = $"รายงานภาษีขาย - {dtpCreateDateFirst.Value.ToString("dd/MM/yyyy")} ถึง {dtpCreateDateLast.Value.ToString("dd/MM/yyyy")}";

                using (SqlConnection connection = DBconfig.GetConnection())
                {
                    reportData = new DataTable();
                    reportData.Columns.Add("Date", typeof(DateTime));
                    reportData.Columns.Add("Tax Inv. no", typeof(string));
                    reportData.Columns.Add("Folio No.#", typeof(string));
                    reportData.Columns.Add("Guest Name", typeof(string));
                    reportData.Columns.Add("Tax ID", typeof(string));
                    reportData.Columns.Add("Payment Description", typeof(string));
                    reportData.Columns.Add("Vatable Amt.", typeof(decimal));
                    reportData.Columns.Add("Vat 7%", typeof(decimal));
                    reportData.Columns.Add("Total", typeof(decimal));

                    connection.Open();
                    // Query to get receipt data with customer names from Order table
                    // Only get receipts that have been printed/completed
                    string query = @"
                            SELECT 
                                CAST(r.ReceiptID AS int) AS 'Tax Inv. no',
                                r.ReceiptDate AS 'Date',
                                r.CustomReceiptId AS 'Folio No.#',
                                ISNULL(o.CustomerID, '') AS 'Tax ID',
                                ISNULL(c.FullName, '') AS 'Guest Name',
                                r.PaymentMethod AS 'Payment Description',
                                ISNULL(r.TotalAfterDiscount - r.VAT, 0) AS 'Vatable Amt.',
                                ISNULL(r.VAT, 0) AS 'Vat 7%',
                                ISNULL(r.TotalAfterDiscount, 0) AS 'Total'
                            FROM Receipt r
                            LEFT JOIN [OrderHeader] o ON r.OrderID = o.OrderID
                            LEFT JOIN Customer c ON o.CustomerID = c.CustomerID
                            WHERE r.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว' 
                            AND r.ReceiptDate BETWEEN @StartDate AND @EndDate
                            ORDER BY r.ReceiptDate, r.ReceiptID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // กำหนดเวลาให้ครอบคลุมทั้งวัน
                        DateTime startDateTime = dtpCreateDateFirst.Value.Date; // เริ่มต้นที่ 00:00:00
                        DateTime endDateTime = dtpCreateDateLast.Value.Date.AddDays(1).AddSeconds(-1); // สิ้นสุดที่ 23:59:59

                        command.Parameters.AddWithValue("@StartDate", startDateTime);
                        command.Parameters.AddWithValue("@EndDate", endDateTime);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DataRow row = reportData.NewRow();
                                row["Date"] = reader["Date"];
                                row["Tax Inv. no"] = reader["Tax Inv. no"].ToString();
                                row["Folio No.#"] = reader["Folio No.#"].ToString();
                                row["Guest Name"] = reader["Guest Name"].ToString();
                                row["Tax ID"] = reader["Tax ID"].ToString();
                                row["Payment Description"] = reader["Payment Description"].ToString();
                                row["Vatable Amt."] = Convert.ToDecimal(reader["Vatable Amt."]);
                                row["Vat 7%"] = Convert.ToDecimal(reader["Vat 7%"]);
                                row["Total"] = Convert.ToDecimal(reader["Total"]);
                                reportData.Rows.Add(row);
                            }
                        }
                    }

                    // Get summary data
                    string summaryQuery = @"
                            SELECT 
                                SUM(ISNULL(r.TotalAfterDiscount - r.VAT, 0)) AS 'Vatable Amt.',
                                0 AS 'Non Vat',
                                SUM(ISNULL(r.VAT, 0)) AS 'Vat 7%',
                                SUM(ISNULL(r.TotalAfterDiscount, 0)) AS 'Total'
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
                    }
                }

                // Display data
                DisplayReportData();

                // Update the form title to include record count
                if (reportData != null && reportData.Rows.Count > 0)
                {
                    this.Text = $"{this.Text} - จำนวน {reportData.Rows.Count} รายการ";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading report data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayReportData()
        {
            if (reportData == null || reportData.Rows.Count == 0)
            {
                dgvReport.DataSource = null;
                MessageBox.Show("No data found for the selected date range.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create a new DataTable that combines the report data and the summary
            DataTable displayData = reportData.Copy();

            // Add a summary row
            if (summaryData != null && summaryData.Rows.Count > 0)
            {
                DataRow summaryRow = displayData.NewRow();
                summaryRow["Date"] = DBNull.Value;
                summaryRow["Tax Inv. no"] = "";
                summaryRow["Folio No.#"] = "";
                summaryRow["Guest Name"] = "รวมทั้งสิ้น";
                summaryRow["Tax ID"] = "";
                summaryRow["Payment Description"] = "";
                summaryRow["Vatable Amt."] = summaryData.Rows[0]["Vatable Amt."];
                summaryRow["Vat 7%"] = summaryData.Rows[0]["Vat 7%"];
                summaryRow["Total"] = summaryData.Rows[0]["Total"];
                displayData.Rows.Add(summaryRow);
            }

            dgvReport.DataSource = displayData;

            // Format the numeric columns
            foreach (DataGridViewColumn column in dgvReport.Columns)
            {
                if (column.Name == "Non Vat" || column.Name == "Vatable Amt." ||
                    column.Name == "Vat 7%" || column.Name == "Total")
                {
                    column.DefaultCellStyle.Format = "N2";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (column.Name == "Date")
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

            // Apply proper column sizing for Thai text
            // First force cells to use their content width
            dgvReport.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            // Set minimum widths for specific columns that may contain Thai text
            dgvReport.Columns["Guest Name"].MinimumWidth = 200;
            dgvReport.Columns["Payment Description"].MinimumWidth = 150;

            // Ensure numeric columns have adequate width
            if (dgvReport.Columns["Vatable Amt."] != null)
                dgvReport.Columns["Vatable Amt."].MinimumWidth = 100;

            if (dgvReport.Columns["Vat 7%"] != null)
                dgvReport.Columns["Vat 7%"].MinimumWidth = 80;

            if (dgvReport.Columns["Total"] != null)
                dgvReport.Columns["Total"].MinimumWidth = 100;

            // Apply padding to all columns for better readability
            foreach (DataGridViewColumn col in dgvReport.Columns)
            {
                col.Width += 20; // Add extra padding
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
                    if (row.Cells["Tax Inv. no"].Value != null && !string.IsNullOrEmpty(row.Cells["Tax Inv. no"].Value.ToString()))
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
                            "เลขที่ใบกำกับภาษี",
                            "เลขที่ใบรับผ้า",
                            "ชื่อลูกค้า",
                            "เลขประจำตัวผู้เสียภาษี",
                            "รายละเอียดการชำระเงิน",
                            "ยอดก่อนภาษี",
                            "ภาษี 7%",
                            "ยอดรวม"
                        };

                        var columnProperties = new List<string> {
                            "Date",
                            "Tax Inv. no",
                            "Folio No.#",
                            "Guest Name",
                            "Tax ID",
                            "Payment Description",
                            "Vatable Amt.",
                            "Vat 7%",
                            "Total"
                        };

                        // Create a StringBuilder to build CSV content
                        StringBuilder csv = new StringBuilder();

                        // Add UTF-8 BOM for Thai character support
                        csv.Append("\uFEFF");

                        // Add CSV header row with column names
                        csv.AppendLine(string.Join(",", columnNames.Select(name => $"\"{name}\"")));

                        // Add data rows
                        decimal totalVatableAmt = 0;
                        decimal totalVat = 0;
                        decimal totalAmount = 0;
                        int validRowCount = 0;

                        // Export all rows except the summary row at the end
                        for (int i = 0; i < dgvReport.Rows.Count - 1; i++) // Skip the last row (summary)
                        {
                            DataGridViewRow row = dgvReport.Rows[i];

                            // Skip rows with no invoice number
                            if (row.Cells["Tax Inv. no"].Value == null || string.IsNullOrEmpty(row.Cells["Tax Inv. no"].Value.ToString()))
                            {
                                continue;
                            }

                            validRowCount++;
                            var rowValues = new List<string>();

                            for (int j = 0; j < columnProperties.Count; j++)
                            {
                                string cellValue = "";
                                var cell = row.Cells[columnProperties[j]];

                                if (cell.Value != null && cell.Value != DBNull.Value)
                                {
                                    // Format based on column type
                                    if (columnProperties[j] == "Date" && cell.Value is DateTime date)
                                    {
                                        cellValue = date.ToString("dd/MM/yyyy");
                                    }
                                    else if (columnProperties[j] == "Vatable Amt.")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalVatableAmt += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else if (columnProperties[j] == "Vat 7%")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalVat += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else if (columnProperties[j] == "Total")
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
                                    if (columnProperties[j] == "Tax Inv. no" || columnProperties[j] == "Folio No.#" || columnProperties[j] == "Tax ID")
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

            // Table dimensions calculation - needed to position registration number
            int[] columnWidths = { 60, 80, 80, 150, 80, 100, 80, 60, 60 };
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
    "เลขที่ID",
    "เลขที่ใบเสร็จ",
    "ชื่อลูกค้า",
    "เลขประจำตัวIDลูกค้า",
    "รายละเอียดการชำระเงิน",
    "ยอดก่อนภาษี",
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
                StringFormat cellFormat = new StringFormat
                {
                    Alignment = (i >= 6 && i <= 8) ? StringAlignment.Far : StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    // Enable text wrapping for multi-line headers
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
                        Alignment = (col >= 6 && col <= 8) ? StringAlignment.Far : StringAlignment.Near,
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
