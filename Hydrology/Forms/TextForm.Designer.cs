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
            this.type = new System.Windows.Forms.GroupBox();
            this.water = new System.Windows.Forms.RadioButton();
            this.rain = new System.Windows.Forms.RadioButton();
            this.ExldateTime = new System.Windows.Forms.DateTimePicker();
            this.label6 = new System.Windows.Forms.Label();
            this.models = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.Export = new System.Windows.Forms.Button();
            this.SubCenter = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TablePanel = new System.Windows.Forms.Panel();
            this.StationSelect = new System.Windows.Forms.CheckedListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.MessagePanel.SuspendLayout();
            this.type.SuspendLayout();
            this.TablePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MessagePanel
            // 
            this.MessagePanel.Controls.Add(this.type);
            this.MessagePanel.Controls.Add(this.ExldateTime);
            this.MessagePanel.Controls.Add(this.label6);
            this.MessagePanel.Controls.Add(this.models);
            this.MessagePanel.Controls.Add(this.label5);
            this.MessagePanel.Controls.Add(this.checkBox1);
            this.MessagePanel.Controls.Add(this.Export);
            this.MessagePanel.Controls.Add(this.SubCenter);
            this.MessagePanel.Controls.Add(this.label1);
            this.MessagePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.MessagePanel.Location = new System.Drawing.Point(0, 0);
            this.MessagePanel.Name = "MessagePanel";
            this.MessagePanel.Size = new System.Drawing.Size(510, 227);
            this.MessagePanel.TabIndex = 0;
            // 
            // type
            // 
            this.type.Controls.Add(this.water);
            this.type.Controls.Add(this.rain);
            this.type.Location = new System.Drawing.Point(122, 61);
            this.type.Name = "type";
            this.type.Size = new System.Drawing.Size(278, 62);
            this.type.TabIndex = 17;
            this.type.TabStop = false;
            this.type.Text = "报表类型：";
            // 
            // water
            // 
            this.water.AutoSize = true;
            this.water.Checked = true;
            this.water.Location = new System.Drawing.Point(93, 14);
            this.water.Name = "water";
            this.water.Size = new System.Drawing.Size(83, 16);
            this.water.TabIndex = 1;
            this.water.TabStop = true;
            this.water.Text = "电测针读数";
            this.water.UseVisualStyleBackColor = true;
            // 
            // rain
            // 
            this.rain.AutoSize = true;
            this.rain.Location = new System.Drawing.Point(93, 37);
            this.rain.Name = "rain";
            this.rain.Size = new System.Drawing.Size(107, 16);
            this.rain.TabIndex = 2;
            this.rain.Text = "蒸发换算后示数";
            this.rain.UseVisualStyleBackColor = true;
            // 
            // ExldateTime
            // 
            this.ExldateTime.CustomFormat = "yyyy年MM月";
            this.ExldateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.ExldateTime.Location = new System.Drawing.Point(223, 27);
            this.ExldateTime.Name = "ExldateTime";
            this.ExldateTime.ShowUpDown = true;
            this.ExldateTime.Size = new System.Drawing.Size(117, 21);
            this.ExldateTime.TabIndex = 15;
            this.ExldateTime.Value = new System.DateTime(2010, 1, 1, 0, 0, 0, 0);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(128, 36);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "统计日期：";
            // 
            // models
            // 
            this.models.FormattingEnabled = true;
            this.models.Location = new System.Drawing.Point(223, 131);
            this.models.Name = "models";
            this.models.Size = new System.Drawing.Size(126, 20);
            this.models.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(128, 137);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 12);
            this.label5.TabIndex = 12;
            this.label5.Text = "报表类型：";
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
            this.Export.Location = new System.Drawing.Point(397, 15);
            this.Export.Name = "Export";
            this.Export.Size = new System.Drawing.Size(80, 54);
            this.Export.TabIndex = 8;
            this.Export.Text = "导出报表";
            this.Export.UseVisualStyleBackColor = true;
            this.Export.Click += new System.EventHandler(this.export_Click);
            // 
            // SubCenter
            // 
            this.SubCenter.FormattingEnabled = true;
            this.SubCenter.Location = new System.Drawing.Point(223, 162);
            this.SubCenter.Name = "SubCenter";
            this.SubCenter.Size = new System.Drawing.Size(126, 20);
            this.SubCenter.TabIndex = 3;
            this.SubCenter.SelectedIndexChanged += new System.EventHandler(this.center_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(131, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "分中心：";
            // 
            // TablePanel
            // 
            this.TablePanel.Controls.Add(this.StationSelect);
            this.TablePanel.Controls.Add(this.label3);
            this.TablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TablePanel.Location = new System.Drawing.Point(0, 227);
            this.TablePanel.Name = "TablePanel";
            this.TablePanel.Size = new System.Drawing.Size(510, 512);
            this.TablePanel.TabIndex = 2;
            // 
            // StationSelect
            // 
            this.StationSelect.FormattingEnabled = true;
            this.StationSelect.Location = new System.Drawing.Point(130, 18);
            this.StationSelect.Name = "StationSelect";
            this.StationSelect.Size = new System.Drawing.Size(323, 468);
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
            this.panel1.Size = new System.Drawing.Size(124, 512);
            this.panel1.TabIndex = 0;
            // 
            // TextForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(510, 739);
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
            this.type.ResumeLayout(false);
            this.type.PerformLayout();
            this.TablePanel.ResumeLayout(false);
            this.TablePanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel MessagePanel;
        private System.Windows.Forms.Panel TablePanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox SubCenter;
        private System.Windows.Forms.Button Export;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckedListBox StationSelect;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.ComboBox models;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker ExldateTime;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox type;
        private System.Windows.Forms.RadioButton water;
        private System.Windows.Forms.RadioButton rain;
    }
}