namespace Laundry_Management.Laundry
{
    partial class Find_Service
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnSearch = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.dtpCreateDate = new System.Windows.Forms.DateTimePicker();
            this.txtOrderId = new System.Windows.Forms.TextBox();
            this.Back = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtCustomerFilter = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.dgvItems = new System.Windows.Forms.DataGridView();
            this.dgvOrders = new System.Windows.Forms.DataGridView();
            this.btnPrintReceipt = new System.Windows.Forms.Button();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtDiscount = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.lblTotal = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.lblPaymentamount = new System.Windows.Forms.Label();
            this.chkPercent = new System.Windows.Forms.CheckBox();
            this.chkBaht = new System.Windows.Forms.CheckBox();
            this.chkDebit = new System.Windows.Forms.CheckBox();
            this.chkCash = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.lblVat = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.chkQRCode = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSearch
            // 
            this.btnSearch.AutoSize = true;
            this.btnSearch.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSearch.Location = new System.Drawing.Point(398, 109);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(144, 53);
            this.btnSearch.TabIndex = 31;
            this.btnSearch.Text = "ค้นหา";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(40, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(130, 43);
            this.label1.TabIndex = 30;
            this.label1.Text = "รหัสใบรับผ้า";
            // 
            // dtpCreateDate
            // 
            this.dtpCreateDate.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpCreateDate.Location = new System.Drawing.Point(105, 111);
            this.dtpCreateDate.Name = "dtpCreateDate";
            this.dtpCreateDate.Size = new System.Drawing.Size(245, 51);
            this.dtpCreateDate.TabIndex = 40;
            // 
            // txtOrderId
            // 
            this.txtOrderId.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOrderId.Location = new System.Drawing.Point(176, 36);
            this.txtOrderId.Name = "txtOrderId";
            this.txtOrderId.Size = new System.Drawing.Size(174, 51);
            this.txtOrderId.TabIndex = 41;
            // 
            // Back
            // 
            this.Back.AutoSize = true;
            this.Back.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Back.Location = new System.Drawing.Point(1748, 12);
            this.Back.Name = "Back";
            this.Back.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Back.Size = new System.Drawing.Size(144, 53);
            this.Back.TabIndex = 42;
            this.Back.Text = "กลับ";
            this.Back.UseVisualStyleBackColor = true;
            this.Back.Click += new System.EventHandler(this.Back_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(376, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 43);
            this.label2.TabIndex = 44;
            this.label2.Text = "ชื่อ-นามสกุล";
            // 
            // txtCustomerFilter
            // 
            this.txtCustomerFilter.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCustomerFilter.Location = new System.Drawing.Point(511, 36);
            this.txtCustomerFilter.Name = "txtCustomerFilter";
            this.txtCustomerFilter.Size = new System.Drawing.Size(516, 51);
            this.txtCustomerFilter.TabIndex = 43;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(40, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 43);
            this.label3.TabIndex = 45;
            this.label3.Text = "วันที่";
            // 
            // dgvItems
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvItems.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvItems.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvItems.Location = new System.Drawing.Point(585, 304);
            this.dgvItems.Name = "dgvItems";
            this.dgvItems.Size = new System.Drawing.Size(1321, 738);
            this.dgvItems.TabIndex = 47;
            // 
            // dgvOrders
            // 
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvOrders.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvOrders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvOrders.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvOrders.Location = new System.Drawing.Point(0, 304);
            this.dgvOrders.Name = "dgvOrders";
            this.dgvOrders.ReadOnly = true;
            this.dgvOrders.Size = new System.Drawing.Size(579, 738);
            this.dgvOrders.TabIndex = 46;
            this.dgvOrders.SelectionChanged += new System.EventHandler(this.dgvOrders_SelectionChanged);
            // 
            // btnPrintReceipt
            // 
            this.btnPrintReceipt.AutoSize = true;
            this.btnPrintReceipt.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPrintReceipt.Location = new System.Drawing.Point(1748, 245);
            this.btnPrintReceipt.Name = "btnPrintReceipt";
            this.btnPrintReceipt.Size = new System.Drawing.Size(144, 53);
            this.btnPrintReceipt.TabIndex = 48;
            this.btnPrintReceipt.Text = "พิมพ์ใบเสร็จ";
            this.btnPrintReceipt.UseVisualStyleBackColor = true;
            this.btnPrintReceipt.Click += new System.EventHandler(this.btnPrintReceipt_Click);
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.AutoSize = true;
            this.btnSaveChanges.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveChanges.Location = new System.Drawing.Point(1566, 245);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(148, 53);
            this.btnSaveChanges.TabIndex = 49;
            this.btnSaveChanges.Text = "ยกเลิกรายการ";
            this.btnSaveChanges.UseVisualStyleBackColor = true;
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
            // 
            // button1
            // 
            this.button1.AutoSize = true;
            this.button1.Enabled = false;
            this.button1.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(1748, 87);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(144, 53);
            this.button1.TabIndex = 50;
            this.button1.Text = "รีเฟรชข้อมูล";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Visible = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(1161, 35);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(140, 43);
            this.label4.TabIndex = 51;
            this.label4.Text = "ชื่อ-นามสกุล :";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(1307, 35);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(26, 43);
            this.label5.TabIndex = 52;
            this.label5.Text = "-";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(603, 168);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(96, 43);
            this.label6.TabIndex = 53;
            this.label6.Text = "ส่วนลด :";
            // 
            // txtDiscount
            // 
            this.txtDiscount.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDiscount.Location = new System.Drawing.Point(705, 165);
            this.txtDiscount.Name = "txtDiscount";
            this.txtDiscount.Size = new System.Drawing.Size(130, 51);
            this.txtDiscount.TabIndex = 54;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(1161, 78);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(107, 43);
            this.label7.TabIndex = 55;
            this.label7.Text = "ราคารวม :";
            // 
            // lblTotal
            // 
            this.lblTotal.AutoSize = true;
            this.lblTotal.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotal.Location = new System.Drawing.Point(1329, 78);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Size = new System.Drawing.Size(26, 43);
            this.lblTotal.TabIndex = 56;
            this.lblTotal.Text = "-";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(1161, 250);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(105, 43);
            this.label9.TabIndex = 57;
            this.label9.Text = "รวมสุทธิ :";
            // 
            // lblPaymentamount
            // 
            this.lblPaymentamount.AutoSize = true;
            this.lblPaymentamount.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPaymentamount.Location = new System.Drawing.Point(1329, 250);
            this.lblPaymentamount.Name = "lblPaymentamount";
            this.lblPaymentamount.Size = new System.Drawing.Size(26, 43);
            this.lblPaymentamount.TabIndex = 58;
            this.lblPaymentamount.Text = "-";
            // 
            // chkPercent
            // 
            this.chkPercent.AutoSize = true;
            this.chkPercent.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPercent.Location = new System.Drawing.Point(963, 167);
            this.chkPercent.Name = "chkPercent";
            this.chkPercent.Size = new System.Drawing.Size(56, 47);
            this.chkPercent.TabIndex = 60;
            this.chkPercent.Text = "%";
            this.chkPercent.UseVisualStyleBackColor = true;
            // 
            // chkBaht
            // 
            this.chkBaht.AutoSize = true;
            this.chkBaht.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkBaht.Location = new System.Drawing.Point(864, 167);
            this.chkBaht.Name = "chkBaht";
            this.chkBaht.Size = new System.Drawing.Size(75, 47);
            this.chkBaht.TabIndex = 59;
            this.chkBaht.Text = "บาท";
            this.chkBaht.UseVisualStyleBackColor = true;
            // 
            // chkDebit
            // 
            this.chkDebit.AutoSize = true;
            this.chkDebit.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDebit.Location = new System.Drawing.Point(901, 249);
            this.chkDebit.Name = "chkDebit";
            this.chkDebit.Size = new System.Drawing.Size(197, 47);
            this.chkDebit.TabIndex = 62;
            this.chkDebit.Text = "จ่ายด้วยบัตรเครดิต";
            this.chkDebit.UseVisualStyleBackColor = true;
            // 
            // chkCash
            // 
            this.chkCash.AutoSize = true;
            this.chkCash.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkCash.Location = new System.Drawing.Point(734, 249);
            this.chkCash.Name = "chkCash";
            this.chkCash.Size = new System.Drawing.Size(161, 47);
            this.chkCash.TabIndex = 61;
            this.chkCash.Text = "จ่ายด้วยเงินสด";
            this.chkCash.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(1161, 121);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(37, 43);
            this.label11.TabIndex = 63;
            this.label11.Text = "- :";
            // 
            // lblDiscount
            // 
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDiscount.Location = new System.Drawing.Point(1329, 121);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(26, 43);
            this.lblDiscount.TabIndex = 64;
            this.lblDiscount.Text = "-";
            // 
            // lblVat
            // 
            this.lblVat.AutoSize = true;
            this.lblVat.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVat.Location = new System.Drawing.Point(1329, 207);
            this.lblVat.Name = "lblVat";
            this.lblVat.Size = new System.Drawing.Size(26, 43);
            this.lblVat.TabIndex = 68;
            this.lblVat.Text = "-";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(1161, 207);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(73, 43);
            this.label16.TabIndex = 67;
            this.label16.Text = "VAT :";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(1161, 164);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(162, 43);
            this.label8.TabIndex = 69;
            this.label8.Text = "หลังหักส่วนลด :";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(1329, 164);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(26, 43);
            this.label10.TabIndex = 70;
            this.label10.Text = "-";
            // 
            // chkQRCode
            // 
            this.chkQRCode.AutoSize = true;
            this.chkQRCode.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkQRCode.Location = new System.Drawing.Point(611, 249);
            this.chkQRCode.Name = "chkQRCode";
            this.chkQRCode.Size = new System.Drawing.Size(117, 47);
            this.chkQRCode.TabIndex = 71;
            this.chkQRCode.Text = "QR Code";
            this.chkQRCode.UseVisualStyleBackColor = true;
            // 
            // Find_Service
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1904, 1041);
            this.Controls.Add(this.chkQRCode);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.lblVat);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.lblDiscount);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.chkDebit);
            this.Controls.Add(this.chkCash);
            this.Controls.Add(this.chkPercent);
            this.Controls.Add(this.chkBaht);
            this.Controls.Add(this.lblPaymentamount);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.lblTotal);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtDiscount);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnSaveChanges);
            this.Controls.Add(this.btnPrintReceipt);
            this.Controls.Add(this.dgvItems);
            this.Controls.Add(this.dgvOrders);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtCustomerFilter);
            this.Controls.Add(this.Back);
            this.Controls.Add(this.txtOrderId);
            this.Controls.Add(this.dtpCreateDate);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.label1);
            this.Name = "Find_Service";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "เรียกใบรับผ้า";
            this.Load += new System.EventHandler(this.Find_Service_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpCreateDate;
        private System.Windows.Forms.TextBox txtOrderId;
        private System.Windows.Forms.Button Back;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtCustomerFilter;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dgvItems;
        private System.Windows.Forms.DataGridView dgvOrders;
        private System.Windows.Forms.Button btnPrintReceipt;
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtDiscount;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblPaymentamount;
        private System.Windows.Forms.CheckBox chkPercent;
        private System.Windows.Forms.CheckBox chkBaht;
        private System.Windows.Forms.CheckBox chkDebit;
        private System.Windows.Forms.CheckBox chkCash;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label lblDiscount;
        private System.Windows.Forms.Label lblVat;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox chkQRCode;
    }
}