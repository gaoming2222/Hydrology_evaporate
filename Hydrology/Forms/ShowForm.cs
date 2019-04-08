using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Hydrology.Entity;

namespace Hydrology.Forms
{
    public partial class ShowForm : Form
    {
        public EventHandler<CEventSingleArgs<bool>> FormShowChanged;

        public ShowForm()
        {
            InitializeComponent();
            Init();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            //if (Rain_ckb.Checked == false && Eva_ckb.Checked == false)
            //{
            //    MessageBox.Show("请至少选择一项需要显示的界面！");
            //    return;
            //}
            if(FormShowChanged != null)
            {
                FormShowChanged.Invoke(this, new CEventSingleArgs<bool>(Eva_ckb.Checked));
            }
            Protocol.Manager.XMLEvaInfo.Instance.Serialize_ShowForm(Eva_ckb.Checked);
            this.Close();
        }

        private void Init()
        {
            //this.Rain_ckb.Checked = true;
            this.Eva_ckb.Checked = Protocol.Manager.XMLEvaInfo.Instance.DeSerialize_FormShow();

        }

        private void Cancel_btn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
