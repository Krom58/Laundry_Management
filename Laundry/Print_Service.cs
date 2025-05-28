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

namespace Laundry_Management.Laundry
{
    public partial class Print_Service : Form
    {
        public class ServiceItem
        {
            public string CustomerName { get; set; }
            public string CustomerPhone { get; set; }
            public decimal CustomerDiscount { get; set; }
            public string OrderId { get; set; }
            public List<ServiceItem> Items { get; set; }
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
        public Print_Service()
        {
            InitializeComponent();
            // สร้าง PrintDocument และผูกอีเวนต์
            _printDocument = new PrintDocument();
            _printDocument.PrintPage += PrintPageHandler;
        }
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
                        // หลังพิมพ์เสร็จให้ถือว่า OK
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("เกิดข้อผิดพลาดขณะพิมพ์:\n" + ex.Message,
                                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // ยกเลิกการพิมพ์ ก็ถือเป็น DialogResult.Cancel
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            }
        }
        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float leftX = e.MarginBounds.Left;
            float topY = e.MarginBounds.Top;
            float rightX = e.MarginBounds.Right;
            float y = topY;

            using (Font headerF = new Font("Arial", 8, FontStyle.Bold))
            using (Font subF = new Font("Arial", 6))
            using (Font bodyF = new Font("Arial", 6))
            {
                // header เดิม...
                DrawHeader(g, headerF, subF, leftX, ref y, rightX);
                y += 5;
                DrawServiceDetails(g, bodyF, leftX, ref y, rightX);
                // checklist ฝั่งซ้าย
                y += 5;
                DrawChecklistLeft(g, bodyF, leftX, y);
                // สรุปยอด...
                DrawSummaryRight(g, bodyF, e.PageBounds, y);
            }

            e.HasMorePages = false;
        }
        private DateTime _orderDate; // Add this field to the class
        private DateTime _PickupDate;
        private void LoadOrderDateFromDatabase(string orderId)
        {
            // 1) your real connection string
            string cs = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            using (var connection = new SqlConnection(cs))
            {
                connection.Open();

                // 2) correct table & column names
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
            // ชื่อร้าน (ฝั่งซ้าย)
            g.DrawString("เอเชียซักแห้ง", headerFont, Brushes.Black, leftX, y);

            // Order ID + วันที่ (ฝั่งขวา) ให้ตัวอักษรตัวแรกตรงกับ leftX + same indent
            string idLine = $"Order ID: {_orderId}";
            SizeF idSz = g.MeasureString(idLine, headerFont);
            g.DrawString(idLine, headerFont, Brushes.Black,
                         rightX - idSz.Width, y);

            y += headerFont.GetHeight(g) + 3;

            // แถวถัดมา: โทร / วันที่
            g.DrawString("โทร. 02-217-0808 ต่อ 5340", subHeaderFont, Brushes.Black, leftX, y);
            string dateLine = $"วันที่: {_orderDate:dd/MM/yyyy}";
            SizeF dtSz = g.MeasureString(dateLine, subHeaderFont);
            g.DrawString(dateLine, subHeaderFont, Brushes.Black,
                         rightX - dtSz.Width, y);

            y += subHeaderFont.GetHeight(g) + 2;

            // แถวที่ 3: ที่อยู่ / ชื่อลูกค้า
            g.DrawString("296 ถนนพญาไท กทม. 10400", subHeaderFont, Brushes.Black, leftX, y);
            string customerLine = $"ชื่อลูกค้า: {_customerName}";
            SizeF custSz = g.MeasureString(customerLine, subHeaderFont);
            g.DrawString(customerLine, subHeaderFont, Brushes.Black,
                         rightX - custSz.Width, y);

            y += subHeaderFont.GetHeight(g) + 2;

            // แถวที่ 4: เลขประจำตัวผู้เสียภาษี / โทรศัพท์
            g.DrawString("เลขประจำตัวผู้เสียภาษีอากร: 0107535000346", subHeaderFont, Brushes.Black, leftX, y);
            string phoneLine = $"โทรศัพท์: {_customerPhone}";
            SizeF phSz = g.MeasureString(phoneLine, subHeaderFont);
            g.DrawString(phoneLine, subHeaderFont, Brushes.Black,
                         rightX - phSz.Width, y);

            y += subHeaderFont.GetHeight(g) + 2;

            // แถวที่ 5: ชั่วโมงบริการ / วันรับผ้า
            g.DrawString("เปิดบริการ 7.00-19.00 น. ทุกวัน", subHeaderFont, Brushes.Black, leftX, y);
            string pickLine = $"วันที่ลูกค้ามารับ: {_PickupDate:dd/MM/yyyy}";
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
                g.DrawString(item.Name, font, Brushes.Black, xs[0] + 2, y + 2);

                // จำนวน (กึ่งกลาง)
                var qty = item.Quantity.ToString();
                var szQty = g.MeasureString(qty, font);
                g.DrawString(qty, font, Brushes.Black,
                             xs[1] + (colWidth - szQty.Width) / 2f,
                             y + (rowHeight - szQty.Height) / 2f);

                // ราคา (กึ่งกลาง)
                var price = item.Price.ToString("N2");
                var szPrice = g.MeasureString(price, font);
                g.DrawString(price, font, Brushes.Black,
                             xs[2] + (colWidth - szPrice.Width) / 2f,
                             y + (rowHeight - szPrice.Height) / 2f);

                // จำนวนเงิน = Quantity * Price (กึ่งกลาง)
                decimal amount = item.Quantity * item.Price;
                var amt = amount.ToString("N2");
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
                lineY += font.GetHeight(g) + 2;
            }
        }
        private void DrawSummaryRight(Graphics g, Font font, Rectangle page, float y)
        {
            decimal total = _items.Sum(i => i.Price * i.Quantity);
            decimal discounted = total * (1 - _customerDiscount);

            string totalLine = $"รวม: {total:N2} บาท";
            var szTot = g.MeasureString(totalLine, font);
            float xTot = page.Right - szTot.Width - 20;
            g.DrawString(totalLine, font, Brushes.Black, xTot, y);

            if (_customerDiscount > 0m)
            {
                y += font.GetHeight(g) + 4;
                string discLine = $"หลังหักส่วนลด: {discounted:N2} บาท";
                var szDisc = g.MeasureString(discLine, font);
                float xDisc = page.Right - szDisc.Width - 20;
                g.DrawString(discLine, font, Brushes.Black, xDisc, y);
            }
        }
    }
}
