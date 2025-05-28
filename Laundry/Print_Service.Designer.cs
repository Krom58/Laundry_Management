namespace Laundry_Management.Laundry
{
    partial class Print_Service
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
            this.PrintDoc = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PrintDoc
            // 
            this.PrintDoc.Font = new System.Drawing.Font("Angsana New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PrintDoc.Location = new System.Drawing.Point(323, 208);
            this.PrintDoc.Name = "PrintDoc";
            this.PrintDoc.Size = new System.Drawing.Size(323, 138);
            this.PrintDoc.TabIndex = 21;
            this.PrintDoc.Text = "สั่งพิมพ์";
            this.PrintDoc.UseVisualStyleBackColor = true;
            this.PrintDoc.Click += new System.EventHandler(this.PrintDoc_Click);
            // 
            // Print_Service
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 611);
            this.Controls.Add(this.PrintDoc);
            this.Name = "Print_Service";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "พิมพ์ใบรับผ้า";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button PrintDoc;
    }
}