using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laundry_Management.Laundry
{
    public partial class Check_List : Form
    {
        private readonly Find_Service.OrderRepository _repo = new Find_Service.OrderRepository();
        public Check_List()
        {
            InitializeComponent();
            LoadOrders(null, null, null);
            dgvOrders.DataBindingComplete += DgvOrders_DataBindingComplete;
        }
        private void DgvOrders_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (dgvOrders.Columns["CustomerName"] != null)
                dgvOrders.Columns["CustomerName"].HeaderText = "ชื่อ-นามสกุล ลูกค้า";
            if (dgvOrders.Columns["Phone"] != null)
                dgvOrders.Columns["Phone"].HeaderText = "เบอร์โทร";
            if (dgvOrders.Columns["Discount"] != null)
                dgvOrders.Columns["Discount"].HeaderText = "ส่วนลด";
            if (dgvOrders.Columns["OrderDate"] != null)
                dgvOrders.Columns["OrderDate"].HeaderText = "วันที่สั่งOrder";
            if (dgvOrders.Columns["PickupDate"] != null)
                dgvOrders.Columns["PickupDate"].HeaderText = "วันรับ";
            if (dgvOrders.Columns["GrandTotalPrice"] != null)
                dgvOrders.Columns["GrandTotalPrice"].HeaderText = "ราคารวมทั้งหมด";
            if (dgvOrders.Columns["DiscountedTotal"] != null)
                dgvOrders.Columns["DiscountedTotal"].HeaderText = "ราคาหลังลดราคา";
            if (dgvOrders.Columns["ReceiptId"] != null)
                dgvOrders.Columns["ReceiptId"].HeaderText = "เลขใบเสร็จ";
            if (dgvOrders.Columns["ReceivedStatus"] != null)
                dgvOrders.Columns["ReceivedStatus"].HeaderText = "สถานะการรับผ้า";
        }
        private void LoadOrders(string customerFilter = null, int? orderIdFilter = null, DateTime? createDateFilter = null)
        {
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvOrders.DataSource = _repo.GetOrders(customerFilter, orderIdFilter, createDateFilter);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string cust = txtCustomerFilter.Text.Trim();

            int? oid = null;
            if (int.TryParse(txtOrderId.Text.Trim(), out int tmp))
                oid = tmp;

            DateTime? createDt = null;
            if (dtpCreateDate.Checked)
                createDt = dtpCreateDate.Value.Date;

            LoadOrders(cust, oid, createDt);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtCustomerFilter.Text = "";
            txtOrderId.Text = "";
            dtpCreateDate.Checked = false;
            LoadOrders(null, null, null);
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
