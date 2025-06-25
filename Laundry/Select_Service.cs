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
    public partial class Select_Service : Form
    {
        public string SelectedItemNumber { get; private set; }
        public string SelectedItemName { get; private set; }
        public decimal SelectedPrice { get; private set; }
        public Select_Service()
        {
            InitializeComponent();
        }

        private void Search_Click(object sender, EventArgs e)
        {
            // ดึงค่าจาก TextBox และ ComboBox
            string itemName = ItemName.Text.Trim();
            string serviceType = ServiceType.SelectedItem?.ToString() ?? "";
            string gender = Gender.SelectedItem?.ToString() ?? "";
            string itemNumber = ItemNumber.Text.Trim();

            // โหลดข้อมูลตามเงื่อนไขค้นหา
            LoadServices(itemName, serviceType, gender, itemNumber);
        }

        private void Select_Service_Load(object sender, EventArgs e)
        {
            // ตั้งค่า DataGridView ให้เลือกทั้งแถว
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // ตั้งค่า AutoSize ให้ DataGridView
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // ตั้งค่า ComboBox สำหรับ ServiceType
            ServiceType.Items.Add("");  // Empty option for "all"
            ServiceType.Items.Add("ซักแห้ง (Dry Cleaning Service)");
            ServiceType.Items.Add("ซักน้ำ (Laundry Service)");
            ServiceType.DropDownStyle = ComboBoxStyle.DropDownList;
            ServiceType.SelectedIndex = 0;  // Default to empty (all)

            Gender.Items.Add("");
            Gender.Items.Add("สุภาพบุรุษ (Gentleman)");
            Gender.Items.Add("สุภาพสตรี (Ladies)");
            Gender.DropDownStyle = ComboBoxStyle.DropDownList;
            Gender.SelectedIndex = 0;  // Default to empty (all)

            // โหลดข้อมูลเริ่มต้น
            LoadServices();
        }
        private void LoadServices(
            string itemNameFilter = null,
            string serviceTypeFilter = null,
            string Gender = null,
            string itemNumberFilter = null)
        {
            try
            {
                string query = @"
                    SELECT 
                        ServiceID, 
                        ItemNumber, 
                        ItemName, 
                        ServiceType, 
                        Price, 
                        Gender 
                    FROM 
                        LaundryService 
                    WHERE 
                        IsCancelled = N'ใช้งาน'";

                // เพิ่มเงื่อนไขการค้นหา
                if (!string.IsNullOrEmpty(itemNameFilter))
                {
                    query += " AND ItemName LIKE @itemName";
                }
                if (!string.IsNullOrEmpty(serviceTypeFilter))
                {
                    query += " AND ServiceType = @serviceType";
                }
                if (!string.IsNullOrEmpty(itemNumberFilter))
                {
                    query += " AND ItemNumber LIKE @itemNumber";
                }

                using (SqlConnection connection = DBconfig.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // เพิ่มพารามิเตอร์
                        if (!string.IsNullOrEmpty(itemNameFilter))
                        {
                            command.Parameters.AddWithValue("@itemName", "%" + itemNameFilter + "%");
                        }
                        if (!string.IsNullOrEmpty(serviceTypeFilter))
                        {
                            command.Parameters.AddWithValue("@serviceType", serviceTypeFilter);
                        }
                        if (!string.IsNullOrEmpty(itemNumberFilter))
                        {
                            command.Parameters.AddWithValue("@itemNumber", "%" + itemNumberFilter + "%");
                        }

                        // โหลดข้อมูล
                        connection.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            dataGridView1.DataSource = dataTable;

                            // ซ่อน ServiceID column
                            if (dataGridView1.Columns["ServiceID"] != null)
                            {
                                dataGridView1.Columns["ServiceID"].Visible = false;
                            }

                            // ตั้งชื่อหัวข้อคอลัมน์เป็นภาษาไทย
                            if (dataGridView1.Columns["ItemNumber"] != null)
                                dataGridView1.Columns["ItemNumber"].HeaderText = "รหัสสินค้า";
                            if (dataGridView1.Columns["ItemName"] != null)
                                dataGridView1.Columns["ItemName"].HeaderText = "ชื่อสินค้า";
                            if (dataGridView1.Columns["ServiceType"] != null)
                                dataGridView1.Columns["ServiceType"].HeaderText = "ประเภทบริการ";
                            if (dataGridView1.Columns["Price"] != null)
                                dataGridView1.Columns["Price"].HeaderText = "ราคา";
                            if (dataGridView1.Columns["Gender"] != null)
                                dataGridView1.Columns["Gender"].HeaderText = "ประเภทเพศ";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดข้อมูล: {ex.Message}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการเลือกแถวหรือไม่
            if (dataGridView1.CurrentRow == null || dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกรายการ", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ดึงข้อมูลจากแถวที่เลือก
            DataGridViewRow row = dataGridView1.CurrentRow;
            SelectedItemNumber = row.Cells["ItemNumber"].Value.ToString();
            SelectedItemName = row.Cells["ItemName"].Value.ToString();
            SelectedPrice = Convert.ToDecimal(row.Cells["Price"].Value);

            // ส่งผลกลับไปที่ฟอร์มที่เรียกและปิดฟอร์มนี้
            DialogResult = DialogResult.OK;
            Close();
        }
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // เมื่อดับเบิ้ลคลิกที่แถว ให้ทำการเลือกรายการนั้น
            if (e.RowIndex >= 0)
            {
                // เอฟเฟคเดียวกับการกดปุ่มตกลง
                btnOk_Click(sender, e);
            }
        }
    }
}
