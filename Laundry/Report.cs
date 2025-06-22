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

            // กำหนดค่าเริ่มต้นของ DateTimePicker และเปลี่ยนรูปแบบให้แสดงเฉพาะเดือนและปี
            dtpCreateDate.Value = DateTime.Today;
            dtpCreateDate.Format = DateTimePickerFormat.Custom;
            dtpCreateDate.CustomFormat = "MMMM yyyy";
            dtpCreateDate.ShowUpDown = true; // เปลี่ยนเป็นแบบเลื่อนขึ้นลงแทนปฏิทิน

            // เปลี่ยนข้อความคำอธิบาย
            label3.Text = "เดือนที่ต้องการดูรายงาน";

            // เพิ่ม Event Handler
            dtpCreateDate.ValueChanged += DtpCreateDate_ValueChanged;
            this.Load += Report_Load;
            dgvReport.DataBindingComplete += DgvReport_DataBindingComplete;
        }

        private void Report_Load(object sender, EventArgs e)
        {
            // โหลดข้อมูลเมื่อเปิดฟอร์ม
            LoadReceiptDataByMonth(dtpCreateDate.Value);
        }

        private void DtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            // โหลดข้อมูลใหม่เมื่อมีการเปลี่ยนเดือน
            LoadReceiptDataByMonth(dtpCreateDate.Value);
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

            // จัดรูปแบบคอลัมน์เงิน
            foreach (DataGridViewColumn col in dgvReport.Columns)
            {
                if (col.Name == "TotalBeforeDiscount" || col.Name == "Discount" || col.Name == "TotalAfterDiscount")
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

        private void LoadReceiptDataByMonth(DateTime date)
        {
            try
            {
                // สร้าง DTO สำหรับเก็บข้อมูลรายงาน
                var reportData = new List<ReceiptReportDto>();

                // ดึงเดือนและปีจากวันที่ที่เลือก
                int month = date.Month;
                int year = date.Year;

                // สร้างวันที่เริ่มต้นและสิ้นสุดของเดือน
                DateTime firstDayOfMonth = new DateTime(year, month, 1);
                DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // อัพเดทชื่อฟอร์มเพื่อแสดงเดือนและปีที่กำลังดูรายงาน
                this.Text = $"รายงานการขายประจำเดือน {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} {year}";

                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = cn;

                    // สร้าง SQL query ที่กรองตามเดือนและปี
                    var sql = @"
                                SELECT 
                                    R.ReceiptDate, 
                                    R.CustomReceiptId, 
                                    OH.CustomOrderId, 
                                    R.TotalBeforeDiscount, 
                                    R.Discount, 
                                    R.TotalAfterDiscount
                                FROM Receipt R
                                INNER JOIN OrderHeader OH ON R.OrderID = OH.OrderID
                                WHERE R.ReceiptDate >= @startDate AND R.ReceiptDate <= @endDate
                                ORDER BY R.ReceiptDate ASC"; // เปลี่ยนจาก DESC เป็น ASC เพื่อให้วันที่เก่าขึ้นก่อน

                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@startDate", firstDayOfMonth);
                    cmd.Parameters.AddWithValue("@endDate", lastDayOfMonth.AddDays(1).AddSeconds(-1)); // ถึงเวลา 23:59:59 ของวันสุดท้าย

                    cn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reportData.Add(new ReceiptReportDto
                            {
                                ReceiptDate = reader["ReceiptDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReceiptDate"]) : DateTime.MinValue,
                                CustomReceiptId = reader["CustomReceiptId"] != DBNull.Value ? reader["CustomReceiptId"].ToString() : "",
                                CustomOrderId = reader["CustomOrderId"] != DBNull.Value ? reader["CustomOrderId"].ToString() : "",
                                TotalBeforeDiscount = reader["TotalBeforeDiscount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalBeforeDiscount"]) : 0,
                                Discount = reader["Discount"] != DBNull.Value ? Convert.ToDecimal(reader["Discount"]) : 0,
                                TotalAfterDiscount = reader["TotalAfterDiscount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAfterDiscount"]) : 0
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
                return;
            }

            decimal totalBeforeDiscount = data.Sum(r => r.TotalBeforeDiscount);
            decimal totalDiscount = data.Sum(r => r.Discount);
            decimal totalAfterDiscount = data.Sum(r => r.TotalAfterDiscount);

            // แสดงผลรวมในฟอร์ม
            lblTotal.Text = totalBeforeDiscount.ToString("N2") + " บาท";
            lblDiscount.Text = totalDiscount.ToString("N2") + " บาท";
            lblTotalAfterDiscount.Text = totalAfterDiscount.ToString("N2") + " บาท";

            // เพิ่มข้อความแสดงจำนวนรายการทั้งหมด
            this.Text = $"{this.Text} - จำนวน {data.Count} รายการ";
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // DTO สำหรับเก็บข้อมูลรายงาน
        public class ReceiptReportDto
        {
            public DateTime ReceiptDate { get; set; }
            public string CustomReceiptId { get; set; }
            public string CustomOrderId { get; set; }
            public decimal TotalBeforeDiscount { get; set; }
            public decimal Discount { get; set; }
            public decimal TotalAfterDiscount { get; set; }
        }
    }
}
