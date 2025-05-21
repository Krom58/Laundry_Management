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

            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";

            // ตรวจสอบความซ้ำของ ItemNumber หรือ ItemName
            string checkQuery = "SELECT COUNT(*) FROM LaundryService WHERE ItemNumber = @ItemNumber";
            using (SqlConnection connection = new SqlConnection(connectionString))
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
            using (SqlConnection connection = new SqlConnection(connectionString))
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
                    LoadData(); // รีเฟรชข้อมูลหลังบันทึก
                    ClearForm(); // ล้างข้อมูลในฟอร์มหลังบันทึก
                }
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                // ดึงค่า ServiceID จากแถวที่เลือก
                int serviceID = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["ServiceID"].Value);

                string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
                string query = "DELETE FROM LaundryService WHERE ServiceID = @ServiceID";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceID);

                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("ลบข้อมูลสำเร็จ");
                            LoadData(); // โหลดข้อมูลใหม่ใน DataGridView
                            ClearForm(); // ล้างข้อมูลในฟอร์มหลังลบ
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
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "SELECT ServiceID, ItemNumber, ServiceType, ItemName, Price, Gender FROM LaundryService";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridView1.DataSource = dataTable;
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
            ServiceType.Items.Add("ซักแห้ง (Dry Cleaning Service)");
            ServiceType.Items.Add("ซักน้ำ (Laundry Service)");

            // ตั้งค่าตัวเลือกเริ่มต้น
            if (ServiceType.Items.Count > 0)
            {
                ServiceType.SelectedIndex = 0; // เลือกตัวเลือกแรก
            }

            // เพิ่มข้อมูลใน ComboBox Gender
            Gender.Items.Add("สุภาพบุรุษ (Gentleman)");
            Gender.Items.Add("สุภาพสตรี (Ladies)");

            // ตั้งค่าตัวเลือกเริ่มต้น
            if (Gender.Items.Count > 0)
            {
                Gender.SelectedIndex = 0; // เลือกตัวเลือกแรก
            }

            LoadData();
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dataGridView1.SelectedRows[0];

                int serviceId = Convert.ToInt32(row.Cells["ServiceID"].Value);
                string serviceType = row.Cells["ServiceType"].Value.ToString();
                string itemName = row.Cells["ItemName"].Value.ToString();
                string price = row.Cells["Price"].Value.ToString();
                string gender = row.Cells["Gender"].Value.ToString();
                string itemNumber = row.Cells["ItemNumber"].Value.ToString();


                // To this:
                var modifyForm = new Modify_Type_Service(itemName, serviceType, gender, price, itemNumber, serviceId);

                if (modifyForm.ShowDialog() == DialogResult.OK)
                    LoadData();
            }
            else
            {
                MessageBox.Show("กรุณาเลือกแถวที่ต้องการแก้ไข");
            }
        }
    }
}
