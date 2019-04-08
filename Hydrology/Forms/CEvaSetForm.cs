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
            if (MessageBox.Show("确定退出？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
            this.rain_numPk.Value = EvaConf.kp;
            this.eva_numPk.Value = EvaConf.ke;
            this.dh_numPk.Value = EvaConf.dh;
            m_vComPLists.Add("否");
            m_vComPLists.Add("是");
            this.comP_cmb.DataSource = m_vComPLists;
            this.comP_cmb.SelectedIndex = EvaConf.comP ? 1 : 0;
        }

        private void Save()
        {
            Protocol.Manager.XMLEvaInfo.Instance.Serialize(this.rain_numPk.Value, this.eva_numPk.Value, this.dh_numPk.Value, this.comP_cmb.Text == "是" ? true : false);
            EvaConf.kp = this.rain_numPk.Value;
            EvaConf.ke = this.eva_numPk.Value;
            EvaConf.dh = this.dh_numPk.Value;
            EvaConf.comP = this.comP_cmb.Text == "是" ? true : false;
        }

    }
}
