namespace Hydrology.Forms
{
    partial class CEvaSetForm
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
            this.rain_numPk = new System.Windows.Forms.NumericUpDown();
            this.kp_lbl = new System.Windows.Forms.Label();
            this.save_btn = new System.Windows.Forms.Button();
            this.quit_btn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.dh_numPk = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.eva_numPk = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.comP_cmb = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.rain_numPk)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dh_numPk)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.eva_numPk)).BeginInit();
            this.SuspendLayout();
            // 
            // rain_numPk
            // 
            this.rain_numPk.DecimalPlaces = 3;
            this.rain_numPk.Location = new System.Drawing.Point(184, 32);
            this.rain_numPk.Name = "rain_numPk";
            this.rain_numPk.Size = new System.Drawing.Size(120, 21);
            this.rain_numPk.TabIndex = 0;
            this.rain_numPk.Value = new decimal(new int[] {
            356,
            0,
            0,
            196608});
            // 
            // kp_lbl
            // 
            this.kp_lbl.AutoSize = true;
            this.kp_lbl.Location = new System.Drawing.Point(59, 34);
            this.kp_lbl.Name = "kp_lbl";
            this.kp_lbl.Size = new System.Drawing.Size(89, 12);
            this.kp_lbl.TabIndex = 1;
            this.kp_lbl.Text = "降雨转换系数：";
            // 
            // save_btn
            // 
            this.save_btn.Location = new System.Drawing.Point(61, 239);
            this.save_btn.Name = "save_btn";
            this.save_btn.Size = new System.Drawing.Size(75, 23);
            this.save_btn.TabIndex = 2;
            this.save_btn.Text = "保存";
            this.save_btn.UseVisualStyleBackColor = true;
            this.save_btn.Click += new System.EventHandler(this.save_btn_Click);
            // 
            // quit_btn
            // 
            this.quit_btn.Location = new System.Drawing.Point(220, 239);
            this.quit_btn.Name = "quit_btn";
            this.quit_btn.Size = new System.Drawing.Size(75, 23);
            this.quit_btn.TabIndex = 3;
            this.quit_btn.Text = "退出";
            this.quit_btn.UseVisualStyleBackColor = true;
            this.quit_btn.Click += new System.EventHandler(this.quit_btn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(59, 134);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "初始高度差：";
            // 
            // dh_numPk
            // 
            this.dh_numPk.DecimalPlaces = 3;
            this.dh_numPk.Location = new System.Drawing.Point(184, 132);
            this.dh_numPk.Name = "dh_numPk";
            this.dh_numPk.Size = new System.Drawing.Size(120, 21);
            this.dh_numPk.TabIndex = 4;
            this.dh_numPk.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(59, 81);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 12);
            this.label2.TabIndex = 7;
            this.label2.Text = "蒸发转换系数：";
            // 
            // eva_numPk
            // 
            this.eva_numPk.DecimalPlaces = 3;
            this.eva_numPk.Location = new System.Drawing.Point(184, 79);
            this.eva_numPk.Name = "eva_numPk";
            this.eva_numPk.Size = new System.Drawing.Size(120, 21);
            this.eva_numPk.TabIndex = 6;
            this.eva_numPk.Value = new decimal(new int[] {
            1037,
            0,
            0,
            196608});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(59, 185);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 12);
            this.label3.TabIndex = 9;
            this.label3.Text = "降雨补偿：";
            // 
            // comP_cmb
            // 
            this.comP_cmb.FormattingEnabled = true;
            this.comP_cmb.Location = new System.Drawing.Point(184, 182);
            this.comP_cmb.Name = "comP_cmb";
            this.comP_cmb.Size = new System.Drawing.Size(121, 20);
            this.comP_cmb.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(310, 141);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(17, 12);
            this.label4.TabIndex = 11;
            this.label4.Text = "mm";
            // 
            // CEvaSetForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(371, 298);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.comP_cmb);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.eva_numPk);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dh_numPk);
            this.Controls.Add(this.quit_btn);
            this.Controls.Add(this.save_btn);
            this.Controls.Add(this.kp_lbl);
            this.Controls.Add(this.rain_numPk);
            this.Name = "CEvaSetForm";
            this.Text = "蒸发参数设置";
            ((System.ComponentModel.ISupportInitialize)(this.rain_numPk)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dh_numPk)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eva_numPk)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown rain_numPk;
        private System.Windows.Forms.Label kp_lbl;
        private System.Windows.Forms.Button save_btn;
        private System.Windows.Forms.Button quit_btn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown dh_numPk;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown eva_numPk;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comP_cmb;
        private System.Windows.Forms.Label label4;
    }
}