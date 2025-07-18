﻿using System;
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
        public int? SelectedCustomerId { get; private set; }
        public Select_Customer(List<Item> selectedItems)
        {
            InitializeComponent();

            // เพิ่ม event handlers สำหรับการกด Enter ในช่องค้นหา
            txtFullName.KeyPress += TxtSearch_KeyPress;
            txtPhone.KeyPress += TxtSearch_KeyPress;

            // ตั้งค่าปุ่ม Search เป็น AcceptButton ของฟอร์ม
            this.AcceptButton = btnSearch;

            LoadCustomerData();
            _selectedItems = selectedItems;
        }
        public Select_Customer()
        {
            InitializeComponent();

            // เพิ่ม event handlers สำหรับการกด Enter ในช่องค้นหา
            txtFullName.KeyPress += TxtSearch_KeyPress;
            txtPhone.KeyPress += TxtSearch_KeyPress;

            // ตั้งค่าปุ่ม Search เป็น AcceptButton ของฟอร์ม
            this.AcceptButton = btnSearch;

            LoadCustomerData();
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
        private void Select_Customer_Load(object sender, EventArgs e)
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;

            LoadCustomerData();

            // เพิ่มการเคลียร์การเลือกอีกครั้งหลังจาก LoadCustomerData()
            dataGridView1.ClearSelection();

            // เพื่อให้แน่ใจว่าจะไม่มีเซลล์ใดถูกเลือก แม้จะมีโค้ดอื่นพยายามเลือกเซลล์
            dataGridView1.CurrentCell = null;
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void LoadCustomerData()
        {
            string query = "SELECT CustomerID, FullName, Phone, Discount, Note FROM Customer";

            using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
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

            // เคลียร์การเลือกทั้งหมด หลังจากโหลดข้อมูล
            dataGridView1.ClearSelection();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
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

            using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
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

            // เคลียร์การเลือกทั้งหมดหลังจากค้นหา
            dataGridView1.ClearSelection();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการเลือกเซลล์หรือไม่
            if (dataGridView1.CurrentCell == null)
            {
                MessageBox.Show("กรุณาเลือกลูกค้า");
                return;
            }

            // ดึงแถวที่เซลล์ปัจจุบันอยู่
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            DataGridViewRow row = dataGridView1.Rows[rowIndex];

            // ตรวจสอบว่าแถวนั้นมีข้อมูลหรือไม่
            if (row.Cells["FullName"].Value == null || row.Cells["Phone"].Value == null)
            {
                MessageBox.Show("ข้อมูลลูกค้าไม่ครบถ้วน");
                return;
            }

            // ดึงข้อมูลลูกค้าจากแถว
            SelectedCustomerId = row.Cells["CustomerID"].Value != DBNull.Value
                                ? (int?)Convert.ToInt32(row.Cells["CustomerID"].Value)
                                : null;
            SelectedCustomerName = row.Cells["FullName"].Value.ToString();
            SelectedPhone = row.Cells["Phone"].Value.ToString();
            SelectedDiscount = row.Cells["Discount"].Value != DBNull.Value
                                   ? Convert.ToDecimal(row.Cells["Discount"].Value)
                                   : 0m;

            // ตรงนี้แค่ปิดฟอร์มเลือกลูกค้า ส่ง DialogResult
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
