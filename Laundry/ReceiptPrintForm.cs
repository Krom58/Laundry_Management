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

namespace Laundry_Management.Laundry
{
    public partial class ReceiptPrintForm : Form
    {
        public bool IsPrinted { get; private set; } = false;
        private readonly int _receiptId;
        private readonly OrderHeaderDto _header;
        private readonly List<OrderItemDto> _items;
        private PrintDocument _printDocument;
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

            // เตรียม PrintDocument
            _printDocument = new PrintDocument();
            _printDocument.PrintPage += PrintPageHandler;

            // Set A5 paper size in portrait orientation - เหมือนกับ Print_Service
            PaperSize paperSize = new PaperSize("A5", 583, 827); // A5 dimensions in hundredths of an inch
            _printDocument.DefaultPageSettings.PaperSize = paperSize;
            _printDocument.DefaultPageSettings.Landscape = false; // false = portrait orientation

            // Adjust margins to be smaller for A5 - เหมือนกับ Print_Service
            _printDocument.DefaultPageSettings.Margins = new Margins(15, 35, 15, 15);
        }
        // Modify PrintPageHandler to use smaller fonts for A5 paper - เหมือนกับ Print_Service
        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float leftX = e.MarginBounds.Left;
            float topY = e.MarginBounds.Top;
            float rightX = e.MarginBounds.Right;
            float y = topY;

            // Use smaller fonts for A5 paper - เหมือนกับ Print_Service
            using (Font headerF = new Font("Arial", 9, FontStyle.Bold))
            using (Font subF = new Font("Arial", 7))
            using (Font bodyF = new Font("Arial", 7))
            {
                DrawHeader(g, headerF, subF, leftX, ref y, rightX);
                y += 3; // Reduced spacing - เหมือนกับ Print_Service
                DrawServiceDetails(g, bodyF, leftX, ref y, rightX);
                y += 3; // Reduced spacing - เหมือนกับ Print_Service
                DrawChecklistLeft(g, bodyF, leftX, y);
                DrawSummaryRight(g, bodyF, e.PageBounds, y, rightX);
            }

            e.HasMorePages = false;
        }
        private void DrawHeader(Graphics g, Font headerFont, Font subHeaderFont,
                        float leftX, ref float y, float rightX)
        {
            // ชื่อร้าน (ฝั่งซ้าย)
            g.DrawString($"หมายเลขใบเสร็จ : {_header.CustomReceiptId}", headerFont, Brushes.Black, leftX, y);
            y += headerFont.GetHeight(g) + 3;
            g.DrawString("เอเชียซักแห้ง", headerFont, Brushes.Black, leftX, y);

            // Order ID + วันที่ (ฝั่งขวา) ให้ตัวอักษรตัวแรกตรงกับ leftX + same indent
            string idLine = $"หมายเลขใบรับผ้า : {_header.CustomOrderId}";
            SizeF idSz = g.MeasureString(idLine, headerFont);
            g.DrawString(idLine, headerFont, Brushes.Black,
                         rightX - idSz.Width, y);

            y += headerFont.GetHeight(g) + 3;

            // แถวถัดมา: โทร / วันที่
            g.DrawString("โทร. 02-217-0808 ต่อ 5340", subHeaderFont, Brushes.Black, leftX, y);
            string dateLine = $"วันที่: {_header.OrderDate:dd/MM/yyyy}";
            SizeF dtSz = g.MeasureString(dateLine, subHeaderFont);
            g.DrawString(dateLine, subHeaderFont, Brushes.Black,
                         rightX - dtSz.Width, y);

            y += subHeaderFont.GetHeight(g) + 2;

            // แถวที่ 3: ที่อยู่ / ชื่อลูกค้า
            g.DrawString("296 ถนนพญาไท กทม. 10400", subHeaderFont, Brushes.Black, leftX, y);
            string customerLine = $"ชื่อลูกค้า: {_header.CustomerName}";
            SizeF custSz = g.MeasureString(customerLine, subHeaderFont);
            g.DrawString(customerLine, subHeaderFont, Brushes.Black,
                         rightX - custSz.Width, y);

            y += subHeaderFont.GetHeight(g) + 2;

            // แถวที่ 4: เลขประจำตัวผู้เสียภาษี / โทรศัพท์
            g.DrawString("เลขประจำตัวผู้เสียภาษีอากร: 0107535000346", subHeaderFont, Brushes.Black, leftX, y);
            string phoneLine = $"โทรศัพท์: {_header.Phone}";
            SizeF phSz = g.MeasureString(phoneLine, subHeaderFont);
            g.DrawString(phoneLine, subHeaderFont, Brushes.Black,
                         rightX - phSz.Width, y);

            y += subHeaderFont.GetHeight(g) + 2;

            // แถวที่ 5: ชั่วโมงบริการ / วันรับผ้า
            g.DrawString("เปิดบริการ 7.00-19.00 น. ทุกวัน", subHeaderFont, Brushes.Black, leftX, y);
            string pickLine = $"วันที่ลูกค้ามารับ: {_header.PickupDate:dd/MM/yyyy}";
            SizeF pkSz = g.MeasureString(pickLine, subHeaderFont);
            g.DrawString(pickLine, subHeaderFont, Brushes.Black,
                         rightX - pkSz.Width, y);

            y += subHeaderFont.GetHeight(g) + 2;

            // ข้อความเตือน Quick service
            string[] warnings = {
                "กรณีซักผ้าด่วน ส่งก่อน 10.00 น.",
                "[  ] รับผ้าภายในเวลา 17.00 น. ในวันเดียวกัน คิดค่าบริการเพิ่ม 100%",
                "[  ] รับผ้าในวันถัดไปภายใน 17.00 น. คิดค่าบริการเพิ่ม 50%",
                "[  ] ผ้าอบไอน้ำ เศษของเมตรคิดเป็น 1 เมตร"
            };
            foreach (var w in warnings)
            {
                g.DrawString(w, subHeaderFont, Brushes.Black, leftX, y);
                y += subHeaderFont.GetHeight(g) + 2;
            }

            y += 10;
        }
        private void DrawServiceDetails(Graphics g, Font font, float leftX, ref float y, float rightX)
        {
            // ขยายเป็น 4 คอลัมน์: รายการ | จำนวน | ราคา | จำนวนเงิน
            int cols = 4;
            float tableWidth = rightX - leftX;
            float colWidth = tableWidth / cols;
            float rowHeight = font.GetHeight(g) + 6; // padding

            // ตำแหน่ง X แต่ละคอลัมน์
            float[] xs = new float[cols + 1];
            for (int i = 0; i <= cols; i++)
                xs[i] = leftX + colWidth * i;

            // เส้นกรอบ header แถวบนและล่าง
            g.DrawLine(Pens.Black, leftX, y, rightX, y);
            g.DrawLine(Pens.Black, leftX, y + rowHeight, rightX, y + rowHeight);
            // เส้นคั่นคอลัมน์
            for (int i = 0; i <= cols; i++)
                g.DrawLine(Pens.Black, xs[i], y, xs[i], y + rowHeight);

            // หัวตารางกึ่งกลางในแต่ละเซลล์
            string[] headers = { "รายการ", "จำนวน", "ราคา", "จำนวนเงิน" };
            for (int i = 0; i < cols; i++)
            {
                var sz = g.MeasureString(headers[i], font);
                float tx = xs[i] + (colWidth - sz.Width) / 2f;
                float ty = y + (rowHeight - sz.Height) / 2f;
                g.DrawString(headers[i], font, Brushes.Black, tx, ty);
            }

            y += rowHeight;

            // แถวข้อมูล
            foreach (var item in _items)
            {
                // เส้นกรอบแต่ละแถว
                g.DrawLine(Pens.Black, leftX, y, rightX, y);
                g.DrawLine(Pens.Black, leftX, y + rowHeight, rightX, y + rowHeight);
                for (int i = 0; i <= cols; i++)
                    g.DrawLine(Pens.Black, xs[i], y, xs[i], y + rowHeight);

                // รายการ (ซ้ายสุด)
                g.DrawString(item.ItemName, font, Brushes.Black, xs[0] + 2, y + 2);

                // จำนวน (กึ่งกลาง)
                var qty = item.Quantity.ToString();
                var szQty = g.MeasureString(qty, font);
                g.DrawString(qty, font, Brushes.Black,
                             xs[1] + (colWidth - szQty.Width) / 2f,
                             y + (rowHeight - szQty.Height) / 2f);

                // ราคา (กึ่งกลาง)
                var price = (item.TotalAmount / item.Quantity).ToString("N2");
                var szPrice = g.MeasureString(price, font);
                g.DrawString(price, font, Brushes.Black,
                             xs[2] + (colWidth - szPrice.Width) / 2f,
                             y + (rowHeight - szPrice.Height) / 2f);

                var amt = item.TotalAmount.ToString("N2");
                var szAmt = g.MeasureString(amt, font);
                g.DrawString(amt, font, Brushes.Black,
                             xs[3] + (colWidth - szAmt.Width) / 2f,
                             y + (rowHeight - szAmt.Height) / 2f);

                y += rowHeight;
            }

            // เส้น bottom สุด
            g.DrawLine(Pens.Black, leftX, y, rightX, y);
            y += 10;
        }

        // Modify spacing in DrawChecklistLeft to match Print_Service
        private void DrawChecklistLeft(Graphics g, Font font, float leftX, float y)
        {
            string[] checks = new[]
            {
                "[  ] หดรอยเตารีด เหลือง ไหม้                   [  ] สีเปลี่ยนจากเดิมจุดด่าง",
                "[  ] สีกล้ำ เปื้อนน้ำมัน                         [  ] ขาดแยกปริ",
                "[  ] เปื้อนสี หมึก เลือด อาหาร                   [  ] เป็นเงามันจากเตารีด",
                "[  ] ไม่สมประกอบ กระดุมแตก หาย                [  ] เปื้อนชา กาแฟ",
                "[  ] รอยย่น                                [  ] ขอบยางยืด"
            };

            float lineY = y;
            foreach (var line in checks)
            {
                g.DrawString(line, font, Brushes.Black, leftX, lineY);
                lineY += font.GetHeight(g) + 1; // Reduced spacing - เหมือนกับ Print_Service
            }
        }
        private void DrawSummaryRight(Graphics g, Font font, Rectangle page, float y, float rightX)
        {
            // รวมเฉพาะรายการที่ไม่ถูกยกเลิก
            var validItems = _items.Where(i => !i.IsCanceled).ToList();
            decimal total = validItems.Sum(i => i.TotalAmount);

            // ส่วนลดคิดจากเปอร์เซ็นต์ Discount ของ _header (ส่วนลดปกติ)
            decimal discountPercent = _header.Discount;
            decimal discountAmount = (discountPercent > 0m) ? (total * (discountPercent / 100m)) : 0m;
            decimal discountedTotal = total - discountAmount;

            // รวม
            string totalLine = $"รวม: {total:N2} บาท";
            SizeF toL = g.MeasureString(totalLine, font);
            g.DrawString(totalLine, font, Brushes.Black, rightX - toL.Width, y);

            // หลังหักส่วนลด (กรณีมีส่วนลดเท่านั้น)
            if (discountAmount > 0m)
            {
                y += font.GetHeight(g) + 4;
                string discLine = $"หลังหักส่วนลด: {discountedTotal:N2} บาท";
                SizeF diL = g.MeasureString(discLine, font);
                g.DrawString(discLine, font, Brushes.Black, rightX - diL.Width, y);
            }
            y += font.GetHeight(g) + 25;
            string sign = $"เซ็นชื่อผู้รับเงิน : _______________________________";
            SizeF si = g.MeasureString(sign, font);
            g.DrawString(sign, font, Brushes.Black, rightX - si.Width, y);
            // คำนวณส่วนลดพิเศษของวันนี้ (TodayDiscount) ปิดท้าย
            //if (_header.TodayDiscount > 0m)
            //{
            //    // เลือกใช้ discountedTotal หรือ total ในการคำนวณ
            //    // หากไม่มีส่วนลดปกติ (discountAmount เป็น 0) ให้ใช้ total แทน
            //    decimal baseAmountForTodayDiscount = discountAmount > 0 ? discountedTotal : total;

            //    decimal todayDiscountAmount;
            //    decimal finalTotal;

            //    // ตรวจสอบว่าส่วนลดวันนี้เป็นบาทหรือเปอร์เซ็นต์
            //    // โดยเช็คจาก _header.IsTodayDiscountPercent
            //    if (_header.IsTodayDiscountPercent)
            //    {
            //        // ถ้าเป็นเปอร์เซ็นต์ คำนวณส่วนลดจากเปอร์เซ็นต์
            //        todayDiscountAmount = baseAmountForTodayDiscount * (_header.TodayDiscount / 100m);
            //        finalTotal = baseAmountForTodayDiscount - todayDiscountAmount;

            //        // แสดงส่วนลดของวันนี้
            //        y += font.GetHeight(g) + 4;
            //        string todayDiscLine = $"ส่วนลดพิเศษประจำวัน {_header.TodayDiscount:N2}%: {todayDiscountAmount:N2} บาท";
            //        SizeF todayDisL = g.MeasureString(todayDiscLine, font);
            //        g.DrawString(todayDiscLine, font, Brushes.Black, rightX - todayDisL.Width, y);
            //    }
            //    else
            //    {
            //        // ถ้าเป็นบาท ใช้ค่าส่วนลดเป็นบาทโดยตรง
            //        todayDiscountAmount = _header.TodayDiscount;
            //        // ป้องกันไม่ให้ส่วนลดมากกว่ายอดเงินที่เหลือ
            //        todayDiscountAmount = Math.Min(todayDiscountAmount, baseAmountForTodayDiscount);
            //        finalTotal = baseAmountForTodayDiscount - todayDiscountAmount;

            //        // แสดงส่วนลดของวันนี้
            //        y += font.GetHeight(g) + 4;
            //        string todayDiscLine = $"ส่วนลดพิเศษประจำวัน: {todayDiscountAmount:N2} บาท";
            //        SizeF todayDisL = g.MeasureString(todayDiscLine, font);
            //        g.DrawString(todayDiscLine, font, Brushes.Black, rightX - todayDisL.Width, y);
            //    }

            //    // แสดงยอดสุทธิหลังหักส่วนลดทั้งหมด
            //    y += font.GetHeight(g) + 4;
            //    string finalLine = $"ยอดสุทธิ: {finalTotal:N2} บาท";
            //    SizeF finalL = g.MeasureString(finalLine, font);
            //    g.DrawString(finalLine, font, Brushes.Black, rightX - finalL.Width, y);
            //}
        }
        private void PrintDoc_Click(object sender, EventArgs e)
        {
            using (var dlg = new PrintDialog { Document = _printDocument })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _printDocument.Print();
                    IsPrinted = true; // กด print จริง

                    // อัพเดทสถานะการพิมพ์ในฐานข้อมูล
                    UpdateReceiptPrintStatus(_receiptId, _header.OrderID);
                }
            }
            this.Close();
        }
        private void UpdateReceiptPrintStatus(int receiptId, int orderId)
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";

            using (var connection = new SqlConnection(connectionString))
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
    }
}
