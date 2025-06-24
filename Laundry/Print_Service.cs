using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Data.SqlClient;
using System.IO;
using System.Drawing.Drawing2D;

namespace Laundry_Management.Laundry
{
    public partial class Print_Service : Form
    {
        private Bitmap _logoImage;
        public class ServiceItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }
        public Print_Service(string customerName, string customerPhone, decimal customerDiscount,
                         string orderId, List<ServiceItem> items)
        : this()
        {
            _customerName = customerName;
            _customerPhone = customerPhone;
            _customerDiscount = customerDiscount;
            _orderId = orderId;
            _items = items;

            LoadOrderDateFromDatabase(orderId);
        }
        private string _orderId;
        private PrintDocument _printDocument;
        private string _customerName;
        private string _customerPhone;
        private decimal _customerDiscount;
        private List<ServiceItem> _items = new List<ServiceItem>();
        private PrintPreviewDialog _previewDialog;
        private int _currentPage = 0;
        private int _totalPages = 1;
        private List<ServiceItem> _remainingItems;
        public Print_Service()
        {
            InitializeComponent();
            _printDocument = new PrintDocument();
            _printDocument.PrintPage += PrintPageHandler;
            _previewDialog = new PrintPreviewDialog { Document = _printDocument };

            // Set A5 paper size in portrait orientation
            PaperSize paperSize = new PaperSize("A5", 583, 827); // A5 dimensions in hundredths of an inch
            _printDocument.DefaultPageSettings.PaperSize = paperSize;
            _printDocument.DefaultPageSettings.Landscape = false; // false = portrait orientation

            // Adjust margins to be smaller for A5
            _printDocument.DefaultPageSettings.Margins = new Margins(15, 40, 15, 15);
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
            Bitmap bmp = new Bitmap(70, 70);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawRectangle(Pens.Gray, 0, 0, 69, 69);

                using (Font f = new Font("Arial", 12, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("LOGO", f, Brushes.Gray, new RectangleF(0, 0, 70, 70), sf);
                }
            }
            return bmp;
        }
        public bool IsPrinted { get; private set; } = false;
        private void PrintDoc_Click(object sender, EventArgs e)
        {
            using (var dlg = new PrintDialog())
            {
                dlg.Document = _printDocument;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _printDocument.Print();
                        // ถ้าถึงตรงนี้ แปลว่าพิมพ์ไม่เกิด Exception
                        IsPrinted = true;
                        this.DialogResult = DialogResult.OK;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("เกิดข้อผิดพลาดขณะพิมพ์:\n" + ex.Message);
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
        // Modify PrintPageHandler to use smaller fonts for A5 paper
        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            // Enable high-quality rendering
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float leftX = e.MarginBounds.Left;
            float topY = e.MarginBounds.Top;
            float rightX = e.MarginBounds.Right;
            float y = topY;

            // Initialize remaining items on first page
            if (_currentPage == 0)
            {
                _remainingItems = new List<ServiceItem>(_items);

                // Calculate total pages based on how many items we can fit per page
                int itemsPerFirstPage = CalculateItemsPerPage(e.Graphics, e.MarginBounds.Height - 400); // First page has header
                int itemsPerSubsequentPage = CalculateItemsPerPage(e.Graphics, e.MarginBounds.Height - 200); // Subsequent pages have less header

                if (_items.Count <= itemsPerFirstPage)
                {
                    _totalPages = 1;
                }
                else
                {
                    int remainingItems = _items.Count - itemsPerFirstPage;
                    _totalPages = 1 + (int)Math.Ceiling((double)remainingItems / itemsPerSubsequentPage);
                }
            }

            // Use slightly nicer fonts for A5 paper
            using (Font headerF = new Font("Tahoma", 9.5f, FontStyle.Bold))
            using (Font subF = new Font("Tahoma", 7.5f))
            using (Font bodyF = new Font("Tahoma", 7.5f))
            {
                // Draw page header - only on first page or simplified on subsequent pages
                if (_currentPage == 0)
                {
                    DrawHeader(g, headerF, subF, leftX, ref y, rightX);
                }
                else
                {
                    // Draw a simplified continuation header
                    DrawContinuationHeader(g, headerF, subF, leftX, ref y, rightX);
                }

                y += 3; // Reduced spacing

                // Draw the service items table with pagination
                bool hasMoreItems = DrawServiceDetailsWithPagination(g, bodyF, leftX, ref y, rightX);

                // Only draw footer on the last page
                if (!hasMoreItems)
                {
                    y += 3; // Reduced spacing
                    DrawFooter(g, bodyF, e.PageBounds, rightX);
                }

                // Add page number
                using (Font pageFont = new Font("Tahoma", 6f))
                {
                    string pageText = $"หน้า {_currentPage + 1}/{_totalPages}";
                    SizeF textSize = g.MeasureString(pageText, pageFont);
                    g.DrawString(pageText, pageFont, Brushes.Black,
                        rightX - textSize.Width, e.PageBounds.Bottom - 30);
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
        private DateTime _orderDate; // Add this field to the class
        private DateTime _PickupDate;
        private void LoadOrderDateFromDatabase(string orderId)
        {
            using (var connection = Laundry_Management.Laundry.DBconfig.GetConnection())
            {
                connection.Open();

                string query =
                    "SELECT OrderDate, PickupDate " +
                    "  FROM OrderHeader " +
                    " WHERE OrderID = @OrderId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _orderDate = reader["OrderDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["OrderDate"])
                                        : DateTime.Now;
                            _PickupDate = reader["PickupDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["PickupDate"])
                                        : DateTime.Now.AddDays(2);
                        }
                        else
                        {
                            _orderDate = DateTime.Now;
                            _PickupDate = DateTime.Now.AddDays(2);
                        }
                    }
                }
            }
        }

        private void DrawHeader(Graphics g, Font headerFont, Font subHeaderFont,
                        float leftX, ref float y, float rightX)
        {
            // กำหนดสีและสไตล์
            Color primaryColor = Color.FromArgb(192, 0, 0);     // สีแดงเข้ม ASIB HOTEL
            Color accentColor = Color.FromArgb(60, 60, 60);     // สีเทาเข้มสำหรับข้อความหลัก
            Color secondaryColor = Color.FromArgb(90, 90, 90);  // สีเทาสำหรับข้อความรอง
            Color separatorColor = Color.FromArgb(220, 220, 220); // สีเทาอ่อนสำหรับเส้นคั่น

            // กำหนดพื้นที่และระยะห่างเริ่มต้น
            float initialY = y;
            float headerHeight = 80; // ความสูงส่วนหัว

            // 1. วาดสีพื้นเบาๆ สำหรับส่วนหัว
            using (LinearGradientBrush headerBrush = new LinearGradientBrush(
                new RectangleF(leftX, y, rightX - leftX, headerHeight),
                Color.FromArgb(252, 252, 252),
                Color.FromArgb(248, 248, 248),
                90f))
            {
                g.FillRectangle(headerBrush, leftX, y, rightX - leftX, headerHeight);
            }

            // 2. วาดชื่อร้านซ้ายและหมายเลขใบรับผ้าขวา (ด้วยสไตล์สวยงาม)
            string storeName = "เอเชียซักแห้ง";
            SizeF storeNameSize = g.MeasureString(storeName, headerFont);

            // วาดชื่อร้านด้วยสีดำ
            g.DrawString(storeName, headerFont, new SolidBrush(accentColor), leftX, y);

            // วาดหมายเลขใบรับผ้าด้านขวา
            string idLine = $"หมายเลขใบรับผ้า : {_orderId}";
            SizeF idSz = g.MeasureString(idLine, headerFont);
            g.DrawString(idLine, headerFont, new SolidBrush(accentColor), rightX - idSz.Width, y);

            // 3. วาดโลโก้ตรงกลางของหัวกระดาษ
            if (_logoImage != null)
            {
                // ปรับขนาดและตำแหน่งภาพ
                float imgWidth = 70;
                float imgHeight = 70;

                // วางภาพตรงกลางของกระดาษ (แนวนอน)
                float pageCenter = leftX + (rightX - leftX) / 2;
                float imgX = pageCenter - imgWidth / 2;

                // วาง logo ด้านบนสุดของกระดาษ
                float imgY = y - 10;  // ปรับตามความเหมาะสม

                // กำหนดคุณภาพการวาดให้สูงขึ้น
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // วาดโลโก้ด้วยคุณภาพสูง
                g.DrawImage(_logoImage, new RectangleF(imgX, imgY, imgWidth, imgHeight));
            }

            // เลื่อน y ลงตามโลโก้
            y += 25;  // ปรับตามความสูงโลโก้

            // 4. ข้อมูลร้าน - จัดแบบคู่ขนาน ซ้าย-ขวา และชิดหัวข้อ
            float leftColumnX = leftX + 5;      // ช่องข้อความซ้าย
            float rightColumnX = leftX + (rightX - leftX) / 2 + 10;  // ช่องข้อความขวา
            float infoRowHeight = subHeaderFont.GetHeight(g) * 1.3f;  // ระยะห่างแต่ละแถว

            // ข้อมูลที่จะแสดง - จัดคู่กัน
            string[][] infoData = new string[][] {
                new[] { "โทร. 02-217-0808 ต่อ 5340", $"วันที่: {_orderDate:dd/MM/yyyy}" },
                new[] { "296 ถนนพญาไท กทม. 10400", $"ชื่อลูกค้า: {_customerName}" },
                new[] { "เลขประจำตัวผู้เสียภาษีอากร: 0107535000346", $"โทรศัพท์: {_customerPhone}" },
                new[] { "เปิดบริการ 7.00-19.00 น. ทุกวัน", $"วันที่ลูกค้ามารับ: {_PickupDate:dd/MM/yyyy}" }
            };

            // วาดแต่ละแถว
            for (int i = 0; i < infoData.Length; i++)
            {
                // ฝั่งซ้าย
                g.DrawString(infoData[i][0], subHeaderFont, new SolidBrush(secondaryColor),
                            leftColumnX, y);

                // ฝั่งขวา - เช็คความยาวและจัดให้อยู่ชิดขวา
                SizeF rightSize = g.MeasureString(infoData[i][1], subHeaderFont);
                g.DrawString(infoData[i][1], subHeaderFont, new SolidBrush(secondaryColor),
                            rightX - rightSize.Width, y);

                y += infoRowHeight;
            }

            // 5. เส้นคั่นเบาๆ ก่อนข้อมูลบริการด่วน
            using (Pen separatorPen = new Pen(separatorColor, 1))
            {
                g.DrawLine(separatorPen, leftX, y + 3, rightX, y + 3);
            }
            y += 8;

            // 6. ข้อความเตือน Quick service - ปรับให้สวยงาม
            // แถบพื้นหลังสำหรับข้อความเตือน
            RectangleF warningHeaderRect = new RectangleF(leftX, y, rightX - leftX, subHeaderFont.GetHeight(g) + 6);
            using (LinearGradientBrush warningBgBrush = new LinearGradientBrush(
                warningHeaderRect, Color.FromArgb(252, 248, 248), Color.FromArgb(252, 242, 242), 0f))
            {
                g.FillRectangle(warningBgBrush, warningHeaderRect);
            }

            // หัวข้อ "กรณีซักผ้าด่วน ส่งก่อน 10.00 น." ด้วยตัวหนา
            using (Font boldSubHeader = new Font(subHeaderFont.FontFamily, subHeaderFont.Size, FontStyle.Bold))
            {
                g.DrawString("กรณีซักผ้าด่วน ส่งก่อน 10.00 น.", boldSubHeader,
                            new SolidBrush(primaryColor), leftX + 5, y + 3);
            }
            y += subHeaderFont.GetHeight(g) + 8;

            // รายการข้อมูลเตือน - จัดชิดซ้ายและมีเครื่องหมายนำข้อความ
            string[] warnings = {
                "[  ] รับผ้าภายในเวลา 17.00 น. ในวันเดียวกัน คิดค่าบริการเพิ่ม 100%",
                "[  ] รับผ้าในวันถัดไปภายใน 17.00 น. คิดค่าบริการเพิ่ม 50%",
                "[  ] ผ้าอบไอน้ำ เศษของเมตรคิดเป็น 1 เมตร"
            };

            foreach (var warning in warnings)
            {
                g.DrawString(warning, subHeaderFont, Brushes.Black, leftX + 10, y);
                y += subHeaderFont.GetHeight(g) + 2;
            }

            y += 8;
        }
        private void DrawServiceDetails(Graphics g, Font font, float leftX, ref float y, float rightX)
        {
            // สี
            Color tableHeaderBgColor = Color.FromArgb(240, 240, 245);
            Color tableHeaderTextColor = Color.FromArgb(60, 60, 60);
            Color tableBorderColor = Color.FromArgb(180, 180, 180);
            Color tableAlternateRowColor = Color.FromArgb(250, 250, 252);
            Color tableCellTextColor = Color.FromArgb(50, 50, 50);

            // ขยายเป็น 4 คอลัมน์: รายการ | จำนวน | ราคา | จำนวนเงิน
            int cols = 4;
            float tableWidth = rightX - leftX;

            // กำหนดความกว้างแต่ละคอลัมน์ตามสัดส่วน (รายการ 40%, จำนวน 15%, ราคา 20%, จำนวนเงิน 25%)
            float[] colWidthPercents = { 0.40f, 0.15f, 0.20f, 0.25f };
            float[] colWidths = new float[cols];
            float[] xs = new float[cols + 1];

            xs[0] = leftX;
            for (int i = 0; i < cols; i++)
            {
                colWidths[i] = tableWidth * colWidthPercents[i];
                xs[i + 1] = xs[i] + colWidths[i];
            }

            float rowHeight = font.GetHeight(g) + 8; // เพิ่มความสูงแถวเล็กน้อย

            // ปากกาสำหรับเส้นตาราง
            Pen tablePen = new Pen(tableBorderColor, 0.8f);

            // --- ส่วนหัวตาราง ---

            // พื้นหลังหัวตาราง
            g.FillRectangle(new SolidBrush(tableHeaderBgColor), leftX, y, tableWidth, rowHeight);

            // กรอบหัวตาราง
            g.DrawRectangle(tablePen, leftX, y, tableWidth, rowHeight);

            // เส้นคั่นคอลัมน์หัวตาราง
            for (int i = 1; i < cols; i++)
            {
                g.DrawLine(tablePen, xs[i], y, xs[i], y + rowHeight);
            }

            // ข้อความหัวตารางพร้อมเงา
            using (Font headerFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
            {
                string[] headers = { "รายการ", "จำนวน", "ราคา", "จำนวนเงิน" };
                for (int i = 0; i < cols; i++)
                {
                    float colCenter = xs[i] + colWidths[i] / 2;
                    var sz = g.MeasureString(headers[i], headerFont);

                    // เงาข้อความ
                    g.DrawString(headers[i], headerFont, new SolidBrush(Color.FromArgb(30, 0, 0, 0)),
                                colCenter - sz.Width / 2 + 1, y + (rowHeight - sz.Height) / 2 + 1);

                    // ข้อความหลัก
                    g.DrawString(headers[i], headerFont, new SolidBrush(tableHeaderTextColor),
                                colCenter - sz.Width / 2, y + (rowHeight - sz.Height) / 2);
                }
            }

            y += rowHeight;

            // --- ส่วนข้อมูล ---

            bool isAlternate = false;
            foreach (var item in _items)
            {
                // พื้นหลังแถวสลับสี
                if (isAlternate)
                {
                    g.FillRectangle(new SolidBrush(tableAlternateRowColor), leftX, y, tableWidth, rowHeight);
                }
                isAlternate = !isAlternate;

                // กรอบแถว
                g.DrawRectangle(tablePen, leftX, y, tableWidth, rowHeight);

                // เส้นคั่นคอลัมน์
                for (int i = 1; i < cols; i++)
                {
                    g.DrawLine(tablePen, xs[i], y, xs[i], y + rowHeight);
                }

                // ข้อมูลแต่ละคอลัมน์

                // 1. รายการ (ซ้ายสุด)
                g.DrawString(item.Name, font, new SolidBrush(tableCellTextColor), xs[0] + 5, y + (rowHeight - font.GetHeight(g)) / 2);

                // 2. จำนวน (กึ่งกลาง)
                string qty = item.Quantity.ToString();
                SizeF qtySize = g.MeasureString(qty, font);
                g.DrawString(qty, font, new SolidBrush(tableCellTextColor),
                            xs[1] + (colWidths[1] - qtySize.Width) / 2,
                            y + (rowHeight - qtySize.Height) / 2);

                // 3. ราคา (กึ่งกลาง)
                string price = item.Price.ToString("N2");
                SizeF priceSize = g.MeasureString(price, font);
                g.DrawString(price, font, new SolidBrush(tableCellTextColor),
                            xs[2] + (colWidths[2] - priceSize.Width) / 2,
                            y + (rowHeight - priceSize.Height) / 2);

                // 4. จำนวนเงิน (กึ่งกลาง)
                decimal amount = item.Quantity * item.Price;
                string amt = amount.ToString("N2");
                SizeF amtSize = g.MeasureString(amt, font);
                g.DrawString(amt, font, new SolidBrush(tableCellTextColor),
                            xs[3] + (colWidths[3] - amtSize.Width) / 2,
                            y + (rowHeight - amtSize.Height) / 2);

                y += rowHeight;
            }

            y += 10; // เพิ่มระยะห่างท้ายตาราง
        }

        private void DrawChecklistLeft(Graphics g, Font font, float leftX, float y)
        {
            string[] checks = new[]
    {
        "[  ] หดรอยเตารีด เหลือง ไหม้",
        "[  ] สีกล้ำ เปื้อนน้ำมัน",
        "[  ] เปื้อนสี หมึก เลือด อาหาร",
        "[  ] ไม่สมประกอบ กระดุมแตก หาย",
        "[  ] รอยย่น",
        "[  ] สีเปลี่ยนจากเดิมจุดด่าง",
        "[  ] ขาดแยกปริ",
        "[  ] เป็นเงามันจากเตารีด",
        "[  ] เปื้อนชา กาแฟ",
        "[  ] ขอบยางยืด"
    };

            float lineY = y;

            // Draw a border around the checklist area for better visual separation
            float checklistWidth = 180;
            float checklistHeight = checks.Length * (font.GetHeight(g) + 1) + 10; // Height based on number of lines

            using (Pen borderPen = new Pen(Color.FromArgb(230, 230, 235), 1))
            {
                g.DrawRectangle(borderPen, leftX, y, checklistWidth, checklistHeight);
            }

            // Add a small padding inside the border
            lineY += 5;
            leftX += 5;

            // Draw the checklist items
            foreach (var line in checks)
            {
                g.DrawString(line, font, Brushes.Black, leftX, lineY);
                lineY += font.GetHeight(g) + 1;
            }
        }
        private void DrawSummaryRight(Graphics g, Font font, Rectangle page, float y, float rightX)
        {
            // คำนวณยอดรวม
            decimal total = _items.Sum(i => i.Price * i.Quantity);

            // สี
            Color summaryBgColor = Color.FromArgb(248, 248, 252);
            Color summaryBorderColor = Color.FromArgb(230, 230, 235);
            Color totalTextColor = Color.FromArgb(70, 70, 70);

            float leftX = page.Left + 15; // Match the left margin

            // สร้างพื้นที่สรุปยอด
            float summaryWidth = 180;
            float summaryHeight = 50; // Reduced height since we don't have VAT/discount sections
            float summaryX = rightX - summaryWidth;

            // วาดพื้นหลังและกรอบ
            using (SolidBrush bgBrush = new SolidBrush(summaryBgColor))
            {
                g.FillRectangle(bgBrush, summaryX, y, summaryWidth, summaryHeight);
            }

            using (Pen borderPen = new Pen(summaryBorderColor, 1))
            {
                g.DrawRectangle(borderPen, summaryX, y, summaryWidth, summaryHeight);
            }

            // คำนวณความสูงของตัวอักษรสำหรับเว้นบรรทัด
            float lineHeight = font.GetHeight(g);
            float currentY = y + 8;

            // ยอดรวมสุทธิ
            string totalLine = $"ยอดรวมสุทธิ : {total:N2} บาท";
            SizeF totalSize = g.MeasureString(totalLine, font);
            using (Font totalFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
            {
                g.DrawString(totalLine, totalFont, new SolidBrush(totalTextColor),
                            rightX - 5 - totalSize.Width - 5, currentY);
            }
            DrawSignatureLine(g, font, rightX, y + summaryHeight);
        }
        private void DrawFooter(Graphics g, Font font, Rectangle page, float rightX)
        {
            float leftX = page.Left + 15; // Match the left margin

            // Calculate the position for footer (at the bottom of the page)
            float footerY = page.Bottom - 180; // Space for both checklist and summary

            // Draw the checklist on the left side of the footer
            DrawChecklistLeft(g, font, leftX, footerY);

            // Draw the summary on the right side of the footer - modified version without VAT/discount
            DrawSummaryRight(g, font, page, footerY, rightX);
        }
        private void DrawSignatureLine(Graphics g, Font font, float rightX, float y)
        {
            float signatureLineWidth = 150;
            float signatureX = rightX - signatureLineWidth;
            float signatureY = y + 35; // เว้นระยะใต้กล่องสรุป

            // วาดขีดเส้น
            g.DrawLine(Pens.Black, signatureX, signatureY, signatureX + signatureLineWidth, signatureY);

            // วาดข้อความใต้เส้น
            string signatureLabel = "ลายเซ้นผู้รับผ้า";
            SizeF labelSize = g.MeasureString(signatureLabel, font);
            float labelX = signatureX + (signatureLineWidth - labelSize.Width) / 2;
            g.DrawString(signatureLabel, font, Brushes.Black, labelX, signatureY + 5);
        }
        private void DrawContinuationHeader(Graphics g, Font headerFont, Font subHeaderFont, float leftX, ref float y, float rightX)
        {
            // Define colors
            Color primaryColor = Color.FromArgb(192, 0, 0);
            Color accentColor = Color.FromArgb(60, 60, 60);

            // Draw simplified header with store name and order ID
            string storeName = "เอเชียซักแห้ง";
            g.DrawString(storeName, headerFont, new SolidBrush(accentColor), leftX, y);

            // Draw order ID on the right
            string idLine = $"หมายเลขใบรับผ้า : {_orderId} (ต่อ)";
            SizeF idSz = g.MeasureString(idLine, headerFont);
            g.DrawString(idLine, headerFont, new SolidBrush(accentColor), rightX - idSz.Width, y);

            // Draw customer name
            y += headerFont.GetHeight(g) * 1.5f;
            g.DrawString($"ลูกค้า: {_customerName}", subHeaderFont, new SolidBrush(accentColor), leftX, y);

            // Add a separator line
            y += subHeaderFont.GetHeight(g) * 1.5f;
            g.DrawLine(Pens.LightGray, leftX, y, rightX, y);

            y += 5; // Add a bit more space after the line
        }

        // New method to calculate how many items can fit on a page
        private int CalculateItemsPerPage(Graphics g, float availableHeight)
        {
            using (Font font = new Font("Tahoma", 7.5f))
            {
                float rowHeight = font.GetHeight(g) + 8; // Same as in DrawServiceDetails

                // Calculate max number of rows that can fit
                int maxRows = (int)Math.Floor(availableHeight / rowHeight);

                // Return at least 1 item per page
                return Math.Max(1, maxRows);
            }
        }

        // New method for drawing the service details table with pagination support
        private bool DrawServiceDetailsWithPagination(Graphics g, Font font, float leftX, ref float y, float rightX)
        {
            // Colors same as the original method
            Color tableHeaderBgColor = Color.FromArgb(240, 240, 245);
            Color tableHeaderTextColor = Color.FromArgb(60, 60, 60);
            Color tableBorderColor = Color.FromArgb(180, 180, 180);
            Color tableAlternateRowColor = Color.FromArgb(250, 250, 252);
            Color tableCellTextColor = Color.FromArgb(50, 50, 50);

            // Define columns same as the original method
            int cols = 4;
            float tableWidth = rightX - leftX;
            float[] colWidthPercents = { 0.40f, 0.15f, 0.20f, 0.25f };
            float[] colWidths = new float[cols];
            float[] xs = new float[cols + 1];

            xs[0] = leftX;
            for (int i = 0; i < cols; i++)
            {
                colWidths[i] = tableWidth * colWidthPercents[i];
                xs[i + 1] = xs[i] + colWidths[i];
            }

            float rowHeight = font.GetHeight(g) + 8;
            Pen tablePen = new Pen(tableBorderColor, 0.8f);

            // Calculate how many items can fit on this page
            float availableHeight = 650 - y - 100; // Reserve space for footer on last page
            int maxItemsThisPage = (int)Math.Floor(availableHeight / rowHeight);

            // Get items for this page
            List<ServiceItem> itemsForThisPage = _remainingItems.Take(maxItemsThisPage).ToList();

            // --- Draw table header ---
            g.FillRectangle(new SolidBrush(tableHeaderBgColor), leftX, y, tableWidth, rowHeight);
            g.DrawRectangle(tablePen, leftX, y, tableWidth, rowHeight);

            // Draw column dividers for header
            for (int i = 1; i < cols; i++)
            {
                g.DrawLine(tablePen, xs[i], y, xs[i], y + rowHeight);
            }

            // Draw header text with shadow
            using (Font headerFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
            {
                string[] headers = { "รายการ", "จำนวน", "ราคา", "จำนวนเงิน" };
                for (int i = 0; i < cols; i++)
                {
                    float colCenter = xs[i] + colWidths[i] / 2;
                    var sz = g.MeasureString(headers[i], headerFont);

                    // Shadow
                    g.DrawString(headers[i], headerFont, new SolidBrush(Color.FromArgb(30, 0, 0, 0)),
                                colCenter - sz.Width / 2 + 1, y + (rowHeight - sz.Height) / 2 + 1);

                    // Main text
                    g.DrawString(headers[i], headerFont, new SolidBrush(tableHeaderTextColor),
                                colCenter - sz.Width / 2, y + (rowHeight - sz.Height) / 2);
                }
            }

            y += rowHeight;

            // --- Draw data rows ---
            bool isAlternate = false;
            int startItemNumber = _currentPage * maxItemsThisPage;

            foreach (var item in itemsForThisPage)
            {
                // Alternate row background
                if (isAlternate)
                {
                    g.FillRectangle(new SolidBrush(tableAlternateRowColor), leftX, y, tableWidth, rowHeight);
                }
                isAlternate = !isAlternate;

                // Draw row border
                g.DrawRectangle(tablePen, leftX, y, tableWidth, rowHeight);

                // Draw column dividers
                for (int i = 1; i < cols; i++)
                {
                    g.DrawLine(tablePen, xs[i], y, xs[i], y + rowHeight);
                }

                // Draw data in each column

                // 1. Item name (left aligned)
                g.DrawString(item.Name, font, new SolidBrush(tableCellTextColor),
                            xs[0] + 5, y + (rowHeight - font.GetHeight(g)) / 2);

                // 2. Quantity (centered)
                string qty = item.Quantity.ToString();
                SizeF qtySize = g.MeasureString(qty, font);
                g.DrawString(qty, font, new SolidBrush(tableCellTextColor),
                            xs[1] + (colWidths[1] - qtySize.Width) / 2,
                            y + (rowHeight - qtySize.Height) / 2);

                // 3. Price (centered)
                string price = item.Price.ToString("N2");
                SizeF priceSize = g.MeasureString(price, font);
                g.DrawString(price, font, new SolidBrush(tableCellTextColor),
                            xs[2] + (colWidths[2] - priceSize.Width) / 2,
                            y + (rowHeight - priceSize.Height) / 2);

                // 4. Total amount (centered)
                decimal amount = item.Quantity * item.Price;
                string amt = amount.ToString("N2");
                SizeF amtSize = g.MeasureString(amt, font);
                g.DrawString(amt, font, new SolidBrush(tableCellTextColor),
                            xs[3] + (colWidths[3] - amtSize.Width) / 2,
                            y + (rowHeight - amtSize.Height) / 2);

                y += rowHeight;
            }

            // Remove printed items from the remaining items
            _remainingItems.RemoveRange(0, itemsForThisPage.Count);

            // Return true if there are more items to print
            return _remainingItems.Count > 0;
        }
    }
}
