using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Laundry_Management
{
    public partial class Modify_Type_Service : Form
    {
        public int ServiceID { get; set; } // ใช้สำหรับอ้างอิง record ที่จะแก้ไข

        public Modify_Type_Service(string itemName, string serviceType, string gender, string price, string itemNumber, int serviceId)
        {
            InitializeComponent();
            ItemName.Text = itemName;
            ServiceType.Text = serviceType;
            Gender.Text = gender;
            Price.Text = price;
            ItemNumber.Text = itemNumber;
            ServiceID = serviceId;
        }

        public Modify_Type_Service()
        {
            InitializeComponent();
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                if (chkIsCancelled.Checked)
                {
                    string insertQuery = "INSERT INTO LaundryService (ServiceType, ItemName, Price, Gender, ItemNumber) VALUES (@ServiceType, @ItemName, @Price, @Gender, @ItemNumber)";
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceType", ServiceType.Text);
                        command.Parameters.AddWithValue("@ItemName", ItemName.Text);
                        command.Parameters.AddWithValue("@Price", Price.Text);
                        command.Parameters.AddWithValue("@Gender", Gender.Text);
                        command.Parameters.AddWithValue("@ItemNumber", ItemNumber.Text);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    cmd.CommandText = @"UPDATE LaundryService 
                SET IsCancelled = N'ยกเลิกแล้ว', CancelledDate = @CancelledDate
                WHERE ServiceID = @ServiceID";
                    cmd.Parameters.AddWithValue("@CancelledDate", dtpCancelledDate.Value);
                }
                else
                {
                    cmd.CommandText = @"UPDATE LaundryService 
                SET ServiceType = @ServiceType, ItemName = @ItemName, Price = @Price, Gender = @Gender, ItemNumber = @ItemNumber, 
                    IsCancelled = N'ยังไม่ยกเลิก', CancelledDate = NULL
                WHERE ServiceID = @ServiceID";
                }

                cmd.Parameters.AddWithValue("@ServiceType", ServiceType.Text);
                cmd.Parameters.AddWithValue("@ItemName", ItemName.Text);
                cmd.Parameters.AddWithValue("@Price", Price.Text);
                cmd.Parameters.AddWithValue("@Gender", Gender.Text);
                cmd.Parameters.AddWithValue("@ItemNumber", ItemNumber.Text);
                cmd.Parameters.AddWithValue("@ServiceID", ServiceID);

                cmd.ExecuteNonQuery();
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Modify_Type_Service_Load(object sender, EventArgs e)
        {
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
        }
    }
}
