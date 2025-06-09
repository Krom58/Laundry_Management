using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laundry_Management.Laundry
{
    public partial class Pickup_List : Form
    {
        public Pickup_List()
        {
            InitializeComponent();
            LoadPickupOrders();
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvOrders.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }
        private void LoadPickupOrders()
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = @"
        SELECT 
            o.OrderID, 
            o.CustomerName, 
            o.Phone, 
            r.ReceiptID, 
            r.ReceiptDate, 
            r.IsPickedUp, 
            r.CustomerPickupDate
        FROM OrderHeader o
        INNER JOIN Receipt r ON o.OrderID = r.OrderID
        WHERE r.IsPickedUp IS NULL OR r.IsPickedUp <> 'Yes'
    ";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dgvOrders.DataSource = dt;
            }
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string orderId = txtOrderId.Text.Trim();
            string customerFilter = txtCustomerFilter.Text.Trim();
            DateTime? createDate = dtpCreateDate.Checked ? (DateTime?)dtpCreateDate.Value.Date : null;

            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            var query = @"
        SELECT 
            o.OrderID, 
            o.CustomerName, 
            o.Phone, 
            r.ReceiptID, 
            r.ReceiptDate, 
            r.IsPickedUp, 
            r.CustomerPickupDate
        FROM OrderHeader o
        INNER JOIN Receipt r ON o.OrderID = r.OrderID
        WHERE (r.IsPickedUp IS NULL OR r.IsPickedUp <> 'Yes')
    ";

            var filters = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(orderId))
            {
                filters.Add("o.OrderID = @OrderID");
                parameters.Add(new SqlParameter("@OrderID", orderId));
            }
            if (!string.IsNullOrEmpty(customerFilter))
            {
                filters.Add("o.CustomerName LIKE @CustomerName");
                parameters.Add(new SqlParameter("@CustomerName", "%" + customerFilter + "%"));
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

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dgvOrders.DataSource = dt;
            }
        }

        private void Customer_Pickup_Check_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการบันทึกการรับผ้า", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ตรวจสอบสถานะ IsPickedUp ก่อน
            var isPickedUpObj = dgvOrders.CurrentRow.Cells["IsPickedUp"].Value;
            if (isPickedUpObj != null && isPickedUpObj.ToString() == "มารับแล้ว")
            {
                MessageBox.Show("ไม่สามารถบันทึกซ้ำได้", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ดึง ReceiptID จากแถวที่เลือก
            var receiptIdObj = dgvOrders.CurrentRow.Cells["ReceiptID"].Value;
            if (receiptIdObj == null)
            {
                MessageBox.Show("ไม่พบ ReceiptID ในแถวที่เลือก", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int receiptId = Convert.ToInt32(receiptIdObj);

            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string updateQuery = @"
        UPDATE Receipt
        SET IsPickedUp = N'มารับแล้ว', CustomerPickupDate = @PickupDate
        WHERE ReceiptID = @ReceiptID
    ";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@PickupDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@ReceiptID", receiptId);

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                conn.Close();

                if (rowsAffected > 0)
                {
                    MessageBox.Show("บันทึกการรับผ้าเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadPickupOrders(); // รีเฟรชข้อมูล
                }
                else
                {
                    MessageBox.Show("ไม่สามารถบันทึกข้อมูลได้", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
