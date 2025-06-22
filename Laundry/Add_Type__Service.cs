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
using Laundry_Management.Laundry;

namespace Laundry_Management
{
    public partial class Add_Type__Service : Form
    {
        public Add_Type__Service()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ากรอกข้อมูลครบทุกช่องหรือไม่
            if (ServiceType.SelectedItem == null ||
                string.IsNullOrWhiteSpace(ItemNumber.Text) ||
                string.IsNullOrWhiteSpace(ItemName.Text) ||
                string.IsNullOrWhiteSpace(Price.Text) ||
                Gender.SelectedItem == null)
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบทุกช่อง", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string checkQuery = "SELECT COUNT(*) FROM LaundryService WHERE ItemNumber = @ItemNumber";
            using (SqlConnection connection = DBconfig.GetConnection())
            {
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@ItemNumber", ItemNumber.Text);

                    connection.Open();
                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0)
                    {
                        MessageBox.Show("หมายเลขรายการ นี้มีอยู่แล้วในระบบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // ถ้าไม่ซ้ำ ให้บันทึกข้อมูล
            string query = "INSERT INTO LaundryService (ServiceType, ItemNumber, ItemName, Price, Gender) VALUES (@ServiceType, @ItemNumber, @ItemName, @Price, @Gender)";
            using (SqlConnection connection = DBconfig.GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ServiceType", ServiceType.SelectedItem.ToString());
                    command.Parameters.AddWithValue("@ItemNumber", ItemNumber.Text);
                    command.Parameters.AddWithValue("@ItemName", ItemName.Text);
                    command.Parameters.AddWithValue("@Price", Price.Text);
                    command.Parameters.AddWithValue("@Gender", Gender.SelectedItem.ToString());

                    connection.Open();
                    command.ExecuteNonQuery();
                    MessageBox.Show("บันทึกข้อมูลสำเร็จ");
                    LoadData();
                    ClearForm();
                }
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                int serviceID = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["ServiceID"].Value);

                string query = "DELETE FROM LaundryService WHERE ServiceID = @ServiceID";
                using (SqlConnection connection = DBconfig.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceID);

                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("ลบข้อมูลสำเร็จ");
                            LoadData();
                            ClearForm();
                        }
                        else
                        {
                            MessageBox.Show("ไม่พบข้อมูลที่จะลบ");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("เลือกข้อมูลก่อนที่จะลบ");
            }
        }
        private void LoadData()
        {
            string query = "SELECT ServiceID, ItemNumber, ServiceType, ItemName, Price, Gender, CreatedAt, IsCancelled, CancelledDate FROM LaundryService";
            using (SqlConnection connection = DBconfig.GetConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridView1.DataSource = dataTable;
                }
                // สมมติว่าชื่อ DataGridView ของคุณคือ dataGridView1
                dataGridView1.Columns["ItemNumber"].HeaderText = "หมายเลขรายการ";
                dataGridView1.Columns["ServiceType"].HeaderText = "ประเภทการซัก";
                dataGridView1.Columns["ItemName"].HeaderText = "ชื่อ-รายการ";
                dataGridView1.Columns["Price"].HeaderText = "ราคา";
                dataGridView1.Columns["Gender"].HeaderText = "เพศ";
                dataGridView1.Columns["CreatedAt"].HeaderText = "สร้างเมื่อ";
                dataGridView1.Columns["IsCancelled"].HeaderText = "การใช้งาน";
                dataGridView1.Columns["CancelledDate"].HeaderText = "วันที่ยกเลิกการใช้งาน";
                if (dataGridView1.Columns["ServiceID"] != null)
                {
                    dataGridView1.Columns["ServiceID"].Visible = false;
                }
            }
        }

        // เพิ่มเมธอดสำหรับล้างข้อมูลในฟอร์ม
        private void ClearForm()
        {
            ServiceType.SelectedIndex = 0;
            ItemNumber.Clear();
            ItemName.Clear();
            Price.Clear();
            Gender.SelectedIndex = 0;
        }

        private void Add_Type__Service_Load(object sender, EventArgs e)
        {
            // ตั้งค่า AutoSizeColumnsMode และ AutoSizeRowsMode
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // ป้องกันการพิมพ์ใน ComboBox
            ServiceType.DropDownStyle = ComboBoxStyle.DropDownList;
            Gender.DropDownStyle = ComboBoxStyle.DropDownList;

            // เพิ่มข้อมูลใน ComboBox ServiceType
            ServiceType.Items.Add("");
            ServiceType.Items.Add("ซักแห้ง (Dry Cleaning Service)");
            ServiceType.Items.Add("ซักน้ำ (Laundry Service)");

            Gender.Items.Add("");
            Gender.Items.Add("สุภาพบุรุษ (Gentleman)");
            Gender.Items.Add("สุภาพสตรี (Ladies)");

            chkUsing.CheckedChanged += ChkStatus_CheckedChanged;
            chkNotUse.CheckedChanged += ChkStatus_CheckedChanged;

            LoadData();
        }
        private void ChkStatus_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox currentCheckBox = (CheckBox)sender;

            // ถ้า checkbox ที่เพิ่งถูกคลิกถูก checked
            if (currentCheckBox.Checked)
            {
                // ถ้าเป็น chkBaht (ใช้งาน) ให้ยกเลิกการเลือก chkPercent (ไม่ใช้งาน)
                if (currentCheckBox == chkUsing)
                    chkNotUse.Checked = false;
                // ถ้าเป็น chkPercent (ไม่ใช้งาน) ให้ยกเลิกการเลือก chkBaht (ใช้งาน)
                else if (currentCheckBox == chkNotUse)
                    chkUsing.Checked = false;
            }
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการเลือกเซลล์ใดๆ หรือไม่
            if (dataGridView1.CurrentCell != null)
            {
                // ดึงข้อมูลจากแถวที่เซลล์ปัจจุบันอยู่
                int rowIndex = dataGridView1.CurrentCell.RowIndex;
                DataGridViewRow row = dataGridView1.Rows[rowIndex];

                // ตรวจสอบว่าแถวนั้นมีข้อมูลหรือไม่
                if (row.Cells["ServiceID"].Value != null)
                {
                    int serviceId = Convert.ToInt32(row.Cells["ServiceID"].Value);
                    string serviceType = row.Cells["ServiceType"].Value?.ToString() ?? "";
                    string itemName = row.Cells["ItemName"].Value?.ToString() ?? "";
                    string price = row.Cells["Price"].Value?.ToString() ?? "";
                    string gender = row.Cells["Gender"].Value?.ToString() ?? "";
                    string itemNumber = row.Cells["ItemNumber"].Value?.ToString() ?? "";

                    // เปิดฟอร์มแก้ไข
                    var modifyForm = new Modify_Type_Service(itemName, serviceType, gender, price, itemNumber, serviceId);

                    if (modifyForm.ShowDialog() == DialogResult.OK)
                        LoadData();
                }
                else
                {
                    MessageBox.Show("ไม่พบข้อมูลในแถวที่เลือก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("กรุณาเลือกรายการที่ต้องการแก้ไข", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            string itemName = ItemName.Text.Trim();
            string itemNumber = ItemNumber.Text.Trim();
            string gender = Gender.SelectedItem?.ToString() ?? "";
            string price = Price.Text.ToString();
            string serviceType = ServiceType.SelectedItem?.ToString() ?? "";

            string query = "SELECT ServiceID, ItemNumber, ServiceType, ItemName, Price, Gender, CreatedAt, IsCancelled, CancelledDate FROM LaundryService WHERE 1=1";
            if (!string.IsNullOrEmpty(itemName))
                query += " AND ItemName LIKE @itemName";
            if (!string.IsNullOrEmpty(itemNumber))
                query += " AND ItemNumber LIKE @itemNumber";
            if (!string.IsNullOrEmpty(price))
                query += " AND Price LIKE @price";
            if (!string.IsNullOrEmpty(gender))
                query += " AND Gender = @gender";
            if (!string.IsNullOrEmpty(serviceType))
                query += " AND ServiceType = @serviceType";
            if (chkUsing.Checked && !chkNotUse.Checked)
                query += " AND (IsCancelled IS NULL OR IsCancelled = N'ใช้งาน')";
            else if (!chkUsing.Checked && chkNotUse.Checked)
                query += " AND IsCancelled = N'ไม่ใช้งาน'";

            using (SqlConnection connection = DBconfig.GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(itemName))
                        command.Parameters.AddWithValue("@itemName", "%" + itemName + "%");
                    if (!string.IsNullOrEmpty(itemNumber))
                        command.Parameters.AddWithValue("@itemNumber", "%" + itemNumber + "%");
                    if (!string.IsNullOrEmpty(price))
                        command.Parameters.AddWithValue("@price", "%" + price + "%");
                    if (!string.IsNullOrEmpty(gender))
                        command.Parameters.AddWithValue("@gender", gender);
                    if (!string.IsNullOrEmpty(serviceType))
                        command.Parameters.AddWithValue("@serviceType", serviceType);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        dataGridView1.DataSource = dataTable;
                    }
                }
            }
        }
    }
}
