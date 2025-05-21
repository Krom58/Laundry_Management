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
    public partial class Item : Form
    {
        public bool IsEditMode { get; set; }
        public int Quantity { get; set; }
        private decimal unitPrice;
        private string itemNumber; // รหัสสินค้า (ถ้ามี)
        private string itemName;
        private int quantity;
        // เพิ่ม constructor รับราคาและรหัสสินค้า
        public Item(decimal unitPrice, string itemNumber, string itemName, int quantity)
        {
            InitializeComponent();
            this.unitPrice = unitPrice;
            this.itemNumber = itemNumber;
            this.itemName = itemName;
            this.quantity = quantity;
            txtQuantity.Text = quantity.ToString();
        }

        public Item()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("กรุณากรอกจำนวนที่ถูกต้อง");
                return;
            }

            Quantity = quantity;
            decimal totalAmount = unitPrice * quantity;
            string connectionString = "Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;";

            if (IsEditMode)
            {
                // UPDATE เฉพาะแถวเดิม
                string updateQuery = "UPDATE SelectedItems SET ItemName = @itemName, Quantity = @quantity, TotalAmount = @totalAmount WHERE ItemNumber = @itemNumber";
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@itemNumber", itemNumber);
                    command.Parameters.AddWithValue("@itemName", itemName);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@totalAmount", totalAmount);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                // INSERT เฉพาะกรณีเพิ่มใหม่
                string insertQuery = "INSERT INTO SelectedItems (ItemNumber, ItemName, Quantity, TotalAmount) VALUES (@itemNumber, @itemName, @quantity, @totalamount)";
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@itemNumber", itemNumber);
                    command.Parameters.AddWithValue("@itemName", itemName);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@totalamount", totalAmount);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
