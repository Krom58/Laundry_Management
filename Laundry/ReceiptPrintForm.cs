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
using static Laundry_Management.Laundry.Find_Service;
using System.Drawing.Drawing2D;
using System.IO;

namespace Laundry_Management.Laundry
{
    public partial class ReceiptPrintForm : Form
    {
        public bool IsPrinted { get; private set; } = false;
        private readonly int _receiptId;
        private readonly OrderHeaderDto _header;
        private readonly List<OrderItemDto> _items;
        private PrintDocument _printDocument;
        private Bitmap _logoImage;
        private int _currentPage = 0;
        private int _totalPages = 1;
        private List<OrderItemDto> _remainingItems;
        public ReceiptPrintForm()
        {
            InitializeComponent();
        }

        public ReceiptPrintForm(int receiptId, OrderHeaderDto header, List<OrderItemDto> items)
        {
            InitializeComponent();

            _receiptId = receiptId;
            _header = header;
            _items = items;

            // เพิ่มการตรวจสอบและแก้ไขค่า PaymentMethod ตรงนี้
            if (_header != null && string.IsNullOrEmpty(_header.PaymentMethod))
            {
                // กำหนดค่าเริ่มต้น หรือลองดึงจากฐานข้อมูล
                using (SqlConnection conn = DBconfig.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT PaymentMethod FROM Receipt WHERE ReceiptID = @rid", conn))
                    {
                        cmd.Parameters.AddWithValue("@rid", receiptId);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            _header.PaymentMethod = result.ToString();
                        }
                    }
                }
            }

            // เตรียม PrintDocument
            _printDocument = new PrintDocument();
            _printDocument.PrintPage += PrintPageHandler;

            // Set A5 paper size in portrait orientation
            PaperSize paperSize = new PaperSize("A5", 583, 827); // A5 dimensions in hundredths of an inch
            _printDocument.DefaultPageSettings.PaperSize = paperSize;
            _printDocument.DefaultPageSettings.Landscape = false; // false = portrait orientation

            // Adjust margins to be smaller for A5
            _printDocument.DefaultPageSettings.Margins = new Margins(15, 40, 15, 15);

            // โหลดโลโก้
            LoadLogoImage();
        }

        private void LoadLogoImage()
        {
            try
            {
                // โฟลเดอร์ Images จะถูกคัดลอกมาพร้อม exe 
                string imagesFolder = Path.Combine(Application.StartupPath, "Images");
                string logoPath = Path.Combine(imagesFolder, "Asia.jpg");

                if (File.Exists(logoPath))
                {
                    _logoImage = new Bitmap(logoPath);
                }
                else
                {
                    // ถ้าไม่เจอ ให้ใช้ dummy
                    _logoImage = CreateDummyLogo();
                }
            }
            catch
            {
                _logoImage = CreateDummyLogo();
            }
        }

        private Bitmap CreateDummyLogo()
        {
            // สร้างรูปเปล่าที่มีข้อความ "LOGO" ไว้ใช้แทนในกรณีที่หารูปไม่เจอ
            Bitmap bmp = new Bitmap(100, 100);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawRectangle(Pens.Gray, 0, 0, 99, 99);

                using (Font f = new Font("Arial", 14, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("LOGO", f, Brushes.Gray, new RectangleF(0, 0, 100, 100), sf);
                }
            }
            return bmp;
        }

        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Page setup measurements
            float leftX = e.MarginBounds.Left;
            float topY = e.MarginBounds.Top;
            float rightX = e.MarginBounds.Right;
            float pageWidth = e.PageBounds.Width;
            float pageBottom = e.PageBounds.Bottom - 20; // Bottom of page with margin

            // Initialize _remainingItems on first page
            if (_currentPage == 0)
            {
                _remainingItems = new List<OrderItemDto>(_items);
            }

            // Reduced font sizes
            using (Font titleF = new Font("Tahoma", 9f, FontStyle.Bold))     // Smaller title font
            using (Font headerF = new Font("Tahoma", 8f, FontStyle.Bold))    // Smaller header font
            using (Font subF = new Font("Tahoma", 7f))                       // Smaller sub font
            using (Font bodyF = new Font("Tahoma", 7f))                      // Smaller body font
            {
                float y = topY;
                float infoLineHeight = subF.GetHeight(g) * 1.2f;

                // Draw header only on the first page
                if (_currentPage == 0)
                {
                    // Add the logo at the top center
                    if (_logoImage != null)
                    {
                        // Set the desired logo height (adjust as needed)
                        float logoHeight = 60;
                        // Calculate width while maintaining aspect ratio
                        float aspectRatio = (float)_logoImage.Width / _logoImage.Height;
                        float logoWidth = logoHeight * aspectRatio;

                        // Calculate position to center the logo horizontally
                        float logoX = (leftX + rightX - logoWidth) / 2;

                        // Draw the logo
                        g.DrawImage(_logoImage, logoX, y, logoWidth, logoHeight);

                        // Move y position down after logo
                        y += logoHeight + 5; // Add some spacing after the logo
                    }

                    // 1. Company info (top left) - now positioned after the logo
                    g.DrawString("บริษัท เอเชียโฮเต็ล จำกัด ( มหาชน ) สำนักงานใหญ่", headerF, Brushes.Black, leftX, y);
                    y += infoLineHeight;

                    g.DrawString("296 ถนนพญาไท แขวงถนนเพชรบุรี เขตราชเทวี กรุงเทพมหานคร 10400", subF, Brushes.Black, leftX, y);
                    y += infoLineHeight;

                    g.DrawString("เลขประจำตัวผู้เสียภาษี 0107535000346", subF, Brushes.Black, leftX, y);
                    y += infoLineHeight;

                    g.DrawString("โทร 02-2170808 ต่อ 5340", subF, Brushes.Black, leftX, y);
                    y += infoLineHeight;

                    g.DrawString("เปิดบริการ 7.00 -19.00 น.", subF, Brushes.Black, leftX, y);
                    y += infoLineHeight;

                    // 2. Title box (top right) - smaller text, positioned after the logo
                    float titleBoxWidth = 220;
                    float titleBoxHeight = 36;
                    float titleBoxX = rightX - titleBoxWidth;
                    float titleBoxY = y - infoLineHeight * 5; // Align with company info

                    using (SolidBrush titleBg = new SolidBrush(Color.FromArgb(200, 210, 245)))
                    using (Pen borderPen = new Pen(Color.FromArgb(100, 100, 200), 1))
                    {
                        g.FillRectangle(titleBg, titleBoxX, titleBoxY, titleBoxWidth, titleBoxHeight);
                        g.DrawRectangle(borderPen, titleBoxX, titleBoxY, titleBoxWidth, titleBoxHeight);
                    }

                    // Use smaller font for the title
                    using (Font smallerTitleF = new Font("Tahoma", 8f, FontStyle.Bold))
                    {
                        string titleText = "ใบเสร็จรับเงิน/ใบกำกับภาษีอย่างย่อ";
                        SizeF titleSize = g.MeasureString(titleText, smallerTitleF);
                        float titleY = titleBoxY + (titleBoxHeight - titleSize.Height) / 2;
                        float titleX = titleBoxX + (titleBoxWidth - titleSize.Width) / 2; // Center horizontally
                        g.DrawString(titleText, smallerTitleF, new SolidBrush(Color.FromArgb(0, 0, 102)), titleX, titleY);
                    }

                    // 3. Customer/Receipt info boxes (side by side with adjusted dimensions)
                    float boxY = y + 8; // Add more space between company info and boxes
                    float customerBoxHeight = 85; // Taller box
                    float customerBoxWidth = 280; // Narrower box
                    float receiptBoxWidth = 140;
                    float receiptBoxHeight = customerBoxHeight;
                    float receiptBoxX = rightX - receiptBoxWidth; // Right aligned

                    using (Pen boxPen = new Pen(Color.FromArgb(100, 100, 200), 1))
                    {
                        // Customer box - narrower but taller
                        g.DrawRectangle(boxPen, leftX, boxY, customerBoxWidth, customerBoxHeight);
                        float bx = leftX + 5, by = boxY + 5;

                        g.DrawString("ลูกค้า / Customers: " + _header.CustomerName, subF, Brushes.Black, bx, by);
                        by += infoLineHeight;

                        g.DrawString("หมายเลขรับผ้า: " + _header.CustomOrderId, subF, Brushes.Black, bx, by);
                        by += infoLineHeight;

                        g.DrawString("ที่อยู่ / Address:", subF, Brushes.Black, bx, by);
                        by += infoLineHeight;

                        g.DrawString("เลขประจำตัวผู้เสียภาษีอากร", subF, Brushes.Black, bx, by);
                        by += infoLineHeight;

                        g.DrawString("โทรศัพท์: " + _header.Phone, subF, Brushes.Black, bx, by);

                        // Receipt box - right aligned with centered text
                        g.DrawRectangle(boxPen, receiptBoxX, boxY, receiptBoxWidth, receiptBoxHeight);

                        // For receipt number
                        string receiptLabel = "เลขที่ ";
                        string receiptValue = _header.CustomReceiptId;
                        string fullReceiptText = receiptLabel + receiptValue;

                        // Calculate the size first before using it
                        SizeF fullReceiptSize = g.MeasureString(fullReceiptText, subF);

                        // Create a StringFormat for center alignment
                        StringFormat centerFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Near
                        };

                        // Create rectangle for the receipt text area
                        RectangleF receiptTextRect = new RectangleF(receiptBoxX, boxY + 5, receiptBoxWidth, fullReceiptSize.Height);
                        g.DrawString(fullReceiptText, subF, Brushes.Black, receiptTextRect, centerFormat);

                        // For date - using the same centering approach
                        string dateLabel = "วันที่ ";
                        string dateValue = $"{_header.OrderDate.Day:00} / {_header.OrderDate.Month:00} / {_header.OrderDate.Year + 543}";
                        string fullDateText = dateLabel + dateValue;

                        // Calculate vertical spacing with proper positioning
                        float dateY = boxY + 5 + fullReceiptSize.Height + 8;

                        // Create rectangle for the date text area
                        RectangleF dateTextRect = new RectangleF(receiptBoxX, dateY, receiptBoxWidth, g.MeasureString(fullDateText, subF).Height);
                        g.DrawString(fullDateText, subF, Brushes.Black, dateTextRect, centerFormat);
                    }

                    // Update y to position where table will start
                    y = boxY + customerBoxHeight + 15; // More space between boxes and table
                }
                else
                {
                    // For continuation pages, add a simple header
                    string continuationHeader = $"ใบเสร็จรับเงิน/ใบกำกับภาษีอย่างย่อ (ต่อ) - เลขที่ {_header.CustomReceiptId}";
                    g.DrawString(continuationHeader, headerF, Brushes.Black, leftX, y);
                    y += headerF.GetHeight(g) * 1.5f;
                }

                // Store the Y position to place the table
                float tableY = y;
                float endTableY = y;

                // Draw the table with pagination
                List<OrderItemDto> itemsForCurrentPage = new List<OrderItemDto>();
                bool hasMoreItems = DrawModifiedServiceTableWithPagination(g, bodyF, leftX, ref endTableY, rightX, ref itemsForCurrentPage);

                // On the last page, draw the signature line and payment info
                if (!hasMoreItems)
                {
                    // Draw signature line and payment method info
                    DrawSignatureLine(g, bodyF, rightX, endTableY);

                    // Draw payment method info
                    float paymentMethodY = endTableY + 35; // Position below the signature line
                    string paymentMethod = "ชำระโดย: ";

                    // Determine the payment method and highlight it
                    if (!string.IsNullOrEmpty(_header.PaymentMethod))
                    {
                            // สำหรับวิธีการชำระเงินอื่นๆ ที่ไม่อยู่ในเงื่อนไขด้านบน แสดงทั้งข้อความดั้งเดิม
                            paymentMethod += $"{_header.PaymentMethod}";
                        
                    }
                    g.DrawString(paymentMethod, bodyF, Brushes.Black, leftX + 5, paymentMethodY);
                }

                // Add page number at the bottom
                using (Font smallFont = new Font("Tahoma", 6f))
                {
                    string pageText = $"หน้า {_currentPage + 1} / {_totalPages}";
                    SizeF pageTextSize = g.MeasureString(pageText, smallFont);
                    g.DrawString(pageText, smallFont, Brushes.Black,
                        rightX - pageTextSize.Width, pageBottom);
                }

                // Set HasMorePages based on whether we have more items to print
                if (hasMoreItems)
                {
                    _currentPage++;
                    e.HasMorePages = true;
                }
                else
                {
                    // Reset for next print job
                    _currentPage = 0;
                    e.HasMorePages = false;
                }
            }
        }

        // Helper method to convert number to Thai text
        private string ThaiNumberToText(decimal number)
        {
            if (number == 0)
                return "ศูนย์บาทถ้วน";

            // Split the number into integer and decimal parts
            string[] parts = number.ToString("0.00").Split('.');
            string intPart = parts[0];
            string decPart = parts[1];

            // Process integer part
            string intText = ConvertIntegerToThaiText(intPart);

            // Process decimal part
            string decText = "";
            if (decPart != "00")
            {
                decText = ConvertIntegerToThaiText(decPart);
                // Add "สตางค์" only if decimal part is not zero
                if (!string.IsNullOrEmpty(decText))
                    decText += "สตางค์";
            }

            // Combine with the appropriate suffixes
            string result = intText + "บาท";

            // Add decimal part if exists, otherwise add "ถ้วน"
            if (!string.IsNullOrEmpty(decText))
                result += decText;
            else
                result += "ถ้วน";

            return result;
        }
        private string ConvertIntegerToThaiText(string number)
        {
            // Thai digit names
            string[] digitNames = { "", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า" };

            // Thai position names (units, tens, hundreds, etc.)
            string[] positionNames = { "", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน", "ล้าน" };

            // Remove leading zeros
            number = number.TrimStart('0');

            // Handle empty string (all zeros)
            if (string.IsNullOrEmpty(number))
                return "";

            StringBuilder result = new StringBuilder();

            // Process each million group separately (ล้าน)
            for (int millionGroup = 0; millionGroup < (number.Length + 5) / 6; millionGroup++)
            {
                // Extract current million group
                int startPos = Math.Max(0, number.Length - (millionGroup + 1) * 6);
                int length = Math.Min(6, number.Length - millionGroup * 6);
                string group = number.Substring(startPos, length);

                if (group == "000000")
                    continue;

                StringBuilder groupText = new StringBuilder();

                // Process each digit in the current group
                for (int i = 0; i < group.Length; i++)
                {
                    int digit = group[i] - '0';
                    int position = group.Length - i - 1;

                    // Skip if digit is 0
                    if (digit == 0)
                        continue;

                    // Special case for digit 1 at tens position
                    if (digit == 1 && position == 1)
                        groupText.Append("สิบ");
                    // Special case for digit 2 at tens position
                    else if (digit == 2 && position == 1)
                        groupText.Append("ยี่สิบ");
                    // General case
                    else
                        groupText.Append(digitNames[digit] + positionNames[position]);
                }

                // Append million if not the last group and not empty
                if (millionGroup > 0 && groupText.Length > 0)
                    groupText.Append("ล้าน");

                // Prepend to result (we process from right to left)
                result.Insert(0, groupText);
            }

            // Special case: if the number ends with 1, use "เอ็ด" instead of "หนึ่ง"
            // but only if it's not exactly "หนึ่ง" (1)
            if (number.Length > 1 && number[number.Length - 1] == '1')
            {
                result.Replace("หนึ่ง", "เอ็ด", result.Length - 3, 3);
            }

            return result.ToString();
        }

        private void PrintDoc_Click(object sender, EventArgs e)
        {
            // Existing implementation
            using (var dlg = new PrintDialog { Document = _printDocument })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _printDocument.Print();
                        IsPrinted = true;
                        UpdateReceiptPrintStatus(_receiptId, _header.OrderID);
                        this.DialogResult = DialogResult.OK;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"เกิดข้อผิดพลาดขณะพิมพ์: {ex.Message}", "ข้อผิดพลาด",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        IsPrinted = false;
                        this.DialogResult = DialogResult.Cancel;
                    }
                }
                else
                {
                    IsPrinted = false;
                    this.DialogResult = DialogResult.Cancel;
                }
            }
            this.Close();
        }

        private void UpdateReceiptPrintStatus(int receiptId, int orderId)
        {
            using (var connection = Laundry_Management.Laundry.DBconfig.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // อัพเดทสถานะ Receipt
                        using (var cmd = new SqlCommand(
                            "UPDATE Receipt SET ReceiptStatus = @status WHERE ReceiptID = @rid",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@status", "พิมพ์เรียบร้อยแล้ว");
                            cmd.Parameters.AddWithValue("@rid", receiptId);
                            cmd.ExecuteNonQuery();
                        }

                        // บันทึกประวัติการเปลี่ยนสถานะ
                        using (var cmd = new SqlCommand(
                            @"INSERT INTO ReceiptStatusHistory (ReceiptID, PreviousStatus, NewStatus, ChangeBy)
                                  VALUES (@rid, @prevStatus, @newStatus, @changeBy)",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@rid", receiptId);
                            cmd.Parameters.AddWithValue("@prevStatus", "ออกใบเสร็จแล้ว");
                            cmd.Parameters.AddWithValue("@newStatus", "พิมพ์เรียบร้อยแล้ว");
                            cmd.Parameters.AddWithValue("@changeBy", Environment.UserName);
                            cmd.ExecuteNonQuery();
                        }

                        // อัพเดทสถานะ OrderHeader
                        using (var cmd = new SqlCommand(
                            "UPDATE OrderHeader SET OrderStatus = @status, IsReceiptPrinted = 1 WHERE OrderID = @oid",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@status", "ออกใบเสร็จแล้ว");
                            cmd.Parameters.AddWithValue("@oid", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"เกิดข้อผิดพลาดในการอัพเดทสถานะ: {ex.Message}", "ข้อผิดพลาด",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DrawModifiedServiceTable(Graphics g, Font font, float leftX, ref float y, float rightX)
        {
            // Colors matching the printed output
            Color tableHeaderBgColor = Color.FromArgb(200, 210, 245);
            Color tableBorderColor = Color.FromArgb(100, 100, 200);
            Color discountColor = Color.Red; // Color for discount text

            // Define columns
            int cols = 5;
            float tableWidth = rightX - leftX;
            float[] colWidthPercents = { 0.08f, 0.47f, 0.15f, 0.15f, 0.15f };
            float[] colWidths = new float[cols];
            float[] xs = new float[cols + 1];

            xs[0] = leftX;
            for (int i = 0; i < cols; i++)
            {
                colWidths[i] = tableWidth * colWidthPercents[i];
                xs[i + 1] = xs[i] + colWidths[i];
            }

            float rowHeight = 20; // Row height for data rows
            float headerRowHeight = 32; // Taller header row

            // Border pen
            Pen tablePen = new Pen(tableBorderColor, 0.5f);

            // Calculate available space for the table
            // Leave space for signature (approximately 30 units of height)
            float availableHeight = 650 - y - 30;

            // Items plus summary rows (subtotal, discount if any, total)
            int totalDataRows = _items.Count;
            int summaryRows = _header.TodayDiscount > 0 ? 3 : 2; // Include subtotal and total (+ discount if applicable)

            // Calculate how many empty rows we need to fill the page
            int maxEmptyRows = Math.Max(10, (int)((availableHeight - (headerRowHeight + totalDataRows * rowHeight)) / rowHeight) - summaryRows);

            // Draw table header (taller for two rows of text)
            g.FillRectangle(new SolidBrush(tableHeaderBgColor), leftX, y, tableWidth, headerRowHeight);
            g.DrawRectangle(tablePen, leftX, y, tableWidth, headerRowHeight);

            // Draw column dividers for header
            for (int i = 1; i < cols; i++)
            {
                g.DrawLine(tablePen, xs[i], y, xs[i], y + headerRowHeight);
            }

            // Draw header text - center aligned in two rows
            using (Font headerFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
            {
                string[] headers = { "ลำดับที่", "รายการ", "จำนวน", "หน่วยละ", "จำนวนเงิน" };
                string[] subHeaders = { "Item", "Descriptions", "Quantity", "Unit price", "Amount" };

                // Vertical spacing within header - adjusted for taller header
                float thaiTextY = y + 5; // Increased padding from top
                float engTextY = thaiTextY + headerFont.GetHeight(g) + 3; // More space between Thai and English text

                for (int i = 0; i < cols; i++)
                {
                    float colCenter = xs[i] + colWidths[i] / 2;

                    // Thai header on top row
                    SizeF thaiHeaderSize = g.MeasureString(headers[i], headerFont);
                    g.DrawString(headers[i], headerFont, Brushes.Black,
                              colCenter - thaiHeaderSize.Width / 2, thaiTextY);

                    // English header on bottom row
                    SizeF engHeaderSize = g.MeasureString(subHeaders[i], font);
                    g.DrawString(subHeaders[i], font, Brushes.Black,
                              colCenter - engHeaderSize.Width / 2, engTextY);
                }
            }

            y += headerRowHeight;
            float tableStartY = y; // Remember where the data rows begin

            // Draw item rows
            int itemNumber = 1;
            foreach (var item in _items)
            {
                // Draw row border - just horizontal lines
                g.DrawLine(tablePen, leftX, y, rightX, y);
                if (itemNumber == _items.Count)
                {
                    g.DrawLine(tablePen, leftX, y + rowHeight, rightX, y + rowHeight);
                }

                // Draw vertical dividers for the data row
                g.DrawLine(tablePen, leftX, y, leftX, y + rowHeight);
                for (int i = 1; i < cols; i++)
                {
                    g.DrawLine(tablePen, xs[i], y, xs[i], y + rowHeight);
                }
                g.DrawLine(tablePen, rightX, y, rightX, y + rowHeight);

                // Item number (centered)
                string itemNum = itemNumber.ToString();
                SizeF numSize = g.MeasureString(itemNum, font);
                g.DrawString(itemNum, font, Brushes.Black,
                           xs[0] + (colWidths[0] - numSize.Width) / 2,
                           y + (rowHeight - numSize.Height) / 2);

                // Item name (left aligned)
                g.DrawString(item.ItemName, font, Brushes.Black,
                           xs[1] + 5, y + (rowHeight - font.GetHeight(g)) / 2);

                // Quantity (centered) - align vertically with text
                string qty = item.Quantity.ToString();
                SizeF qtySize = g.MeasureString(qty, font);
                float qtyMiddleY = y + (rowHeight - qtySize.Height) / 2;
                g.DrawString(qty, font, Brushes.Black,
                           xs[2] + (colWidths[2] - qtySize.Width) / 2, qtyMiddleY);

                // Unit price (right aligned) - align vertically with text
                var price = (item.TotalAmount / item.Quantity).ToString("N2");
                SizeF priceSize = g.MeasureString(price, font);
                float priceMiddleY = y + (rowHeight - priceSize.Height) / 2;
                g.DrawString(price, font, Brushes.Black,
                           xs[3] + colWidths[3] - priceSize.Width - 5, priceMiddleY);

                // Total amount (right aligned) - align vertically with text
                var amount = item.TotalAmount.ToString("N2");
                SizeF amountSize = g.MeasureString(amount, font);
                float amountMiddleY = y + (rowHeight - amountSize.Height) / 2;
                g.DrawString(amount, font, Brushes.Black,
                           xs[4] + colWidths[4] - amountSize.Width - 5, amountMiddleY);

                y += rowHeight;
                itemNumber++;
            }

            // Draw the empty rows section - just the outer frame without internal dividers
            // Calculate height of empty section
            float emptyRowsStartY = y;
            float emptyRowsHeight = maxEmptyRows * rowHeight;

            // Draw vertical borders of empty section
            g.DrawLine(tablePen, leftX, emptyRowsStartY, leftX, emptyRowsStartY + emptyRowsHeight);
            g.DrawLine(tablePen, rightX, emptyRowsStartY, rightX, emptyRowsStartY + emptyRowsHeight);

            // Update y to account for empty rows
            y += emptyRowsHeight;

            // Draw the final horizontal line at the bottom of empty section
            g.DrawLine(tablePen, leftX, y, rightX, y);

            // ----------------------------------------------------------------------
            // Draw summary section at the bottom - Modified to match the first image
            // ----------------------------------------------------------------------

            float summaryRowHeight = 22; // Slightly taller for summary rows

            // 1. Subtotal row
            float summaryRowStartY = y;

            // Calculate horizontal line positions first - Fixed to declare thaiBahtLineY before it's used
            float thaiBahtLineY = y + (summaryRows - 1) * summaryRowHeight;

            // Draw the outer frame for the summary section
            g.DrawLine(tablePen, leftX, y, rightX, y); // Top border
            g.DrawLine(tablePen, leftX, y, leftX, y + (summaryRows * summaryRowHeight)); // Left border
            g.DrawLine(tablePen, rightX, y, rightX, y + (summaryRows * summaryRowHeight)); // Right border
            g.DrawLine(tablePen, leftX, y + (summaryRows * summaryRowHeight), rightX, y + (summaryRows * summaryRowHeight)); // Bottom border

            // Draw vertical dividers - MODIFIED to keep all needed dividers
            g.DrawLine(tablePen, xs[3], y, xs[3], thaiBahtLineY); // Divider for "ยอดรวม" and "ส่วนลด" rows only
            g.DrawLine(tablePen, xs[4], y, xs[4], y + (summaryRows * summaryRowHeight)); // Divider before amount column

            // Draw horizontal divider after first row
            g.DrawLine(tablePen, xs[3], y + summaryRowHeight, rightX, y + summaryRowHeight);

            // If discount exists, draw horizontal divider after second row
            if (_header.TodayDiscount > 0)
            {
                g.DrawLine(tablePen, xs[3], y + (summaryRowHeight * 2), rightX, y + (summaryRowHeight * 2));
            }

            // Draw horizontal line at the bottom for the Thai baht text
            g.DrawLine(tablePen, leftX, thaiBahtLineY, rightX, thaiBahtLineY);

            // Subtotal value in top right columns
            string subtotalLabel = "ยอดรวม";
            string subtotalValue = _header.SubTotal.ToString("N2");
            float verticalCenter = y + (summaryRowHeight - font.GetHeight(g)) / 2;

            // Label in the 4th column
            g.DrawString(subtotalLabel, font, Brushes.Black,
                        xs[3] + 5, verticalCenter);

            // Value in the 5th column (right aligned)
            g.DrawString(subtotalValue, font, Brushes.Black,
                       rightX - 5 - g.MeasureString(subtotalValue, font).Width, verticalCenter);

            // 2. Discount row (if applicable)
            if (_header.TodayDiscount > 0)
            {
                y += summaryRowHeight;
                verticalCenter = y + (summaryRowHeight - font.GetHeight(g)) / 2;

                string discountLabel = "ส่วนลด";
                string discountValue;

                if (_header.IsTodayDiscountPercent)
                {
                    // Calculate the actual discount amount if it's stored as a percentage
                    decimal discountAmount = Math.Round(_header.SubTotal * _header.TodayDiscount / 100m, 2);
                    discountValue = $"- {discountAmount:N2}";
                }
                else
                {
                    // For fixed amount discount, use the value directly
                    discountValue = $"- {_header.TodayDiscount:N2}";
                }

                g.DrawString(discountLabel, font, Brushes.Red,
                            xs[3] + 5, verticalCenter);
                g.DrawString(discountValue, font, Brushes.Red,
                           rightX - 5 - g.MeasureString(discountValue, font).Width, verticalCenter);
            }

            // 3. Total row with bold text - Bottom right section
            y += summaryRowHeight;
            verticalCenter = y + (summaryRowHeight - font.GetHeight(g)) / 2;

            // Thai text at the bottom left (in the first 3 columns combined)
            string thaiBaht = "ตัวอักษร       ( " + ThaiNumberToText(_header.DiscountedTotal) + " )";
            float textStartX = leftX + 5; // Left margin for Thai text
            g.DrawString(thaiBaht, font, Brushes.Black, textStartX, verticalCenter);

            float paymentMethodY = verticalCenter + font.GetHeight(g) + 35; // Position below the Thai baht text
            string paymentMethod = "ชำระโดย: ";

            // Determine the payment method and highlight it
            if (!string.IsNullOrEmpty(_header.PaymentMethod))
            {
                // Format the payment method display
                if (_header.PaymentMethod.ToUpper().Contains("เงินสด"))
                {
                    paymentMethod += "☑ เงินสด   ☐ บัตรเครดิต   ☐ QR Code";
                }
                else if (_header.PaymentMethod.ToUpper().Contains("บัตรเครดิต"))
                {
                    paymentMethod += "☐ เงินสด   ☑ บัตรเครดิต   ☐ QR Code";
                }
                else if (_header.PaymentMethod.ToUpper().Contains("QR Code"))
                {
                    paymentMethod += "☐ เงินสด   ☐ บัตรเครดิต   ☑ QR Code";
                }
                else
                {
                    // For any other payment method or if none is specified
                    paymentMethod += _header.PaymentMethod;
                }
            }
            else
            {
                // If no payment method is provided
                paymentMethod += "☐ เงินสด   ☐ บัตรเครดิต   ☐ QR Code";
            }

            g.DrawString(paymentMethod, font, Brushes.Black, textStartX, paymentMethodY);

            // Total amount text and value
            string totalLabel = "ราคาสุทธิ (รวมภาษีมูลค่าเพิ่ม)";
            string totalValue = _header.DiscountedTotal.ToString("N2");

            // Add a vertical divider in front of totalLabel (at column 2)
            g.DrawLine(tablePen, xs[2], thaiBahtLineY, xs[2], y + summaryRowHeight); // Add vertical line only for last row

            // Move the total label to column 3 (index 2)
            float totalLabelX = xs[2] + 5; // Position at column 3 with small padding

            // Draw the total label in column 3 (instead of column 4)
            g.DrawString(totalLabel, font, Brushes.Black, totalLabelX, verticalCenter);

            // Total value - right aligned in the last column
            using (Font boldFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
            {
                // Right align the total amount
                float totalValueWidth = g.MeasureString(totalValue, boldFont).Width;
                float totalValueX = rightX - 5 - totalValueWidth;
                g.DrawString(totalValue, boldFont, Brushes.Black, totalValueX, verticalCenter);

                // Draw red underline ONLY under the total amount value
                using (Pen redPen = new Pen(Color.Red, 2))
                {
                    g.DrawLine(redPen, totalValueX, y + summaryRowHeight - 2,
                              totalValueX + totalValueWidth, y + summaryRowHeight - 2);
                }
            }

            // Update final y position
            y += summaryRowHeight;
        }
        // Method for drawing signature line with increased space
        private void DrawSignatureLine(Graphics g, Font font, float rightX, float y)
        {
            // Draw signature line with increased space from the table
            float signatureLineWidth = 170;
            float signatureX = rightX - 170;

            // Increase the vertical space before drawing the signature line
            float signatureY = y + 40; // Increased space from 35 to 40

            // Draw the line first
            g.DrawLine(Pens.Black, signatureX, signatureY, signatureX + signatureLineWidth, signatureY);

            // Draw "ผู้รับเงิน" label below the line
            string signatureLabel = "ผู้รับเงิน";
            SizeF labelSize = g.MeasureString(signatureLabel, font);
            float labelX = signatureX + (signatureLineWidth - labelSize.Width) / 2;
            g.DrawString(signatureLabel, font, Brushes.Black, labelX, signatureY + 5);
        }
        private bool DrawModifiedServiceTableWithPagination(Graphics g, Font font, float leftX, ref float y, float rightX, ref List<OrderItemDto> itemsForCurrentPage)
        {
            // Colors matching the printed output
            Color tableHeaderBgColor = Color.FromArgb(200, 210, 245);
            Color tableBorderColor = Color.FromArgb(100, 100, 200);
            Color discountColor = Color.Red; // Color for discount text

            // Define columns
            int cols = 5;
            float tableWidth = rightX - leftX;
            float[] colWidthPercents = { 0.08f, 0.47f, 0.15f, 0.15f, 0.15f };
            float[] colWidths = new float[cols];
            float[] xs = new float[cols + 1];

            xs[0] = leftX;
            for (int i = 0; i < cols; i++)
            {
                colWidths[i] = tableWidth * colWidthPercents[i];
                xs[i + 1] = xs[i] + colWidths[i];
            }
            float endTableY = y;
            float rowHeight = 20; // Row height for data rows
            float headerRowHeight = 32; // Taller header row

            // Border pen
            Pen tablePen = new Pen(tableBorderColor, 0.5f);

            // Calculate available space for the table (leave space for footer with page number)
            float availableHeight = 650 - y - 30;

            // If this is the last page, reserve space for summary and signature
            int summaryRows = _header.TodayDiscount > 0 ? 3 : 2; // Include subtotal and total (+ discount if applicable)
            float summaryHeight = summaryRows * 22 + 40; // Summary rows + space for signature

            // We'll only show the summary on the last page
            float tableAvailableHeight = availableHeight;
            if (_remainingItems.Count <= GetMaxItemsPerPage(g, font, y, availableHeight))
            {
                tableAvailableHeight -= summaryHeight;
            }

            // Calculate max items that can fit on this page
            int maxItemsOnPage = GetMaxItemsPerPage(g, font, y, tableAvailableHeight);

            // Get items for this page
            itemsForCurrentPage = _remainingItems.Take(maxItemsOnPage).ToList();

            // Update the total pages count if needed
            if (_currentPage == 0)
            {
                int totalItems = _items.Count;
                int itemsPerPage = maxItemsOnPage;
                _totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
                if (_totalPages == 0) _totalPages = 1;
            }

            // Draw table header (taller for two rows of text)
            g.FillRectangle(new SolidBrush(tableHeaderBgColor), leftX, y, tableWidth, headerRowHeight);
            g.DrawRectangle(tablePen, leftX, y, tableWidth, headerRowHeight);

            // Draw column dividers for header
            for (int i = 1; i < cols; i++)
            {
                g.DrawLine(tablePen, xs[i], y, xs[i], y + headerRowHeight);
            }

            // Draw header text - center aligned in two rows
            using (Font headerFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
            {
                string[] headers = { "ลำดับที่", "รายการ", "จำนวน", "หน่วยละ", "จำนวนเงิน" };
                string[] subHeaders = { "Item", "Descriptions", "Quantity", "Unit price", "Amount" };

                // Vertical spacing within header - adjusted for taller header
                float thaiTextY = y + 5; // Increased padding from top
                float engTextY = thaiTextY + headerFont.GetHeight(g) + 3; // More space between Thai and English text

                for (int i = 0; i < cols; i++)
                {
                    float colCenter = xs[i] + colWidths[i] / 2;

                    // Thai header on top row
                    SizeF thaiHeaderSize = g.MeasureString(headers[i], headerFont);
                    g.DrawString(headers[i], headerFont, Brushes.Black,
                              colCenter - thaiHeaderSize.Width / 2, thaiTextY);

                    // English header on bottom row
                    SizeF engHeaderSize = g.MeasureString(subHeaders[i], font);
                    g.DrawString(subHeaders[i], font, Brushes.Black,
                              colCenter - engHeaderSize.Width / 2, engTextY);
                }
            }

            y += headerRowHeight;
            float tableStartY = y; // Remember where the data rows begin

            // Draw item rows for this page
            int startItemNumber = 1;
            if (_currentPage > 0)
            {
                // คำนวณจำนวนรายการที่พิมพ์ไปแล้วในหน้าก่อนหน้า
                startItemNumber = _items.Count - _remainingItems.Count + 1;
            }
            for (int i = 0; i < itemsForCurrentPage.Count; i++)
            {
                var item = itemsForCurrentPage[i];
                int itemNumber = startItemNumber + i;

                // Draw row border - just horizontal lines
                g.DrawLine(tablePen, leftX, y, rightX, y);
                if (i == itemsForCurrentPage.Count - 1)
                {
                    g.DrawLine(tablePen, leftX, y + rowHeight, rightX, y + rowHeight);
                }

                // Draw vertical dividers for the data row
                g.DrawLine(tablePen, leftX, y, leftX, y + rowHeight);
                for (int j = 1; j < cols; j++)
                {
                    g.DrawLine(tablePen, xs[j], y, xs[j], y + rowHeight);
                }
                g.DrawLine(tablePen, rightX, y, rightX, y + rowHeight);

                // Item number (centered)
                string itemNum = itemNumber.ToString();
                SizeF numSize = g.MeasureString(itemNum, font);
                g.DrawString(itemNum, font, Brushes.Black,
                           xs[0] + (colWidths[0] - numSize.Width) / 2,
                           y + (rowHeight - numSize.Height) / 2);

                // Item name (left aligned)
                g.DrawString(item.ItemName, font, Brushes.Black,
                           xs[1] + 5, y + (rowHeight - font.GetHeight(g)) / 2);

                // Quantity (centered) - align vertically with text
                string qty = item.Quantity.ToString();
                SizeF qtySize = g.MeasureString(qty, font);
                float qtyMiddleY = y + (rowHeight - qtySize.Height) / 2;
                g.DrawString(qty, font, Brushes.Black,
                           xs[2] + (colWidths[2] - qtySize.Width) / 2, qtyMiddleY);

                // Unit price (right aligned) - align vertically with text
                var price = (item.TotalAmount / item.Quantity).ToString("N2");
                SizeF priceSize = g.MeasureString(price, font);
                float priceMiddleY = y + (rowHeight - priceSize.Height) / 2;
                g.DrawString(price, font, Brushes.Black,
                           xs[3] + colWidths[3] - priceSize.Width - 5, priceMiddleY);

                // Total amount (right aligned) - align vertically with text
                var amount = item.TotalAmount.ToString("N2");
                SizeF amountSize = g.MeasureString(amount, font);
                float amountMiddleY = y + (rowHeight - amountSize.Height) / 2;
                g.DrawString(amount, font, Brushes.Black,
                           xs[4] + colWidths[4] - amountSize.Width - 5, amountMiddleY);

                y += rowHeight;
            }

            // Remove the items we've drawn from the remaining items
            _remainingItems.RemoveRange(0, itemsForCurrentPage.Count);

            // If this is the last page (no more items), draw the summary section
            if (_remainingItems.Count == 0)
            {
                // Fill the empty area if needed
                float emptyRowsHeight = tableAvailableHeight - (itemsForCurrentPage.Count * rowHeight) - headerRowHeight;
                if (emptyRowsHeight > 0)
                {
                    // Draw vertical borders of empty section
                    g.DrawLine(tablePen, leftX, y, leftX, y + emptyRowsHeight);
                    g.DrawLine(tablePen, rightX, y, rightX, y + emptyRowsHeight);

                    // Update y to account for empty rows
                    y += emptyRowsHeight;

                    // Draw the final horizontal line at the bottom of empty section
                    g.DrawLine(tablePen, leftX, y, rightX, y);
                }

                // ----------------------------------------------------------------------
                // Draw summary section at the bottom
                // ----------------------------------------------------------------------

                float summaryRowHeight = 22; // Slightly taller for summary rows

                // 1. Subtotal row
                float summaryRowStartY = y;

                // Calculate horizontal line positions first
                float thaiBahtLineY = y + (summaryRows - 1) * summaryRowHeight;

                // Draw the outer frame for the summary section
                g.DrawLine(tablePen, leftX, y, rightX, y); // Top border
                g.DrawLine(tablePen, leftX, y, leftX, y + (summaryRows * summaryRowHeight)); // Left border
                g.DrawLine(tablePen, rightX, y, rightX, y + (summaryRows * summaryRowHeight)); // Right border
                g.DrawLine(tablePen, leftX, y + (summaryRows * summaryRowHeight), rightX, y + (summaryRows * summaryRowHeight)); // Bottom border

                // Draw vertical dividers
                g.DrawLine(tablePen, xs[3], y, xs[3], thaiBahtLineY); // Divider for "ยอดรวม" and "ส่วนลด" rows only
                g.DrawLine(tablePen, xs[4], y, xs[4], y + (summaryRows * summaryRowHeight)); // Divider before amount column

                // Draw horizontal divider after first row
                g.DrawLine(tablePen, xs[3], y + summaryRowHeight, rightX, y + summaryRowHeight);

                // If discount exists, draw horizontal divider after second row
                if (_header.TodayDiscount > 0)
                {
                    g.DrawLine(tablePen, xs[3], y + (summaryRowHeight * 2), rightX, y + (summaryRowHeight * 2));
                }

                // Draw horizontal line at the bottom for the Thai baht text
                g.DrawLine(tablePen, leftX, thaiBahtLineY, rightX, thaiBahtLineY);

                // Subtotal value in top right columns
                string subtotalLabel = "ยอดรวม";
                string subtotalValue = _header.SubTotal.ToString("N2");
                float verticalCenter = y + (summaryRowHeight - font.GetHeight(g)) / 2;

                // Label in the 4th column
                g.DrawString(subtotalLabel, font, Brushes.Black,
                            xs[3] + 5, verticalCenter);

                // Value in the 5th column (right aligned)
                g.DrawString(subtotalValue, font, Brushes.Black,
                           rightX - 5 - g.MeasureString(subtotalValue, font).Width, verticalCenter);

                // 2. Discount row (if applicable)
                if (_header.TodayDiscount > 0)
                {
                    y += summaryRowHeight;
                    verticalCenter = y + (summaryRowHeight - font.GetHeight(g)) / 2;

                    string discountLabel = "ส่วนลด";
                    string discountValue;

                    if (_header.IsTodayDiscountPercent)
                    {
                        // Calculate the actual discount amount if it's stored as a percentage
                        decimal discountAmount = Math.Round(_header.SubTotal * _header.TodayDiscount / 100m, 2);
                        discountValue = $"- {discountAmount:N2}";
                    }
                    else
                    {
                        // For fixed amount discount, use the value directly
                        discountValue = $"- {_header.TodayDiscount:N2}";
                    }

                    g.DrawString(discountLabel, font, Brushes.Red,
                                xs[3] + 5, verticalCenter);
                    g.DrawString(discountValue, font, Brushes.Red,
                               rightX - 5 - g.MeasureString(discountValue, font).Width, verticalCenter);
                }

                // 3. Total row with bold text - Bottom right section
                y += summaryRowHeight;
                verticalCenter = y + (summaryRowHeight - font.GetHeight(g)) / 2;

                // Thai text at the bottom left (in the first 3 columns combined)
                string thaiBaht = "ตัวอักษร       ( " + ThaiNumberToText(_header.DiscountedTotal) + " )";
                float textStartX = leftX + 5; // Left margin for Thai text
                g.DrawString(thaiBaht, font, Brushes.Black, textStartX, verticalCenter);

                // Total amount text and value
                string totalLabel = "ราคาสุทธิ (รวมภาษีมูลค่าเพิ่ม)";
                string totalValue = _header.DiscountedTotal.ToString("N2");

                // Add a vertical divider in front of totalLabel (at column 2)
                g.DrawLine(tablePen, xs[2], thaiBahtLineY, xs[2], y + summaryRowHeight); // Add vertical line only for last row

                // Move the total label to column 3 (index 2)
                float totalLabelX = xs[2] + 5; // Position at column 3 with small padding

                // Draw the total label in column 3 (instead of column 4)
                g.DrawString(totalLabel, font, Brushes.Black, totalLabelX, verticalCenter);

                // Total value - right aligned in the last column
                using (Font boldFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
                {
                    // Right align the total amount
                    float totalValueWidth = g.MeasureString(totalValue, boldFont).Width;
                    float totalValueX = rightX - 5 - totalValueWidth;
                    g.DrawString(totalValue, boldFont, Brushes.Black, totalValueX, verticalCenter);

                    // Draw red underline ONLY under the total amount value
                    using (Pen redPen = new Pen(Color.Red, 2))
                    {
                        g.DrawLine(redPen, totalValueX, y + summaryRowHeight - 2,
                                  totalValueX + totalValueWidth, y + summaryRowHeight - 2);
                    }
                }

                // Update final y position
                y += summaryRowHeight;
            }
            else
            {
                // For pages that will continue, just close the table
                g.DrawLine(tablePen, leftX, y, rightX, y);
            }

            // Update the end table Y position
            endTableY = y;

            // Return true if there are more items to print
            return _remainingItems.Count > 0;
        }

        // Helper method to calculate how many items can fit on a page
        private int GetMaxItemsPerPage(Graphics g, Font font, float startY, float availableHeight)
        {
            float rowHeight = 20; // Row height for data rows
            float headerRowHeight = 32; // Taller header row

            // Calculate space available for data rows
            float dataRowsSpace = availableHeight - headerRowHeight;

            // Calculate max number of rows that can fit
            int maxRows = (int)Math.Floor(dataRowsSpace / rowHeight);

            // Return at least 1 row (even if it doesn't fit perfectly)
            return Math.Max(1, maxRows);
        }
    }
}
