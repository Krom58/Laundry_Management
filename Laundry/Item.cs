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
        // Define the enum for source form
        public enum CallingForm
        {
            Service,
            ModifyServiceItem
        }

        // Property to track which form called this Item form
        public CallingForm SourceForm { get; set; } = CallingForm.Service; // Default to Service

        public string ItemNumber { get; set; } // Add this property
        public string ItemName { get; set; }   // Add this property
        public decimal TotalAmount { get; set; }
        public bool IsEditMode { get; set; }
        public int Quantity { get; set; }
        public int OrderItemId { get; set; } // Added property to store OrderItemID
        private decimal unitPrice;
        private string itemNumber; // รหัสสินค้า (ถ้ามี)
        private string itemName;
        private int quantity;
        // เพิ่ม constructor รับราคาและรหัสสินค้า

        public Item(decimal unitPrice, int OrderItemId, string itemNumber, string itemName, int quantity)
        {
            InitializeComponent();

            // แก้ไข TabIndex เพื่อให้ปุ่ม OK ได้รับโฟกัส
            btnOk.TabIndex = 0;
            btnCancle.TabIndex = 1;
            txtQuantity.TabIndex = 0;  // ให้ txtQuantity มี TabIndex เท่ากับ btnOk เพื่อให้ได้รับโฟกัสก่อน

            this.unitPrice = unitPrice;
            this.OrderItemId = OrderItemId;
            this.itemNumber = itemNumber;
            this.itemName = itemName;
            this.quantity = quantity;
            txtQuantity.Text = quantity.ToString();

            // กำหนดให้ปุ่ม OK เป็น AcceptButton ของฟอร์ม (เมื่อกด Enter จะทำงานเหมือนกดปุ่มนี้)
            this.AcceptButton = btnOk;

            // กำหนดให้ปุ่ม Cancel เป็น CancelButton ของฟอร์ม (เมื่อกด Esc จะทำงานเหมือนกดปุ่มนี้)
            this.CancelButton = btnCancle;

            // เพิ่ม event handler สำหรับการกด Enter ในช่อง txtQuantity
            txtQuantity.KeyPress += TxtSearch_KeyPress;

            // เพิ่ม event handler สำหรับการโหลดฟอร์ม
            this.Load += Item_Load;

            // ตั้งค่าให้ฟอร์มรับการกด Enter
            this.KeyPreview = true;
        }

        private void Item_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ตรวจสอบว่ากด Enter หรือไม่
            if (e.KeyChar == (char)Keys.Enter)
            {
                // ป้องกันเสียง beep และไม่ให้ event ไปทำงานที่ control อื่น
                e.Handled = true;

                // เรียกฟังก์ชั่นของปุ่ม OK โดยตรง
                ProcessQuantityAndClose();
            }
        }

        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ตรวจสอบว่ากด Enter หรือไม่ (รหัส ASCII 13)
            if (e.KeyChar == (char)Keys.Enter)
            {
                // ป้องกันเสียง beep
                e.Handled = true;

                // เรียกฟังก์ชั่นประมวลผลโดยตรง
                ProcessQuantityAndClose();
            }
        }

        public Item()
        {
            InitializeComponent();
            // แก้ไข TabIndex เพื่อให้ปุ่ม OK ได้รับโฟกัส
            btnOk.TabIndex = 0;
            btnCancle.TabIndex = 1;
            txtQuantity.TabIndex = 0;

            // กำหนดให้ปุ่ม OK เป็น AcceptButton ของฟอร์ม
            this.AcceptButton = btnOk;

            // กำหนดให้ปุ่ม Cancel เป็น CancelButton ของฟอร์ม
            this.CancelButton = btnCancle;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            ProcessQuantityAndClose();
        }

        private void ProcessQuantityAndClose()
        {
            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("กรุณากรอกจำนวนที่ถูกต้อง", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQuantity.Focus();
                txtQuantity.SelectAll();
                return;
            }

            // กำหนดค่า Quantity สำหรับส่งกลับไปยังฟอร์มที่เรียก
            this.Quantity = quantity;

            // คำนวณยอดรวม
            decimal totalAmount = unitPrice * quantity;
            this.TotalAmount = totalAmount;

            try
            {
                // ตรวจสอบว่ามาจาก Modify_Service_Item หรือไม่
                if (SourceForm == CallingForm.ModifyServiceItem)
                {
                    // ถ้ามาจาก Modify_Service_Item ไม่ต้องบันทึกลงฐานข้อมูล
                    // แค่ส่งค่า Quantity และ TotalAmount กลับไปที่ฟอร์มหลัก
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }

                // ถ้าไม่ได้มาจาก Modify_Service_Item ให้ทำงานตามปกติ
                if (IsEditMode)
                {
                    // มาจาก Service ให้อัพเดตที่ SelectedItems
                    string updateQuery = "UPDATE SelectedItems SET ItemName = @itemName, Quantity = @quantity, TotalAmount = @totalAmount WHERE ItemNumber = @itemNumber";
                    using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@itemName", itemName);
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.Parameters.AddWithValue("@totalAmount", totalAmount);
                        command.Parameters.AddWithValue("@itemNumber", itemNumber);
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            throw new Exception("ไม่สามารถอัพเดทข้อมูลได้ ไม่พบรายการที่ต้องการแก้ไข (ItemNumber: " + itemNumber + ")");
                        }
                    }
                }
                else
                {
                    // กรณีเพิ่มใหม่ ให้ทำเหมือนเดิม (เพิ่มที่ SelectedItems)
                    string insertQuery = "INSERT INTO SelectedItems (ItemNumber, ItemName, Quantity, TotalAmount) VALUES (@itemNumber, @itemName, @quantity, @totalamount)";
                    using (SqlConnection connection = Laundry_Management.Laundry.DBconfig.GetConnection())
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

                // กำหนด DialogResult เพื่อให้ฟอร์มที่เรียกรู้ว่าดำเนินการสำเร็จ
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการบันทึกข้อมูล: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // ให้ focus กลับมาที่ช่องกรอกจำนวน
                txtQuantity.Focus();
                txtQuantity.SelectAll();
            }
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void Item_Load(object sender, EventArgs e)
        {
            // ให้ focus ไปที่ช่อง txtQuantity ทันทีที่เปิดฟอร์ม
            txtQuantity.Focus();

            // เลือกข้อความทั้งหมดเพื่อให้พร้อมแก้ไข
            txtQuantity.SelectAll();
        }
    }
}
