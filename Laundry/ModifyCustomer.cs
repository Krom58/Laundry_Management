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

namespace Laundry_Management
{
    public partial class ModifyCustomer : Form
    {
        private int customerId;

        public ModifyCustomer(int customerId, string fullName, string phone, int? discount, string note)
        {
            InitializeComponent();
            this.customerId = customerId;
            txtFullName.Text = fullName;
            txtPhone.Text = phone;
            txtDiscount.Text = discount?.ToString() ?? "";
            txtNote.Text = note;
        }

        public ModifyCustomer()
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
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string discountText = txtDiscount.Text.Trim();
            int? discount = null;
            string note = txtNote.Text.Trim();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("กรุณากรอกชื่อ-นามสกุล และเบอร์โทร");
                return;
            }

            if (!string.IsNullOrEmpty(discountText))
            {
                if (int.TryParse(discountText, out int d))
                    discount = d;
                else
                {
                    MessageBox.Show("กรุณากรอกส่วนลดเป็นตัวเลขจำนวนเต็ม");
                    return;
                }
            }

            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";
            string query = "UPDATE Customer SET FullName = @FullName, Phone = @Phone, Discount = @Discount, Note = @Note WHERE CustomerID = @CustomerID";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Phone", phone);
                command.Parameters.AddWithValue("@Discount", (object)discount ?? DBNull.Value);
                command.Parameters.AddWithValue("@CustomerID", customerId);
                command.Parameters.AddWithValue("@Note", note);

                connection.Open();
                command.ExecuteNonQuery();
            }

            MessageBox.Show("แก้ไขข้อมูลลูกค้าเรียบร้อยแล้ว");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
