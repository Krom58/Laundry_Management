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
using static Laundry_Management.Laundry.Print_Service;

namespace Laundry_Management.Laundry
{
    public partial class Modify_Service_Item : Form
    {
        // Add these fields at the class level to track items
        private BindingList<OrderItemDto> _modifiedItems;
        private Dictionary<int, OrderItemDto> _originalItems;
        private readonly OrderRepository _repo = new OrderRepository();
        private List<OrderItemDto> _currentItems;
        private bool _changesSaved = false; // Track if changes are saved

        // Add at class level with other fields
        private int? _pendingCustomerId = null;
        private string _pendingCustomerName = null;
        private string _pendingPhone = null;
        private decimal? _pendingDiscount = null;
        private bool _hasCustomerChanges = false;

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

            // Add event handler for _modifiedItems changes
            _modifiedItems.ListChanged += ModifiedItems_ListChanged;
            // Add this line to the constructor
            this.FormClosing += Modify_Service_Item_FormClosing;
            // โหลดข้อมูลเริ่มต้น
            LoadOrders(null, null, today, null);
            SelectFirstRow();
        }
        // Add this property to the Select_Customer class
        public int? SelectedCustomerId { get; private set; }
        private void UpdateModifiedItemsGrid()
        {
            dgvItems.DataSource = _modifiedItems;
            dgvItems.Refresh();

            UpdateTotalFromDataGridView();
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

            // Reset the changes saved flag when loading new orders
            _changesSaved = true;

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
            if (dgvOrders.Columns["CustomerId"] != null)
                dgvOrders.Columns["CustomerId"].Visible = false;
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
                // Set the _changesSaved flag to true when switching to a different order
                // because we're loading fresh data from the database
                _changesSaved = true;
                
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

                // Re-wire the ListChanged event after creating a new BindingList
                _modifiedItems.ListChanged += ModifiedItems_ListChanged;

                dgvItems.DataSource = _modifiedItems;

                // Store the current items for reference
                _currentItems = items;

                // Calculate and update the total price display based on selected order
                UpdateTotalFromDataGridView();
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

                // Update the total price display
                UpdateTotalFromDataGridView();
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
            public int? CustomerId { get; set; } // This field is important for customer relationship
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
            SELECT OH.OrderID, OH.CustomOrderId, OH.CustomerId, C.FullName as CustomerName, C.Phone, OH.Discount,
                   OH.OrderDate, OH.PickupDate, OH.GrandTotalPrice, OH.DiscountedTotal, 
                   R.ReceiptID, R.IsPickedUp, R.CustomReceiptId, OH.OrderStatus
            FROM OrderHeader OH
            LEFT JOIN Customer C ON OH.CustomerId = C.CustomerID
            LEFT JOIN Receipt R ON OH.OrderID = R.OrderID
            WHERE 1=1
            AND (OH.OrderStatus <> N'รายการถูกยกเลิก' OR OH.OrderStatus IS NULL)");

                    if (!string.IsNullOrWhiteSpace(customerFilter))
                    {
                        sb.Append(" AND C.FullName LIKE @cust");
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
                                CustomerId = r["CustomerId"] != DBNull.Value ? (int?)Convert.ToInt32(r["CustomerId"]) : null,
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
                        // Get the current order ID
                        int orderId = (int)dgvOrders.CurrentRow.Cells["OrderID"].Value;

                        // Create a temporary negative ID to identify new items
                        // We'll use negative numbers that will be replaced with real IDs when saving to database
                        int tempOrderItemId = -1;

                        // If there are existing items in the modified list, decrement the temporary ID
                        // to ensure each new item has a unique temporary ID
                        if (_modifiedItems.Count > 0)
                        {
                            var existingTempIds = _modifiedItems
                                .Where(i => i.OrderItemID < 0)
                                .Select(i => i.OrderItemID)
                                .ToList();

                            if (existingTempIds.Any())
                            {
                                tempOrderItemId = existingTempIds.Min() - 1;
                            }
                        }

                        var itemForm = new Item(price, tempOrderItemId, itemNumber, itemName, 1);
                        if (itemForm.ShowDialog() == DialogResult.OK)
                        {
                            // Add to modified items list with temporary ID
                            _modifiedItems.Add(new OrderItemDto
                            {
                                OrderItemID = tempOrderItemId, // Temporary ID (negative)
                                ItemNumber = itemNumber,
                                ItemName = itemName,
                                Quantity = itemForm.Quantity,
                                TotalAmount = price * itemForm.Quantity,
                                IsCanceled = false,
                                CancelReason = ""
                            });

                            // Update the grid to show the changes
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
                    // For existing items (positive OrderItemID), we track their deletion
                    // but don't actually delete from the database yet
                    if (itemToRemove.OrderItemID > 0)
                    {
                        // Keep track that this item has been "marked for deletion"
                        // We'll only apply the deletion during Save
                        if (_originalItems.ContainsKey(itemToRemove.OrderItemID))
                        {
                            // We don't remove from _originalItems, so we can restore it if needed
                            // The item is just removed from _modifiedItems
                        }
                    }
                    
                    // Whether it's a new or existing item, remove it from the modified list
                    _modifiedItems.RemoveAt(selectedIndex);
                    
                    // Update the grid display and totals
                    UpdateModifiedItemsGrid();
                    
                    // Provide feedback to the user
                    MessageBox.Show("รายการถูกลบออกจากรายการแล้ว (จะบันทึกเมื่อกดปุ่มบันทึก)", 
                        "ลบรายการชั่วคราว", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                // เพิ่มเงื่อนไขสำหรับรหัสผ้า A00 เพื่อให้แก้ไขราคาได้
                if (item.ItemNumber == "A00")
                {
                    // แสดง dialog สำหรับการแก้ไขราคา
                    using (var priceForm = new Form())
                    {
                        priceForm.Text = "แก้ไขราคา";
                        priceForm.Size = new Size(400, 200);
                        priceForm.StartPosition = FormStartPosition.CenterParent;
                        priceForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                        priceForm.MaximizeBox = false;
                        priceForm.MinimizeBox = false;

                        Label lblPrice = new Label();
                        lblPrice.Text = "ราคาต่อชิ้น:";
                        lblPrice.Font = new Font("Angsana New", 20, FontStyle.Regular);
                        lblPrice.Location = new Point(20, 20);
                        lblPrice.Size = new Size(150, 40);

                        TextBox txtPrice = new TextBox();
                        txtPrice.Text = unitPrice.ToString("0.00");
                        txtPrice.Font = new Font("Angsana New", 20, FontStyle.Regular);
                        txtPrice.Location = new Point(170, 20);
                        txtPrice.Size = new Size(180, 40);

                        Button btnConfirm = new Button();
                        btnConfirm.Text = "ตกลง";
                        btnConfirm.Font = new Font("Angsana New", 18, FontStyle.Regular);
                        btnConfirm.Location = new Point(80, 80);
                        btnConfirm.Size = new Size(100, 50);
                        btnConfirm.DialogResult = DialogResult.OK;

                        Button btnCancel = new Button();
                        btnCancel.Text = "ยกเลิก";
                        btnCancel.Font = new Font("Angsana New", 18, FontStyle.Regular);
                        btnCancel.Location = new Point(210, 80);
                        btnCancel.Size = new Size(100, 50);
                        btnCancel.DialogResult = DialogResult.Cancel;

                        priceForm.Controls.Add(lblPrice);
                        priceForm.Controls.Add(txtPrice);
                        priceForm.Controls.Add(btnConfirm);
                        priceForm.Controls.Add(btnCancel);
                        priceForm.AcceptButton = btnConfirm;
                        priceForm.CancelButton = btnCancel;

                        if (priceForm.ShowDialog() == DialogResult.OK)
                        {
                            if (decimal.TryParse(txtPrice.Text, out decimal newPrice) && newPrice > 0)
                            {
                                // คำนวณราคารวมใหม่
                                decimal newTotalAmount = newPrice * item.Quantity;

                                // อัพเดทราคาในรายการที่กำลังแก้ไข (แค่ในหน่วยความจำ)
                                item.TotalAmount = newTotalAmount;

                                // อัพเดทการแสดงผลในตาราง
                                UpdateModifiedItemsGrid();

                                // แสดงข้อความว่าการเปลี่ยนแปลงยังไม่ถูกบันทึก
                                MessageBox.Show("แก้ไขราคาเรียบร้อย (จะบันทึกเมื่อกดปุ่มบันทึก)",
                                    "แก้ไขชั่วคราว", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                
                                // Mark changes as not saved
                                _changesSaved = false;
                                return;
                            }
                            else
                            {
                                MessageBox.Show("กรุณาระบุราคาที่ถูกต้อง", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        else
                        {
                            // ผู้ใช้ยกเลิกการแก้ไขราคา
                            return;
                        }
                    }
                }

                // สำหรับรายการปกติที่ไม่ใช่ A00
                // Create a new Item form but with a flag to prevent database updates
                
                var itemForm = new Item(unitPrice, item.OrderItemID, item.ItemNumber, item.ItemName, item.Quantity);
                
                // Important: Set these properties to control behavior
                itemForm.IsEditMode = true;
                itemForm.SourceForm = Item.CallingForm.ModifyServiceItem;

                if (itemForm.ShowDialog() == DialogResult.OK)
                {
                    // Get the new quantity from the form
                    int newQuantity = itemForm.Quantity;
                    
                    // Calculate new total amount
                    decimal newTotalAmount = unitPrice * newQuantity;
                    
                    // Update the in-memory item only (don't update database yet)
                    item.Quantity = newQuantity;
                    item.TotalAmount = newTotalAmount;
                    
                    // Update the grid display
                    UpdateModifiedItemsGrid();
                    
                    // Mark changes as not saved
                    _changesSaved = false;
                    
                    // Show message that changes will be saved later
                    MessageBox.Show("แก้ไขจำนวนเรียบร้อย (จะบันทึกเมื่อกดปุ่มบันทึกและพิมพ์สำเร็จ)",
                        "แก้ไขชั่วคราว", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                
                // Get customer information - use pending changes if available
                string customerName = _hasCustomerChanges ? _pendingCustomerName : dgvOrders.CurrentRow.Cells["CustomerName"].Value.ToString();
                string phone = _hasCustomerChanges ? _pendingPhone : dgvOrders.CurrentRow.Cells["Phone"].Value.ToString();
                decimal discount = _hasCustomerChanges && _pendingDiscount.HasValue ? _pendingDiscount.Value : Convert.ToDecimal(dgvOrders.CurrentRow.Cells["Discount"].Value);

                // Calculate grand total
                decimal grandTotal = _modifiedItems.Sum(i => i.TotalAmount);
                decimal discountedTotal = discount > 0
                    ? grandTotal - (grandTotal * (discount / 100m))
                    : grandTotal;

                // Prepare for printing - Create the serviceItems list
                var serviceItems = _modifiedItems
                    .Select(i => new Print_Service.ServiceItem
                    {
                        Name = i.ItemName,
                        Quantity = i.Quantity,
                        Price = i.Quantity > 0 ? i.TotalAmount / i.Quantity : 0
                    })
                    .ToList();

                // Show print form first to ensure user wants to print
                using (var printForm = new Print_Service(
                    customerName,
                    phone,
                    discount / 100,
                    customOrderId,
                    serviceItems))
                {
                    // If user cancels printing or printing fails, we won't save changes
                    if (printForm.ShowDialog() != DialogResult.OK || !printForm.IsPrinted)
                    {
                        MessageBox.Show("การพิมพ์ถูกยกเลิก การแก้ไขจะไม่ถูกบันทึก", 
                            "ยกเลิกการบันทึก", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Only proceed with database operations if printing was successful
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
                                // 1. Apply customer changes if there are any
                                if (_hasCustomerChanges)
                                {
                                    using (var cmd = new SqlCommand(
                                        @"UPDATE OrderHeader 
                                          SET CustomerId = @customerId, 
                                              Discount = @discount,
                                              DiscountedTotal = GrandTotalPrice - (GrandTotalPrice * @discountRate)
                                          WHERE OrderID = @orderId", cn, transaction))
                                    {
                                        if (_pendingCustomerId.HasValue)
                                            cmd.Parameters.AddWithValue("@customerId", _pendingCustomerId.Value);
                                        else
                                            cmd.Parameters.AddWithValue("@customerId", DBNull.Value);

                                        cmd.Parameters.AddWithValue("@discount", _pendingDiscount.Value);
                                        cmd.Parameters.AddWithValue("@discountRate", _pendingDiscount.Value / 100m);
                                        cmd.Parameters.AddWithValue("@orderId", orderId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                // 2. Handle deletions: Mark deleted items as canceled in the database
                                foreach (var originalItem in _originalItems.Values)
                                {
                                    // If item exists in original but not in modified, it was deleted
                                    if (!_modifiedItems.Any(m => m.OrderItemID == originalItem.OrderItemID))
                                    {
                                        using (var cmd = new SqlCommand(
                                            @"UPDATE OrderItem
                                              SET IsCanceled = 1, 
                                                  CancelReason = N'ลบโดยผู้ใช้ในการแก้ไขรายการ'
                                              WHERE OrderItemID = @id", cn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@id", originalItem.OrderItemID);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }

                                // 3. Handle modifications to existing items
                                foreach (var modifiedItem in _modifiedItems)
                                {
                                    // Only process existing items (positive IDs)
                                    if (modifiedItem.OrderItemID > 0)
                                    {
                                        // Get the original item for comparison
                                        if (_originalItems.TryGetValue(modifiedItem.OrderItemID, out var originalItem))
                                        {
                                            // Check if this item was actually modified
                                            if (modifiedItem.Quantity != originalItem.Quantity ||
                                                modifiedItem.TotalAmount != originalItem.TotalAmount ||
                                                modifiedItem.IsCanceled != originalItem.IsCanceled)
                                            {
                                                // Update the item in the database
                                                using (var cmd = new SqlCommand(
                                                    @"UPDATE OrderItem
                                                      SET Quantity = @qty,
                                                          TotalAmount = @total,
                                                          IsCanceled = @canceled,
                                                          CancelReason = @reason
                                                      WHERE OrderItemID = @id", cn, transaction))
                                                {
                                                    cmd.Parameters.AddWithValue("@qty", modifiedItem.Quantity);
                                                    cmd.Parameters.AddWithValue("@total", modifiedItem.TotalAmount);
                                                    cmd.Parameters.AddWithValue("@canceled", modifiedItem.IsCanceled);
                                                    if (modifiedItem.IsCanceled)
                                                        cmd.Parameters.AddWithValue("@reason", modifiedItem.CancelReason);
                                                    else
                                                        cmd.Parameters.AddWithValue("@reason", DBNull.Value);
                                                    cmd.Parameters.AddWithValue("@id", modifiedItem.OrderItemID);
                                                    cmd.ExecuteNonQuery();
                                                }
                                            }
                                        }
                                    }
                                }

                                // 4. Handle new items (with negative temporary IDs)
                                foreach (var newItem in _modifiedItems.Where(i => i.OrderItemID < 0))
                                {
                                    using (var cmd = new SqlCommand(
                                        @"INSERT INTO OrderItem 
                                          (OrderID, ItemNumber, ItemName, Quantity, TotalAmount, IsCanceled, CancelReason)
                                          VALUES 
                                          (@orderId, @itemNum, @itemName, @qty, @total, @canceled, @reason)", cn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@orderId", orderId);
                                        cmd.Parameters.AddWithValue("@itemNum", newItem.ItemNumber);
                                        cmd.Parameters.AddWithValue("@itemName", newItem.ItemName);
                                        cmd.Parameters.AddWithValue("@qty", newItem.Quantity);
                                        cmd.Parameters.AddWithValue("@total", newItem.TotalAmount);
                                        cmd.Parameters.AddWithValue("@canceled", newItem.IsCanceled);
                                        if (newItem.IsCanceled)
                                            cmd.Parameters.AddWithValue("@reason", newItem.CancelReason);
                                        else
                                            cmd.Parameters.AddWithValue("@reason", DBNull.Value);
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                // 5. Update order header with new totals
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

                                // Commit all changes
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
                }

                // Reload order items to refresh the display
                var updatedItems = _repo.GetOrderItems(orderId);
                
                // Reset the modified items and original items to match the updated data
                _modifiedItems = new BindingList<OrderItemDto>();
                _originalItems = new Dictionary<int, OrderItemDto>();
                
                foreach (var item in updatedItems)
                {
                    // Store original values
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
                    
                    // Add non-canceled items to the modified list
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
                
                // Re-attach event handler
                _modifiedItems.ListChanged += ModifiedItems_ListChanged;
                
                // Update data bindings
                dgvItems.DataSource = _modifiedItems;
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

                // Reset customer change tracking
                _hasCustomerChanges = false;
                _pendingCustomerId = null;
                _pendingCustomerName = null;
                _pendingPhone = null;
                _pendingDiscount = null;

                // Mark changes as saved
                _changesSaved = true;

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

        private void Select_Customer_Click(object sender, EventArgs e)
        {
            if (!CanEditOrder()) return;

            try
            {
                // Get current order data
                if (dgvOrders.CurrentRow == null)
                {
                    MessageBox.Show("กรุณาเลือกรายการที่ต้องการแก้ไขข้อมูลลูกค้า", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int orderId = (int)dgvOrders.CurrentRow.Cells["OrderID"].Value;
                int? currentCustomerId = null;
                if (dgvOrders.CurrentRow.Cells["CustomerId"].Value != null &&
                    dgvOrders.CurrentRow.Cells["CustomerId"].Value != DBNull.Value)
                {
                    currentCustomerId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["CustomerId"].Value);
                }
                string currentCustomerName = dgvOrders.CurrentRow.Cells["CustomerName"].Value?.ToString() ?? "";
                string currentPhone = dgvOrders.CurrentRow.Cells["Phone"].Value?.ToString() ?? "";
                decimal currentDiscount = Convert.ToDecimal(dgvOrders.CurrentRow.Cells["Discount"].Value);

                // Open Select_Customer form
                using (var selectCustomerForm = new Select_Customer())
                {
                    // Show the form and check if user clicked OK
                    if (selectCustomerForm.ShowDialog() == DialogResult.OK)
                    {
                        // Get the selected customer data
                        int? newCustomerId = selectCustomerForm.SelectedCustomerId;
                        string newCustomerName = selectCustomerForm.SelectedCustomerName;
                        string newPhone = selectCustomerForm.SelectedPhone;
                        decimal newDiscount = selectCustomerForm.SelectedDiscount;

                        // Check if data has changed
                        bool dataChanged = newCustomerId != currentCustomerId ||
                                          newCustomerName != currentCustomerName ||
                                          newPhone != currentPhone ||
                                          newDiscount != currentDiscount;

                        // If no changes, do nothing
                        if (!dataChanged)
                        {
                            MessageBox.Show("ไม่มีการเปลี่ยนแปลงข้อมูลลูกค้า", "ข้อมูล", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        // Confirm the changes
                        string message = "ยืนยันการเปลี่ยนแปลงข้อมูลลูกค้า:\n\n";
                        if (newCustomerName != currentCustomerName)
                            message += $"ชื่อลูกค้า: {currentCustomerName} → {newCustomerName}\n";
                        if (newPhone != currentPhone)
                            message += $"เบอร์โทร: {currentPhone} → {newPhone}\n";
                        if (newDiscount != currentDiscount)
                            message += $"ส่วนลด: {currentDiscount}% → {newDiscount}%\n";

                        DialogResult confirmResult = MessageBox.Show(
                            message,
                            "ยืนยันการเปลี่ยนแปลง",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (confirmResult != DialogResult.Yes)
                        {
                            return; // User canceled
                        }

                        // Store the pending changes - not applying to database yet
                        _pendingCustomerId = newCustomerId;
                        _pendingCustomerName = newCustomerName;
                        _pendingPhone = newPhone;
                        _pendingDiscount = newDiscount;
                        _hasCustomerChanges = true;
                        
                        // Update the current row in the grid with new values for display only
                        dgvOrders.CurrentRow.Cells["CustomerName"].Value = newCustomerName;
                        dgvOrders.CurrentRow.Cells["Phone"].Value = newPhone;
                        dgvOrders.CurrentRow.Cells["Discount"].Value = newDiscount;
                        dgvOrders.CurrentRow.Cells["CustomerId"].Value = newCustomerId.HasValue ? (object)newCustomerId.Value : DBNull.Value;

                        // Recalculate discounted total for display only
                        decimal grandTotal = Convert.ToDecimal(dgvOrders.CurrentRow.Cells["GrandTotalPrice"].Value);
                        decimal newDiscountedTotal = grandTotal - (grandTotal * (newDiscount / 100m));
                        dgvOrders.CurrentRow.Cells["DiscountedTotal"].Value = newDiscountedTotal;

                        // Set the changes saved flag to false since we have pending changes
                        _changesSaved = false;

                        MessageBox.Show("ข้อมูลลูกค้าถูกเปลี่ยนแปลงแล้ว (จะบันทึกเมื่อกดปุ่มบันทึก)", 
                            "เปลี่ยนแปลงชั่วคราว", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateTotalFromDataGridView()
        {
            decimal total = 0;

            // Calculate total based on what's available - either the modified items collection or the current items
            if (_modifiedItems != null && _modifiedItems.Count > 0)
            {
                // Use _modifiedItems when it's populated
                total = _modifiedItems
                    .Where(item => !item.IsCanceled)
                    .Sum(item => item.TotalAmount);
            }
            else if (_currentItems != null && _currentItems.Count > 0)
            {
                // Use _currentItems when _modifiedItems is empty
                total = _currentItems
                    .Where(item => !item.IsCanceled)
                    .Sum(item => item.TotalAmount);
            }

            // Update the lblTotal with the calculated total
            lblTotal.Text = total.ToString("N2") + " บาท";
        }
        private void ModifiedItems_ListChanged(object sender, ListChangedEventArgs e)
        {
            UpdateTotalFromDataGridView();
            _changesSaved = false; // Reset the saved flag when changes are made
        }
        private void Modify_Service_Item_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If changes have already been saved, no need to check or show confirmation
            if (_changesSaved)
                return;

            // Rest of your existing code to check for unsaved changes
            bool hasChanges = false;

            // Check for new items (temporary IDs)
            if (_modifiedItems.Any(i => i.OrderItemID < 0))
            {
                hasChanges = true;
            }
            else
            {
                // Check for modified existing items
                foreach (var modifiedItem in _modifiedItems)
                {
                    if (_originalItems.TryGetValue(modifiedItem.OrderItemID, out var originalItem))
                    {
                        // Compare item properties
                        if (modifiedItem.Quantity != originalItem.Quantity ||
                            modifiedItem.TotalAmount != originalItem.TotalAmount ||
                            modifiedItem.IsCanceled != originalItem.IsCanceled)
                        {
                            hasChanges = true;
                            break;
                        }
                    }
                }

                // Check for removed items
                if (!hasChanges)
                {
                    foreach (var originalItem in _originalItems.Values)
                    {
                        if (!_modifiedItems.Any(m => m.OrderItemID == originalItem.OrderItemID))
                        {
                            hasChanges = true;
                            break;
                        }
                    }
                }
            }

            // If there are unsaved changes, ask user for confirmation
            if (hasChanges)
            {
                DialogResult result = MessageBox.Show(
                    "มีการเปลี่ยนแปลงที่ยังไม่ได้บันทึก คุณต้องการออกโดยไม่บันทึกหรือไม่?",
                    "ยืนยันการออก",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true; // Cancel the form closing
                }
            }
        }
    }
}
