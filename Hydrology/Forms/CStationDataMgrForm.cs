﻿using Hydrology.CControls;
using Hydrology.DataMgr;
using Hydrology.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Hydrology.Forms
{
    public partial class CStationDataMgrForm : Form
    {
        #region 静态常量
        private static readonly string CS_CMB_Rain = "雨量";
        private static readonly string CS_CMB_Water = "水位";
        private static readonly string CS_CMB_Voltage = "电压";
        private static readonly string CS_CMB_Eva = "蒸发-降雨";
        private static readonly string CS_CMB_Temp = "温度";
        //private static readonly string CS_CMB_Temp = "温度-电压";

        private static readonly string CS_CMB_ViewStyle_All = "图表";
        private static readonly string CS_CMB_ViewStyle_Table = "表格";
        private static readonly string CS_CMB_ViewStyle_Chart = "图形";

        private static readonly string CS_CMB_RainShape_Periodrain = "时段雨量";
        private static readonly string CS_CMB_RainShape_Differencerain = "差值雨量";
        private static readonly string CS_CMB_ViewStyle_Dayrain = "日雨量";

        private static readonly string CS_CMB_AllData = "全部数据";
        private static readonly string CS_CMB_TimeData = "小时数据";
        private static readonly string CS_CMB_DayData = "日数据";
        private static readonly string CS_CMB_RawData = "原始数据";
        private static readonly string CS_CMB_8Data = "8时数据";

        private readonly string CS_All_Station = "所有站点";
        #endregion ///<静态常量

        #region 数据成员
        private CDataGridViewRain m_dgvRain;        //自定义雨量数据表对象
        private CDataGridViewWater m_dgvWater;      //自定义水量数据表对象
        private CDataGridViewVoltage m_dgvVoltage;  //自定义电压数据表对象
        private CDataGridViewEva m_dgvEva;    //自定义蒸发数据查询对象

        private Panel m_panelChart;                     // chart容量
        private CChartRain m_chartRain;                 //自定义雨量图
        private CChartVoltage m_chartVoltage;           //自定义电压过程线
        private CChartWaterStage m_chartWaterFlow;      //自定义水位流量图
        private CChartEva m_chartEva;          //自定义蒸发雨量柱状图
        private CChartTemp m_chartTemp;          //自定义温度电压过程线

        private bool m_bIsEditable = false;         //默认是查询模式
        private bool m_bIsHEva = true;         //默认是小时表
        private bool m_bIsREva = false;         //默认是小时表

        private List<CEntityStation> m_listStations; //所有水情站点的引用
        #endregion  ///<DATA_MENBER

        #region 属性
        public bool Editable
        {
            get { return m_bIsEditable; }
            set { SetEditable(value); }
        }
        public bool IsHEva
        {
            get { return m_bIsHEva; }
            set { SetHEva(value); }
        }

        public bool IsREva
        {
            get { return m_bIsREva; }
            set { SetREva(value); }
        }
        private bool isRawData;
        #endregion ///<PREOPERTY

        #region 公共方法
        public CStationDataMgrForm()
        {
            isRawData = false;
            InitializeComponent();

            // 自定义初始化
            InitUI();
            InitDB();

            cmbQueryInfo_SelectedIndexChanged(null, null);
            cmbQueryInfo_SelectedIndexChanged(null, null);
            // 生成测试数据
            //InitRainData();
            //InitWaterStage();
            //InitVoltageData();
        }
        /// <summary>
        /// 设置当前模式，默认是查询模式
        /// </summary>
        /// <param name="bEditable">是否可编辑</param>
        public void SetEditable(bool bEditable)
        {
            m_bIsEditable = bEditable;
            if (m_bIsEditable)
            {
                //cmbQueryInfo
                cmbQueryInfo.SelectedIndex = 0;
                cmbQueryInfo.Enabled = false;

                cmb_TimeSelect.SelectedIndex = 0;
                
                cmb_TimeSelect.Enabled = false;
                cmb_TimeSelect.SelectedIndex = 1;
                // 如果可以编辑，调整位置
                Point pNewRecord = btnNewRecord.Location;
                btnNewRecord.Location = btnExit.Location;
                btnExit.Location = pNewRecord;
                btnNewRecord.Visible = true;
                btnApply.Visible = true;

                


                //cmbQueryInfo.Items.Add(CS_CMB_SoilData);

                // 将控件启用编辑状态
                m_dgvRain.Editable = true;
                m_dgvWater.Editable = true;
                m_dgvVoltage.Editable = true;
                m_dgvEva.Editable = true;
                this.Text = "数据校正";
                labelInfoSelect.Text = "配置检索信息";

                FormHelper.InitUserModeEvent(this);
            }
            else
            {
                cmbQueryInfo.SelectedIndex = 0;
                cmbQueryInfo.Enabled = true; ;

                cmb_TimeSelect.SelectedIndex = 0;
                cmb_TimeSelect.Enabled = true;

                //cmbQueryInfo.Items.Add(CS_CMB_SoilData);
                // 不可以编辑，判断当前的位置
                m_dgvRain.Editable = false;
                m_dgvVoltage.Editable = false;
                m_dgvWater.Editable = false;
                m_dgvEva.Editable = false;
                if (btnNewRecord.Visible)
                {
                    // 如果由编辑模式调整为非编辑模式，需要调整退出按钮的位置
                    Point pNewRecord = btnNewRecord.Location;
                    btnNewRecord.Location = btnExit.Location;
                    btnExit.Location = pNewRecord;
                    btnNewRecord.Visible = false;
                    btnApply.Visible = false;
                    this.Text = "数据查询";
                    labelInfoSelect.Text = "配置查询信息";
                }
                else
                {
                    btnApply.Visible = false;
                    btnNewRecord.Visible = false;
                }
            }//end of bIsEditable
        }

        public void SetHEva(bool bIsHEva)
        {
            m_bIsHEva = bIsHEva;
            if (m_bIsHEva)
            {
                m_dgvEva.IsHEva = true;
            }
            else
            {
                m_dgvEva.IsHEva = false;
            }
        }


        public void SetREva(bool bIsREva)
        {
            m_bIsREva = bIsREva;
            if (m_bIsREva)
            {
                m_dgvEva.IsREva = true;
            }
            else
            {
                m_dgvEva.IsREva = false;
            }
        }

        #endregion ///< PUBLIC_METHOD

        #region 帮助方法
        // 初始化自定义界面
        private void InitUI()
        {
            // 初始化测站
            // 初始化查询信息类型
            this.SuspendLayout();
            cmbQueryInfo.Items.AddRange(new string[] { CS_CMB_Eva,CS_CMB_Voltage,CS_CMB_Temp });

            cmb_RainShape.Items.AddRange(new string[] { CS_CMB_RainShape_Periodrain, CS_CMB_RainShape_Differencerain, CS_CMB_ViewStyle_Dayrain });

            cmb_TimeSelect.Items.AddRange(new string[] { CS_CMB_TimeData, CS_CMB_AllData, CS_CMB_RawData, CS_CMB_8Data });
            // 设置日期
            this.dptTimeStart.Format = DateTimePickerFormat.Custom;
            this.dptTimeEnd.Format = DateTimePickerFormat.Custom;
            dptTimeStart.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dptTimeEnd.CustomFormat = "yyyy-MM-dd HH:mm:ss";

            TimeSpan span = new TimeSpan(1, 0, 0, 0);
            DateTime now = DateTime.Now;
            dptTimeEnd.Value = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            dptTimeStart.Value = dptTimeEnd.Value.Subtract(span);// 减少一天


            // 
            // cmbStation
            // 
            this.panelLeft.Controls.Remove(this.cmbStation);
            // this.cmbStation = new CStationComboBox();
            this.cmbStation = new CStationComboBox_1();
            this.cmbStation.FormattingEnabled = true;
            this.cmbStation.Location = new System.Drawing.Point(80, 96);
            this.cmbStation.Name = "cmbStation";
            this.cmbStation.Size = new System.Drawing.Size(117, 20);
            this.cmbStation.TabIndex = 1;
            this.panelLeft.Controls.Add(this.cmbStation);
            this.cmbStation.SelectedIndexChanged += new EventHandler(EHStationChanged);


            #region 表

            // 初始化雨量查询数据表
            m_dgvRain = new CDataGridViewRain();
            m_dgvRain.AllowUserToAddRows = false;
            m_dgvRain.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            //m_dgvRain.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            //m_dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            m_dgvRain.Dock = DockStyle.Fill;
            m_dgvRain.AutoSize = true;
            //m_dataGridView.ReadOnly = true; //只读
            m_dgvRain.AllowUserToResizeRows = false;
            m_dgvRain.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvRain.RowHeadersWidth = 50;
            m_dgvRain.ColumnHeadersHeight = 25;
            m_dgvRain.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            //m_dgvRain.Dock = DockStyle.Fill;

            // 初始化水量查询数据表
            m_dgvWater = new CDataGridViewWater();
            m_dgvWater.AllowUserToAddRows = false;
            m_dgvWater.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            //m_dgvRain.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            //m_dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            m_dgvWater.Dock = DockStyle.Fill;
            m_dgvWater.AutoSize = true;
            //m_dataGridView.ReadOnly = true; //只读
            m_dgvWater.AllowUserToResizeRows = false;
            m_dgvWater.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvWater.RowHeadersWidth = 50;
            m_dgvWater.ColumnHeadersHeight = 25;
            m_dgvWater.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);

            // 初始化电压查询数据表
            m_dgvVoltage = new CDataGridViewVoltage();
            m_dgvVoltage.AllowUserToAddRows = false;
            m_dgvVoltage.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            //m_dgvVoltage.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            //m_dgvVoltage.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            m_dgvVoltage.Dock = DockStyle.Fill;
            m_dgvWater.AutoSize = true;
            //m_dgvVoltage.ReadOnly = true; //只读
            m_dgvVoltage.AllowUserToResizeRows = false;
            m_dgvVoltage.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvVoltage.RowHeadersWidth = 50;
            m_dgvVoltage.ColumnHeadersHeight = 25;
            m_dgvVoltage.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);

            m_dgvEva = new CDataGridViewEva();
            m_dgvEva.AllowUserToAddRows = false;
            m_dgvEva.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            m_dgvEva.Dock = DockStyle.Fill;
            m_dgvEva.AutoSize = true;
            m_dgvEva.AllowUserToResizeRows = false;
            m_dgvEva.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvEva.RowHeadersWidth = 50;
            m_dgvEva.ColumnHeadersHeight = 25;
            m_dgvEva.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);

            tLayoutRight.SuspendLayout();
            tLayoutRight.Controls.Add(m_dgvRain, 0, 0);
            tLayoutRight.Controls.Add(m_dgvWater, 0, 0);
            tLayoutRight.Controls.Add(m_dgvVoltage, 0, 0);
            tLayoutRight.Controls.Add(m_dgvEva, 0, 0);
            tLayoutRight.ResumeLayout(false);

            #endregion 表

            #region 图
            m_panelChart = new Panel();
            m_panelChart.Dock = DockStyle.Bottom;
            m_chartRain = new CChartRain();
            m_chartRain.Dock = DockStyle.Fill;

            m_chartVoltage = new CChartVoltage();
            m_chartVoltage.Dock = DockStyle.Fill;

            m_chartWaterFlow = new CChartWaterStage();
            m_chartWaterFlow.Dock = DockStyle.Fill;

            m_chartEva = new CChartEva();
            m_chartEva.Dock = DockStyle.Fill;

            m_chartTemp = new CChartTemp();
            m_chartTemp.Dock = DockStyle.Fill;

            m_panelChart.Controls.Add(m_chartRain);
            m_panelChart.Controls.Add(m_chartVoltage);
            m_panelChart.Controls.Add(m_chartWaterFlow);
            m_panelChart.Controls.Add(m_chartEva);
            m_panelChart.Controls.Add(m_chartTemp);

            panelRight.Controls.Add(m_panelChart);

            m_chartVoltage.Visible = false;
            m_chartWaterFlow.Visible = false;
            m_chartEva.Visible = false;
            m_chartTemp.Visible = false;

            m_panelChart.Height = panelRight.Height / 2;

            #endregion 图

            // 绑定消息
            m_dgvRain.PageNumberChanged += new EventHandler<CEventSingleArgs<int>>(this.EHPageNumberChanged);
            m_dgvRain.DataReady += new EventHandler<CEventDBUIDataReadyArgs>(this.EHTableDataReady);

            m_dgvWater.PageNumberChanged += new EventHandler<CEventSingleArgs<int>>(this.EHPageNumberChanged);
            m_dgvWater.DataReady += new EventHandler<CEventDBUIDataReadyArgs>(this.EHTableDataReady);

            m_dgvVoltage.PageNumberChanged += new EventHandler<CEventSingleArgs<int>>(this.EHPageNumberChanged);
            m_dgvVoltage.DataReady += new EventHandler<CEventDBUIDataReadyArgs>(this.EHTableDataReady);

            m_dgvEva.PageNumberChanged += new EventHandler<CEventSingleArgs<int>>(this.EHPageNumberChanged);
            m_dgvEva.DataReady += new EventHandler<CEventDBUIDataReadyArgs>(this.EHTableDataReady);

            // 初始化视图样式列表框
            cmb_ViewStyle.Items.Add(CS_CMB_ViewStyle_All);
            cmb_ViewStyle.Items.Add(CS_CMB_ViewStyle_Chart);
            cmb_ViewStyle.Items.Add(CS_CMB_ViewStyle_Table);

            cmb_ViewStyle.SelectedIndex = 0;

            // 初始化焦点切换
            FormHelper.InitControlFocusLoop(this);

            this.ResumeLayout(false);

            cmbQueryInfo.SelectedIndex = 0;
            cmb_RainShape.SelectedIndex = 0;
            cmb_TimeSelect.SelectedIndex = 0;

            this.cmb_SubCenter.SelectedIndexChanged += new EventHandler(EHSubCenterChanged);
        }
        private void EHStationChanged(object sender, EventArgs e)
        {
            int i = 0;
        }
        /// <summary>
        /// 分中心变动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EHSubCenterChanged(object sender, EventArgs e)
        {
            this.cmbStation.Text = "";
            int selectindex = cmb_SubCenter.SelectedIndex;
            if (0 == selectindex)
            {
                this.cmbStation.m_listBoxStation.Items.Clear();
                // 根据分中心查找测站
                m_listStations = CDBDataMgr.Instance.GetAllStation();
                //   m_dgvStatioin.SetSubCenterName(null); //所有分中
                for (int i = 0; i < m_listStations.Count; ++i)
                {
                    this.cmbStation.m_listBoxStation.Items.Add(string.Format("({0,-4}|{1})", m_listStations[i].StationID, m_listStations[i].StationName));
                }
                //    this.cmbStation.Text = this.cmbStation.m_listBoxStation.Items[0].ToString();
            }
            else
            {
                string subcentername = cmb_SubCenter.Text;
                //  m_dgvStatioin.SetSubCenterName(subcentername);
                //this.panelLeft.Controls.Remove(this.cmbStation);
                //  this.cmbStation = new CStationComboBox();
                // this.cmbStation.SetSubcenter(subcentername);
                this.cmbStation.m_listBoxStation.Items.Clear();

                // 根据分中心查找测站
                //List<CEntityStation> listAllStation = CDBDataMgr.Instance.GetAllStation();
                m_listStations = CDBDataMgr.Instance.GetAllStation();
                CEntitySubCenter subcenter = CDBDataMgr.Instance.GetSubCenterByName(subcentername);
                if (null != subcenter)
                {
                    // 如果不为空           
                    for (int i = 0; i < m_listStations.Count; ++i)
                    {
                        if (m_listStations[i].SubCenterID == subcenter.SubCenterID)
                        {

                            this.cmbStation.m_listBoxStation.Items.Add(string.Format("({0,-4}|{1})", m_listStations[i].StationID, m_listStations[i].StationName));
                        }
                    }
                    //      this.cmbStation.Text = this.cmbStation.m_listBoxStation.Items[0].ToString();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("分中心 Error");
                }
                //  this.cmbStation.m_listBoxStation.Items.Add(GetDisplayStationName(station));
            }

            //   this.labelUserCount.Text = string.Format("共{0}个站点", m_dgvStatioin.Rows.Count);
        }

        // 初始化数据连接
        private void InitDB()
        {
            // 考虑真的要用单例吗
            m_dgvRain.InitDataSource(CDBDataMgr.GetInstance().GetRainProxy());
            m_dgvWater.InitDataSource(CDBDataMgr.GetInstance().GetWaterProxy());
            m_dgvVoltage.InitDataSource(CDBDataMgr.GetInstance().GetVoltageProxy());
            m_dgvEva.InitDataSource(CDBDataMgr.GetInstance().GetHEvaProxy());
            m_dgvEva.InitDataSource(CDBDataMgr.GetInstance().GetDEvaProxy());

            m_chartRain.InitDataSource(CDBDataMgr.GetInstance().GetRainProxy());
            m_chartWaterFlow.InitDataSource(CDBDataMgr.GetInstance().GetWaterProxy());
           // m_chartVoltage.InitDataSource(CDBDataMgr.GetInstance().GetVoltageProxy());
            m_chartEva.InitDataSource(CDBDataMgr.GetInstance().GetHEvaProxy());
            m_chartEva.InitDataSource(CDBDataMgr.GetInstance().GetDEvaProxy());
            m_chartTemp.InitDataSource(CDBDataMgr.GetInstance().GetHEvaProxy());
            m_chartTemp.InitDataSource(CDBDataMgr.GetInstance().GetDEvaProxy());

            List<CEntitySubCenter> listSubCenter = CDBDataMgr.Instance.GetAllSubCenter();

            m_listStations = CDBDataMgr.Instance.GetAllStation();
            cmb_SubCenter.Items.Add(CS_All_Station);
            for (int i = 0; i < listSubCenter.Count; ++i)
            {
                cmb_SubCenter.Items.Add(listSubCenter[i].SubCenterName);
            }
            this.cmb_SubCenter.SelectedIndex = 0;
        }
        #endregion ///<HELP_METHOD

        #region 事件响应

        private void EHPageNumberChanged(object sender, CEventSingleArgs<int> e)
        {
            int separatorIndex = m_lablePageIndex.Text.IndexOf('/') + 1;
            int totalPage = Int32.Parse(m_lablePageIndex.Text.Substring(separatorIndex, m_lablePageIndex.Text.Length - separatorIndex));
            m_lablePageIndex.Text = String.Format("页码：{0} / {1}", e.Value, totalPage);
        }

        private void EHTableDataReady(object sender, CEventDBUIDataReadyArgs e)
        {
            m_lablePageIndex.Text = String.Format("页码：{0} / {1}", e.CurrentPageIndex, e.TotalPageCount);
            m_lableRowCount.Text = String.Format("{0} 行", e.TotalRowCount);
        }


        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (m_dgvRain.IsModifiedUnSaved() || m_dgvWater.IsModifiedUnSaved() || m_dgvVoltage.IsModifiedUnSaved() || m_dgvEva.IsModifiedUnSaved())
            {
                DoSave();
            }
            else
            {
                MessageBox.Show("没有任何修改，无需保存");
            }
        }

        private void btnNewRecord_Click(object sender, EventArgs e)
        {
            //添加新的记录
            CStationDataAddForm form = new CStationDataAddForm();

            //  form.SetCurrentStation((cmbStation as CStationComboBox).GetStation());
            CStationComboBox cmbStation_2 = new CStationComboBox();
            //11.12
            //    form.SetCurrentStation(cmbStation_2.GetStation());
            if (cmbStation.Text != "")
            {
                form.SetCurrentStation(CDBDataMgr.Instance.GetStationById(cmbStation.Text.Substring(1, 4)));
            }
            //form.Parent = this;
            form.StartPosition = FormStartPosition.CenterScreen; //父窗口居中
            DialogResult result = form.ShowDialog();
            if (DialogResult.OK == result)
            {
                if (form.GetAddedRain() != null)
                {
                    m_dgvRain.AddRain(form.GetAddedRain());
                }
                if (form.GetAddedVoltage() != null)
                {
                    m_dgvVoltage.AddVoltage(form.GetAddedVoltage());
                }
                if (form.GetAddedWater() != null)
                {
                    m_dgvWater.AddWater(form.GetAddedWater());
                }
            }
            else if (DialogResult.Abort == result)
            {
                // 强行被退出，由于权限过时引起
                this.FormClosing -= this.CStationDataMgrForm_FormClosing;
                this.Close(); //关闭窗口
            }
        }

        private void cmbQueryInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 更改选项
            if (cmbQueryInfo.Text.Equals(CS_CMB_Rain))
            {
                //查询雨量
                //tLayoutRight.Controls.Add(m_dgvRain, 0, 0);
                cmb_TimeSelect.Items.Clear();
                cmb_TimeSelect.Items.AddRange(new string[] { CS_CMB_TimeData, CS_CMB_AllData });
                cmb_TimeSelect.SelectedIndex = 0;
                m_dgvRain.Show();
                m_dgvWater.Hide();
                m_dgvVoltage.Hide();
                m_dgvEva.Hide();
                // 图形
                m_chartRain.Show();
                m_chartVoltage.Hide();
                m_chartWaterFlow.Hide();
                m_chartEva.Hide();
                m_chartTemp.Hide();
                this.label6.Show();
                this.cmb_RainShape.Show();
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Water))
            {
                //查询水量
                cmb_TimeSelect.Items.Clear();
                cmb_TimeSelect.Items.AddRange(new string[] { CS_CMB_TimeData, CS_CMB_AllData });
                cmb_TimeSelect.SelectedIndex = 0;
                m_dgvRain.Hide();
                m_dgvWater.Show();
                m_dgvVoltage.Hide();
                m_dgvEva.Hide();
                //tLayoutRight.Controls.Add(m_dgvWater, 0, 0);
                // 图形
                m_chartRain.Hide();
                m_chartVoltage.Hide();
                m_chartWaterFlow.Show();
                m_chartEva.Hide();
                m_chartTemp.Hide();
                this.label6.Hide();
                this.cmb_RainShape.Hide();
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Voltage))
            {
                //查询电压
                cmb_TimeSelect.Items.Clear();
                cmb_TimeSelect.Items.AddRange(new string[] { CS_CMB_TimeData });
                cmb_TimeSelect.SelectedIndex = 0;
                m_dgvRain.Hide();
                m_dgvWater.Hide();
                m_dgvVoltage.Hide();
                m_dgvEva.Show();
                // 图形
                m_chartRain.Hide();
                m_chartVoltage.Show();
                m_chartWaterFlow.Hide();
                m_chartEva.Hide();
                m_chartTemp.Hide();
                this.label6.Hide();
                this.cmb_RainShape.Hide();
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Eva))
            {
                //查询蒸发数据
                cmb_TimeSelect.Items.Clear();
                cmb_TimeSelect.Items.AddRange(new string[] { CS_CMB_TimeData, CS_CMB_DayData, CS_CMB_RawData, CS_CMB_8Data });
                cmb_TimeSelect.SelectedIndex = 0;
                m_dgvRain.Hide();
                m_dgvWater.Hide();
                m_dgvVoltage.Hide();
                m_dgvEva.Show();
                // 图形
                m_chartRain.Hide();
                m_chartVoltage.Hide();
                m_chartWaterFlow.Hide();
                m_chartEva.Show();
                m_chartTemp.Hide();
                this.label6.Hide();
                this.cmb_RainShape.Hide();
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Temp))
            {
                //查询蒸发数据
                cmb_TimeSelect.Items.Clear();
                cmb_TimeSelect.Items.AddRange(new string[] { CS_CMB_TimeData, CS_CMB_DayData });
                cmb_TimeSelect.SelectedIndex = 0;
                m_dgvRain.Hide();
                m_dgvWater.Hide();
                m_dgvVoltage.Hide();
                m_dgvEva.Show();
                // 图形
                m_chartRain.Hide();
                m_chartVoltage.Hide();
                m_chartWaterFlow.Hide();
                m_chartEva.Hide();
                m_chartTemp.Show();
                this.label6.Hide();
                this.cmb_RainShape.Hide();
            }
        }

        private void cmbTimeSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbQueryInfo.Text.Equals(CS_CMB_Eva)|| cmbQueryInfo.Text.Equals(CS_CMB_Temp))
            {
                if (cmb_TimeSelect.Text.Equals(CS_CMB_TimeData))
                {
                    IsREva = false;
                    IsHEva = true;
                    isRawData = false;
                    dptTimeStart.Value = dptTimeEnd.Value.AddDays(-1);// 减少一天
                }else if (cmb_TimeSelect.Text.Equals(CS_CMB_RawData))
                {
                    isRawData = true;
                    IsHEva = false;
                    IsREva = true;
                    dptTimeStart.Value = dptTimeEnd.Value.AddDays(-1);// 减少一天
                }
                else if (cmb_TimeSelect.Text.Equals(CS_CMB_8Data))
                {
                    isRawData = true;
                    IsHEva = false;
                    IsREva = true;
                    dptTimeStart.Value = dptTimeEnd.Value.AddDays(-10);// 减少一天
                }
                else
                {
                    
                    IsHEva = false;
                    IsREva = false;
                    isRawData = false;
                    DateTime now = DateTime.Now;
                    int day = now.Day;
                    dptTimeStart.Value = day >= 15 ? new DateTime(now.Year, now.Month, 15, 0, 0 ,0) : new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                }
            }
            else
            {
                dptTimeStart.Value = dptTimeEnd.Value.AddDays(-1);// 减少一天
            }
        }

        private void cmb_ViewStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 选择的图表样式发生了改变
            if (cmb_ViewStyle.Text == CS_CMB_ViewStyle_All)
            {
                // 查看所有
                tLayoutRight.Visible = true;
                m_panelChart.Visible = true;
                m_panelChart.Height = panelRight.Height / 2;
            }
            else if (cmb_ViewStyle.Text == CS_CMB_ViewStyle_Chart)
            {
                // 仅图形
                tLayoutRight.Visible = false;
                m_panelChart.Visible = true;
                m_panelChart.Height = panelRight.Height;

            }
            else if (cmb_ViewStyle.Text == CS_CMB_ViewStyle_Table)
            {
                // 仅表格
                tLayoutRight.Visible = true;
                m_panelChart.Visible = false;
            }
        }

        //private void CStationDataMgrForm_SizeChanged(object sender, EventArgs e)
        //{
        //    // 更新画图区域大小
        //    if (m_panelChart.Visible)
        //    {
        //        if (!tLayoutRight.Visible)
        //        {
        //            m_panelChart.Height = panelRight.Height;
        //        }
        //        else
        //        {
        //            m_panelChart.Height = panelRight.Height / 2;
        //        }
        //    }
        //}

        private void CStationDataMgrForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 窗口关闭方法，判断是否有数据需要保存
            if (m_dgvVoltage.IsModifiedUnSaved() || m_dgvRain.IsModifiedUnSaved() || m_dgvWater.IsModifiedUnSaved())
            {
                DialogResult result = MessageBox.Show("当前所做修改尚未保存，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (DialogResult.Cancel == result)
                {
                    e.Cancel = true;
                }
                else if (DialogResult.Yes == result)
                {
                    // 保存当前修改
                    if (!DoSave())
                    {
                        // 如果保存失败，不允许退出
                        e.Cancel = true;
                    }
                }
                else if (DialogResult.No == result)
                {
                    //直接退出
                }
            }
        }

        // 根据设定的条件查询数据表
        private void btnQuery_Click(object sender, EventArgs e)
        {
            queryData();

        }
        public void queryData()
        {
            // 获取站点ID
            // 获取起始日期
            // 获取结束日期
            // 判断输入是否合法
            string stationId = "";
            Object stationObject = (cmbStation as CStationComboBox_1).GetStation_1();

            //  CEntityStation station = (cmbStation as CStationComboBox).GetStation();
            CEntityStation station = new CEntityStation();
            CEntitySoilStation soilstation = new CEntitySoilStation();
            if (stationObject is CEntityStation)
            {
                station = (CEntityStation)stationObject;
            }
            else
            {
                soilstation = (CEntitySoilStation)stationObject;
            }
            if (null == station || null == soilstation)
            {
                MessageBox.Show("请选择正确的站点后再进行查询");
                return;
            }
            // 判断开始时间是否小于结束时间
            if (dptTimeStart.Value >= dptTimeEnd.Value)
            {
                MessageBox.Show("开始时间必须小于结束时间");
                return;
            }
            if (null != station.StationID)
            {
                stationId = station.StationID;
            }
            else
            {
                stationId = soilstation.StationID;
            }

            m_statusLable.Text = "正在查询...";
            this.Enabled = false;
            bool TimeSelect = false;
            if (cmb_TimeSelect.Text.Equals(CS_CMB_TimeData))
            {
                TimeSelect = true;
            }
            if (cmbQueryInfo.Text.Equals(CS_CMB_Rain))
            {
                #region 雨量
                if (m_dgvRain.IsModifiedUnSaved())
                {
                    DialogResult result = MessageBox.Show("当前所做修改尚未保存，强行查询会导致修改丢失,是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (DialogResult.Yes == result)
                    {
                        // 保存当前修改
                        if (DoSave())
                        {
                            // 保存成功以后， 继续查询
                        }
                        else
                        {
                            // 保存失败，不查询
                            return;
                        }
                    }
                    else if (DialogResult.No == result)
                    {
                        //return;
                        //直接退出
                    }
                    else if (DialogResult.Cancel == result)
                    {
                        this.Close();
                        return;// 退出查询
                    }
                }
                if (m_dgvRain.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, TimeSelect))
                {
                    // 如果查询成功
                    m_chartRain.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, cmb_RainShape.SelectedIndex, TimeSelect);
                }

                m_dgvRain.UpdateDataToUI();
                #endregion 雨量
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Water))
            {
                #region 水位
                if (m_dgvWater.IsModifiedUnSaved())
                {
                    DialogResult result = MessageBox.Show("当前所做修改尚未保存，强行查询会导致修改丢失,是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (DialogResult.Yes == result)
                    {
                        // 保存当前修改
                        if (DoSave())
                        {
                            // 保存成功以后， 继续查询
                        }
                        else
                        {
                            // 保存失败，不查询
                            return;
                        }
                    }
                    else if (DialogResult.No == result)
                    {
                        //直接退出
                    }
                    else if (DialogResult.Cancel == result)
                    {
                        this.Close();
                        return;// 退出查询
                    }
                }
                if (m_dgvWater.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, TimeSelect))
                {
                    m_chartWaterFlow.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, TimeSelect);
                }
                m_dgvWater.UpdateDataToUI();
                #endregion 水位
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Voltage))
            {
                #region 电压
                // 查询电压
                if (m_dgvVoltage.IsModifiedUnSaved())
                {
                    DialogResult result = MessageBox.Show("当前所做修改尚未保存，强行查询会导致修改丢失,是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (DialogResult.Yes == result)
                    {
                        // 保存当前修改
                        if (DoSave())
                        {
                            // 保存成功以后， 继续查询
                        }
                        else
                        {
                            // 保存失败，不查询
                            return;
                        }
                    }
                    else if (DialogResult.No == result)
                    {
                        //直接退出
                    }
                    else if (DialogResult.Cancel == result)
                    {
                        this.Close();
                        return;// 退出查询
                    }
                }
                bool updateData = false;
                if (m_dgvEva.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, isRawData))
                {
                    updateData = m_chartVoltage.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, TimeSelect);
                }
                m_dgvEva.UpdateDataToUI();
                //if (updateData == true)
                //{
                //m_dgvVoltage.UpdateDataToUI();
                //}
                #endregion 电压
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Eva))
            {
                #region 蒸发数据
                // 查询蒸发
                if (m_dgvEva.IsModifiedUnSaved())
                {
                    DialogResult result = MessageBox.Show("当前所做修改尚未保存，强行查询会导致修改丢失,是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (DialogResult.Yes == result)
                    {
                        // 保存当前修改
                        if (DoSave())
                        {
                            // 保存成功以后， 继续查询
                        }
                        else
                        {
                            // 保存失败，不查询
                            return;
                        }
                    }
                    else if (DialogResult.No == result)
                    {
                        //直接退出
                    }
                    else if (DialogResult.Cancel == result)
                    {
                        this.Close();
                        return;// 退出查询
                    }
                }
                bool updateData = false;
                if (cmb_TimeSelect.Text.Equals(CS_CMB_8Data))
                {
                    if (m_dgvEva.SetFilter_1(stationId, dptTimeStart.Value, dptTimeEnd.Value, isRawData))
                    {

                    }
                }else if (m_dgvEva.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, isRawData))
                {
                    if (!isRawData)
                    {
                        updateData = m_chartEva.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, TimeSelect);
                    }
                }
                m_dgvEva.UpdateDataToUI();
                #endregion 蒸发数据
            }
            else if (cmbQueryInfo.Text.Equals(CS_CMB_Temp))
            {
                #region 温度数据
                // 查询蒸发
                if (m_dgvEva.IsModifiedUnSaved())
                {
                    DialogResult result = MessageBox.Show("当前所做修改尚未保存，强行查询会导致修改丢失,是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (DialogResult.Yes == result)
                    {
                        // 保存当前修改
                        if (DoSave())
                        {
                            // 保存成功以后， 继续查询
                        }
                        else
                        {
                            // 保存失败，不查询
                            return;
                        }
                    }
                    else if (DialogResult.No == result)
                    {
                        //直接退出
                    }
                    else if (DialogResult.Cancel == result)
                    {
                        this.Close();
                        return;// 退出查询
                    }
                }
                bool updateData = false;
                if (m_dgvEva.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, isRawData))
                {
                    updateData = m_chartTemp.SetFilter(stationId, dptTimeStart.Value, dptTimeEnd.Value, TimeSelect);
                }
                m_dgvEva.UpdateDataToUI();
                #endregion 蒸发数据
            }
            this.Enabled = true;
            m_statusLable.Text = "查询已成功完成";
        }


        #endregion ///<事件响应

        #region 帮助方法

        private bool DoSave()
        {
            try
            {
                //保存当前的修改
                if (m_dgvRain.IsModifiedUnSaved())
                {
                    m_dgvRain.DoSave();
                    m_dgvRain.UpdateDataToUI();
                }
                if (m_dgvWater.IsModifiedUnSaved())
                {
                    m_dgvWater.DoSave();
                    m_dgvWater.UpdateDataToUI();
                }
                if (m_dgvVoltage.IsModifiedUnSaved())
                {
                    m_dgvVoltage.DoSave();
                    m_dgvVoltage.UpdateDataToUI();
                }
                if (m_dgvEva.IsModifiedUnSaved())
                {
                    m_dgvEva.DoSave();
                    m_dgvEva.UpdateDataToUI();
                }

                MessageBox.Show("保存成功！");
                return true; //保存结束
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                //这个时候保存失败
                MessageBox.Show("保存失败!");
                return false;
            }
        }

        #endregion ///<帮助方法

        #region 测试数据
        public void InitVoltageData()
        {
            Random random = new Random();
            DateTime currentDate = DateTime.Now.Date;
            List<CEntityVoltage> lists = new List<CEntityVoltage>();
            for (int i = 0; i < 24 * 10; ++i)
            {
                CEntityVoltage voltage = new CEntityVoltage();
                voltage.TimeCollect = currentDate;
                voltage.Voltage = random.Next(1000, 4000);
                currentDate = currentDate.AddHours(1).AddMinutes(5).AddSeconds(1);
                lists.Add(voltage);
            }
           // m_chartVoltage.AddVoltages(lists);
        }

        //public void InitRainData()
        //{
        //    Random random = new Random();
        //    DateTime currentDate = DateTime.Now.Date;
        //    List<CEntityRain> lists = new List<CEntityRain>();
        //    for (int i = 0; i < 24 * 10; ++i)
        //    {
        //        CEntityRain rain = new CEntityRain();
        //        rain.TimeCollect = currentDate;
        //        rain.PeriodRain = (decimal)random.NextDouble();
        //        currentDate = currentDate.AddHours(1).AddMinutes(5).AddSeconds(1);
        //        lists.Add(rain);
        //    }
        //    m_chartRain.AddRains(lists);
        //}

        public void InitWaterStage()
        {
            Random random = new Random();
            DateTime currentDate = DateTime.Now.Date;
            List<CEntityWater> lists = new List<CEntityWater>();
            for (int i = 0; i < 24 * 10; ++i)
            {
                CEntityWater water = new CEntityWater();
                water.TimeCollect = currentDate;
                water.WaterStage = (decimal)random.NextDouble() + 100;
                water.WaterFlow = (decimal)random.NextDouble() * 10;
                currentDate = currentDate.AddHours(1).AddMinutes(5).AddSeconds(1);
                lists.Add(water);
            }
            m_chartWaterFlow.AddWaters(lists);
        }
        #endregion 测试数据



        private void cmb_RainShape_SelectedIndexChanged(object sender, EventArgs e)
        {

            //if (cmb_RainShape.Text.Equals("时段雨量"))
            //{

            //}
            //else if (cmb_RainShape.Text.Equals("差值雨量"))
            //{
            //}
            //else if (cmb_RainShape.Text.Equals("日雨量"))
            //{

            //}

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }
    }
}
