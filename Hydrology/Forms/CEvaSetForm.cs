using Hydrology.Entity.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Hydrology.Forms
{
    public partial class CEvaSetForm : Form
    {
        private List<string> m_vComPLists = new List<string>();

        public CEvaSetForm()
        {
            InitializeComponent();
            Init();
        }

        private void save_btn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否保存？", "是", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Save();
            }
        }

        private void quit_btn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要取消更改？", "取消", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                this.Close();
            }
            else
            {
                this.Close();
            }
        }

        private void Init()
        {
            this.rain_numPk.Value = EvaConf.Kp;
            this.eva_numPk.Value = EvaConf.Ke;
            this.dh_numPk.Value = EvaConf.Dh;
            m_vComPLists.Add("否");
            m_vComPLists.Add("是");
            this.comP_cmb.DataSource = m_vComPLists;
        }

        private void Save()
        {
            Protocol.Manager.XMLEvaInfo.Instance.Serialize(this.rain_numPk.Value, this.eva_numPk.Value, this.dh_numPk.Value, this.comP_cmb.Text == "是" ? true : false);
            EvaConf.Kp = this.rain_numPk.Value;
            EvaConf.Ke = this.eva_numPk.Value;
            EvaConf.Dh = this.dh_numPk.Value;
            EvaConf.ComP = this.comP_cmb.Text == "是" ? true : false;
        }

    }
}
