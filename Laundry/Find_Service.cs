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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Laundry_Management.Laundry
{
    public partial class Find_Service : Form
    {
        private readonly OrderRepository _repo = new OrderRepository();
        private List<OrderItemDto> _currentItems;
        private decimal _totalAmount;
        public Find_Service()
        {
            InitializeComponent();

            // เมื่อเปิดหน้านี้ครั้งแรก ให้โหลดเฉพาะข้อมูลของวันปัจจุบันที่มีสถานะ "รอดำเนินการ"
            DateTime today = DateTime.Today;
            LoadOrders(null, null, today, "ดำเนินการสำเร็จ");

            // เลือกวันที่ปัจจุบันใน DateTimePicker
            dtpCreateDate.Value = today;
            dtpCreateDate.Checked = true;

            // ตั้งค่า DataGridView ให้เลือกทั้งแถวเมื่อคลิกที่ cell ใดก็ได้
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // เพิ่ม event handler สำหรับการคลิกที่ cell
            dgvOrders.CellClick += DgvOrders_CellClick;
            dgvItems.CellClick += DgvItems_CellClick;

            dgvOrders.DataBindingComplete += DgvOrders_DataBindingComplete;
            dgvItems.DataBindingComplete += DgvItems_DataBindingComplete;

        }
        private void DgvOrders_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // ป้องกันการคลิกที่ส่วนหัวคอลัมน์
            {
                // เลือกทั้งแถวเมื่อคลิกที่เซลล์ใดก็ได้
                dgvOrders.CurrentCell = dgvOrders.Rows[e.RowIndex].Cells[dgvOrders.CurrentCell.ColumnIndex];

                // ทำให้แน่ใจว่า SelectionChanged event จะถูกเรียก
                dgvOrders_SelectionChanged(sender, EventArgs.Empty);
            }
        }

        // เพิ่ม event handler เมื่อคลิกที่ cell ใน dgvItems
        private void DgvItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // ป้องกันการคลิกที่ส่วนหัวคอลัมน์
            {
                // เลือกทั้งแถวเมื่อคลิกที่เซลล์ใดก็ได้
                dgvItems.CurrentCell = dgvItems.Rows[e.RowIndex].Cells[dgvItems.CurrentCell.ColumnIndex];
            }
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
            if (dgvOrders.Columns["CustomOrderId"] != null)
                dgvOrders.Columns["CustomOrderId"].HeaderText = "หมายเลขรายการ";
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
            if (dgvOrders.Columns["CustomerName"] != null)
                dgvOrders.Columns["CustomerName"].Visible = false;
            if (dgvOrders.Columns["Phone"] != null)
                dgvOrders.Columns["Phone"].Visible = false;
            if (dgvOrders.Columns["GrandTotalPrice"] != null)
                dgvOrders.Columns["GrandTotalPrice"].Visible = false;
            if (dgvOrders.Columns["DiscountedTotal"] != null)
                dgvOrders.Columns["DiscountedTotal"].Visible = false;
            if (dgvOrders.Columns["ReceiptId"] != null)
                dgvOrders.Columns["ReceiptId"].Visible = false;
            if (dgvOrders.Columns["ReceivedStatus"] != null)
                dgvOrders.Columns["ReceivedStatus"].Visible = false;
            if (dgvOrders.Columns["Discount"] != null)
                dgvOrders.Columns["Discount"].Visible = false;
            if (dgvOrders.Columns["OrderID"] != null)
                dgvOrders.Columns["OrderID"].Visible = false;
            if (dgvOrders.Columns["CustomReceiptId"] != null)
                dgvOrders.Columns["CustomReceiptId"].Visible = false;
        }
        // ปรับปรุงเมธอด LoadOrders ให้รองรับพารามิเตอร์ statusFilter
        private void LoadOrders(
            string customerFilter = null,
            int? orderIdFilter = null,
            DateTime? createDateFilter = null,
            string statusFilter = null)
        {
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvOrders.DataSource = _repo.GetOrders(customerFilter, orderIdFilter, createDateFilter, statusFilter);
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
            public string CustomOrderId { get; set; }
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
            public string CustomReceiptId { get; set; }
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
                DateTime? createDateFilter = null,
                string statusFilter = null)
            {
                var list = new List<OrderHeaderDto>();
                using (var cn = new SqlConnection(_cs))
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = cn;
                    var sb = new StringBuilder(@"
            SELECT OH.OrderID, OH.CustomOrderId, OH.Discount, OH.CustomerName, OH.Phone, OH.OrderDate,
                   OH.PickupDate, OH.GrandTotalPrice, OH.DiscountedTotal, R.ReceiptID, R.IsPickedUp, R.CustomReceiptId, OH.OrderStatus
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
                    if (!string.IsNullOrWhiteSpace(statusFilter))
                    {
                        sb.Append(" AND OH.OrderStatus = @status");
                        cmd.Parameters.AddWithValue("@status", statusFilter);
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
                                CustomOrderId = r["CustomOrderId"] as string ?? "",
                                CustomerName = r["CustomerName"] as string ?? "",
                                Phone = r["Phone"] as string ?? "",
                                Discount = r["Discount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Discount"]),
                                OrderDate = r["OrderDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["OrderDate"]),
                                PickupDate = r["PickupDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["PickupDate"]),
                                GrandTotalPrice = r["GrandTotalPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["GrandTotalPrice"]),
                                DiscountedTotal = r["DiscountedTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(r["DiscountedTotal"]),
                                ReceiptId = r["ReceiptID"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ReceiptID"]),
                                ReceivedStatus = r["IsPickedUp"] as string ?? "",
                                CustomReceiptId = r["CustomReceiptId"] as string ?? ""
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

            public int CreateReceipt(int orderId, string customReceiptId = null)
            {
                using (var cn = new SqlConnection(_cs))
                {
                    cn.Open(); // เปิด connection ก่อน
                    using (var tx = cn.BeginTransaction())
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO Receipt (OrderID, TotalBeforeDiscount, TotalAfterDiscount, CustomReceiptId)
                        OUTPUT INSERTED.ReceiptID
                        SELECT @oid, OH.GrandTotalPrice, OH.DiscountedTotal, @customId
                          FROM OrderHeader OH
                         WHERE OH.OrderID = @oid", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@oid", orderId);
                        cmd.Parameters.AddWithValue("@customId", customReceiptId ?? (object)DBNull.Value);
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

            // เก็บข้อมูลสินค้าปัจจุบัน
            _currentItems = items;

            // อัพเดทข้อมูลในส่วนแสดงผล
            UpdateDisplayInfo();
        }
        private void UpdateDisplayInfo()
        {
            if (dgvOrders.CurrentRow == null || _currentItems == null) return;

            // ดึงข้อมูลจาก dgvOrders
            var row = dgvOrders.CurrentRow;
            string customerName = row.Cells["CustomerName"].Value?.ToString() ?? "";
            decimal originalDiscount = 0;
            if (row.Cells["Discount"].Value != null && row.Cells["Discount"].Value != DBNull.Value)
            {
                originalDiscount = Convert.ToDecimal(row.Cells["Discount"].Value);
            }

            // คำนวณราคารวมทั้งหมดจากรายการที่ไม่ถูกยกเลิก
            _totalAmount = _currentItems
                .Where(i => !i.IsCanceled)
                .Sum(i => i.TotalAmount);

            // กำหนดค่าให้กับ label และ textbox
            label5.Text = customerName;  // ชื่อ-นามสกุล
            label8.Text = _totalAmount.ToString("N2") + " บาท";  // ราคารวมทั้งหมด

            // กำหนดค่าเริ่มต้นให้กับ txtDiscount
            txtDiscount.Text = originalDiscount.ToString();

            // กำหนดค่า checkbox ตามค่าเริ่มต้น (สมมติว่าเป็นเปอร์เซ็นต์)
            chkPercent.Checked = true;
            chkBaht.Checked = false;

            // อัพเดท label12 ด้วยค่าส่วนลดและหน่วย
            string unit = chkPercent.Checked ? "%" : "บาท";
            label12.Text = $"{originalDiscount} {unit}";

            // คำนวณราคาหลังหักส่วนลด
            CalculateDiscountedPrice();
        }
        private void CalculateDiscountedPrice()
        {
            if (string.IsNullOrWhiteSpace(txtDiscount.Text) || _totalAmount <= 0)
            {
                // ถ้าไม่มีส่วนลดหรือราคารวมเป็น 0
                label10.Text = _totalAmount.ToString("N2") + " บาท";
                return;
            }

            // แปลงค่าส่วนลดเป็น decimal
            if (!decimal.TryParse(txtDiscount.Text, out decimal discountValue))
            {
                // ถ้าแปลงไม่ได้ ให้ใช้ราคาเดิม
                label10.Text = _totalAmount.ToString("N2") + " บาท";
                return;
            }

            decimal discountedPrice;

            if (chkPercent.Checked)
            {
                // คำนวณแบบเปอร์เซ็นต์
                if (discountValue < 0 || discountValue > 100)
                {
                    MessageBox.Show("ส่วนลดเปอร์เซ็นต์ต้องอยู่ระหว่าง 0-100", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDiscount.Text = "0";
                    discountValue = 0;
                }
                discountedPrice = _totalAmount * (100 - discountValue) / 100;
            }
            else if (chkBaht.Checked)
            {
                // คำนวณแบบบาท
                if (discountValue < 0)
                {
                    MessageBox.Show("ส่วนลดต้องไม่ติดลบ", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDiscount.Text = "0";
                    discountValue = 0;
                }
                else if (discountValue > _totalAmount)
                {
                    MessageBox.Show("ส่วนลดต้องไม่เกินราคารวม", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDiscount.Text = _totalAmount.ToString();
                    discountValue = _totalAmount;
                }
                discountedPrice = _totalAmount - discountValue;
            }
            else
            {
                // ถ้าไม่ได้เลือกทั้งสองอย่าง ให้ใช้ราคาเดิม
                discountedPrice = _totalAmount;
            }

            // แสดงผลราคาหลังหักส่วนลด
            label10.Text = discountedPrice.ToString("N2") + " บาท";
        }
        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            CalculateDiscountedPrice();

            // Update label12 with the discount value and unit
            if (decimal.TryParse(txtDiscount.Text, out decimal discountValue))
            {
                string unit = chkPercent.Checked ? "%" : "บาท";
                label12.Text = $"{discountValue} {unit}";
            }
            else
            {
                label12.Text = "-";
            }
        }
        private void chkDiscount_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            if (chk == null) return;

            if (chk.Checked)
            {
                // If this is chkBaht and it's now checked, uncheck chkPercent
                if (chk == chkBaht)
                    chkPercent.Checked = false;
                // If this is chkPercent and it's now checked, uncheck chkBaht
                else if (chk == chkPercent)
                    chkBaht.Checked = false;
            }
            else
            {
                // If we're trying to uncheck this checkbox, make sure at least one is checked
                if (!chkBaht.Checked && !chkPercent.Checked)
                {
                    chk.Checked = true;
                    return;
                }
            }

            // Update the unit in label12 based on the selected checkbox
            if (decimal.TryParse(txtDiscount.Text, out decimal discountValue))
            {
                string unit = chkPercent.Checked ? "%" : "บาท";
                label12.Text = $"{discountValue} {unit}";
            }

            // Calculate the discount with the new checkbox state
            CalculateDiscountedPrice();
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

            // ใช้ค่าส่วนลดจาก txtDiscount แทน todayDiscount
            if (!decimal.TryParse(txtDiscount.Text, out decimal discountValue))
            {
                MessageBox.Show("กรุณากรอกส่วนลดให้ถูกต้อง", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!chkCash.Checked && !chkDebit.Checked)
            {
                MessageBox.Show("กรุณาเลือกวิธีชำระเงิน", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ตรวจสอบส่วนลด
            if (chkPercent.Checked)
            {
                // ตรวจสอบเปอร์เซ็นต์
                if (discountValue < 0 || discountValue > 100)
                {
                    MessageBox.Show("ส่วนลดเปอร์เซ็นต์ต้องอยู่ระหว่าง 0-100", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // กำหนดส่วนลดใน header
                header.Discount = discountValue;
                header.IsTodayDiscountPercent = true; // เป็นเปอร์เซ็นต์
            }
            else if (chkBaht.Checked)
            {
                // ตรวจสอบบาท
                decimal total = items.Sum(i => i.TotalAmount);
                if (discountValue < 0)
                {
                    MessageBox.Show("ส่วนลดต้องไม่ติดลบ", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (discountValue > total)
                {
                    MessageBox.Show("ส่วนลดต้องไม่เกินราคารวม", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // แปลงส่วนลดจากบาทเป็นเปอร์เซ็นต์สำหรับเก็บในฐานข้อมูล
                decimal percentDiscount = 0;
                if (total > 0)
                {
                    percentDiscount = (discountValue / total) * 100;
                }

                // กำหนดส่วนลดใน header
                header.Discount = percentDiscount;
                header.IsTodayDiscountPercent = false; // เป็นบาท
            }
            else
            {
                // ถ้าไม่ได้เลือกทั้งสองอย่าง
                header.Discount = 0;
                header.IsTodayDiscountPercent = true; // ค่าเริ่มต้นเป็นเปอร์เซ็นต์
            }

            // คำนวณราคาหลังหักส่วนลด
            decimal discountedTotal;
            if (chkPercent.Checked)
            {
                // คำนวณแบบเปอร์เซ็นต์
                discountedTotal = _totalAmount * (100 - discountValue) / 100;
            }
            else if (chkBaht.Checked)
            {
                // คำนวณแบบบาท
                discountedTotal = _totalAmount - discountValue;
            }
            else
            {
                discountedTotal = _totalAmount;
            }

            // ดึงเลขที่ใบเสร็จใหม่จาก AppSettingsManager เหมือนกับหน้า Service
            string customReceiptId = AppSettingsManager.GetNextReceiptId();

            int receiptId = -1;
            try
            {
                // 1) สร้าง Receipt พร้อมกับ CustomReceiptId
                receiptId = CreateReceiptWithCustomId(header.OrderID, customReceiptId);

                foreach (var item in items)
                {
                    _repo.CreateReceiptItem(
                        receiptId: receiptId,
                        orderItemId: item.OrderItemID,
                        quantity: item.Quantity,
                        amount: item.TotalAmount
                    );
                }

                // อัพเดทข้อมูลใน header เพื่อส่งไปให้ Print Form
                header.CustomReceiptId = customReceiptId;
                header.DiscountedTotal = discountedTotal; // เพิ่มเพื่อส่งค่าที่คำนวณแล้วไปยัง Print Form

                // 2) เปิดฟอร์ม ReceiptPrintForm พร้อม receiptId และข้อมูลที่ต้องการ
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
                                // อัพเดทสถานะของ Receipt เป็น "ยกเลิกการพิมพ์"
                                using (var cmd1 = new SqlCommand(
                                    "UPDATE Receipt SET ReceiptStatus = @status WHERE ReceiptID = @rid", cn, tx))
                                {
                                    cmd1.Parameters.AddWithValue("@status", "ยกเลิกการพิมพ์");
                                    cmd1.Parameters.AddWithValue("@rid", receiptId);
                                    cmd1.ExecuteNonQuery();
                                }

                                // บันทึกประวัติการเปลี่ยนสถานะ
                                using (var cmd2 = new SqlCommand(
                                    @"INSERT INTO ReceiptStatusHistory (ReceiptID, PreviousStatus, NewStatus, ChangeBy)
                  VALUES (@rid, @prevStatus, @newStatus, @changeBy)", cn, tx))
                                {
                                    cmd2.Parameters.AddWithValue("@rid", receiptId);
                                    cmd2.Parameters.AddWithValue("@prevStatus", "ออกใบเสร็จแล้ว");
                                    cmd2.Parameters.AddWithValue("@newStatus", "ยกเลิกการพิมพ์");
                                    cmd2.Parameters.AddWithValue("@changeBy", Environment.UserName);
                                    cmd2.ExecuteNonQuery();
                                }
                                tx.Commit();
                            }
                        }

                        // คืนค่า ReceiptId เพื่อให้ใช้ซ้ำได้
                        string currentId = AppSettingsManager.GetSetting("NextReceiptId");
                        int nextId;
                        if (int.TryParse(currentId, out nextId) && nextId > 1)
                        {
                            // ตั้งค่า NextReceiptId ให้กลับไปเป็นค่าเดิม (ลดลง 1)
                            AppSettingsManager.UpdateSetting("NextReceiptId", (nextId - 1).ToString());
                        }

                        // แจ้งว่ายกเลิกการออกใบเสร็จ
                        MessageBox.Show("ยกเลิกการออกใบเสร็จ และบันทึกเป็นสถานะ 'ยกเลิกการพิมพ์'");
                        return;
                    }
                }

                using (var cn = new SqlConnection(_cs))
                {
                    cn.Open();
                    using (var tx = cn.BeginTransaction()) // เพิ่ม transaction เพื่อให้แน่ใจว่าการอัพเดทจะสมบูรณ์
                    {
                        try
                        {
                            using (var cmd = new SqlCommand(
                                @"UPDATE OrderHeader 
                 SET Discount = @discount, 
                     DiscountedTotal = @discountedTotal,
                     OrderStatus = @status
                 WHERE OrderID = @id", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@discount", header.Discount);
                                cmd.Parameters.AddWithValue("@discountedTotal", discountedTotal);
                                cmd.Parameters.AddWithValue("@status", "ออกใบเสร็จแล้ว"); // กำหนดค่าที่แน่นอน
                                cmd.Parameters.AddWithValue("@id", header.OrderID);
                                cmd.ExecuteNonQuery();
                            }

                            // อัพเดทสถานะของ Receipt เป็น "พิมพ์เรียบร้อยแล้ว"
                            using (var cmd2 = new SqlCommand(
                                "UPDATE Receipt SET ReceiptStatus = @status WHERE ReceiptID = @rid", cn, tx))
                            {
                                cmd2.Parameters.AddWithValue("@status", "พิมพ์เรียบร้อยแล้ว");
                                cmd2.Parameters.AddWithValue("@rid", receiptId);
                                cmd2.ExecuteNonQuery();
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            throw new Exception("ไม่สามารถอัพเดทสถานะได้: " + ex.Message);
                        }
                    }
                }

                MessageBox.Show("พิมพ์และบันทึกใบเสร็จสำเร็จ");

                // รีเฟรชข้อมูลในตารางเพื่อแสดงสถานะใหม่
                string cust = txtCustomerFilter.Text.Trim();
                int? oid = null;
                if (int.TryParse(txtOrderId.Text.Trim(), out int tmp))
                    oid = tmp;
                DateTime? createDt = null;
                if (dtpCreateDate.Checked)
                    createDt = dtpCreateDate.Value.Date;
                LoadOrders(cust, oid, createDt, "ดำเนินการสำเร็จ");
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

            // เพิ่มพารามิเตอร์ statusFilter เป็น "ดำเนินการสำเร็จ" เพื่อค้นหาเฉพาะรายการที่มีสถานะนี้
            LoadOrders(cust, oid, createDt, "ดำเนินการสำเร็จ");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // แสดงทั้งหมดแต่เฉพาะรายการที่มีสถานะ "ดำเนินการสำเร็จ"
            LoadOrders(null, null, null, "ดำเนินการสำเร็จ");
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        public int CreateReceiptWithCustomId(int orderId, string customReceiptId)
        {
            string paymentMethod = chkCash.Checked ? "เงินสด" : "บัตรเครดิต";

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                using (var cmd = new SqlCommand(@"
                    INSERT INTO Receipt (OrderID, TotalBeforeDiscount, TotalAfterDiscount, CustomReceiptId, ReceiptStatus, PaymentMethod)
                    OUTPUT INSERTED.ReceiptID
                    SELECT @oid, OH.GrandTotalPrice, OH.DiscountedTotal, @customId, @status, @paymentMethod
                      FROM OrderHeader OH
                     WHERE OH.OrderID = @oid", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    cmd.Parameters.AddWithValue("@customId", customReceiptId);
                    cmd.Parameters.AddWithValue("@status", "ออกใบเสร็จแล้ว");
                    cmd.Parameters.AddWithValue("@paymentMethod", paymentMethod);
                    int receiptId = (int)cmd.ExecuteScalar();

                    // อัพเดทสถานะใน OrderHeader ว่าถูกทำเป็นใบเสร็จแล้ว
                    using (var updateCmd = new SqlCommand(@"
                        UPDATE OrderHeader
                        SET IsReceiptPrinted = 1
                        WHERE OrderID = @oid", cn, tx))
                    {
                        updateCmd.Parameters.AddWithValue("@oid", orderId);
                        updateCmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return receiptId;
                }
            }
        }

        public void UpdateOrderWithCustomId(int orderId, string customOrderId)
        {
            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(
                @"UPDATE OrderHeader 
                 SET CustomOrderId = @customId
                 WHERE OrderID = @id", cn))
            {
                cmd.Parameters.AddWithValue("@customId", customOrderId);
                cmd.Parameters.AddWithValue("@id", orderId);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void Find_Service_Load(object sender, EventArgs e)
        {
            // ตั้งค่า event handler สำหรับ txtDiscount
            txtDiscount.TextChanged += txtDiscount_TextChanged;

            // ตั้งค่า event handler สำหรับ checkbox ส่วนลด
            chkBaht.CheckedChanged += chkDiscount_CheckedChanged;
            chkPercent.CheckedChanged += chkDiscount_CheckedChanged;

            // ตั้งค่า event handler สำหรับ checkbox วิธีชำระเงิน
            chkCash.CheckedChanged += chkPayment_CheckedChanged;
            chkDebit.CheckedChanged += chkPayment_CheckedChanged;

            // ตั้งค่าเริ่มต้นให้ chkPercent เป็น checked
            chkPercent.Checked = true;
            chkBaht.Checked = false;

            // ตั้งค่าเริ่มต้นให้ chkCash เป็น checked
            chkCash.Checked = true;
            chkDebit.Checked = false;
        }
        private void chkPayment_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            if (chk == null) return;

            if (chk.Checked)
            {
                // If this is chkCash and it's now checked, uncheck chkDebit
                if (chk == chkCash)
                    chkDebit.Checked = false;
                // If this is chkDebit and it's now checked, uncheck chkCash
                else if (chk == chkDebit)
                    chkCash.Checked = false;
            }
            else
            {
                // If we're trying to uncheck this checkbox, make sure at least one is checked
                if (!chkCash.Checked && !chkDebit.Checked)
                {
                    chk.Checked = true;
                    return;
                }
            }
        }
    }
}
