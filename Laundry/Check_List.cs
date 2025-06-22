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

namespace Laundry_Management.Laundry
{
    public partial class Check_List : Form
    {
        public Check_List()
        {
            InitializeComponent();

            // Wire up checkbox event handlers
            chkCompleted.CheckedChanged += chkCompleted_CheckedChanged;
            chkPending.CheckedChanged += chkPending_CheckedChanged;

            // Add this line to wire up the DateTimePicker ValueChanged event
            dtpCreateDate.ValueChanged += dtpCreateDate_ValueChanged;

            // Set default checkbox state
            chkPending.Checked = true;
            chkCompleted.Checked = false;

            // Set date picker to today and checked
            dtpCreateDate.Value = DateTime.Today;
            dtpCreateDate.Checked = true;

            // Load today's orders by default
            LoadOrders(null, null, DateTime.Today);

            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvOrders.DataBindingComplete += DgvOrders_DataBindingComplete;
        }

        private void DgvOrders_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // ปรับชื่อหัวคอลัมน์ให้เป็นภาษาไทย
            if (dgvOrders.Columns["OrderID"] != null)
                dgvOrders.Columns["OrderID"].Visible = false;

            if (dgvOrders.Columns["ReceiptID"] != null)
                dgvOrders.Columns["ReceiptID"].Visible = false;

            if (dgvOrders.Columns["หมายเลขใบรับผ้า"] != null)
                dgvOrders.Columns["หมายเลขใบรับผ้า"].HeaderText = "หมายเลขใบรับผ้า";

            if (dgvOrders.Columns["ชื่อลูกค้า"] != null)
                dgvOrders.Columns["ชื่อลูกค้า"].HeaderText = "ชื่อ-นามสกุล ลูกค้า";

            if (dgvOrders.Columns["เบอร์โทรศัพท์"] != null)
                dgvOrders.Columns["เบอร์โทรศัพท์"].HeaderText = "เบอร์โทร";

            if (dgvOrders.Columns["หมายเลขใบเสร็จ"] != null)
                dgvOrders.Columns["หมายเลขใบเสร็จ"].HeaderText = "หมายเลขใบเสร็จ";

            if (dgvOrders.Columns["วันที่รับผ้า"] != null)
                dgvOrders.Columns["วันที่รับผ้า"].HeaderText = "วันที่รับผ้า";

            if (dgvOrders.Columns["สถานะ"] != null)
                dgvOrders.Columns["สถานะ"].HeaderText = "สถานะการรับผ้า";

            if (dgvOrders.Columns["วันที่ลูกค้ามารับ"] != null)
                dgvOrders.Columns["วันที่ลูกค้ามารับ"].HeaderText = "วันที่ลูกค้ามารับ";
        }

        private void LoadOrders(string customId = null, string customerName = null, DateTime? createDate = null)
        {
            string query = @"
                            SELECT 
                                o.OrderID, 
                                r.CustomReceiptId as 'หมายเลขใบเสร็จ',
                                o.CustomOrderId as 'หมายเลขใบรับผ้า', 
                                o.CustomerName as 'ชื่อลูกค้า', 
                                o.Phone as 'เบอร์โทรศัพท์',
                                r.TotalBeforeDiscount as 'ราคารวม',
                                r.Discount as 'ส่วนลด',
                                r.TotalAfterDiscount as 'ราคารวมหลังหักส่วนลด',
                                o.OrderDate as 'วันที่ออกใบรับผ้า',
                                o.PickupDate as 'วันที่ต้องมารับผ้า',
                                r.ReceiptID, 
                                r.ReceiptStatus as 'สถานะใบเสร็จ',
                                r.PaymentMethod as 'วิธีการชำระเงิน',
                                r.IsPickedUp as 'สถานะ', 
                                r.CustomerPickupDate as 'วันที่ลูกค้ามารับ'
                            FROM OrderHeader o
                            INNER JOIN Receipt r ON o.OrderID = r.OrderID
                            WHERE o.OrderStatus = N'ออกใบเสร็จแล้ว'
                        ";

            var filters = new List<string>();
            var parameters = new List<SqlParameter>();

            // ตรวจสอบ checkbox สถานะการรับผ้า
            if (chkPending.Checked)
            {
                filters.Add("(r.IsPickedUp IS NULL OR r.IsPickedUp <> N'มารับแล้ว')");
            }
            else if (chkCompleted.Checked)
            {
                filters.Add("r.IsPickedUp = N'มารับแล้ว'");
            }

            if (!string.IsNullOrEmpty(customId))
            {
                filters.Add("(o.CustomOrderId LIKE @CustomID OR r.CustomReceiptId LIKE @CustomID)");
                parameters.Add(new SqlParameter("@CustomID", "%" + customId + "%"));
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                filters.Add("o.CustomerName LIKE @CustomerName");
                parameters.Add(new SqlParameter("@CustomerName", "%" + customerName + "%"));
            }

            if (createDate.HasValue)
            {
                filters.Add("CAST(r.ReceiptDate AS DATE) = @ReceiptDate");
                parameters.Add(new SqlParameter("@ReceiptDate", createDate.Value));
            }

            if (filters.Count > 0)
            {
                query += " AND " + string.Join(" AND ", filters);
            }

            using (SqlConnection conn = DBconfig.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dgvOrders.DataSource = dt;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchId = txtSearchId.Text.Trim();
            string customerName = txtCustomerFilter.Text.Trim();
            DateTime? createDate = dtpCreateDate.Checked ? (DateTime?)dtpCreateDate.Value.Date : null;

            LoadOrders(searchId, customerName, createDate);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearchId.Clear();
            txtCustomerFilter.Clear();
            dtpCreateDate.Checked = true;
            dtpCreateDate.Value = DateTime.Today;
            chkPending.Checked = true;
            chkCompleted.Checked = false;
            LoadOrders(null, null, DateTime.Today);
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chkPending_CheckedChanged(object sender, EventArgs e)
        {
            // ป้องกันการยกเลิกทั้งสองรายการ
            if (!chkPending.Checked && !chkCompleted.Checked)
            {
                chkPending.Checked = true;
                return;
            }

            // ถ้า chkPending ถูกเลือก ให้ยกเลิกการเลือก chkCompleted
            if (chkPending.Checked)
            {
                chkCompleted.Checked = false;
            }

            // ดำเนินการค้นหาอีกครั้ง
            btnSearch_Click(sender, e);
        }

        private void chkCompleted_CheckedChanged(object sender, EventArgs e)
        {
            // ป้องกันการยกเลิกทั้งสองรายการ
            if (!chkCompleted.Checked && !chkPending.Checked)
            {
                chkCompleted.Checked = true;
                return;
            }

            // ถ้า chkCompleted ถูกเลือก ให้ยกเลิกการเลือก chkPending
            if (chkCompleted.Checked)
            {
                chkPending.Checked = false;
            }

            // ดำเนินการค้นหาอีกครั้ง
            btnSearch_Click(sender, e);
        }
        // Add this method to handle the DateTimePicker ValueChanged event
        private void dtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            // Only trigger search if the DateTimePicker is checked (date is selected)
            if (dtpCreateDate.Checked)
            {
                // Call the existing search functionality
                btnSearch_Click(sender, e);
            }
        }
    }
}
