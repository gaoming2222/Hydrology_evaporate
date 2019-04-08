namespace Hydrology.Forms
{
    partial class TextForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextForm));
            this.MessagePanel = new System.Windows.Forms.Panel();
            this.endDateTime = new System.Windows.Forms.DateTimePicker();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.Export = new System.Windows.Forms.Button();
            this.DateTimer = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.SubCenter = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TableType = new System.Windows.Forms.GroupBox();
            this.year = new System.Windows.Forms.RadioButton();
            this.day = new System.Windows.Forms.RadioButton();
            this.month = new System.Windows.Forms.RadioButton();
            this.TablePanel = new System.Windows.Forms.Panel();
            this.StationSelect = new System.Windows.Forms.CheckedListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.models = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.ExldateTime = new System.Windows.Forms.DateTimePicker();
            this.MessagePanel.SuspendLayout();
            this.TableType.SuspendLayout();
            this.TablePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MessagePanel
            // 
            this.MessagePanel.Controls.Add(this.ExldateTime);
            this.MessagePanel.Controls.Add(this.label6);
            this.MessagePanel.Controls.Add(this.models);
            this.MessagePanel.Controls.Add(this.label5);
            this.MessagePanel.Controls.Add(this.endDateTime);
            this.MessagePanel.Controls.Add(this.label4);
            this.MessagePanel.Controls.Add(this.checkBox1);
            this.MessagePanel.Controls.Add(this.Export);
            this.MessagePanel.Controls.Add(this.DateTimer);
            this.MessagePanel.Controls.Add(this.label2);
            this.MessagePanel.Controls.Add(this.SubCenter);
            this.MessagePanel.Controls.Add(this.label1);
            this.MessagePanel.Controls.Add(this.TableType);
            this.MessagePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.MessagePanel.Location = new System.Drawing.Point(0, 0);
            this.MessagePanel.Name = "MessagePanel";
            this.MessagePanel.Size = new System.Drawing.Size(505, 227);
            this.MessagePanel.TabIndex = 0;
            // 
            // endDateTime
            // 
            this.endDateTime.CustomFormat = "yyyy年MM月dd日";
            this.endDateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.endDateTime.Location = new System.Drawing.Point(297, 63);
            this.endDateTime.Name = "endDateTime";
            this.endDateTime.ShowUpDown = true;
            this.endDateTime.Size = new System.Drawing.Size(117, 21);
            this.endDateTime.TabIndex = 11;
            this.endDateTime.Value = new System.DateTime(2010, 1, 1, 0, 0, 0, 0);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(199, 69);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 12);
            this.label4.TabIndex = 10;
            this.label4.Text = "结束日期：";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(133, 194);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(48, 16);
            this.checkBox1.TabIndex = 9;
            this.checkBox1.Text = "全选";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // Export
            // 
            this.Export.Location = new System.Drawing.Point(413, 156);
            this.Export.Name = "Export";
            this.Export.Size = new System.Drawing.Size(80, 54);
            this.Export.TabIndex = 8;
            this.Export.Text = "导出Excel";
            this.Export.UseVisualStyleBackColor = true;
            this.Export.Click += new System.EventHandler(this.export_Click);
            // 
            // DateTimer
            // 
            this.DateTimer.CustomFormat = "yyyy年MM月dd日";
            this.DateTimer.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.DateTimer.Location = new System.Drawing.Point(297, 9);
            this.DateTimer.Name = "DateTimer";
            this.DateTimer.ShowUpDown = true;
            this.DateTimer.Size = new System.Drawing.Size(117, 21);
            this.DateTimer.TabIndex = 7;
            this.DateTimer.Value = new System.DateTime(2010, 1, 1, 0, 0, 0, 0);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(199, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 4;
            this.label2.Text = "开始日期：";
            // 
            // SubCenter
            // 
            this.SubCenter.FormattingEnabled = true;
            this.SubCenter.Location = new System.Drawing.Point(201, 153);
            this.SubCenter.Name = "SubCenter";
            this.SubCenter.Size = new System.Drawing.Size(169, 20);
            this.SubCenter.TabIndex = 3;
            this.SubCenter.SelectedIndexChanged += new System.EventHandler(this.center_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(128, 156);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "分中心：";
            // 
            // TableType
            // 
            this.TableType.Controls.Add(this.year);
            this.TableType.Controls.Add(this.day);
            this.TableType.Controls.Add(this.month);
            this.TableType.Location = new System.Drawing.Point(3, 3);
            this.TableType.Name = "TableType";
            this.TableType.Size = new System.Drawing.Size(130, 89);
            this.TableType.TabIndex = 0;
            this.TableType.TabStop = false;
            this.TableType.Text = "报表类型";
            // 
            // year
            // 
            this.year.AutoSize = true;
            this.year.Location = new System.Drawing.Point(0, 67);
            this.year.Name = "year";
            this.year.Size = new System.Drawing.Size(47, 16);
            this.year.TabIndex = 2;
            this.year.TabStop = true;
            this.year.Text = "年表";
            this.year.UseVisualStyleBackColor = true;
            // 
            // day
            // 
            this.day.AutoSize = true;
            this.day.Checked = true;
            this.day.Location = new System.Drawing.Point(0, 20);
            this.day.Name = "day";
            this.day.Size = new System.Drawing.Size(53, 16);
            this.day.TabIndex = 1;
            this.day.TabStop = true;
            this.day.Text = "日 表";
            this.day.UseVisualStyleBackColor = true;
            this.day.CheckedChanged += new System.EventHandler(this.TableTypeChanged);
            // 
            // month
            // 
            this.month.AutoSize = true;
            this.month.Location = new System.Drawing.Point(0, 42);
            this.month.Name = "month";
            this.month.Size = new System.Drawing.Size(53, 16);
            this.month.TabIndex = 1;
            this.month.TabStop = true;
            this.month.Text = "月 表";
            this.month.UseVisualStyleBackColor = true;
            this.month.CheckedChanged += new System.EventHandler(this.TableTypeChanged);
            // 
            // TablePanel
            // 
            this.TablePanel.Controls.Add(this.StationSelect);
            this.TablePanel.Controls.Add(this.label3);
            this.TablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TablePanel.Location = new System.Drawing.Point(0, 227);
            this.TablePanel.Name = "TablePanel";
            this.TablePanel.Size = new System.Drawing.Size(505, 254);
            this.TablePanel.TabIndex = 2;
            // 
            // StationSelect
            // 
            this.StationSelect.FormattingEnabled = true;
            this.StationSelect.Location = new System.Drawing.Point(130, 18);
            this.StationSelect.Name = "StationSelect";
            this.StationSelect.Size = new System.Drawing.Size(323, 388);
            this.StationSelect.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(130, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 12);
            this.label3.TabIndex = 0;
            this.label3.Text = "站点选择:";
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 227);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(124, 254);
            this.panel1.TabIndex = 0;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(131, 111);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 12);
            this.label5.TabIndex = 12;
            this.label5.Text = "模板类型：";
            // 
            // models
            // 
            this.models.FormattingEnabled = true;
            this.models.Location = new System.Drawing.Point(201, 103);
            this.models.Name = "models";
            this.models.Size = new System.Drawing.Size(169, 20);
            this.models.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(199, 42);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "统计日期：";
            // 
            // ExldateTime
            // 
            this.ExldateTime.CustomFormat = "yyyy年MM月";
            this.ExldateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.ExldateTime.Location = new System.Drawing.Point(297, 36);
            this.ExldateTime.Name = "ExldateTime";
            this.ExldateTime.ShowUpDown = true;
            this.ExldateTime.Size = new System.Drawing.Size(117, 21);
            this.ExldateTime.TabIndex = 15;
            this.ExldateTime.Value = new System.DateTime(2010, 1, 1, 0, 0, 0, 0);
            // 
            // TextForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(505, 481);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.TablePanel);
            this.Controls.Add(this.MessagePanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "TextForm";
            this.Tag = "TxtExport";
            this.Text = "文本导出";
            this.MessagePanel.ResumeLayout(false);
            this.MessagePanel.PerformLayout();
            this.TableType.ResumeLayout(false);
            this.TableType.PerformLayout();
            this.TablePanel.ResumeLayout(false);
            this.TablePanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel MessagePanel;
        private System.Windows.Forms.Panel TablePanel;
        private System.Windows.Forms.GroupBox TableType;
        //private System.Windows.Forms.RadioButton soil;
        private System.Windows.Forms.RadioButton day;
        private System.Windows.Forms.RadioButton month;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox SubCenter;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker DateTimer;
        private System.Windows.Forms.Button Export;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckedListBox StationSelect;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.DateTimePicker endDateTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton year;
        private System.Windows.Forms.ComboBox models;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker ExldateTime;
        private System.Windows.Forms.Label label6;
    }
}