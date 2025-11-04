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
using System.Globalization;
using System.Drawing.Printing;

namespace Laundry_Management.Laundry
{
    public partial class Report : Form
    {
        public Report()
        {
            InitializeComponent();

            // ตั้งค่า DataGridView
            dgvReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvReport.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // กำหนดค่าเริ่มต้นของ DateTimePicker
            dtpCreateDateFirst.Value = DateTime.Today;
            dtpCreateDateLast.Value = DateTime.Today;

            // เปลี่ยนรูปแบบให้แสดงวันที่แบบเต็ม
            dtpCreateDateFirst.Format = DateTimePickerFormat.Custom;
            dtpCreateDateFirst.CustomFormat = "dd/MM/yyyy";
            dtpCreateDateLast.Format = DateTimePickerFormat.Custom;
            dtpCreateDateLast.CustomFormat = "dd/MM/yyyy";

            // เพิ่ม Event Handler
            dtpCreateDateFirst.ValueChanged += DtpCreateDate_ValueChanged;
            dtpCreateDateLast.ValueChanged += DtpCreateDate_ValueChanged;
            this.Load += Report_Load;
            dgvReport.DataBindingComplete += DgvReport_DataBindingComplete;
        }

        private void Report_Load(object sender, EventArgs e)
        {
            // โหลดข้อมูลเมื่อเปิดฟอร์ม
            LoadReceiptDataByDateRange(dtpCreateDateFirst.Value, dtpCreateDateLast.Value);
        }

        private void DtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            // โหลดข้อมูลใหม่เมื่อมีการเปลี่ยนวัน
            LoadReceiptDataByDateRange(dtpCreateDateFirst.Value, dtpCreateDateLast.Value);
        }

        private void DgvReport_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // กำหนดหัวตารางเป็นภาษาไทย
            if (dgvReport.Columns["ReceiptDate"] != null)
                dgvReport.Columns["ReceiptDate"].HeaderText = "วันที่ออกใบเสร็จ";
            if (dgvReport.Columns["CustomReceiptId"] != null)
                dgvReport.Columns["CustomReceiptId"].HeaderText = "เลขที่ใบเสร็จ";
            if (dgvReport.Columns["CustomOrderId"] != null)
                dgvReport.Columns["CustomOrderId"].HeaderText = "เลขที่รายการ";
            if (dgvReport.Columns["TotalBeforeDiscount"] != null)
                dgvReport.Columns["TotalBeforeDiscount"].HeaderText = "ยอดรวมก่อนหักส่วนลด";
            if (dgvReport.Columns["Discount"] != null)
                dgvReport.Columns["Discount"].HeaderText = "ส่วนลด";
            if (dgvReport.Columns["TotalAfterDiscount"] != null)
                dgvReport.Columns["TotalAfterDiscount"].HeaderText = "ยอดรวมหลังหักส่วนลด";
            if (dgvReport.Columns["PaymentMethod"] != null)
                dgvReport.Columns["PaymentMethod"].HeaderText = "วิธีการชำระเงิน";

            // เพิ่มหัวตารางสำหรับคอลัมน์ประเภทการชำระเงิน
            if (dgvReport.Columns["CashAmount"] != null)
                dgvReport.Columns["CashAmount"].HeaderText = "ชำระด้วยเงินสด";
            if (dgvReport.Columns["QRAmount"] != null)
                dgvReport.Columns["QRAmount"].HeaderText = "ชำระด้วย QR";
            if (dgvReport.Columns["CreditAmount"] != null)
                dgvReport.Columns["CreditAmount"].HeaderText = "ชำระด้วยบัตรเครดิต";

            // จัดรูปแบบคอลัมน์เงิน
            foreach (DataGridViewColumn col in dgvReport.Columns)
            {
                if (col.Name == "TotalBeforeDiscount" || col.Name == "Discount" || col.Name == "TotalAfterDiscount" ||
                    col.Name == "CashAmount" || col.Name == "QRAmount" || col.Name == "CreditAmount")
                {
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (col.Name == "ReceiptDate")
                {
                    col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }
            }
        }

        private void LoadReceiptDataByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                // สร้าง DTO สำหรับเก็บข้อมูลรายงาน
                var reportData = new List<ReceiptReportDto>();

                // ตรวจสอบว่าวันที่เริ่มต้นไม่มากกว่าวันที่สิ้นสุด
                if (startDate > endDate)
                {
                    MessageBox.Show("วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด", "ข้อผิดพลาด",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dtpCreateDateFirst.Value = endDate;
                    return;
                }

                // กำหนดเวลาให้ครอบคลุมทั้งวัน
                DateTime startDateTime = startDate.Date; // เริ่มต้นที่ 00:00:00
                DateTime endDateTime = endDate.Date.AddDays(1).AddSeconds(-1); // สิ้นสุดที่ 23:59:59

                // อัพเดทชื่อฟอร์มเพื่อแสดงวันที่กำลังดูรายงาน
                this.Text = $"{startDate.ToString("dd/MM/yyyy")} ถึง {endDate.ToString("dd/MM/yyyy")}";

                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = cn;

                    // สร้าง SQL query ที่กรองตามช่วงวันที่ และเพิ่มเงื่อนไขตรวจสอบว่าใบเสร็จถูกพิมพ์เรียบร้อยแล้ว
                    var sql = @"
                            SELECT 
                                R.ReceiptDate, 
                                R.CustomReceiptId, 
                                OH.CustomOrderId, 
                                R.TotalBeforeDiscount, 
                                R.Discount, 
                                R.TotalAfterDiscount,
                                R.PaymentMethod
                            FROM Receipt R
                            INNER JOIN OrderHeader OH ON R.OrderID = OH.OrderID
                            WHERE R.ReceiptDate >= @startDate 
                              AND R.ReceiptDate <= @endDate
                              AND R.ReceiptStatus = N'พิมพ์เรียบร้อยแล้ว'
                            ORDER BY R.ReceiptDate ASC"; // เรียงตามวันที่จากเก่าไปใหม่

                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@startDate", startDateTime);
                    cmd.Parameters.AddWithValue("@endDate", endDateTime); // ถึงเวลา 23:59:59 ของวันสุดท้าย

                    cn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var paymentMethod = reader["PaymentMethod"] != DBNull.Value ? reader["PaymentMethod"].ToString() : "";
                            var totalAfterDiscount = reader["TotalAfterDiscount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAfterDiscount"]) : 0;

                            // Initialize all payment type amounts to 0
                            decimal cashAmount = 0;
                            decimal qrAmount = 0;
                            decimal creditAmount = 0;

                            // Set the amount to the appropriate payment method column
                            if (paymentMethod.Trim().ToUpper().Contains("CASH") || paymentMethod.Trim().ToUpper().Contains("เงินสด"))
                            {
                                cashAmount = totalAfterDiscount;
                            }
                            else if (paymentMethod.Trim().ToUpper().Contains("QR") || paymentMethod.Trim().ToUpper().Contains("คิวอาร์"))
                            {
                                qrAmount = totalAfterDiscount;
                            }
                            else if (paymentMethod.Trim().ToUpper().Contains("CREDIT") || paymentMethod.Trim().ToUpper().Contains("เครดิต") ||
                                     paymentMethod.Trim().ToUpper().Contains("บัตร"))
                            {
                                creditAmount = totalAfterDiscount;
                            }

                            reportData.Add(new ReceiptReportDto
                            {
                                ReceiptDate = reader["ReceiptDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReceiptDate"]) : DateTime.MinValue,
                                CustomReceiptId = reader["CustomReceiptId"] != DBNull.Value ? reader["CustomReceiptId"].ToString() : "",
                                CustomOrderId = reader["CustomOrderId"] != DBNull.Value ? reader["CustomOrderId"].ToString() : "",
                                TotalBeforeDiscount = reader["TotalBeforeDiscount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalBeforeDiscount"]) : 0,
                                Discount = reader["Discount"] != DBNull.Value ? Convert.ToDecimal(reader["Discount"]) : 0,
                                TotalAfterDiscount = totalAfterDiscount,
                                PaymentMethod = paymentMethod,
                                CashAmount = cashAmount,
                                QRAmount = qrAmount,
                                CreditAmount = creditAmount
                            });
                        }
                    }
                }

                // ผูกข้อมูลกับ DataGridView
                dgvReport.DataSource = reportData;

                // คำนวณผลรวมท้ายตาราง
                CalculateTotals(reportData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดข้อมูล: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CalculateTotals(List<ReceiptReportDto> data)
        {
            if (data == null || !data.Any())
            {
                lblTotal.Text = "0.00 บาท";
                lblDiscount.Text = "0.00 บาท";
                lblTotalAfterDiscount.Text = "0.00 บาท";
                lblQR.Text = "0.00 บาท";
                lblCash.Text = "0.00 บาท";
                lblCredit.Text = "0.00 บาท";
                return;
            }

            // คำนวณผลรวมทั้งหมด
            decimal totalBeforeDiscount = data.Sum(r => r.TotalBeforeDiscount);
            decimal totalDiscount = data.Sum(r => r.Discount);
            decimal totalAfterDiscount = data.Sum(r => r.TotalAfterDiscount);

            // แสดงผลรวมทั้งหมดในฟอร์ม
            lblTotal.Text = totalBeforeDiscount.ToString("N2") + " บาท";
            lblDiscount.Text = totalDiscount.ToString("N2") + " บาท";
            lblTotalAfterDiscount.Text = totalAfterDiscount.ToString("N2") + " บาท";

            // คำนวณผลรวมตามประเภทการชำระเงิน
            decimal qrTotal = 0;
            decimal cashTotal = 0;
            decimal creditTotal = 0;

            // กรองและรวมตามประเภทการชำระเงิน
            foreach (var item in data)
            {
                if (item.PaymentMethod != null)
                {
                    string paymentMethod = item.PaymentMethod.Trim().ToUpper();

                    if (paymentMethod.Contains("QR") || paymentMethod.Contains("คิวอาร์"))
                    {
                        qrTotal += item.TotalAfterDiscount;
                    }
                    else if (paymentMethod.Contains("CASH") || paymentMethod.Contains("เงินสด"))
                    {
                        cashTotal += item.TotalAfterDiscount;
                    }
                    else if (paymentMethod.Contains("CREDIT") || paymentMethod.Contains("เครดิต") ||
                             paymentMethod.Contains("บัตร"))
                    {
                        creditTotal += item.TotalAfterDiscount;
                    }
                }
            }

            // แสดงผลรวมตามประเภทการชำระเงิน
            lblQR.Text = qrTotal.ToString("N2") + " บาท";
            lblCash.Text = cashTotal.ToString("N2") + " บาท";
            lblCredit.Text = creditTotal.ToString("N2") + " บาท";

            // เพิ่มข้อความแสดงจำนวนรายการทั้งหมด
            this.Text = $"{this.Text} - จำนวน {data.Count} รายการ";
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // DTO สำหรับเก็บข้อมูลรายงาน (เพิ่ม PaymentMethod)
        public class ReceiptReportDto
        {
            public DateTime ReceiptDate { get; set; }
            public string CustomReceiptId { get; set; }
            public string CustomOrderId { get; set; }
            public decimal TotalBeforeDiscount { get; set; }
            public decimal Discount { get; set; }
            public decimal TotalAfterDiscount { get; set; }
            public string PaymentMethod { get; set; }
            public decimal CashAmount { get; set; }
            public decimal QRAmount { get; set; }
            public decimal CreditAmount { get; set; }
        }

        private bool _isPrintSuccessful = false;
        private string _printErrorMessage = "";
        private int _currentPage = 0;
        private void btnPrint_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReport.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะพิมพ์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check for valid data before printing
                bool hasValidRows = false;
                foreach (DataGridViewRow row in dgvReport.Rows)
                {
                    if (row.Cells["CustomReceiptId"].Value != null)
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
                printDoc.DocumentName = "รายงานการขาย";

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

                // Set portrait orientation for all pages
                printDoc.DefaultPageSettings.Landscape = false;

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
        private void PrintDoc_EndPrint(object sender, PrintEventArgs e)
        {
            if (_isPrintSuccessful)
            {
                MessageBox.Show("พิมพ์รายงานการขายเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                // Force portrait orientation for all pages
                e.PageSettings.Landscape = false;

                // Page settings
                float leftMargin = e.MarginBounds.Left;
                float topMargin = e.MarginBounds.Top;
                float rightMargin = e.MarginBounds.Right;
                float bottomMargin = e.MarginBounds.Bottom;
                float availableWidth = rightMargin - leftMargin;
                float availableHeight = bottomMargin - topMargin;

                // ปรับขนาดฟอนต์ให้เล็กลง
                using (Font titleFont = new Font("Angsana New", 16, FontStyle.Bold))
                using (Font headerFont = new Font("Angsana New", 11, FontStyle.Bold))
                using (Font normalFont = new Font("Angsana New", 10))
                using (Font smallFont = new Font("Angsana New", 9))
                using (Font boldFont = new Font("Angsana New", 11, FontStyle.Bold)) // เพิ่มฟอนต์ตัวหนาสำหรับแถวรวม
                {
                    // Draw the page header
                    float yPosition = topMargin;

                    // Title centered at the top
                    string title = "รายงานการขาย";
                    float titleX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(title, titleFont).Width / 2);
                    e.Graphics.DrawString(title, titleFont, Brushes.Black, titleX, yPosition);
                    yPosition += titleFont.GetHeight();

                    // Draw underline below title
                    e.Graphics.DrawLine(new Pen(Color.Black, 1.5f), leftMargin, yPosition, leftMargin + availableWidth, yPosition);
                    yPosition += 10;

                    // Date range information
                    string dateRangeInfo = $"ช่วงวันที่: {dtpCreateDateFirst.Value.ToString("dd/MM/yyyy")} ถึง {dtpCreateDateLast.Value.ToString("dd/MM/yyyy")}";
                    float dateRangeX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(dateRangeInfo, normalFont).Width / 2);
                    e.Graphics.DrawString(dateRangeInfo, normalFont, Brushes.Black, dateRangeX, yPosition);
                    yPosition += normalFont.GetHeight() * 1.5f;

                    // Print date and time info
                    DateTime today = DateTime.Now;
                    string dateTimeInfo = $"พิมพ์เมื่อ: {today.ToString("dd/MM/yyyy HH:mm:ss")}";
                    e.Graphics.DrawString(dateTimeInfo, normalFont, Brushes.Black, leftMargin, yPosition);
                    yPosition += normalFont.GetHeight() * 1.5f;

                    // Display summary totals at the top of the report
                    string totalInfo = $"ยอดรวมก่อนหักส่วนลด: {lblTotal.Text}   ส่วนลดทั้งหมด: {lblDiscount.Text}   ยอดรวมหลังหักส่วนลด: {lblTotalAfterDiscount.Text}";
                    float totalInfoX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(totalInfo, headerFont).Width / 2);
                    e.Graphics.DrawString(totalInfo, headerFont, Brushes.Black, totalInfoX, yPosition);
                    yPosition += headerFont.GetHeight() * 1.5f;

                    // Add payment method totals information
                    string paymentInfo = $"ชำระด้วยเงินสด: {lblCash.Text}   ชำระด้วยบัตรเครดิต: {lblCredit.Text}   ชำระด้วย QR: {lblQR.Text}";
                    float paymentInfoX = leftMargin + (availableWidth / 2) - (e.Graphics.MeasureString(paymentInfo, headerFont).Width / 2);
                    e.Graphics.DrawString(paymentInfo, headerFont, Brushes.Black, paymentInfoX, yPosition);
                    yPosition += headerFont.GetHeight() * 1.5f;

                    string[] columnNames = new string[] {
                "ลำดับ",
                "วันที่\nออกใบเสร็จ",
                "เลขที่\nใบเสร็จ",
                "เลขที่\nใบรับผ้า",
                "ยอดรวมก่อน\nหักส่วนลด",
                "ส่วนลด",
                "ยอดรวมหลัง\nหักส่วนลด",
                "ชำระด้วย\nเงินสด",
                "ชำระด้วย\nQR",
                "ชำระด้วย\nบัตรเครดิต"
            };

                    string[] columnDataProperties = new string[] {
                "", // ลำดับ - ไม่มี property คู่กัน
                "ReceiptDate",
                "CustomReceiptId",
                "CustomOrderId",
                "TotalBeforeDiscount",
                "Discount",
                "TotalAfterDiscount",
                "CashAmount",
                "QRAmount",
                "CreditAmount"
            };

                    // Column width percentages - ปรับความกว้างคอลัมน์ให้เหมาะสมกับแนวตั้ง
                    float[] columnWidthPercentages = new float[] {
                0.05f, // ลำดับ
                0.14f, // วันที่ออกใบเสร็จ
                0.09f, // เลขที่ใบเสร็จ
                0.09f, // เลขที่รายการ
                0.12f, // ยอดรวมก่อนหักส่วนลด
                0.08f, // ส่วนลด
                0.12f, // ยอดรวมหลังหักส่วนลด
                0.11f, // ชำระด้วยเงินสด
                0.10f, // ชำระด้วย QR
                0.10f  // ชำระด้วยบัตรเครดิต
            };

                    // Calculate column widths
                    float[] columnWidths = new float[columnWidthPercentages.Length];
                    for (int i = 0; i < columnWidthPercentages.Length; i++)
                    {
                        columnWidths[i] = availableWidth * columnWidthPercentages[i];
                    }

                    // Draw table header with multi-line support
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

                    // เตรียมข้อมูลสำหรับแถวรวม
                    decimal totalBeforeDiscount = 0;
                    decimal totalDiscount = 0;
                    decimal totalAfterDiscount = 0;
                    decimal totalCashAmount = 0;
                    decimal totalQRAmount = 0;
                    decimal totalCreditAmount = 0;

                    // ดึงข้อมูลรวมจาก DataGridView
                    foreach (DataGridViewRow row in dgvReport.Rows)
                    {
                        if (row.Cells["CustomReceiptId"].Value != null)
                        {
                            try
                            {
                                if (row.Cells["TotalBeforeDiscount"].Value != null)
                                    totalBeforeDiscount += Convert.ToDecimal(row.Cells["TotalBeforeDiscount"].Value);

                                if (row.Cells["Discount"].Value != null)
                                    totalDiscount += Convert.ToDecimal(row.Cells["Discount"].Value);

                                if (row.Cells["TotalAfterDiscount"].Value != null)
                                    totalAfterDiscount += Convert.ToDecimal(row.Cells["TotalAfterDiscount"].Value);

                                if (row.Cells["CashAmount"].Value != null)
                                    totalCashAmount += Convert.ToDecimal(row.Cells["CashAmount"].Value);

                                if (row.Cells["QRAmount"].Value != null)
                                    totalQRAmount += Convert.ToDecimal(row.Cells["QRAmount"].Value);

                                if (row.Cells["CreditAmount"].Value != null)
                                    totalCreditAmount += Convert.ToDecimal(row.Cells["CreditAmount"].Value);
                            }
                            catch { }
                        }
                    }

                    // Calculate how many rows can fit on a page
                    float rowHeight = normalFont.GetHeight() * 1.2f;
                    int rowsPerPage = (int)((availableHeight - (yPosition - topMargin) - 40 - rowHeight) / rowHeight); // ลดจำนวนแถวลง 1 เพื่อเหลือที่ให้แถวรวม

                    // Calculate the range of rows to print for the current page
                    int startRow = _currentPage * rowsPerPage;
                    int endRow = Math.Min(startRow + rowsPerPage, dgvReport.Rows.Count);
                    bool isLastPage = endRow >= dgvReport.Rows.Count;

                    // Print data rows
                    int validRowsPrinted = 0;
                    int sequenceNumber = startRow + 1; // เริ่มต้นลำดับตามหน้าที่พิมพ์

                    for (int i = startRow; i < endRow; i++)
                    {
                        DataGridViewRow row = dgvReport.Rows[i];

                        // Skip rows with no data
                        if (row.Cells["CustomReceiptId"].Value == null)
                        {
                            continue;
                        }

                        currentX = leftMargin;

                        // เพิ่มคอลัมน์ลำดับ
                        RectangleF seqRect = new RectangleF(currentX, yPosition, columnWidths[0], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString(sequenceNumber.ToString(), normalFont, Brushes.Black, seqRect, sf);
                            e.Graphics.DrawRectangle(Pens.Black, seqRect.X, seqRect.Y, seqRect.Width, seqRect.Height);
                        }
                        currentX += columnWidths[0];

                        // ลำดับข้อมูลในแถว
                        for (int j = 1; j < columnDataProperties.Length; j++) // เริ่มที่ 1 เพราะคอลัมน์แรกเป็นลำดับ
                        {
                            string cellValue = "";
                            try
                            {
                                if (row.Cells[columnDataProperties[j]].Value != null &&
                                    row.Cells[columnDataProperties[j]].Value != DBNull.Value)
                                {
                                    // Format value based on column type
                                    if (columnDataProperties[j] == "TotalBeforeDiscount" ||
                                        columnDataProperties[j] == "Discount" ||
                                        columnDataProperties[j] == "TotalAfterDiscount" ||
                                        columnDataProperties[j] == "CashAmount" ||
                                        columnDataProperties[j] == "QRAmount" ||
                                        columnDataProperties[j] == "CreditAmount")
                                    {
                                        decimal amount = Convert.ToDecimal(row.Cells[columnDataProperties[j]].Value);
                                        cellValue = amount.ToString("N2");
                                    }
                                    else if (columnDataProperties[j] == "ReceiptDate")
                                    {
                                        DateTime date = Convert.ToDateTime(row.Cells[columnDataProperties[j]].Value);
                                        cellValue = date.ToString("dd/MM/yyyy HH:mm");
                                    }
                                    else
                                    {
                                        cellValue = row.Cells[columnDataProperties[j]].Value.ToString();
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                cellValue = "-";
                            }

                            RectangleF cellRect = new RectangleF(currentX, yPosition, columnWidths[j], rowHeight);

                            using (StringFormat sf = new StringFormat())
                            {
                                sf.Alignment = StringAlignment.Center;
                                sf.LineAlignment = StringAlignment.Center;
                                sf.Trimming = StringTrimming.EllipsisCharacter;

                                // Apply different formatting for financial columns
                                if (columnDataProperties[j] == "Discount" && !string.IsNullOrEmpty(cellValue) &&
                                     cellValue != "-" && Convert.ToDecimal(row.Cells[columnDataProperties[j]].Value) > 0)
                                {
                                    using (SolidBrush discountBrush = new SolidBrush(Color.Red))
                                    {
                                        e.Graphics.DrawString(cellValue, normalFont, discountBrush, cellRect, sf);
                                    }
                                }
                                else if (columnDataProperties[j] == "TotalBeforeDiscount" ||
                                        columnDataProperties[j] == "TotalAfterDiscount" ||
                                        columnDataProperties[j] == "CashAmount" ||
                                        columnDataProperties[j] == "QRAmount" ||
                                        columnDataProperties[j] == "CreditAmount")
                                {
                                    // Right-align money values
                                    sf.Alignment = StringAlignment.Far;
                                    e.Graphics.DrawString(cellValue, normalFont, Brushes.Black, cellRect, sf);
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
                        sequenceNumber++; // เพิ่มลำดับ
                        validRowsPrinted++;
                    }

                    // พิมพ์แถวรวมท้ายตารางเฉพาะในหน้าสุดท้าย
                    if (isLastPage)
                    {
                        currentX = leftMargin;

                        // คอลัมน์ลำดับ - แสดงเป็นคำว่า "รวม"
                        RectangleF totalLabelRect = new RectangleF(currentX, yPosition, columnWidths[0] + columnWidths[1] + columnWidths[2] + columnWidths[3], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;

                            e.Graphics.FillRectangle(Brushes.LightGray, totalLabelRect);
                            e.Graphics.DrawRectangle(Pens.Black, totalLabelRect.X, totalLabelRect.Y, totalLabelRect.Width, totalLabelRect.Height);
                            e.Graphics.DrawString("รวมทั้งหมด", boldFont, Brushes.Black, totalLabelRect, sf);
                        }

                        currentX += columnWidths[0] + columnWidths[1] + columnWidths[2] + columnWidths[3];

                        // ยอดรวมก่อนหักส่วนลด
                        RectangleF totalBeforeRect = new RectangleF(currentX, yPosition, columnWidths[4], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Far;
                            sf.LineAlignment = StringAlignment.Center;

                            e.Graphics.FillRectangle(Brushes.LightGray, totalBeforeRect);
                            e.Graphics.DrawRectangle(Pens.Black, totalBeforeRect.X, totalBeforeRect.Y, totalBeforeRect.Width, totalBeforeRect.Height);
                            e.Graphics.DrawString(totalBeforeDiscount.ToString("N2"), boldFont, Brushes.Black, totalBeforeRect, sf);
                        }
                        currentX += columnWidths[4];

                        // ส่วนลด
                        RectangleF discountRect = new RectangleF(currentX, yPosition, columnWidths[5], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Far;
                            sf.LineAlignment = StringAlignment.Center;

                            e.Graphics.FillRectangle(Brushes.LightGray, discountRect);
                            e.Graphics.DrawRectangle(Pens.Black, discountRect.X, discountRect.Y, discountRect.Width, discountRect.Height);
                            e.Graphics.DrawString(totalDiscount.ToString("N2"), boldFont, Brushes.Black, discountRect, sf);
                        }
                        currentX += columnWidths[5];

                        // ยอดรวมหลังหักส่วนลด
                        RectangleF totalAfterRect = new RectangleF(currentX, yPosition, columnWidths[6], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Far;
                            sf.LineAlignment = StringAlignment.Center;

                            e.Graphics.FillRectangle(Brushes.LightGray, totalAfterRect);
                            e.Graphics.DrawRectangle(Pens.Black, totalAfterRect.X, totalAfterRect.Y, totalAfterRect.Width, totalAfterRect.Height);
                            e.Graphics.DrawString(totalAfterDiscount.ToString("N2"), boldFont, Brushes.Black, totalAfterRect, sf);
                        }
                        currentX += columnWidths[6];

                        // ชำระด้วยเงินสด
                        RectangleF cashRect = new RectangleF(currentX, yPosition, columnWidths[7], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Far;
                            sf.LineAlignment = StringAlignment.Center;

                            e.Graphics.FillRectangle(Brushes.LightGray, cashRect);
                            e.Graphics.DrawRectangle(Pens.Black, cashRect.X, cashRect.Y, cashRect.Width, cashRect.Height);
                            e.Graphics.DrawString(totalCashAmount.ToString("N2"), boldFont, Brushes.Black, cashRect, sf);
                        }
                        currentX += columnWidths[7];

                        // ชำระด้วย QR
                        RectangleF qrRect = new RectangleF(currentX, yPosition, columnWidths[8], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Far;
                            sf.LineAlignment = StringAlignment.Center;

                            e.Graphics.FillRectangle(Brushes.LightGray, qrRect);
                            e.Graphics.DrawRectangle(Pens.Black, qrRect.X, qrRect.Y, qrRect.Width, qrRect.Height);
                            e.Graphics.DrawString(totalQRAmount.ToString("N2"), boldFont, Brushes.Black, qrRect, sf);
                        }
                        currentX += columnWidths[8];

                        // ชำระด้วยบัตรเครดิต
                        RectangleF creditRect = new RectangleF(currentX, yPosition, columnWidths[9], rowHeight);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Far;
                            sf.LineAlignment = StringAlignment.Center;

                            e.Graphics.FillRectangle(Brushes.LightGray, creditRect);
                            e.Graphics.DrawRectangle(Pens.Black, creditRect.X, creditRect.Y, creditRect.Width, creditRect.Height);
                            e.Graphics.DrawString(totalCreditAmount.ToString("N2"), boldFont, Brushes.Black, creditRect, sf);
                        }

                        yPosition += rowHeight;

                        // เพิ่มข้อความจำนวนรายการใต้ตาราง
                        yPosition += 15;
                        int validRowCount = dgvReport.Rows.Cast<DataGridViewRow>().Count(r =>
                            r.Cells["CustomReceiptId"].Value != null);
                        string summaryText = $"จำนวนรายการขายทั้งหมด {validRowCount} รายการ";
                        e.Graphics.DrawString(summaryText, normalFont, Brushes.Black, leftMargin, yPosition);
                    }

                    // Add page number at the bottom right
                    int totalValidRows = dgvReport.Rows.Cast<DataGridViewRow>().Count(r =>
                        r.Cells["CustomReceiptId"].Value != null);

                    int totalPages = (int)Math.Ceiling((double)totalValidRows / rowsPerPage);
                    if (totalPages == 0) totalPages = 1; // Ensure at least one page

                    string pageText = $"หน้า {_currentPage + 1} จาก {totalPages}";

                    e.Graphics.DrawString(pageText, smallFont, Brushes.Black,
                        rightMargin - e.Graphics.MeasureString(pageText, smallFont).Width,
                        bottomMargin);

                    // Check if more pages are needed
                    if (endRow < dgvReport.Rows.Count)
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
                if (dgvReport.Rows.Count == 0)
                {
                    MessageBox.Show("ไม่มีข้อมูลที่จะส่งออก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check for valid data before exporting
                bool hasValidRows = false;
                foreach (DataGridViewRow row in dgvReport.Rows)
                {
                    if (row.Cells["CustomReceiptId"].Value != null)
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
                    sfd.Title = "บันทึกไฟล์รายงานการขาย";

                    // Get date range string for default filename
                    string dateRangeStr = $"{dtpCreateDateFirst.Value.ToString("yyyy-MM-dd")}_ถึง_{dtpCreateDateLast.Value.ToString("yyyy-MM-dd")}";
                    sfd.FileName = $"รายงานการขาย_{dateRangeStr}";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // Show waiting cursor
                        Cursor = Cursors.WaitCursor;

                        // Define columns to export (เพิ่มคอลัมน์วิธีการชำระเงิน)
                        var columnNames = new List<string> {
                        "วันที่ออกใบเสร็จ", "เลขที่ใบเสร็จ", "เลขที่รายการ",
                        "ยอดรวมก่อนหักส่วนลด", "ส่วนลด", "ยอดรวมหลังหักส่วนลด",
                        "ชำระด้วยเงินสด", "ชำระด้วย QR", "ชำระด้วยบัตรเครดิต"
                    };

                        var columnProperties = new List<string> {
                        "ReceiptDate", "CustomReceiptId", "CustomOrderId",
                        "TotalBeforeDiscount", "Discount", "TotalAfterDiscount",
                        "CashAmount", "QRAmount", "CreditAmount"
                    };

                        // Create a StringBuilder to build CSV content
                        StringBuilder csv = new StringBuilder();

                        // Add UTF-8 BOM for Thai character support
                        csv.Append("\uFEFF");

                        // Add CSV header row with column names
                        csv.AppendLine(string.Join(",", columnNames.Select(name => $"\"{name}\"")));

                        // Add data rows
                        decimal totalBeforeDiscount = 0;
                        decimal totalDiscount = 0;
                        decimal totalAfterDiscount = 0;
                        decimal totalCash = 0;
                        decimal totalQR = 0;
                        decimal totalCredit = 0;
                        int validRowCount = 0;

                        foreach (DataGridViewRow row in dgvReport.Rows)
                        {
                            // Skip rows with no data
                            if (row.Cells["CustomReceiptId"].Value == null)
                            {
                                continue;
                            }

                            validRowCount++;
                            var rowValues = new List<string>();

                            for (int i = 0; i < columnProperties.Count; i++)
                            {
                                string cellValue = "";
                                var cell = row.Cells[columnProperties[i]];

                                if (cell.Value != null && cell.Value != DBNull.Value)
                                {
                                    // Format based on column type
                                    if (columnProperties[i] == "ReceiptDate" && cell.Value is DateTime date)
                                    {
                                        cellValue = date.ToString("dd/MM/yyyy HH:mm");
                                    }
                                    else if (columnProperties[i] == "TotalBeforeDiscount")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalBeforeDiscount += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else if (columnProperties[i] == "Discount")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalDiscount += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else if (columnProperties[i] == "TotalAfterDiscount")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalAfterDiscount += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else if (columnProperties[i] == "CashAmount")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalCash += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else if (columnProperties[i] == "QRAmount")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalQR += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else if (columnProperties[i] == "CreditAmount")
                                    {
                                        if (decimal.TryParse(cell.Value.ToString(), out decimal amount))
                                        {
                                            totalCredit += amount;
                                            cellValue = amount.ToString("0.00");
                                        }
                                    }
                                    else
                                    {
                                        cellValue = cell.Value.ToString();
                                    }

                                    // สำหรับคอลัมน์ที่อาจเป็นตัวเลขแต่ต้องการเก็บ 0 นำหน้า
                                    if (columnProperties[i] == "CustomReceiptId" || columnProperties[i] == "CustomOrderId")
                                    {
                                        // บังคับให้ Excel ตีความเป็นข้อความ
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
                        csv.AppendLine($"\"สรุปรายงานการขาย\"");
                        csv.AppendLine($"\"ช่วงวันที่: {dtpCreateDateFirst.Value.ToString("dd/MM/yyyy")} ถึง {dtpCreateDateLast.Value.ToString("dd/MM/yyyy")}\"");
                        csv.AppendLine($"\"จำนวนรายการขายทั้งหมด: {validRowCount} รายการ\"");
                        csv.AppendLine($"\"ยอดรวมก่อนหักส่วนลด: {totalBeforeDiscount.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"ส่วนลดทั้งหมด: {totalDiscount.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"ยอดรวมหลังหักส่วนลด: {totalAfterDiscount.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"สรุปตามประเภทการชำระเงิน\"");
                        csv.AppendLine($"\"ชำระด้วยเงินสด: {totalCash.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"ชำระด้วย QR: {totalQR.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"ชำระด้วยบัตรเครดิต: {totalCredit.ToString("0.00")} บาท\"");
                        csv.AppendLine($"\"พิมพ์เมื่อ: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}\"");

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
    }
}