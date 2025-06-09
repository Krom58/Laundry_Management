using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Laundry_Management.Laundry
{
    public partial class Sale_Day : Form
    {
        public Sale_Day()
        {
            InitializeComponent();

            // ตั้งค่าเริ่มต้นเป็น 0
            txtDiscount.Text = "0";

            // เพิ่ม event handler สำหรับ chkBaht และ chkPercent
            chkBaht.CheckedChanged += ChkDiscountType_CheckedChanged;
            chkPercent.CheckedChanged += ChkDiscountType_CheckedChanged;

            // ตั้งค่าเริ่มต้นให้ chkBaht เป็น checked
            chkBaht.Checked = true;
        }
        private void ChkDiscountType_CheckedChanged(object sender, EventArgs e)
        {
            // เมื่อ checkbox ใดถูกเปลี่ยนแปลงสถานะ
            CheckBox clickedCheckBox = sender as CheckBox;

            if (clickedCheckBox == chkBaht && clickedCheckBox.Checked)
            {
                // ถ้าติ๊ก chkBaht ให้ยกเลิกการติ๊ก chkPercent
                chkPercent.Checked = false;
            }
            else if (clickedCheckBox == chkPercent && clickedCheckBox.Checked)
            {
                // ถ้าติ๊ก chkPercent ให้ยกเลิกการติ๊ก chkBaht
                chkBaht.Checked = false;
            }

            // ถ้าไม่มีการติ๊กที่ตัวเดิม ให้ติ๊กที่ตัวเองใหม่
            if (!chkBaht.Checked && !chkPercent.Checked)
            {
                clickedCheckBox.Checked = true;
            }
        }
        public bool IsPercentDiscount()
        {
            return chkPercent.Checked;
        }
        public string GetDiscount()
        {
            return txtDiscount.Text;
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // ตรวจสอบว่ามีการติ๊ก checkbox อย่างน้อย 1 อัน
            if (!chkBaht.Checked && !chkPercent.Checked)
            {
                MessageBox.Show("กรุณาเลือกประเภทของส่วนลด (บาท หรือ %)", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ตรวจสอบว่าไม่ได้ติ๊กทั้ง 2 อัน
            if (chkBaht.Checked && chkPercent.Checked)
            {
                MessageBox.Show("กรุณาเลือกประเภทของส่วนลดเพียงประเภทเดียว", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ตรวจสอบว่ามีการกรอกข้อมูลส่วนลดถูกต้อง
            if (!decimal.TryParse(txtDiscount.Text.Trim(), out decimal discountValue))
            {
                MessageBox.Show("กรุณากรอกจำนวนส่วนลดให้ถูกต้อง", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ตรวจสอบว่าค่าส่วนลดไม่ติดลบ
            if (discountValue < 0)
            {
                MessageBox.Show("ส่วนลดต้องมีค่ามากกว่าหรือเท่ากับ 0", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ถ้าเป็นเปอร์เซ็นต์ ตรวจสอบว่าไม่เกิน 100%
            if (chkPercent.Checked && discountValue > 100)
            {
                MessageBox.Show("ส่วนลดเปอร์เซ็นต์ต้องไม่เกิน 100%", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
