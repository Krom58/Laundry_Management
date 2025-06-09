namespace Laundry_Management.Laundry
{
    partial class Sale_Day
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtDiscount = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkBaht = new System.Windows.Forms.CheckBox();
            this.chkPercent = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancle = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtDiscount
            // 
            this.txtDiscount.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDiscount.Location = new System.Drawing.Point(400, 206);
            this.txtDiscount.Name = "txtDiscount";
            this.txtDiscount.Size = new System.Drawing.Size(150, 51);
            this.txtDiscount.TabIndex = 48;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(278, 209);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 43);
            this.label3.TabIndex = 47;
            this.label3.Text = "ส่วนลด";
            // 
            // chkBaht
            // 
            this.chkBaht.AutoSize = true;
            this.chkBaht.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkBaht.Location = new System.Drawing.Point(580, 208);
            this.chkBaht.Name = "chkBaht";
            this.chkBaht.Size = new System.Drawing.Size(75, 47);
            this.chkBaht.TabIndex = 49;
            this.chkBaht.Text = "บาท";
            this.chkBaht.UseVisualStyleBackColor = true;
            // 
            // chkPercent
            // 
            this.chkPercent.AutoSize = true;
            this.chkPercent.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPercent.Location = new System.Drawing.Point(679, 208);
            this.chkPercent.Name = "chkPercent";
            this.chkPercent.Size = new System.Drawing.Size(56, 47);
            this.chkPercent.TabIndex = 50;
            this.chkPercent.Text = "%";
            this.chkPercent.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.AutoSize = true;
            this.btnOk.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOk.Location = new System.Drawing.Point(733, 490);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(239, 109);
            this.btnOk.TabIndex = 52;
            this.btnOk.Text = "ตกลง";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancle
            // 
            this.btnCancle.AutoSize = true;
            this.btnCancle.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancle.Location = new System.Drawing.Point(12, 490);
            this.btnCancle.Name = "btnCancle";
            this.btnCancle.Size = new System.Drawing.Size(239, 109);
            this.btnCancle.TabIndex = 51;
            this.btnCancle.Text = "ยกเลิก";
            this.btnCancle.UseVisualStyleBackColor = true;
            this.btnCancle.Click += new System.EventHandler(this.btnCancle_Click);
            // 
            // Sale_Day
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 611);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancle);
            this.Controls.Add(this.chkPercent);
            this.Controls.Add(this.chkBaht);
            this.Controls.Add(this.txtDiscount);
            this.Controls.Add(this.label3);
            this.Name = "Sale_Day";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ส่วนลดของวัน";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtDiscount;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkBaht;
        private System.Windows.Forms.CheckBox chkPercent;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancle;
    }
}