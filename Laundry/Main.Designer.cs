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
            this.btnFind_Service = new System.Windows.Forms.Button();
            this.Check_List = new System.Windows.Forms.Button();
            this.Pickup_List = new System.Windows.Forms.Button();
            this.btnSetting = new System.Windows.Forms.Button();
            this.Report = new System.Windows.Forms.Button();
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
            this.btnService.Text = "สร้างใบรับผ้า";
            this.btnService.UseVisualStyleBackColor = true;
            this.btnService.Click += new System.EventHandler(this.btnService_Click);
            // 
            // btnCustomer
            // 
            this.btnCustomer.AutoSize = true;
            this.btnCustomer.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCustomer.Location = new System.Drawing.Point(404, 505);
            this.btnCustomer.Name = "btnCustomer";
            this.btnCustomer.Size = new System.Drawing.Size(282, 109);
            this.btnCustomer.TabIndex = 38;
            this.btnCustomer.Text = "เพิ่มรายชื่อลูกค้า";
            this.btnCustomer.UseVisualStyleBackColor = true;
            this.btnCustomer.Click += new System.EventHandler(this.btnCustomer_Click);
            // 
            // btnFind_Service
            // 
            this.btnFind_Service.AutoSize = true;
            this.btnFind_Service.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFind_Service.Location = new System.Drawing.Point(12, 680);
            this.btnFind_Service.Name = "btnFind_Service";
            this.btnFind_Service.Size = new System.Drawing.Size(282, 109);
            this.btnFind_Service.TabIndex = 39;
            this.btnFind_Service.Text = "ออกใบเสร็จ";
            this.btnFind_Service.UseVisualStyleBackColor = true;
            this.btnFind_Service.Click += new System.EventHandler(this.btnFind_Service_Click);
            // 
            // Check_List
            // 
            this.Check_List.AutoSize = true;
            this.Check_List.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Check_List.Location = new System.Drawing.Point(404, 680);
            this.Check_List.Name = "Check_List";
            this.Check_List.Size = new System.Drawing.Size(282, 109);
            this.Check_List.TabIndex = 40;
            this.Check_List.Text = "รายงานการขาย";
            this.Check_List.UseVisualStyleBackColor = true;
            this.Check_List.Click += new System.EventHandler(this.Check_List_Click);
            // 
            // Pickup_List
            // 
            this.Pickup_List.AutoSize = true;
            this.Pickup_List.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Pickup_List.Location = new System.Drawing.Point(807, 680);
            this.Pickup_List.Name = "Pickup_List";
            this.Pickup_List.Size = new System.Drawing.Size(282, 109);
            this.Pickup_List.TabIndex = 41;
            this.Pickup_List.Text = "เช็คการมารับผ้า";
            this.Pickup_List.UseVisualStyleBackColor = true;
            this.Pickup_List.Click += new System.EventHandler(this.Pickup_List_Click);
            // 
            // btnSetting
            // 
            this.btnSetting.AutoSize = true;
            this.btnSetting.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSetting.Location = new System.Drawing.Point(807, 505);
            this.btnSetting.Name = "btnSetting";
            this.btnSetting.Size = new System.Drawing.Size(282, 109);
            this.btnSetting.TabIndex = 42;
            this.btnSetting.Text = "ตั้งค่า";
            this.btnSetting.UseVisualStyleBackColor = true;
            this.btnSetting.Click += new System.EventHandler(this.btnSetting_Click);
            // 
            // Report
            // 
            this.Report.AutoSize = true;
            this.Report.Font = new System.Drawing.Font("Angsana New", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Report.Location = new System.Drawing.Point(1188, 680);
            this.Report.Name = "Report";
            this.Report.Size = new System.Drawing.Size(282, 109);
            this.Report.TabIndex = 43;
            this.Report.Text = "รายงานการขาย";
            this.Report.UseVisualStyleBackColor = true;
            this.Report.Click += new System.EventHandler(this.Report_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1904, 1041);
            this.Controls.Add(this.Report);
            this.Controls.Add(this.btnSetting);
            this.Controls.Add(this.Pickup_List);
            this.Controls.Add(this.Check_List);
            this.Controls.Add(this.btnFind_Service);
            this.Controls.Add(this.btnCustomer);
            this.Controls.Add(this.btnService);
            this.Controls.Add(this.btnAdd_Type_Service);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "หน้าหลัก";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAdd_Type_Service;
        private System.Windows.Forms.Button btnService;
        private System.Windows.Forms.Button btnCustomer;
        private System.Windows.Forms.Button btnFind_Service;
        private System.Windows.Forms.Button Check_List;
        private System.Windows.Forms.Button Pickup_List;
        private System.Windows.Forms.Button btnSetting;
        private System.Windows.Forms.Button Report;
    }
}