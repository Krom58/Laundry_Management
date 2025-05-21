namespace Laundry_Management
{
    partial class Main
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
            this.btnAdd_Type_Service = new System.Windows.Forms.Button();
            this.btnService = new System.Windows.Forms.Button();
            this.btnCustomer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnAdd_Type_Service
            // 
            this.btnAdd_Type_Service.AutoSize = true;
            this.btnAdd_Type_Service.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAdd_Type_Service.Location = new System.Drawing.Point(12, 505);
            this.btnAdd_Type_Service.Name = "btnAdd_Type_Service";
            this.btnAdd_Type_Service.Size = new System.Drawing.Size(282, 109);
            this.btnAdd_Type_Service.TabIndex = 36;
            this.btnAdd_Type_Service.Text = "เพิ่มประเภทการซัก";
            this.btnAdd_Type_Service.UseVisualStyleBackColor = true;
            this.btnAdd_Type_Service.Click += new System.EventHandler(this.btnAdd_Type_Service_Click);
            // 
            // btnService
            // 
            this.btnService.AutoSize = true;
            this.btnService.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnService.Location = new System.Drawing.Point(1632, 505);
            this.btnService.Name = "btnService";
            this.btnService.Size = new System.Drawing.Size(260, 109);
            this.btnService.TabIndex = 37;
            this.btnService.Text = "สร้างรายการ";
            this.btnService.UseVisualStyleBackColor = true;
            this.btnService.Click += new System.EventHandler(this.btnService_Click);
            // 
            // btnCustomer
            // 
            this.btnCustomer.AutoSize = true;
            this.btnCustomer.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCustomer.Location = new System.Drawing.Point(404, 505);
            this.btnCustomer.Name = "btnCustomer";
            this.btnCustomer.Size = new System.Drawing.Size(260, 109);
            this.btnCustomer.TabIndex = 38;
            this.btnCustomer.Text = "เพิ่มรายชื่อลูกค้า";
            this.btnCustomer.UseVisualStyleBackColor = true;
            this.btnCustomer.Click += new System.EventHandler(this.btnCustomer_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1904, 1041);
            this.Controls.Add(this.btnCustomer);
            this.Controls.Add(this.btnService);
            this.Controls.Add(this.btnAdd_Type_Service);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Main";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAdd_Type_Service;
        private System.Windows.Forms.Button btnService;
        private System.Windows.Forms.Button btnCustomer;
    }
}