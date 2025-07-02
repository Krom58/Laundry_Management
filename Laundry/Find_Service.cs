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
        private List<OrderItemDto> _currentItems;
        private decimal _totalAmount;
        private const decimal VAT_RATE = 0.07m;
        public Find_Service()
        {
            InitializeComponent();

            txtCustomerFilter.KeyPress += TxtSearch_KeyPress;
            txtOrderId.KeyPress += TxtSearch_KeyPress;

            // เมื่อเปิดหน้านี้ครั้งแรก ให้โหลดเฉพาะข้อมูลของวันปัจจุบันที่มีสถานะ "รอดำเนินการ"
            DateTime today = DateTime.Today;
            LoadOrders(null, null, today, "ดำเนินการสำเร็จ", null);

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
        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ตรวจสอบว่ากด Enter หรือไม่ (รหัส ASCII 13)
            if (e.KeyChar == (char)Keys.Enter)
            {
                // ป้องกันเสียง beep
                e.Handled = true;

                // เรียกฟังก์ชันค้นหาเหมือนกับการกดปุ่ม
                btnSearch_Click(sender, EventArgs.Empty);
            }
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
        private void SelectFirstRow()
        {
            // Check if there are any rows in the DataGridView
            if (dgvOrders.Rows.Count > 0)
            {
                // Select the first row
                dgvOrders.ClearSelection();
                dgvOrders.Rows[0].Selected = true;

                // Find the first visible cell in the first row
                DataGridViewCell visibleCell = null;
                foreach (DataGridViewCell cell in dgvOrders.Rows[0].Cells)
                {
                    if (cell.Visible)
                    {
                        visibleCell = cell;
                        break;
                    }
                }

                // Set the first row's visible cell as the current cell to trigger selection events
                if (visibleCell != null && (dgvOrders.CurrentCell == null || dgvOrders.CurrentCell.RowIndex != 0))
                {
                    dgvOrders.CurrentCell = visibleCell;
                }

                // Force refresh the selection (helpful for some UI scenarios)
                dgvOrders.Refresh();
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
            if (dgvOrders.Columns["CustomerId"] != null)
                dgvOrders.Columns["CustomerId"].Visible = false;
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
            if (dgvOrders.Columns["SubTotal"] != null)
                dgvOrders.Columns["SubTotal"].Visible = false;
            if (dgvOrders.Columns["VatAmount"] != null)
                dgvOrders.Columns["VatAmount"].Visible = false;
            if (dgvOrders.Columns["PaymentMethod"] != null)
                dgvOrders.Columns["PaymentMethod"].Visible = false;
        }
        // ปรับปรุงเมธอด LoadOrders ให้รองรับพารามิเตอร์ statusFilter
        private void LoadOrders(
    string customerFilter = null,
    int? orderIdFilter = null,
    DateTime? createDateFilter = null,
    string statusFilter = null,
    string customOrderIdFilter = null)
        {
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvOrders.DataSource = _repo.GetOrders(customerFilter, orderIdFilter, createDateFilter, statusFilter, customOrderIdFilter);
            dgvItems.DataSource = null;
            SelectFirstRow();
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
            public int? CustomerId { get; set; } // Add CustomerId property
            public string CustomerName { get; set; }
            public string Phone { get; set; }
            public decimal Discount { get; set; }
            public decimal TodayDiscount { get; set; }
            public bool IsTodayDiscountPercent { get; set; }
            public DateTime OrderDate { get; set; }
            public DateTime PickupDate { get; set; }
            public decimal GrandTotalPrice { get; set; }
            public decimal SubTotal { get; set; }
            public decimal VatAmount { get; set; }
            public decimal DiscountedTotal { get; set; }
            public int? ReceiptId { get; set; }
            public string CustomReceiptId { get; set; }
            public string ReceivedStatus { get; set; }
            public string PaymentMethod { get; set; }
        }
        public class OrderRepository
        {
            public void CreateReceiptItem(int receiptId, int orderItemId, int quantity, decimal amount)
            {
                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
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
    string statusFilter = null,
    string customOrderIdFilter = null)
            {
                var list = new List<OrderHeaderDto>();
                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = cn;
                    var sb = new StringBuilder(@"
SELECT 
    OH.OrderID, 
    OH.CustomOrderId, 
    OH.CustomerId,
    C.FullName       AS CustomerName, 
    C.Phone,
    OH.Discount, 
    OH.OrderDate, 
    OH.PickupDate, 
    OH.GrandTotalPrice, 
    OH.DiscountedTotal, 
    R.ReceiptID, 
    R.IsPickedUp, 
    R.CustomReceiptId, 
    OH.OrderStatus
FROM OrderHeader OH
LEFT JOIN Customer C 
    ON OH.CustomerId = C.CustomerID
-- กรอง ReceiptStatus ที่ไม่ใช่ 'ยกเลิกการพิมพ์' ใน ON ของ LEFT JOIN
LEFT JOIN Receipt R 
    ON OH.OrderID = R.OrderID
   AND R.ReceiptStatus <> N'ยกเลิกการพิมพ์'
WHERE 1=1
  -- กรอง OrderStatus ที่ไม่ใช่ 'รายการถูกยกเลิก'
  AND (OH.OrderStatus IS NULL OR OH.OrderStatus <> N'รายการถูกยกเลิก')");

                    // filter by customer name
                    if (!string.IsNullOrWhiteSpace(customerFilter))
                    {
                        sb.Append(" AND C.FullName LIKE @cust");
                        cmd.Parameters.AddWithValue("@cust", "%" + customerFilter + "%");
                    }

                    // filter by OrderID
                    if (orderIdFilter.HasValue)
                    {
                        sb.Append(" AND OH.OrderID = @oid");
                        cmd.Parameters.AddWithValue("@oid", orderIdFilter.Value);
                    }

                    // filter by OrderDate (cast to date)
                    if (createDateFilter.HasValue)
                    {
                        sb.Append(" AND CAST(OH.OrderDate AS DATE) = @cdate");
                        cmd.Parameters.AddWithValue("@cdate", createDateFilter.Value.Date);
                    }

                    // filter by other status (if นอกเหนือจากรายการถูกยกเลิก)
                    if (!string.IsNullOrWhiteSpace(statusFilter))
                    {
                        sb.Append(" AND OH.OrderStatus = @status");
                        cmd.Parameters.AddWithValue("@status", statusFilter);
                    }

                    // filter by CustomOrderId
                    if (!string.IsNullOrWhiteSpace(customOrderIdFilter))
                    {
                        sb.Append(" AND OH.CustomOrderId LIKE @customId");
                        cmd.Parameters.AddWithValue("@customId", "%" + customOrderIdFilter + "%");
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
                                CustomerId = r["CustomerId"] != DBNull.Value
                                                     ? (int?)Convert.ToInt32(r["CustomerId"])
                                                     : null,
                                CustomerName = r["CustomerName"] as string ?? "",
                                Phone = r["Phone"] as string ?? "",
                                Discount = r["Discount"] == DBNull.Value
                                                     ? 0m
                                                     : Convert.ToDecimal(r["Discount"]),
                                OrderDate = r["OrderDate"] == DBNull.Value
                                                     ? DateTime.MinValue
                                                     : Convert.ToDateTime(r["OrderDate"]),
                                PickupDate = r["PickupDate"] == DBNull.Value
                                                     ? DateTime.MinValue
                                                     : Convert.ToDateTime(r["PickupDate"]),
                                GrandTotalPrice = r["GrandTotalPrice"] == DBNull.Value
                                                     ? 0m
                                                     : Convert.ToDecimal(r["GrandTotalPrice"]),
                                DiscountedTotal = r["DiscountedTotal"] == DBNull.Value
                                                     ? 0m
                                                     : Convert.ToDecimal(r["DiscountedTotal"]),
                                ReceiptId = r["ReceiptID"] == DBNull.Value
                                                     ? (int?)null
                                                     : Convert.ToInt32(r["ReceiptID"]),
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
                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
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
                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
                {
                    cn.Open();
                    using (var tx = cn.BeginTransaction())
                    {
                        try
                        {
                            // Update the OrderItem
                            using (var cmd = new SqlCommand(
                                @"UPDATE OrderItem 
                      SET IsCanceled = @c, 
                          CancelReason = @r 
                      WHERE OrderItemID = @id", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@c", isCancelled);
                                cmd.Parameters.AddWithValue("@r", string.IsNullOrWhiteSpace(cancelReason)
                                                                ? (object)DBNull.Value
                                                                : cancelReason);
                                cmd.Parameters.AddWithValue("@id", orderItemId);
                                cmd.ExecuteNonQuery();
                            }

                            // Find and update any associated ReceiptItems
                            using (var cmdUpdateReceiptItems = new SqlCommand(
                                @"UPDATE ReceiptItem 
                      SET IsCanceled = @isCancelled
                      WHERE OrderItemID = @orderItemId", cn, tx))
                            {
                                cmdUpdateReceiptItems.Parameters.AddWithValue("@isCancelled", isCancelled);
                                cmdUpdateReceiptItems.Parameters.AddWithValue("@orderItemId", orderItemId);
                                cmdUpdateReceiptItems.ExecuteNonQuery();
                            }

                            // Find receipts associated with this order item
                            var receiptIds = new List<int>();
                            using (var cmdFind = new SqlCommand(
                                @"SELECT DISTINCT ReceiptID
                      FROM ReceiptItem 
                      WHERE OrderItemID = @orderItemId", cn, tx))
                            {
                                cmdFind.Parameters.AddWithValue("@orderItemId", orderItemId);

                                using (var reader = cmdFind.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int receiptId = reader.GetInt32(0);
                                        receiptIds.Add(receiptId);
                                    }
                                }
                            }

                            // For each found receipt, update its status and add history record
                            foreach (var receiptId in receiptIds)
                            {
                                // Update Receipt status if all items are cancelled
                                using (var cmdCheckAllCancelled = new SqlCommand(
                                    @"SELECT COUNT(*) 
                          FROM ReceiptItem 
                          WHERE ReceiptID = @receiptId 
                          AND IsCanceled = 0", cn, tx))
                                {
                                    cmdCheckAllCancelled.Parameters.AddWithValue("@receiptId", receiptId);
                                    int nonCancelledCount = (int)cmdCheckAllCancelled.ExecuteScalar();

                                    // If all items are cancelled, update receipt status
                                    if (nonCancelledCount == 0)
                                    {
                                        using (var cmdUpdateReceipt = new SqlCommand(
                                            @"UPDATE Receipt
                                  SET ReceiptStatus = 'ยกเลิกการพิมพ์'
                                  WHERE ReceiptID = @receiptId", cn, tx))
                                        {
                                            cmdUpdateReceipt.Parameters.AddWithValue("@receiptId", receiptId);
                                            cmdUpdateReceipt.ExecuteNonQuery();
                                        }

                                        // Add record to history
                                        using (var cmdHistory = new SqlCommand(
                                            @"INSERT INTO ReceiptStatusHistory 
                                  (ReceiptID, PreviousStatus, NewStatus, ChangeBy, ChangeDate)
                                  SELECT @receiptId, 'พิมพ์เรียบร้อยแล้ว', 'ยกเลิกการพิมพ์', @changeBy, GETDATE()", cn, tx))
                                        {
                                            cmdHistory.Parameters.AddWithValue("@receiptId", receiptId);
                                            cmdHistory.Parameters.AddWithValue("@changeBy", Environment.UserName);
                                            cmdHistory.ExecuteNonQuery();
                                        }
                                    }
                                }
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
            }

            public int CreateReceipt(int orderId, string customReceiptId = null)
            {
                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
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
                using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
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
            lblTotal.Text = _totalAmount.ToString("N2") + " บาท";  // ราคารวมทั้งหมด

            // กำหนดค่าเริ่มต้นให้กับ txtDiscount
            txtDiscount.Text = originalDiscount.ToString();

            // กำหนดค่า checkbox ตามค่าเริ่มต้น (สมมติว่าเป็นเปอร์เซ็นต์)
            chkPercent.Checked = true;
            chkBaht.Checked = false;

            // คำนวณราคาหลังหักส่วนลด
            CalculateDiscountedPrice();
        }
        private void CalculateDiscountedPrice()
        {
            if (_currentItems == null || !_currentItems.Any(i => !i.IsCanceled))
            {
                lblTotal.Text = lblDiscount.Text = label10.Text = lblVat.Text = lblPaymentamount.Text = "0.00 บาท";
                return;
            }

            // 1. lblTotal = ราคารวม
            decimal totalAmount = _currentItems
                .Where(i => !i.IsCanceled)
                .Sum(i => i.TotalAmount);
            lblTotal.Text = totalAmount.ToString("N2") + " บาท";

            // 2. ส่วนลด - ตรวจสอบประเภทส่วนลด (บาทหรือ%)
            decimal discountValue = 0;
            decimal discountAmount = 0;

            if (decimal.TryParse(txtDiscount.Text, out discountValue) && discountValue > 0)
            {
                if (chkPercent.Checked)
                {
                    // กรณีเป็นเปอร์เซ็นต์
                    // label11 แสดงเป็น "ส่วนลด xx%"
                    label11.Text = $"ส่วนลด {discountValue}% :";

                    // คำนวณส่วนลดเป็นบาท: totalAmount * discountValue / 100
                    discountAmount = Math.Round(totalAmount * discountValue / 100m, 2);
                }
                else // chkBaht.Checked
                {
                    // กรณีเป็นบาท
                    // label11 แสดงเป็น "ส่วนลด :"
                    label11.Text = "ส่วนลด :";

                    // ใช้จำนวนเงินที่กรอกโดยตรง (ไม่เกินยอดรวม)
                    discountAmount = Math.Min(discountValue, totalAmount);
                }
            }
            else
            {
                // กรณีไม่มีส่วนลด
                label11.Text = "ส่วนลด :";
            }

            // แสดงส่วนลดเป็นบาท
            lblDiscount.Text = discountAmount.ToString("N2") + " บาท";

            // 3. label10 = ยอดรวมหลังหักส่วนลด (lblTotal - lblDiscount)
            decimal afterDiscount = Math.Round(totalAmount - discountAmount, 2);
            label10.Text = afterDiscount.ToString("N2") + " บาท";

            // 4. lblVat = label10 * 7 / 100 (VAT 7% จากยอดหลังหักส่วนลด)
            decimal vatAmount = Math.Round(afterDiscount / 1.07m * 0.07m, 2);
            lblVat.Text = vatAmount.ToString("N2") + " บาท";

            // 5. lblPaymentamount = label10 + VAT
            decimal paymentAmount = afterDiscount;
            lblPaymentamount.Text = paymentAmount.ToString("N2") + " บาท";
        }
        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            // อัพเดทการคำนวณทั้งหมดเมื่อมีการเปลี่ยนแปลงค่าส่วนลด
            if (decimal.TryParse(txtDiscount.Text, out decimal discountValue))
            {
                if (chkPercent.Checked)
                {
                    // ถ้าเป็นเปอร์เซ็นต์ แสดงเปอร์เซ็นต์ในข้อความ label11
                    label11.Text = $"ส่วนลด {discountValue}% :";
                }
                else
                {
                    // ถ้าเป็นบาท แสดงเป็น "ส่วนลด :"
                    label11.Text = "ส่วนลด :";
                }
            }
            else
            {
                label11.Text = "ส่วนลด :";
            }

            // เรียกฟังก์ชั่นคำนวณใหม่ทั้งหมด
            CalculateDiscountedPrice();
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

            // อัพเดทข้อความใน label11 ตามประเภทที่เลือก
            if (decimal.TryParse(txtDiscount.Text, out decimal discountValue))
            {
                if (chkPercent.Checked)
                {
                    // ถ้าเป็นเปอร์เซ็นต์ แสดงเปอร์เซ็นต์ในข้อความ
                    label11.Text = $"ส่วนลด {discountValue}% :";
                }
                else
                {
                    // ถ้าเป็นบาท แสดงเป็น "ส่วนลด :"
                    label11.Text = "ส่วนลด :";
                }
            }

            // คำนวณใหม่ทั้งหมด
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

            // ใช้ค่าส่วนลดจาก txtDiscount
            if (!decimal.TryParse(txtDiscount.Text, out decimal discountValue))
            {
                MessageBox.Show("กรุณากรอกส่วนลดให้ถูกต้อง", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!chkCash.Checked && !chkDebit.Checked && !chkQRCode.Checked)
            {
                MessageBox.Show("กรุณาเลือกวิธีชำระเงิน", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // คำนวณ SUB TOTAL (ราคาก่อน VAT และก่อนส่วนลด)
            decimal subTotal = items.Sum(i => i.TotalAmount);

            // VAT = 7% ของยอดรวม
            decimal vatAmount = Math.Round(subTotal * VAT_RATE, 2);

            // ตัวแปรสำหรับเก็บค่าที่คำนวณ
            decimal discountAmount = 0;        // จำนวนเงินส่วนลด
            decimal netTotal;                  // ยอดรวมสุทธิ (subTotal - discountAmount)

            // ตรวจสอบส่วนลด
            if (chkPercent.Checked)
            {
                if (discountValue < 0 || discountValue > 100)
                {
                    MessageBox.Show("ส่วนลดเปอร์เซ็นต์ต้องอยู่ระหว่าง 0-100", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                discountAmount = Math.Round(subTotal * discountValue / 100, 2);
                header.Discount = discountValue;
                header.TodayDiscount = discountValue; // Add this line
                header.IsTodayDiscountPercent = true;
            }
            else if (chkBaht.Checked)
            {
                if (discountValue < 0)
                {
                    MessageBox.Show("ส่วนลดต้องไม่ติดลบ", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (discountValue > subTotal)
                {
                    MessageBox.Show("ส่วนลดต้องไม่เกินราคารวม", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                discountAmount = discountValue;
                decimal percentDiscount = 0;
                if (subTotal > 0)
                {
                    percentDiscount = (discountValue / subTotal) * 100;
                }
                header.Discount = percentDiscount;
                header.TodayDiscount = discountValue; // Add this line
                header.IsTodayDiscountPercent = false;
            }
            else
            {
                header.Discount = 0;
                header.TodayDiscount = 0; // Add this line
                header.IsTodayDiscountPercent = true;
            }

            // ยอดรวมสุทธิ = ยอดรวม - ส่วนลด
            netTotal = Math.Round(subTotal - discountAmount, 2);

            // อัพเดทข้อมูลใน header
            header.GrandTotalPrice = subTotal;        // ยอดรวม (รวม VAT)
            header.SubTotal = subTotal;               // ยอดรวม (ก่อนหักส่วนลด)
            header.VatAmount = vatAmount;             // VAT
            header.DiscountedTotal = netTotal;

            // ดึงเลขที่ใบเสร็จใหม่จาก AppSettingsManager
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

                // 2) เปิดฟอร์ม ReceiptPrintForm พร้อม receiptId และข้อมูลที่ต้องการ
                using (var rptForm = new ReceiptPrintForm(receiptId, header, items))
                {
                    rptForm.ShowDialog(this);
                    if (!rptForm.IsPrinted)
                    {
                        using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
                        {
                            cn.Open();
                            using (var tx = cn.BeginTransaction())
                            {
                                try
                                {
                                    // 1. อัพเดทสถานะของ Receipt เป็น "ยกเลิกการพิมพ์"
                                    using (var cmd1 = new SqlCommand(
                                        "UPDATE Receipt SET ReceiptStatus = @status WHERE ReceiptID = @rid", cn, tx))
                                    {
                                        cmd1.Parameters.AddWithValue("@status", "ยกเลิกการพิมพ์");
                                        cmd1.Parameters.AddWithValue("@rid", receiptId);
                                        cmd1.ExecuteNonQuery();
                                    }

                                    // 2. อัพเดท IsCanceled ของทุก ReceiptItem เป็น 1 (ยกเลิก)
                                    using (var cmd2 = new SqlCommand(
                                        "UPDATE ReceiptItem SET IsCanceled = 1 WHERE ReceiptID = @rid", cn, tx))
                                    {
                                        cmd2.Parameters.AddWithValue("@rid", receiptId);
                                        cmd2.ExecuteNonQuery();
                                    }

                                    // 3. บันทึกประวัติการเปลี่ยนสถานะ
                                    using (var cmd3 = new SqlCommand(
                                        @"INSERT INTO ReceiptStatusHistory (ReceiptID, PreviousStatus, NewStatus, ChangeBy)
                                VALUES (@rid, @prevStatus, @newStatus, @changeBy)", cn, tx))
                                    {
                                        cmd3.Parameters.AddWithValue("@rid", receiptId);
                                        cmd3.Parameters.AddWithValue("@prevStatus", "ออกใบเสร็จแล้ว");
                                        cmd3.Parameters.AddWithValue("@newStatus", "ยกเลิกการพิมพ์");
                                        cmd3.Parameters.AddWithValue("@changeBy", Environment.UserName);
                                        cmd3.ExecuteNonQuery();
                                    }

                                    // 4. เพิ่ม - อัพเดท OrderHeader กำหนดให้ IsReceiptPrinted เป็น 0
                                    using (var cmd4 = new SqlCommand(
                                        "UPDATE OrderHeader SET IsReceiptPrinted = 0 WHERE OrderID = @oid", cn, tx))
                                    {
                                        cmd4.Parameters.AddWithValue("@oid", header.OrderID);
                                        cmd4.ExecuteNonQuery();
                                    }

                                    tx.Commit();
                                }
                                catch (Exception ex)
                                {
                                    tx.Rollback();
                                    MessageBox.Show($"เกิดข้อผิดพลาดในการยกเลิกใบเสร็จ: {ex.Message}", "ข้อผิดพลาด",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
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

                        // แก้ไขข้อความที่แสดงเมื่อยกเลิกการพิมพ์
                        MessageBox.Show("ยกเลิกการพิมพ์ใบเสร็จเรียบร้อยแล้ว", "แจ้งเตือน",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                // รีเฟรชข้อมูลในตารางเพื่อแสดงสถานะใหม่
                string cust = txtCustomerFilter.Text.Trim();
                int? oid = null;
                string customOrderId = null;
                string orderText = txtOrderId.Text.Trim();

                if (int.TryParse(orderText, out int tmp))
                {
                    oid = tmp;
                }
                else if (!string.IsNullOrEmpty(orderText))
                {
                    customOrderId = orderText;
                }

                DateTime? createDt = null;
                if (dtpCreateDate.Checked)
                    createDt = dtpCreateDate.Value.Date;

                LoadOrders(cust, oid, createDt, "ดำเนินการสำเร็จ", customOrderId);
            }
            catch (Exception ex)
            {
                // เพิ่มการอัพเดท IsReceiptPrinted เป็น 0 เมื่อเกิดข้อผิดพลาด
                try
                {
                    using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
                    {
                        cn.Open();
                        using (var cmd = new SqlCommand(
                            "UPDATE OrderHeader SET IsReceiptPrinted = 0 WHERE OrderID = @oid", cn))
                        {
                            cmd.Parameters.AddWithValue("@oid", header.OrderID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception updateEx)
                {
                    MessageBox.Show(
        $"ไม่สามารถอัพเดทสถานะ IsReceiptPrinted เป็น 0 ได้\n" +
        $"สาเหตุ: {updateEx.Message}\n\n"
        ,
        "ข้อผิดพลาดในการอัพเดทสถานะ",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
                }

                MessageBox.Show("เกิดข้อผิดพลาดขณะบันทึก:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string orderText = txtOrderId.Text.Trim();
            string cust = txtCustomerFilter.Text.Trim();

            int? oid = null;
            string customOrderId = null;
                    // If it's not a valid integer, use it as CustomOrderId
            customOrderId = orderText;
            DateTime? createDt = null;
            if (dtpCreateDate.Checked)
                createDt = dtpCreateDate.Value.Date;

            // เพิ่มพารามิเตอร์ statusFilter เป็น "ดำเนินการสำเร็จ" เพื่อค้นหาเฉพาะรายการที่มีสถานะนี้
            LoadOrders(cust, oid, createDt, "ดำเนินการสำเร็จ", customOrderId);
            SelectFirstRow();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // แสดงทั้งหมดแต่เฉพาะรายการที่มีสถานะ "ดำเนินการสำเร็จ"
            LoadOrders(null, null, null, "ดำเนินการสำเร็จ", null);
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        public int CreateReceiptWithCustomId(int orderId, string customReceiptId)
        {
            // 1) สร้างข้อมูลรายการที่ไม่ถูกยกเลิก
            var items = _currentItems.Where(i => !i.IsCanceled).ToList();
            if (!items.Any()) throw new InvalidOperationException("ไม่มีรายการสำหรับออกใบเสร็จ");

            // 2) SUB TOTAL = ราคารวมทั้งหมด
            decimal subTotal = items.Sum(i => i.TotalAmount);

            // 3) VAT = 7% ของยอดรวม
            decimal vatAmount = Math.Round(subTotal * VAT_RATE, 2);

            // 4) คำนวณส่วนลด
            decimal discountValue = 0;          // ค่าส่วนลดที่ผู้ใช้ป้อน (เปอร์เซ็นต์หรือบาท)
            decimal discountAmount = 0;         // จำนวนเงินส่วนลด (เป็นบาทเสมอ)
            bool isDiscountPercent = chkPercent.Checked;

            // ดึงค่า OrderHeaderDto สำหรับอัพเดทข้อมูล
            var header = dgvOrders.CurrentRow?.DataBoundItem as OrderHeaderDto;

            // อ่านค่าส่วนลดจาก textbox
            if (decimal.TryParse(txtDiscount.Text, out discountValue) && discountValue > 0)
            {
                if (isDiscountPercent)
                {
                    // คิดส่วนลดแบบ % (เปอร์เซ็นต์)
                    discountAmount = Math.Round(subTotal * discountValue / 100m, 2);

                    // อัพเดทข้อมูลใน header
                    if (header != null)
                    {
                        header.TodayDiscount = discountValue;
                    }
                }
                else
                {
                    // คิดส่วนลดแบบบาท (จำนวนเงินโดยตรง)
                    discountAmount = Math.Min(discountValue, subTotal);

                    // อัพเดทข้อมูลใน header
                    if (header != null)
                    {
                        header.TodayDiscount = discountValue;
                    }
                }
            }

            // 5) ยอดรวมสุทธิ = ยอดรวม - ส่วนลด
            decimal netTotal = Math.Round(subTotal - discountAmount, 2);

            // 6) กำหนดวิธีการชำระเงิน
            string paymentMethod = chkCash.Checked ? "เงินสด" :
                                   chkDebit.Checked ? "บัตรเครดิต" :
                                   "QR Code";

            // 7) บันทึกลงฐานข้อมูล
            using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                using (var cmd = new SqlCommand(@"
        INSERT INTO Receipt (
            OrderID,
            TotalBeforeDiscount,   -- ยอดรวมก่อนหักส่วนลด
            TotalAfterDiscount,
            VAT,                   -- VAT
            CustomReceiptId,
            ReceiptStatus,
            PaymentMethod,
            Discount              -- เก็บค่าส่วนลด
        ) OUTPUT INSERTED.ReceiptID
        VALUES (
            @oid, @subTotal, @netTotal, @vat, @customId, @status, @payMethod, @discount
        )", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    cmd.Parameters.AddWithValue("@subTotal", subTotal);
                    cmd.Parameters.AddWithValue("@netTotal", netTotal);
                    cmd.Parameters.AddWithValue("@vat", vatAmount);
                    cmd.Parameters.AddWithValue("@customId", customReceiptId);
                    cmd.Parameters.AddWithValue("@status", "ออกใบเสร็จแล้ว");
                    cmd.Parameters.AddWithValue("@payMethod", paymentMethod);
                    cmd.Parameters.AddWithValue("@discount", discountAmount);

                    int receiptId = (int)cmd.ExecuteScalar();

                    // อัพเดท OrderHeader
                    using (var upd = new SqlCommand(@"
        UPDATE OrderHeader
        SET IsReceiptPrinted = 1
        WHERE OrderID = @oid", cn, tx))
                    {
                        upd.Parameters.AddWithValue("@oid", orderId);
                        upd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return receiptId;
                }
            }
        }
        public void UpdateOrderWithCustomId(int orderId, string customOrderId)
        {
            using (var cn = Laundry_Management.Laundry.DBconfig.GetConnection())
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
            chkQRCode.CheckedChanged += chkPayment_CheckedChanged;

            // ตั้งค่า event handler สำหรับ dtpCreateDate (เพิ่มบรรทัดนี้)
            dtpCreateDate.ValueChanged += dtpCreateDate_ValueChanged;

            // ตั้งค่าเริ่มต้นให้ chkPercent เป็น checked
            chkPercent.Checked = true;
            chkBaht.Checked = false;

            // ตั้งค่าเริ่มต้นให้ chkCash เป็น checked
            chkCash.Checked = false;
            chkDebit.Checked = false;
            chkQRCode.Checked = false;
            SelectFirstRow();
        }
        private void chkPayment_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            if (chk == null) return;

            if (chk.Checked)
            {
                // If this checkbox is now checked, uncheck the others
                if (chk == chkCash)
                {
                    chkDebit.Checked = false;
                    chkQRCode.Checked = false;
                }
                else if (chk == chkDebit)
                {
                    chkCash.Checked = false;
                    chkQRCode.Checked = false;
                }
                else if (chk == chkQRCode)
                {
                    chkCash.Checked = false;
                    chkDebit.Checked = false;
                }
            }
            else
            {
                // If we're trying to uncheck this checkbox, make sure at least one is checked
                if (!chkCash.Checked && !chkDebit.Checked && !chkQRCode.Checked)
                {
                    chk.Checked = true;
                    return;
                }
            }
        }
        // Add this method to handle the DateTimePicker ValueChanged event
        private void dtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            // Only trigger search if the DateTimePicker is checked (date is selected)
            if (dtpCreateDate.Checked)
            {
                // Get customer filter text
                string cust = txtCustomerFilter.Text.Trim();

                // Get order ID filter
                int? oid = null;
                string customOrderId = null;
                string orderText = txtOrderId.Text.Trim();

                if (int.TryParse(orderText, out int tmp))
                {
                    oid = tmp;
                }
                else if (!string.IsNullOrEmpty(orderText))
                {
                    customOrderId = orderText;
                }

                // Use the currently selected date
                DateTime? createDt = dtpCreateDate.Value.Date;

                // Load orders with the specified filters and status
                LoadOrders(cust, oid, createDt, "ดำเนินการสำเร็จ", customOrderId);
                SelectFirstRow();
            }
        }
    }
}
