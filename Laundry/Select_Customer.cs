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
using static Laundry_Management.Laundry.Print_Service;

namespace Laundry_Management.Laundry
{
    public partial class Select_Customer : Form
    {
        private List<Item> _selectedItems;
        public string SelectedCustomerName { get; private set; }
        public string SelectedPhone { get; private set; }
        public decimal SelectedDiscount { get; private set; }
        public Select_Customer(List<Item> selectedItems)
        {
            InitializeComponent();
            LoadCustomerData();
            _selectedItems = selectedItems;
        }
        public Select_Customer()
        {
            InitializeComponent();
            LoadCustomerData();
        }

        private void Select_Customer_Load(object sender, EventArgs e)
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void LoadCustomerData()
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "SELECT CustomerID, FullName, Phone, Discount, Note FROM Customer";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dataGridView1.DataSource = dt;
            }
            dataGridView1.Columns["FullName"].HeaderText = "ชื่อ-นามสกุล";
            dataGridView1.Columns["Phone"].HeaderText = "เบอร์โทร";
            dataGridView1.Columns["Discount"].HeaderText = "ส่วนลด";
            dataGridView1.Columns["Note"].HeaderText = "หมายเหตุ";
            if (dataGridView1.Columns["CustomerID"] != null)
            {
                dataGridView1.Columns["CustomerID"].Visible = false;
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();

            // Build the SQL query with parameters
            string query = "SELECT CustomerID, FullName, Phone, Discount, Note FROM Customer WHERE 1=1";
            if (!string.IsNullOrEmpty(fullName))
            {
                query += " AND FullName LIKE @FullName";
            }
            if (!string.IsNullOrEmpty(phone))
            {
                query += " AND Phone LIKE @Phone";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(fullName))
                {
                    command.Parameters.AddWithValue("@FullName", "%" + fullName + "%");
                }
                if (!string.IsNullOrEmpty(phone))
                {
                    command.Parameters.AddWithValue("@Phone", "%" + phone + "%");
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt;
                }
            }

            if (dataGridView1.Columns["CustomerID"] != null)
            {
                dataGridView1.Columns["CustomerID"].Visible = false;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกลูกค้า");
                return;
            }
            // เลือกลูกค้าเหมือนเดิม
            var row = dataGridView1.SelectedRows[0];
            SelectedCustomerName = row.Cells["FullName"].Value.ToString();
            SelectedPhone = row.Cells["Phone"].Value.ToString();
            SelectedDiscount = row.Cells["Discount"].Value == DBNull.Value
                                ? 0m
                                : Convert.ToDecimal(row.Cells["Discount"].Value);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
