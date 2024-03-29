﻿namespace Hydrology.Forms
{
    partial class CBatchFlashMgrForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CBatchFlashMgrForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.panel3 = new System.Windows.Forms.Panel();
            this.radioSD = new System.Windows.Forms.RadioButton();
            this.radioWater = new System.Windows.Forms.RadioButton();
            this.radioRain = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.MonthBtb = new System.Windows.Forms.RadioButton();
            this.dtp_EndTime = new System.Windows.Forms.DateTimePicker();
            this.dtp_StartTime = new System.Windows.Forms.DateTimePicker();
            this.radioDay = new System.Windows.Forms.RadioButton();
            this.radioHour = new System.Windows.Forms.RadioButton();
            this.cmbStation = new System.Windows.Forms.ComboBox();
            this.lbl_EndTime = new System.Windows.Forms.Label();
            this.lbl_StartTime = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButtonSave = new System.Windows.Forms.RadioButton();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnStartTrans = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.panel3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tableLayoutPanel1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(434, 562);
            this.panel1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.groupBox3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.groupBox2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(434, 562);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radioButton2);
            this.groupBox3.Controls.Add(this.listView1);
            this.groupBox3.Controls.Add(this.panel3);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(3, 238);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(428, 321);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "返回信息";
            // 
            // listView1
            // 
            this.listView1.BackColor = System.Drawing.SystemColors.Info;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.Location = new System.Drawing.Point(3, 17);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(422, 301);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.radioSD);
            this.panel3.Controls.Add(this.radioButtonSave);
            this.panel3.Controls.Add(this.radioWater);
            this.panel3.Controls.Add(this.radioRain);
            this.panel3.Controls.Add(this.label1);
            this.panel3.Location = new System.Drawing.Point(6, 20);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(332, 29);
            this.panel3.TabIndex = 12;
            this.panel3.Visible = false;
            // 
            // radioSD
            // 
            this.radioSD.AutoSize = true;
            this.radioSD.Location = new System.Drawing.Point(244, 8);
            this.radioSD.Name = "radioSD";
            this.radioSD.Size = new System.Drawing.Size(47, 16);
            this.radioSD.TabIndex = 13;
            this.radioSD.Text = "SD卡";
            this.radioSD.UseVisualStyleBackColor = true;
            this.radioSD.Visible = false;
            // 
            // radioWater
            // 
            this.radioWater.AutoSize = true;
            this.radioWater.Checked = true;
            this.radioWater.Location = new System.Drawing.Point(99, 8);
            this.radioWater.Name = "radioWater";
            this.radioWater.Size = new System.Drawing.Size(47, 16);
            this.radioWater.TabIndex = 10;
            this.radioWater.TabStop = true;
            this.radioWater.Text = "蒸发";
            this.radioWater.UseVisualStyleBackColor = true;
            this.radioWater.Visible = false;
            this.radioWater.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // radioRain
            // 
            this.radioRain.AutoSize = true;
            this.radioRain.Location = new System.Drawing.Point(186, 8);
            this.radioRain.Name = "radioRain";
            this.radioRain.Size = new System.Drawing.Size(47, 16);
            this.radioRain.TabIndex = 11;
            this.radioRain.Text = "雨量";
            this.radioRain.UseVisualStyleBackColor = true;
            this.radioRain.Visible = false;
            this.radioRain.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 12);
            this.label1.TabIndex = 9;
            this.label1.Text = "报类：";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.MonthBtb);
            this.groupBox2.Controls.Add(this.dtp_EndTime);
            this.groupBox2.Controls.Add(this.dtp_StartTime);
            this.groupBox2.Controls.Add(this.radioDay);
            this.groupBox2.Controls.Add(this.radioHour);
            this.groupBox2.Controls.Add(this.cmbStation);
            this.groupBox2.Controls.Add(this.lbl_EndTime);
            this.groupBox2.Controls.Add(this.lbl_StartTime);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(428, 194);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "配置";
            // 
            // MonthBtb
            // 
            this.MonthBtb.AutoSize = true;
            this.MonthBtb.Location = new System.Drawing.Point(171, 125);
            this.MonthBtb.Name = "MonthBtb";
            this.MonthBtb.Size = new System.Drawing.Size(95, 16);
            this.MonthBtb.TabIndex = 13;
            this.MonthBtb.Text = "最近一月记录";
            this.MonthBtb.UseVisualStyleBackColor = true;
            // 
            // dtp_EndTime
            // 
            this.dtp_EndTime.Location = new System.Drawing.Point(171, 173);
            this.dtp_EndTime.Name = "dtp_EndTime";
            this.dtp_EndTime.Size = new System.Drawing.Size(167, 21);
            this.dtp_EndTime.TabIndex = 8;
            this.dtp_EndTime.Visible = false;
            // 
            // dtp_StartTime
            // 
            this.dtp_StartTime.CustomFormat = "\"\"";
            this.dtp_StartTime.Location = new System.Drawing.Point(171, 161);
            this.dtp_StartTime.Name = "dtp_StartTime";
            this.dtp_StartTime.Size = new System.Drawing.Size(167, 21);
            this.dtp_StartTime.TabIndex = 7;
            // 
            // radioDay
            // 
            this.radioDay.AutoSize = true;
            this.radioDay.Checked = true;
            this.radioDay.Location = new System.Drawing.Point(171, 81);
            this.radioDay.Name = "radioDay";
            this.radioDay.Size = new System.Drawing.Size(101, 16);
            this.radioDay.TabIndex = 6;
            this.radioDay.TabStop = true;
            this.radioDay.Text = "指定日8时数据";
            this.radioDay.UseVisualStyleBackColor = true;
            this.radioDay.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // radioHour
            // 
            this.radioHour.AutoSize = true;
            this.radioHour.Location = new System.Drawing.Point(171, 103);
            this.radioHour.Name = "radioHour";
            this.radioHour.Size = new System.Drawing.Size(83, 16);
            this.radioHour.TabIndex = 5;
            this.radioHour.Text = "排注水记录";
            this.radioHour.UseVisualStyleBackColor = true;
            this.radioHour.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // cmbStation
            // 
            this.cmbStation.FormattingEnabled = true;
            this.cmbStation.Location = new System.Drawing.Point(172, 18);
            this.cmbStation.Name = "cmbStation";
            this.cmbStation.Size = new System.Drawing.Size(167, 20);
            this.cmbStation.TabIndex = 4;
            // 
            // lbl_EndTime
            // 
            this.lbl_EndTime.AutoSize = true;
            this.lbl_EndTime.Location = new System.Drawing.Point(73, 179);
            this.lbl_EndTime.Name = "lbl_EndTime";
            this.lbl_EndTime.Size = new System.Drawing.Size(65, 12);
            this.lbl_EndTime.TabIndex = 3;
            this.lbl_EndTime.Text = "结束时间：";
            this.lbl_EndTime.Visible = false;
            // 
            // lbl_StartTime
            // 
            this.lbl_StartTime.AutoSize = true;
            this.lbl_StartTime.Location = new System.Drawing.Point(73, 167);
            this.lbl_StartTime.Name = "lbl_StartTime";
            this.lbl_StartTime.Size = new System.Drawing.Size(65, 12);
            this.lbl_StartTime.TabIndex = 2;
            this.lbl_StartTime.Text = "数据时间：";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(73, 83);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(65, 12);
            this.label8.TabIndex = 1;
            this.label8.Text = "传输方式：";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(73, 24);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 12);
            this.label9.TabIndex = 0;
            this.label9.Text = "选择测站：";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnClear);
            this.panel2.Controls.Add(this.btnStartTrans);
            this.panel2.Controls.Add(this.btnExit);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 203);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(428, 29);
            this.panel2.TabIndex = 7;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(326, 28);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(47, 16);
            this.radioButton2.TabIndex = 12;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "存库";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.Visible = false;
            // 
            // radioButtonSave
            // 
            this.radioButtonSave.AutoSize = true;
            this.radioButtonSave.Location = new System.Drawing.Point(255, 0);
            this.radioButtonSave.Name = "radioButtonSave";
            this.radioButtonSave.Size = new System.Drawing.Size(59, 16);
            this.radioButtonSave.TabIndex = 11;
            this.radioButtonSave.TabStop = true;
            this.radioButtonSave.Text = "存文本";
            this.radioButtonSave.UseVisualStyleBackColor = true;
            this.radioButtonSave.Visible = false;
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(49, 6);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(99, 23);
            this.btnClear.TabIndex = 10;
            this.btnClear.Text = "清空返回信息";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnStartTrans
            // 
            this.btnStartTrans.Location = new System.Drawing.Point(172, 6);
            this.btnStartTrans.Name = "btnStartTrans";
            this.btnStartTrans.Size = new System.Drawing.Size(75, 23);
            this.btnStartTrans.TabIndex = 8;
            this.btnStartTrans.Text = "开始传输";
            this.btnStartTrans.UseVisualStyleBackColor = true;
            this.btnStartTrans.Click += new System.EventHandler(this.btnStartTrans_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(281, 6);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 9;
            this.btnExit.Text = "退出";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // CBatchFlashMgrForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 562);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CBatchFlashMgrForm";
            this.Text = "远程下载";
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.DateTimePicker dtp_EndTime;
        private System.Windows.Forms.DateTimePicker dtp_StartTime;
        private System.Windows.Forms.RadioButton radioDay;
        private System.Windows.Forms.RadioButton radioHour;
        private System.Windows.Forms.Label lbl_EndTime;
        private System.Windows.Forms.Label lbl_StartTime;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnStartTrans;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.ComboBox cmbStation;
        private System.Windows.Forms.RadioButton radioRain;
        private System.Windows.Forms.RadioButton radioWater;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButtonSave;
        private System.Windows.Forms.RadioButton radioSD;
        private System.Windows.Forms.RadioButton MonthBtb;
    }
}