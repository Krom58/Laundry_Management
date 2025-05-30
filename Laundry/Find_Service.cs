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
        private PrintDocument _printDocument;
        public Find_Service()
        {
            InitializeComponent();
            LoadOrders(null, null, null);
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
            public DateTime OrderDate { get; set; }
            public DateTime PickupDate { get; set; }
            public decimal GrandTotalPrice { get; set; }
            public decimal DiscountedTotal { get; set; }
        }

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
                   OH.PickupDate, OH.GrandTotalPrice, OH.DiscountedTotal
              FROM OrderHeader OH
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
                                                  : Convert.ToDecimal(r["DiscountedTotal"])
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

            // 1) เปิดฟอร์ม ReceiptPrintForm ก่อน เพื่อให้ผู้ใช้เลือก print จริง
            int tempReceiptId = -1; // ยังไม่สร้าง receipt จริง
            using (var rptForm = new ReceiptPrintForm(tempReceiptId, header, items))
            {
                rptForm.ShowDialog(this);
                if (!rptForm.IsPrinted)
                {
                    MessageBox.Show("ยกเลิกการออกใบเสร็จ");
                    return;
                }
            }

            try
            {
                // 2) ถ้าผู้ใช้ print จริง ค่อย save Receipt และ Items
                int receiptId = _repo.CreateReceipt(header.OrderID);
                foreach (var item in items)
                {
                    _repo.CreateReceiptItem(
                        receiptId: receiptId,
                        orderItemId: item.OrderItemID,
                        quantity: item.Quantity,
                        amount: item.TotalAmount
                    );
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
