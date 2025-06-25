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
    public partial class Modify_Service_Item : Form
    {
        // Add these fields at the class level to track items
        private BindingList<OrderItemDto> _modifiedItems;
        private Dictionary<int, OrderItemDto> _originalItems;
        private readonly OrderRepository _repo = new OrderRepository();
        private List<OrderItemDto> _currentItems;

        public Modify_Service_Item()
        {
            InitializeComponent();

            _modifiedItems = new BindingList<OrderItemDto>();
            _originalItems = new Dictionary<int, OrderItemDto>();

            // กำหนดค่าเริ่มต้น
            DateTime today = DateTime.Today;
            dtpCreateDate.Value = today;
            dtpCreateDate.Checked = true;

            // ตั้งค่า DataGridView ให้เลือกทั้งแถวเมื่อคลิกที่ cell ใดก็ได้
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // เพิ่ม event handler สำหรับการคลิกที่ cell
            dgvOrders.CellClick += DgvOrders_CellClick;
            dgvItems.CellClick += DgvItems_CellClick;

            // เพิ่ม event handler สำหรับการทำงานกับ DataGridView
            dgvOrders.DataBindingComplete += DgvOrders_DataBindingComplete;
            dgvItems.DataBindingComplete += DgvItems_DataBindingComplete;

            // เพิ่ม event handler สำหรับการเลือกแถวใน dgvOrders
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;

            // เพิ่ม event handler สำหรับ DateTimePicker
            dtpCreateDate.ValueChanged += dtpCreateDate_ValueChanged;

            // โหลดข้อมูลเริ่มต้น
            LoadOrders(null, null, today, null);
            SelectFirstRow();
        }
        private void UpdateModifiedItemsGrid()
        {
            dgvItems.DataSource = _modifiedItems;
            dgvItems.Refresh();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string customerName = txtCustomerFilter.Text.Trim();

            int? orderId = null;
            if (int.TryParse(txtOrderId.Text.Trim(), out int parsedOrderId))
                orderId = parsedOrderId;

            DateTime? createDate = null;
            if (dtpCreateDate.Checked)
                createDate = dtpCreateDate.Value.Date;

            // โหลดข้อมูลตามเงื่อนไขการค้นหา
            LoadOrders(customerName, orderId, createDate, null);
            SelectFirstRow();
        }
        // Add this helper method to check if an order has a receipt
        private bool HasReceipt(int orderId)
        {
            using (var cn = DBconfig.GetConnection())
            {
                cn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Receipt WHERE OrderID = @id", cn))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // Helper method to check if order can be edited
        private bool CanEditOrder()
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกรายการคำสั่งซื้อก่อน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int orderId = (int)dgvOrders.CurrentRow.Cells["OrderID"].Value;

            // ตรวจสอบว่ามีการออกใบเสร็จแล้วหรือไม่
            if (HasReceipt(orderId))
            {
                MessageBox.Show("ไม่สามารถแก้ไขรายการได้เนื่องจากมีการออกใบเสร็จแล้ว",
                    "ไม่อนุญาตให้แก้ไข", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void LoadOrders(
    string customerFilter = null,
    int? orderIdFilter = null,
    DateTime? createDateFilter = null,
    string statusFilter = null)
        {
            // กำหนดขนาดอัตโนมัติให้กับ DataGridView
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // โหลดข้อมูล Order ตามเงื่อนไขการค้นหา
            var allOrders = _repo.GetOrders(customerFilter, orderIdFilter, createDateFilter, statusFilter);

            // กรองรายการที่ยังไม่มีใบเสร็จ (ReceiptId is null)
            var filteredOrders = allOrders.Where(o => !o.ReceiptId.HasValue).ToList();

            dgvOrders.DataSource = filteredOrders;
            dgvItems.DataSource = null;
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

        private void DgvItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // ป้องกันการคลิกที่ส่วนหัวคอลัมน์
            {
                // เลือกทั้งแถวเมื่อคลิกที่เซลล์ใดก็ได้
                dgvItems.CurrentCell = dgvItems.Rows[e.RowIndex].Cells[dgvItems.CurrentCell.ColumnIndex];
            }
        }

        private void DgvOrders_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // กำหนดชื่อหัวคอลัมน์ภาษาไทย
            if (dgvOrders.Columns["CustomOrderId"] != null)
                dgvOrders.Columns["CustomOrderId"].HeaderText = "หมายเลขรายการ";
            if (dgvOrders.Columns["CustomerName"] != null)
                dgvOrders.Columns["CustomerName"].HeaderText = "ชื่อ-นามสกุล ลูกค้า";
            if (dgvOrders.Columns["Phone"] != null)
                dgvOrders.Columns["Phone"].HeaderText = "เบอร์โทร";
            if (dgvOrders.Columns["Discount"] != null)
                dgvOrders.Columns["Discount"].HeaderText = "ส่วนลด";
            if (dgvOrders.Columns["OrderDate"] != null)
                dgvOrders.Columns["OrderDate"].HeaderText = "วันที่สั่ง";
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

            // ซ่อนคอลัมน์ที่ไม่จำเป็นต้องแสดง
            if (dgvOrders.Columns["OrderID"] != null)
                dgvOrders.Columns["OrderID"].Visible = false;
            if (dgvOrders.Columns["TodayDiscount"] != null)
                dgvOrders.Columns["TodayDiscount"].Visible = false;
            if (dgvOrders.Columns["IsTodayDiscountPercent"] != null)
                dgvOrders.Columns["IsTodayDiscountPercent"].Visible = false;
            if (dgvOrders.Columns["CustomReceiptId"] != null)
                dgvOrders.Columns["CustomReceiptId"].Visible = false;
            if (dgvOrders.Columns["SubTotal"] != null)
                dgvOrders.Columns["SubTotal"].Visible = false;
            if (dgvOrders.Columns["VatAmount"] != null)
                dgvOrders.Columns["VatAmount"].Visible = false;
            if (dgvOrders.Columns["PaymentMethod"] != null)
                dgvOrders.Columns["PaymentMethod"].Visible = false;
        }

        private void DgvItems_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // กำหนดชื่อหัวคอลัมน์ภาษาไทย
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

        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;

            try
            {
                int orderId = (int)dgvOrders.CurrentRow.Cells["OrderID"].Value;
                var items = _repo.GetOrderItems(orderId);

                // Create a fresh copy of the items for modification
                _modifiedItems = new BindingList<OrderItemDto>();
                _originalItems = new Dictionary<int, OrderItemDto>();

                foreach (var item in items)
                {
                    // Keep original items for comparison
                    _originalItems[item.OrderItemID] = new OrderItemDto
                    {
                        OrderItemID = item.OrderItemID,
                        ItemNumber = item.ItemNumber,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        TotalAmount = item.TotalAmount,
                        IsCanceled = item.IsCanceled,
                        CancelReason = item.CancelReason
                    };

                    // Add to modified list if not canceled
                    if (!item.IsCanceled)
                    {
                        _modifiedItems.Add(new OrderItemDto
                        {
                            OrderItemID = item.OrderItemID,
                            ItemNumber = item.ItemNumber,
                            ItemName = item.ItemName,
                            Quantity = item.Quantity,
                            TotalAmount = item.TotalAmount,
                            IsCanceled = false,
                            CancelReason = ""
                        });
                    }
                }

                dgvItems.DataSource = _modifiedItems;

                // Store the current items for reference
                _currentItems = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดในการโหลดรายการ: " + ex.Message,
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dtpCreateDate_ValueChanged(object sender, EventArgs e)
        {
            if (dtpCreateDate.Checked)
            {
                string customerFilter = txtCustomerFilter.Text.Trim();

                int? orderId = null;
                if (int.TryParse(txtOrderId.Text.Trim(), out int parsedOrderId))
                    orderId = parsedOrderId;

                DateTime? createDate = dtpCreateDate.Value.Date;

                LoadOrders(customerFilter, orderId, createDate, null);
                SelectFirstRow();
            }
        }

        private void SelectFirstRow()
        {
            // เลือกแถวแรกใน dgvOrders ถ้ามีข้อมูล
            if (dgvOrders.Rows.Count > 0)
            {
                dgvOrders.ClearSelection();
                dgvOrders.Rows[0].Selected = true;

                // หาเซลล์แรกที่มองเห็นได้ในแถวแรก
                DataGridViewCell visibleCell = null;
                foreach (DataGridViewCell cell in dgvOrders.Rows[0].Cells)
                {
                    if (cell.Visible)
                    {
                        visibleCell = cell;
                        break;
                    }
                }

                // กำหนดเซลล์แรกที่มองเห็นได้เป็น CurrentCell
                if (visibleCell != null && (dgvOrders.CurrentCell == null || dgvOrders.CurrentCell.RowIndex != 0))
                {
                    dgvOrders.CurrentCell = visibleCell;
                }

                // บังคับให้แสดงการเลือก
                dgvOrders.Refresh();
            }
        }

        // คลาสสำหรับข้อมูลรายการ Order
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

        // คลาสสำหรับข้อมูล OrderHeader
        public class OrderHeaderDto
        {
            public int OrderID { get; set; }
            public string CustomOrderId { get; set; }
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

        // คลาสสำหรับการเข้าถึงฐานข้อมูล
        public class OrderRepository
        {
            public List<OrderHeaderDto> GetOrders(
                string customerFilter = null,
                int? orderIdFilter = null,
                DateTime? createDateFilter = null,
                string statusFilter = null)
            {
                var list = new List<OrderHeaderDto>();
                using (var cn = DBconfig.GetConnection())
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
                using (var cn = DBconfig.GetConnection())
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
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSaveSelected_Click(object sender, EventArgs e)
        {
            if (!CanEditOrder()) return;

            // Open Select_Service form to choose new items
            using (var selectForm = new Select_Service())
            {
                if (selectForm.ShowDialog() == DialogResult.OK)
                {
                    // Get selected service
                    string itemNumber = selectForm.SelectedItemNumber;
                    string itemName = selectForm.SelectedItemName;
                    decimal price = selectForm.SelectedPrice;

                    // Check if this item already exists in the modified list
                    bool itemExists = false;
                    foreach (var item in _modifiedItems)
                    {
                        if (item.ItemNumber == itemNumber)
                        {
                            MessageBox.Show("รายการนี้ถูกเลือกไว้แล้ว", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            itemExists = true;
                            break;
                        }
                    }

                    if (!itemExists)
                    {
                        // Ask for quantity using Item form
                        var itemForm = new Item(price, itemNumber, itemName, 1);

                        if (itemForm.ShowDialog() == DialogResult.OK)
                        {
                            // Add to modified items list
                            _modifiedItems.Add(new OrderItemDto
                            {
                                OrderItemID = 0, // New item (will be assigned by DB)
                                ItemNumber = itemNumber,
                                ItemName = itemName,
                                Quantity = itemForm.Quantity,
                                TotalAmount = price * itemForm.Quantity,
                                IsCanceled = false,
                                CancelReason = ""
                            });

                            UpdateModifiedItemsGrid();
                        }
                    }
                }
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            if (!CanEditOrder()) return;

            if (dgvItems.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกรายการสินค้าที่ต้องการลบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedIndex = dgvItems.CurrentRow.Index;
            if (selectedIndex >= 0 && selectedIndex < _modifiedItems.Count)
            {
                var itemToRemove = _modifiedItems[selectedIndex];

                // Confirm deletion
                var confirmResult = MessageBox.Show("คุณต้องการลบรายการนี้หรือไม่?",
                    "ยืนยันการลบ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    _modifiedItems.RemoveAt(selectedIndex);
                    UpdateModifiedItemsGrid();
                }
            }
        }

        private void btnFix_Click(object sender, EventArgs e)
        {
            if (!CanEditOrder()) return;

            if (dgvItems.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกรายการสินค้าที่ต้องการแก้ไข", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedIndex = dgvItems.CurrentRow.Index;
            if (selectedIndex >= 0 && selectedIndex < _modifiedItems.Count)
            {
                var item = _modifiedItems[selectedIndex];

                // Calculate unit price
                decimal unitPrice = item.Quantity > 0 ? item.TotalAmount / item.Quantity : 0;

                // Open Item form for editing
                var itemForm = new Item(unitPrice, item.ItemNumber, item.ItemName, item.Quantity);
                itemForm.IsEditMode = true;

                if (itemForm.ShowDialog() == DialogResult.OK)
                {
                    // Update the item
                    item.Quantity = itemForm.Quantity;
                    item.TotalAmount = unitPrice * itemForm.Quantity;

                    // Update grid
                    UpdateModifiedItemsGrid();
                }
            }
        }

        private void btnEnd_Click(object sender, EventArgs e)
        {
            if (!CanEditOrder()) return;

            // Check if there are items to save
            if (_modifiedItems.Count == 0)
            {
                MessageBox.Show("ไม่มีรายการสินค้าที่จะบันทึก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // แสดงกล่องข้อความยืนยันการบันทึก
            DialogResult confirmResult = MessageBox.Show(
                "คุณแน่ใจที่จะบันทึกการเปลี่ยนแปลงนี้หรือไม่?",
                "ยืนยันการบันทึก",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
            {
                return; // ยกเลิกการบันทึก
            }

            try
            {
                int orderId = (int)dgvOrders.CurrentRow.Cells["OrderID"].Value;
                string customOrderId = dgvOrders.CurrentRow.Cells["CustomOrderId"].Value.ToString();
                string customerName = dgvOrders.CurrentRow.Cells["CustomerName"].Value.ToString();
                string phone = dgvOrders.CurrentRow.Cells["Phone"].Value.ToString();
                decimal discount = Convert.ToDecimal(dgvOrders.CurrentRow.Cells["Discount"].Value);

                // Calculate grand total
                decimal grandTotal = _modifiedItems.Sum(i => i.TotalAmount);
                decimal discountedTotal = discount > 0
                    ? grandTotal - (grandTotal * (discount / 100m))
                    : grandTotal;

                using (var cn = DBconfig.GetConnection())
                {
                    cn.Open();

                    // Double-check that order still doesn't have a receipt before proceeding
                    if (HasReceipt(orderId))
                    {
                        MessageBox.Show("ไม่สามารถแก้ไขรายการได้เนื่องจากมีการออกใบเสร็จแล้ว",
                            "ไม่อนุญาตให้แก้ไข", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    using (var transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Identify deleted items (in original but not in modified)
                            var originalItemIds = new HashSet<int>(_originalItems.Keys);
                            var modifiedItemIds = new HashSet<int>(_modifiedItems.Where(i => i.OrderItemID > 0)
                                                                .Select(i => i.OrderItemID));

                            // Items in original but not in modified = deleted
                            foreach (var id in originalItemIds.Except(modifiedItemIds))
                            {
                                // Check if this item is linked to a receipt
                                using (var cmd = new SqlCommand(
                                    "SELECT COUNT(*) FROM ReceiptItem WHERE OrderItemID = @id", cn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@id", id);
                                    int count = (int)cmd.ExecuteScalar();

                                    if (count > 0)
                                    {
                                        // If linked to receipt, mark as canceled instead of deleting
                                        using (var updateCmd = new SqlCommand(
                                            "UPDATE OrderItem SET IsCanceled = 1, CancelReason = @reason WHERE OrderItemID = @id",
                                            cn, transaction))
                                        {
                                            updateCmd.Parameters.AddWithValue("@id", id);
                                            updateCmd.Parameters.AddWithValue("@reason", "ยกเลิกจากการแก้ไขรายการ");
                                            updateCmd.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                        // If not linked, safe to delete
                                        using (var deleteCmd = new SqlCommand(
                                            "DELETE FROM OrderItem WHERE OrderItemID = @id", cn, transaction))
                                        {
                                            deleteCmd.Parameters.AddWithValue("@id", id);
                                            deleteCmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            // 2. Update existing items
                            foreach (var item in _modifiedItems.Where(i => i.OrderItemID > 0))
                            {
                                using (var cmd = new SqlCommand(
                                    @"UPDATE OrderItem 
                    SET Quantity = @quantity, 
                        TotalAmount = @amount 
                    WHERE OrderItemID = @id", cn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@amount", item.TotalAmount);
                                    cmd.Parameters.AddWithValue("@id", item.OrderItemID);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // 3. Add new items
                            foreach (var item in _modifiedItems.Where(i => i.OrderItemID == 0))
                            {
                                using (var cmd = new SqlCommand(
                                    @"INSERT INTO OrderItem (OrderID, ItemNumber, ItemName, Quantity, TotalAmount)
                    VALUES (@orderId, @itemNumber, @itemName, @quantity, @amount)",
                                    cn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@orderId", orderId);
                                    cmd.Parameters.AddWithValue("@itemNumber", item.ItemNumber);
                                    cmd.Parameters.AddWithValue("@itemName", item.ItemName);
                                    cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@amount", item.TotalAmount);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // 4. Update order totals
                            using (var cmd = new SqlCommand(
                                @"UPDATE OrderHeader 
                SET GrandTotalPrice = @grand, 
                    DiscountedTotal = @discounted 
                WHERE OrderID = @id", cn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@grand", grandTotal);
                                cmd.Parameters.AddWithValue("@discounted", discountedTotal);
                                cmd.Parameters.AddWithValue("@id", orderId);
                                cmd.ExecuteNonQuery();
                            }

                            // Commit transaction
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            // Rollback on error
                            transaction.Rollback();
                            throw new Exception("เกิดข้อผิดพลาดในการปรับปรุงข้อมูล: " + ex.Message);
                        }
                    }
                }

                // Prepare for printing
                var serviceItems = _modifiedItems
                    .Select(i => new Print_Service.ServiceItem
                    {
                        Name = i.ItemName,
                        Quantity = i.Quantity,
                        Price = i.Quantity > 0 ? i.TotalAmount / i.Quantity : 0
                    })
                    .ToList();

                // Show print form
                using (var printForm = new Print_Service(
                    customerName,
                    phone,
                    discount / 100,
                    customOrderId,
                    serviceItems))
                {
                    printForm.ShowDialog();
                }

                // Reload order items to refresh the display
                var updatedItems = _repo.GetOrderItems(orderId);
                dgvItems.DataSource = new BindingList<OrderItemDto>(updatedItems);
                _currentItems = updatedItems;

                // Refresh orders list
                string customerFilter = txtCustomerFilter.Text.Trim();
                int? orderIdFilter = null;
                if (int.TryParse(txtOrderId.Text.Trim(), out int parsedOrderId))
                    orderIdFilter = parsedOrderId;
                DateTime? createDate = null;
                if (dtpCreateDate.Checked)
                    createDate = dtpCreateDate.Value.Date;

                LoadOrders(customerFilter, orderIdFilter, createDate, null);

                MessageBox.Show("อัพเดทรายการสำเร็จ", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateOrderTotals(int orderId)
        {
            try
            {
                using (var cn = DBconfig.GetConnection())
                {
                    cn.Open();

                    // 1. ดึงยอดรวมและส่วนลดจากข้อมูลปัจจุบัน
                    decimal discount = 0;
                    using (var cmd = new SqlCommand(
                        "SELECT Discount FROM OrderHeader WHERE OrderID = @id", cn))
                    {
                        cmd.Parameters.AddWithValue("@id", orderId);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            discount = Convert.ToDecimal(result);
                        }
                    }

                    // 2. คำนวณยอดรวมใหม่จาก OrderItem ที่ไม่ถูกยกเลิก
                    decimal grandTotal = 0;
                    using (var cmd = new SqlCommand(
                        "SELECT SUM(TotalAmount) FROM OrderItem WHERE OrderID = @id AND (IsCanceled = 0 OR IsCanceled IS NULL)", cn))
                    {
                        cmd.Parameters.AddWithValue("@id", orderId);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            grandTotal = Convert.ToDecimal(result);
                        }
                    }

                    // 3. คำนวณยอดรวมหลังหักส่วนลด
                    decimal discountedTotal = discount > 0
                        ? grandTotal - (grandTotal * (discount / 100m))
                        : grandTotal;

                    // 4. อัพเดทตาราง OrderHeader
                    using (var cmd = new SqlCommand(
                        @"UPDATE OrderHeader 
                SET GrandTotalPrice = @grand, 
                    DiscountedTotal = @discounted 
                WHERE OrderID = @id", cn))
                    {
                        cmd.Parameters.AddWithValue("@grand", grandTotal);
                        cmd.Parameters.AddWithValue("@discounted", discountedTotal);
                        cmd.Parameters.AddWithValue("@id", orderId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการอัพเดทยอดรวม: {ex.Message}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
