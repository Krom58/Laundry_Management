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

namespace Laundry_Management.Laundry
{
    public partial class Find_Service : Form
    {
        private readonly OrderRepository _repo = new OrderRepository();
        public Find_Service()
        {
            InitializeComponent();
            LoadOrders(null, null, null);
            dgvOrders.DataBindingComplete += DgvOrders_DataBindingComplete;
            dgvItems.DataBindingComplete += DgvItems_DataBindingComplete;
        }
        private void DgvItems_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (dgvItems.Columns["ItemNumber"] != null)
                dgvItems.Columns["ItemNumber"].HeaderText = "รหัสสินค้า";
            if (dgvItems.Columns["ItemName"] != null)
                dgvItems.Columns["ItemName"].HeaderText = "ชื่อผ้า/บริการ";
            if (dgvItems.Columns["Quantity"] != null)
                dgvItems.Columns["Quantity"].HeaderText = "จำนวน";
            if (dgvItems.Columns["TotalAmount"] != null)
                dgvItems.Columns["TotalAmount"].HeaderText = "ราคารวม";
            if (dgvItems.Columns["IsCanceled"] != null)
                dgvItems.Columns["IsCanceled"].HeaderText = "ยกเลิกรายการ";
            if (dgvItems.Columns["CancelReason"] != null)
                dgvItems.Columns["CancelReason"].HeaderText = "เหตุผลการยกเลิก";
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
            // ซ่อนคอลัมน์ TodayDiscount และ IsTodayDiscountPercent
            if (dgvOrders.Columns["TodayDiscount"] != null)
                dgvOrders.Columns["TodayDiscount"].Visible = false;
            if (dgvOrders.Columns["IsTodayDiscountPercent"] != null)
                dgvOrders.Columns["IsTodayDiscountPercent"].Visible = false;
        }
        private void LoadOrders(string customerFilter = null,
    int? orderIdFilter = null,
    DateTime? createDateFilter = null)
        {
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvOrders.DataSource = _repo.GetOrders(customerFilter, orderIdFilter, createDateFilter);
            dgvItems.DataSource = null;
        }
        public class OrderItemDto
        {
            public int OrderItemID { get; set; }
            public string ItemNumber { get; set; }
            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public decimal TotalAmount { get; set; }
            public bool IsCanceled { get; set; }
            public string CancelReason { get; set; }
        }

        public class OrderHeaderDto
        {
            public int OrderID { get; set; }
            public string CustomerName { get; set; }
            public string Phone { get; set; }
            public decimal Discount { get; set; }
            public decimal TodayDiscount { get; set; }
            public bool IsTodayDiscountPercent { get; set; } // Add this property
            public DateTime OrderDate { get; set; }
            public DateTime PickupDate { get; set; }
            public decimal GrandTotalPrice { get; set; }
            public decimal DiscountedTotal { get; set; }
            public int? ReceiptId { get; set; }
            public string ReceivedStatus { get; set; }
        }
        private readonly string _cs =
                "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
        public class OrderRepository
        {
            private readonly string _cs =
                "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            public void CreateReceiptItem(int receiptId, int orderItemId, int quantity, decimal amount)
            {
                using (var cn = new SqlConnection(_cs))
                using (var cmd = new SqlCommand(@"
            INSERT INTO ReceiptItem
              (ReceiptID, OrderItemID, Quantity, Amount)
            VALUES
              (@rid, @oiid, @qty, @amt)", cn))
                {
                    cmd.Parameters.AddWithValue("@rid", receiptId);
                    cmd.Parameters.AddWithValue("@oiid", orderItemId);
                    cmd.Parameters.AddWithValue("@qty", quantity);
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            public List<OrderHeaderDto> GetOrders(
    string customerFilter = null,
    int? orderIdFilter = null,
    DateTime? createDateFilter = null)   // เปลี่ยนชื่อตรงนี้
            {
                var list = new List<OrderHeaderDto>();
                using (var cn = new SqlConnection(_cs))
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = cn;
                    var sb = new StringBuilder(@"
            SELECT OH.OrderID, OH.Discount, OH.CustomerName, OH.Phone, OH.OrderDate,
                   OH.PickupDate, OH.GrandTotalPrice, OH.DiscountedTotal,R.ReceiptID,R.IsPickedUp
              FROM OrderHeader OH
                LEFT JOIN Receipt R ON OH.OrderID = R.OrderID
             WHERE 1=1");

                    if (!string.IsNullOrWhiteSpace(customerFilter))
                    {
                        sb.Append(" AND OH.CustomerName LIKE @cust");
                        cmd.Parameters.AddWithValue("@cust", "%" + customerFilter + "%");
                    }
                    if (orderIdFilter.HasValue)
                    {
                        sb.Append(" AND OH.OrderID = @oid");
                        cmd.Parameters.AddWithValue("@oid", orderIdFilter.Value);
                    }
                    if (createDateFilter.HasValue)
                    {
                        sb.Append(" AND CAST(OH.OrderDate AS DATE) = @cdate");
                        cmd.Parameters.AddWithValue("@cdate", createDateFilter.Value);
                    }

                    cmd.CommandText = sb.ToString();
                    cn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new OrderHeaderDto
                            {
                                OrderID = Convert.ToInt32(r["OrderID"]),
                                CustomerName = r["CustomerName"] as string ?? "",
                                Phone = r["Phone"] as string ?? "",
                                Discount = r["Discount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Discount"]),
                                OrderDate = r["OrderDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["OrderDate"]),
                                PickupDate = r["PickupDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["PickupDate"]),
                                GrandTotalPrice = r["GrandTotalPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["GrandTotalPrice"]),
                                DiscountedTotal = r["DiscountedTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(r["DiscountedTotal"]),
                                ReceiptId = r["ReceiptID"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ReceiptID"]),
                                ReceivedStatus = r["IsPickedUp"] as string ?? ""
                            });
                        }
                    }
                }
                return list;
            }

            public List<OrderItemDto> GetOrderItems(int orderId)
            {
                var list = new List<OrderItemDto>();
                using (var cn = new SqlConnection(_cs))
                using (var cmd = new SqlCommand(
                    "SELECT OrderItemID, ItemNumber, ItemName, Quantity, TotalAmount, IsCanceled, CancelReason " +
                    "  FROM OrderItem WHERE OrderID = @id", cn))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);
                    cn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new OrderItemDto
                            {
                                OrderItemID = Convert.ToInt32(r["OrderItemID"]),
                                ItemNumber = r["ItemNumber"].ToString(),
                                ItemName = r["ItemName"].ToString(),
                                Quantity = Convert.ToInt32(r["Quantity"]),
                                TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                                IsCanceled = r["IsCanceled"] != DBNull.Value && (bool)r["IsCanceled"],
                                CancelReason = r["CancelReason"] == DBNull.Value
                                               ? ""
                                               : r["CancelReason"].ToString()
                            });
                        }
                    }
                }
                return list;
            }

            public void UpdateItemCancelled(int orderItemId, bool isCancelled, string cancelReason)
            {
                using (var cn = new SqlConnection(_cs))
                using (var cmd = new SqlCommand(
                    @"UPDATE OrderItem 
             SET IsCanceled   = @c, 
                 CancelReason = @r 
           WHERE OrderItemID = @id", cn))
                {
                    cmd.Parameters.AddWithValue("@c", isCancelled);
                    cmd.Parameters.AddWithValue("@r", string.IsNullOrWhiteSpace(cancelReason)
                                                      ? (object)DBNull.Value
                                                      : cancelReason);
                    cmd.Parameters.AddWithValue("@id", orderItemId);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            public int CreateReceipt(int orderId)
            {
                using (var cn = new SqlConnection(_cs))
                {
                    cn.Open(); // เปิด connection ก่อน
                    using (var tx = cn.BeginTransaction())
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO Receipt (OrderID, TotalBeforeDiscount, TotalAfterDiscount)
                        OUTPUT INSERTED.ReceiptID
                        SELECT @oid, OH.GrandTotalPrice, OH.DiscountedTotal
                          FROM OrderHeader OH
                         WHERE OH.OrderID = @oid", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@oid", orderId);
                        int receiptId = (int)cmd.ExecuteScalar();
                        tx.Commit();
                        return receiptId;
                    }
                }
            }

            public List<OrderItemDto> GetReceiptItems(int receiptId)
            {
                var list = new List<OrderItemDto>();
                using (var cn = new SqlConnection(_cs))
                using (var cmd = new SqlCommand(@"
                SELECT RI.ReceiptItemID, OI.ItemNumber, OI.ItemName,
                       RI.Quantity, RI.Amount, OI.IsCancelled
                  FROM ReceiptItem RI
                  JOIN OrderItem OI ON RI.OrderItemID = OI.OrderItemID
                 WHERE RI.ReceiptID = @rid", cn))
                {
                    cmd.Parameters.AddWithValue("@rid", receiptId);
                    cn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new OrderItemDto
                            {
                                OrderItemID = (int)r["ReceiptItemID"],
                                ItemNumber = (string)r["ItemNumber"],
                                ItemName = (string)r["ItemName"],
                                Quantity = (int)r["Quantity"],
                                TotalAmount = (decimal)r["Amount"],
                                IsCanceled = (bool)r["IsCanceled"]
                            });
                        }
                    }
                }
                return list;
            }
        }
        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            var list = dgvItems.DataSource as BindingList<OrderItemDto>;
            if (list == null)
            {
                MessageBox.Show("ไม่พบข้อมูลในตาราง");
                return;
            }

            foreach (var item in list)
            {
                _repo.UpdateItemCancelled(
                    item.OrderItemID,
                    item.IsCanceled,
                    item.IsCanceled
                        ? item.CancelReason
                        : null
                );
            }

            MessageBox.Show("บันทึกสถานะยกเลิกรายการเรียบร้อย");
            dgvOrders_SelectionChanged(null, null);
        }

        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;

            int orderId = (int)dgvOrders.CurrentRow.Cells["OrderID"].Value;
            var items = _repo.GetOrderItems(orderId);
            var binding = new BindingList<OrderItemDto>(items);

            dgvItems.DataSource = binding;
            dgvItems.ReadOnly = false;
            dgvItems.EditMode = DataGridViewEditMode.EditOnEnter;

            // ปิดแก้ไขทุกคอลัมน์ก่อน
            foreach (DataGridViewColumn col in dgvItems.Columns)
                col.ReadOnly = true;

            // เปิดเฉพาะ IsCanceled และ CancelReason
            if (dgvItems.Columns["IsCanceled"] is DataGridViewCheckBoxColumn chk)
                chk.ReadOnly = false;
            if (dgvItems.Columns["CancelReason"] is DataGridViewTextBoxColumn txt)
                txt.ReadOnly = false;
        }

        private void btnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            var header = dgvOrders.CurrentRow.DataBoundItem as OrderHeaderDto;
            if (header == null) return;

            // กรองรายการที่ยังไม่ยกเลิก
            var items = dgvItems.Rows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as OrderItemDto)
                .Where(i => i != null && !i.IsCanceled)
                .ToList();

            if (!items.Any())
            {
                MessageBox.Show("ยังไม่มีรายการที่ต้องออกใบเสร็จ", "แจ้งเตือน",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // เปิดฟอร์ม Sale_Day เพื่อดึงส่วนลดของวัน
            decimal todayDiscount = 0;
            using (var saleForm = new Sale_Day())
            {
                if (saleForm.ShowDialog(this) == DialogResult.OK)
                {
                    // ดึงค่าส่วนลดจากฟอร์ม Sale_Day
                    if (decimal.TryParse(saleForm.GetDiscount(), out decimal discount))
                    {
                        // ตรวจสอบว่าเป็นเปอร์เซ็นต์หรือบาท
                        if (saleForm.IsPercentDiscount())
                        {
                            todayDiscount = discount;
                            // อัพเดทส่วนลดให้กับ header ที่จะนำไปใช้ในการพิมพ์
                            header.TodayDiscount = todayDiscount;
                            // เก็บข้อมูลว่าส่วนลดเป็นเปอร์เซ็นต์หรือบาท
                            header.IsTodayDiscountPercent = saleForm.IsPercentDiscount();
                        }
                        else
                        {
                            // ถ้าเป็นบาท ต้องคำนวณเป็นเปอร์เซ็นต์ก่อนใส่ใน header.Discount
                            // หาผลรวมของรายการที่ไม่ถูกยกเลิก
                            decimal total = items.Sum(i => i.TotalAmount);

                            // ถ้ายอดรวมเป็น 0 หรือน้อยกว่าส่วนลด
                            if (total <= 0 || discount > total)
                            {
                                MessageBox.Show("ไม่สามารถใช้ส่วนลดเป็นบาทได้ เนื่องจากยอดรวมน้อยกว่าส่วนลด",
                                    "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            header.TodayDiscount = discount;
                        }
                    }
                    else
                    {
                        header.TodayDiscount = 0;
                    }
                }
                else
                {
                    // ถ้ายกเลิกการใส่ส่วนลด
                    return;
                }
            }

            int receiptId = -1;
            try
            {
                // 1) สร้าง Receipt จริงก่อน
                receiptId = _repo.CreateReceipt(header.OrderID);
                foreach (var item in items)
                {
                    _repo.CreateReceiptItem(
                        receiptId: receiptId,
                        orderItemId: item.OrderItemID,
                        quantity: item.Quantity,
                        amount: item.TotalAmount
                    );
                }

                // 2) เปิดฟอร์ม ReceiptPrintForm พร้อม receiptId จริง
                using (var rptForm = new ReceiptPrintForm(receiptId, header, items))
                {
                    rptForm.ShowDialog(this);
                    if (!rptForm.IsPrinted)
                    {
                        using (var cn = new SqlConnection(_cs))
                        {
                            cn.Open();
                            using (var tx = cn.BeginTransaction())
                            {
                                // ลบ ReceiptItem ที่เกี่ยวข้อง
                                using (var cmd1 = new SqlCommand(
                                    "DELETE FROM ReceiptItem WHERE ReceiptID = @rid", cn, tx))
                                {
                                    cmd1.Parameters.AddWithValue("@rid", receiptId);
                                    cmd1.ExecuteNonQuery();
                                }
                                // ลบ Receipt
                                using (var cmd2 = new SqlCommand(
                                    "DELETE FROM Receipt WHERE ReceiptID = @rid", cn, tx))
                                {
                                    cmd2.Parameters.AddWithValue("@rid", receiptId);
                                    cmd2.ExecuteNonQuery();
                                }
                                tx.Commit();
                            }
                        }
                        // Optional: ลบ Receipt ที่สร้างทิ้ง ถ้าผู้ใช้ยกเลิกการปริ้น
                        MessageBox.Show("ยกเลิกการออกใบเสร็จ");
                        return;
                    }
                }

                MessageBox.Show("พิมพ์และบันทึกใบเสร็จสำเร็จ");
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดขณะบันทึก:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void button1_Click(object sender, EventArgs e)
        {
            LoadOrders(null, null, null);
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
