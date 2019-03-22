using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hydrology.Entity;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using Hydrology.DBManager.Interface;

namespace Hydrology.CControls
{
    public class CChartSanility : CExChart
    {
        #region 静态常量
        // 数据源中盐度的列名
        public static readonly string CS_CN_Sanility = "sanility";

        // 数据源中电导率的列名
        public static readonly string CS_CN_Conduct = "conduct";

        // 盐度的坐标名字
        public static readonly string CS_AsixY_Name = "盐度(%)";

        // 电导率的坐标名字
        public static readonly string CS_AsixY2_Name = "电导率(mS/cm)";

        // 图表名字
        public static readonly string CS_Chart_Name = "盐度电导率过程线";

        // 盐度线条名字
        public static readonly string CS_Serial_Name_Sanility = "Serial_Sanility";

        // 电导率线条名字
        public static readonly string CS_Serial_Name_Conduct = "Serial_Conduct";

        #endregion 静态常量

        private Nullable<decimal> m_dMinSanility; //最小的盐度值,实际值，不是计算后的值
        private Nullable<decimal> m_dMaxSanility; //最大的盐度值，实际值，不是计算后的值

        private Nullable<decimal> m_dMinConductivity; //最小的电导率，实际值，不是计算后的值
        private Nullable<decimal> m_dMaxConductivity; //最大的电导率，实际值，不是计算后的值

        private Nullable<DateTime> m_maxDateTime;   //最大的日期
        private Nullable<DateTime> m_minDateTime;   //最小的日期

        private Series m_serialSanility;         //盐度过程线
        private Series m_serialConduct;         //电导率过程线

        private Legend m_legend;     //图例

        private MenuItem m_MISanilitySerial; //盐度
        private MenuItem m_MIConductSerial;  //电导率

        private IEvaProxy m_proxySanility;

        public CChartSanility()
            : base()
        {
            // 设定数据表的列
            base.m_dataTable.Columns.Add(CS_CN_DateTime, typeof(DateTime));
            base.m_dataTable.Columns.Add(CS_CN_Sanility, typeof(Decimal));
            base.m_dataTable.Columns.Add(CS_CN_Conduct, typeof(Decimal));
        }
        // 外部添加盐度电导率接口
        public void AddSanilities(List<CEntityEva> Sanilities)
        {
            m_dMinSanility = null;
            m_dMaxSanility = null;
            foreach (CEntityEva entity in Sanilities)
            {
                //    if (Sanility.Sanility > 0 && Sanility.Conductivity > 0)
                
                    // 判断盐度最大值和最小值
                    if (m_dMinSanility.HasValue)
                    {
                        m_dMinSanility = m_dMinSanility > entity.Eva ? entity.Eva : m_dMinSanility;
                    }
                    else
                    {
                        m_dMinSanility = entity.Eva;
                    }
                    if (m_dMaxSanility.HasValue)
                    {
                        m_dMaxSanility = m_dMaxSanility < entity.Eva ? entity.Eva : m_dMaxSanility;
                    }
                    else
                    {
                        m_dMaxSanility = entity.Eva;
                    }
                    // 判断电导率的最大值和最小值
                    if (m_dMinConductivity.HasValue)
                    {
                        m_dMinConductivity = m_dMinConductivity > entity.Voltage ? entity.Voltage : m_dMinConductivity;
                    }
                    else
                    {
                        m_dMinConductivity = entity.Voltage;
                    }
                    if (m_dMaxConductivity.HasValue)
                    {
                        m_dMaxConductivity = m_dMaxConductivity < entity.Voltage ? entity.Voltage : m_dMaxConductivity;
                    }
                    else
                    {
                        m_dMaxConductivity = entity.Voltage;
                    }

                // 判断日期, 更新日期最大值和最小值
                if (m_maxDateTime.HasValue)
                {
                    m_maxDateTime = m_maxDateTime < entity.TimeCollect ? entity.TimeCollect : m_maxDateTime;
                }
                else
                {
                    m_maxDateTime = entity.TimeCollect;
                }
                if (m_minDateTime.HasValue)
                {
                    m_minDateTime = m_minDateTime > entity.TimeCollect ? entity.TimeCollect : m_minDateTime;
                }
                else
                {
                    m_minDateTime = entity.TimeCollect;
                }

                if (entity.Eva != -9999 && entity.Voltage >= 0)
                {
                    //赋值到内部数据表中
                    m_dataTable.Rows.Add(entity.TimeCollect, entity.Eva, entity.Voltage);
                    // m_dataTable.Rows.Add(Sanility.TimeCollect, Sanility.Sanility);
                }
                //  if( Sanility.Conductivity != -9999)
                //{
                //    m_dataTable.Rows.Add(Sanility.TimeCollect, Sanility.Conductivity);
                //}


            }
            if (Sanilities.Count >= 3)
            {
                // 盐度和电导率最大值和最小值
                decimal offset = 0;
                m_dMaxSanility = m_dMaxSanility == null ? 0 : m_dMaxSanility;
                m_dMinSanility = m_dMinSanility == null ? 0 : m_dMinSanility;
                if (m_dMaxSanility != m_dMinSanility)
                {
                    offset = (m_dMaxSanility.Value - m_dMinSanility.Value) * (decimal)0.1;
                }
                else
                {
                    // 如果相等的话
                    offset = (decimal)m_dMaxSanility * (decimal)0.1;
                }
                m_chartAreaDefault.AxisY.Maximum = (double)(m_dMaxSanility + offset);
                m_chartAreaDefault.AxisY.Minimum = (double)(m_dMinSanility - offset);
                m_chartAreaDefault.AxisY.Minimum = m_chartAreaDefault.AxisY.Minimum >= 0 ? m_chartAreaDefault.AxisY.Minimum : 0;
                if (offset == 0)
                {
                    // 人为赋值
                    m_chartAreaDefault.AxisY.Maximum = m_chartAreaDefault.AxisY.Minimum + 10;
                }

                if (m_dMaxConductivity.HasValue && m_dMinConductivity.HasValue)
                {
                    if (m_dMaxConductivity != m_dMinConductivity)
                    {
                        offset = (m_dMaxConductivity.Value - m_dMinConductivity.Value) * (decimal)0.1;
                    }
                    else
                    {
                        offset = (decimal)m_dMaxConductivity / 2;
                    }
                    m_chartAreaDefault.AxisY2.Maximum = (double)(m_dMaxConductivity + offset);
                    m_chartAreaDefault.AxisY2.Minimum = (double)(m_dMinConductivity - offset);
                    m_chartAreaDefault.AxisY2.Minimum = m_chartAreaDefault.AxisY2.Minimum >= 0 ? m_chartAreaDefault.AxisY2.Minimum : 0;

                    if (offset == 0)
                    {
                        m_chartAreaDefault.AxisY2.Maximum = m_chartAreaDefault.AxisY2.Minimum + 10; //人为赋值
                    }
                    m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
                }
                else
                {
                    // 没有电导率数据
                    // 人为电导率最大最小值
                    m_chartAreaDefault.AxisY2.Maximum = (double)100;
                    m_chartAreaDefault.AxisY2.Minimum = (double)0;
                    m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.False;
                }
                // 设置日期最大值和最小值
                m_chartAreaDefault.AxisX.Minimum = m_minDateTime.Value.ToOADate();
                m_chartAreaDefault.AxisX.Maximum = m_maxDateTime.Value.ToOADate();

                this.DataBind(); //更新数据到图表
            }
        }

        public bool SetFilter(string iStationId, DateTime timeStart, DateTime timeEnd, bool TimeSelect)
        {
            m_annotation.Visible = false;
            ClearAllDatas();
            m_proxySanility.SetFilter(iStationId, timeStart, timeEnd, TimeSelect);
            if (-1 == m_proxySanility.GetPageCount())
            {
                // 查询失败
                // MessageBox.Show("数据库忙，查询失败，请稍后再试！");
                return false;
            }
            else
            {
                // 并查询数据，显示第一页
                m_dMaxConductivity = null;
                m_dMaxSanility = null;
                m_dMinConductivity = null;
                m_dMinSanility = null;
                int iTotalPage = m_proxySanility.GetPageCount();
                int rowcount = m_proxySanility.GetRowCount();
                if (rowcount > CI_Chart_Max_Count)
                {
                    // 数据量太大，退出绘图
                    MessageBox.Show("查询结果集太大，自动退出绘图");
                    return false;
                }
                for (int i = 0; i < iTotalPage; ++i)
                {
                    // 查询所有的数据
                    this.AddSanilities(m_proxySanility.GetPageData(i + 1, false));
                }
                return true;
            }
        }

        public void InitDataSource(IEvaProxy proxy)
        {
            m_proxySanility = proxy;
        }

        //电导率
        private void EH_MI_ConductSerial(object sender, EventArgs e)
        {
            m_MIConductSerial.Checked = !m_MIConductSerial.Checked;
            m_serialConduct.Enabled = m_MIConductSerial.Checked;
            //m_serialConductivity.Enabled = true;
            //m_serialSanilityState.Enabled = true;
            if (m_MIConductSerial.Checked && (!m_MISanilitySerial.Checked))
            {
                // 开启右边的滚动条，当且仅当电导率可见的时候
                //m_chartAreaDefault.AxisY2.ScaleView.Zoomable = false;
                //m_chartAreaDefault.AxisY2.ScrollBar.Enabled = true;
                //m_chartAreaDefault.CursorY.AxisType = AxisType.Secondary;
                //m_serialConductivity.YAxisType = AxisType.Primary;
                m_chartAreaDefault.CursorY.IsUserEnabled = false;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = false;
            }
            else
            {
                // 关闭右边的滚动条
                m_chartAreaDefault.CursorY.IsUserEnabled = true;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = true;
                //m_chartAreaDefault.AxisY2.ScrollBar.Enabled = false;
                //m_chartAreaDefault.AxisY.ScrollBar.Enabled = true;
                //m_serialConductivity.YAxisType = AxisType.Secondary;
                //m_serialSanilityState.YAxisType = AxisType.Primary;
            }
            //电导率过程线
            if (m_serialConduct.Enabled)
            {
                // 电导率可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
            }
            else
            {
                // 电导率不可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.False;
            }
            //盐度过程线
            if (m_serialSanility.Enabled)
            {
                // 盐度可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.True;
            }
            else
            {
                // 盐度不可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.False;
            }
        }

        //盐度
        private void EH_MI_SanilitySerial(object sender, EventArgs e)
        {
            m_MISanilitySerial.Checked = !m_MISanilitySerial.Checked;
            //盐度
            m_serialSanility.Enabled = m_MISanilitySerial.Checked;
            if (m_MIConductSerial.Checked && (!m_MISanilitySerial.Checked))
            {
                // 开启右边的滚动条，当且仅当电导率可见的时候
                m_chartAreaDefault.CursorY.IsUserEnabled = false;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = false;
                //m_chartAreaDefault.CursorY.AxisType = AxisType.Secondary;
                //m_serialConductivity.YAxisType = AxisType.Primary;
            }
            else
            {
                // 关闭右边的滚动条
                m_chartAreaDefault.CursorY.IsUserEnabled = true;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = true;
                //m_chartAreaDefault.AxisY2.ScrollBar.Enabled = false;
                //m_chartAreaDefault.AxisY2.ScaleView.Zoomable = true;
                //m_serialConductivity.YAxisType = AxisType.Secondary;
                //m_serialSanilityState.YAxisType = AxisType.Primary;
            }
            if (m_serialConduct.Enabled)
            {
                // 电导率可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
            }
            else
            {
                // 电导率不可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.False;
            }
            if (m_serialSanility.Enabled)
            {
                // 盐度可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.True;
            }
            else
            {
                // 盐度不可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.False;
            }
        }


        #region 重载

        // 重新右键菜单
        protected override void InitContextMenu()
        {
            base.InitContextMenu();
            m_MIConductSerial = new MenuItem() { Text = "电导率线" };
            m_MISanilitySerial = new MenuItem() { Text = "盐度线" };
            base.m_contextMenu.MenuItems.Add(0, m_MISanilitySerial);
            base.m_contextMenu.MenuItems.Add(0, m_MIConductSerial);
            m_MIConductSerial.Checked = true;
            m_MISanilitySerial.Checked = true;

            m_MISanilitySerial.Click += new EventHandler(EH_MI_SanilitySerial);
            m_MIConductSerial.Click += new EventHandler(EH_MI_ConductSerial);
        }


        // 重写UI,设置XY轴名字
        protected override void InitUI()
        {
            base.InitUI();
            // 设置图表标题
            m_title.Text = CS_Chart_Name;

            // 设置盐度和电导率格式
            m_chartAreaDefault.AxisY.LabelStyle.Format = "0.000";
            m_chartAreaDefault.AxisY2.LabelStyle.Format = "0.000";

            // m_chartAreaDefault.AxisX.Title = CS_Asix_DateTime; //不显示名字
            m_chartAreaDefault.AxisY.Title = CS_AsixY_Name;
            m_chartAreaDefault.AxisY2.Title = CS_AsixY2_Name;
            m_chartAreaDefault.AxisY.IsStartedFromZero = true;
            m_chartAreaDefault.AxisY2.IsStartedFromZero = true;

            m_chartAreaDefault.AxisX.TextOrientation = TextOrientation.Horizontal;
            m_chartAreaDefault.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            //m_chartAreaDefault.AxisX.a
            m_chartAreaDefault.AxisX.LabelStyle.Format = "MM-dd HH";
            m_chartAreaDefault.AxisX.LabelStyle.Angle = 90;

            #region 盐度
            m_serialSanility = this.Series.Add(CS_Serial_Name_Sanility);
            m_serialSanility.Name = "盐度"; //用来显示图例的
            m_serialSanility.ChartArea = CS_ChartAreaName_Default;
            m_serialSanility.ChartType = SeriesChartType.Line; //如果点数过多， 画图很慢，初步测试不能超过2000个
            m_serialSanility.BorderWidth = 1;
            //m_serialSanilityState.Color = Color.FromArgb(22,99,1);
            m_serialSanility.Color = Color.Red;
            //m_serialSanilityState.BorderColor = Color.FromArgb(120, 147, 190);
            //m_serialSanilityState.ShadowColor = Color.FromArgb(64, 0, 0, 0);
            //m_serialSanilityState.ShadowOffset = 2;
            //  设置时间类型,对于serial来说
            m_serialSanility.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            m_serialSanility.IsXValueIndexed = false; // 自己计算X值，以及边界值,否则翻译不出正确的值

            //  绑定数据
            m_serialSanility.XValueMember = CS_CN_DateTime;
            m_serialSanility.YValueMembers = CS_CN_Sanility;

            m_serialSanility.YAxisType = AxisType.Primary;
            #endregion 盐度

            #region 电导率
            m_serialConduct = this.Series.Add(CS_Serial_Name_Conduct);
            m_serialConduct.Name = "电导率"; //用来显示图例的
            m_serialConduct.ChartArea = CS_ChartAreaName_Default;
            m_serialConduct.ChartType = SeriesChartType.Line; //如果点数过多， 画图很慢，初步测试不能超过2000个
            m_serialConduct.BorderWidth = 1;
            //m_serialConductivity.BorderColor = Color.FromArgb(120, 147, 190);
            m_serialConduct.Color = Color.Blue;
            //m_serialConductivity.ShadowOffset = 2;
            //  设置时间类型,对于serial来说
            m_serialConduct.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            m_serialConduct.IsXValueIndexed = false; // 自己计算X值，以及边界值,否则翻译不出正确的值

            //  绑定数据
            m_serialConduct.XValueMember = CS_CN_DateTime;
            m_serialConduct.YValueMembers = CS_CN_Conduct;
            m_serialConduct.YAxisType = AxisType.Secondary;
            #endregion 电导率

            #region 图例
            m_legend = new System.Windows.Forms.DataVisualization.Charting.Legend();
            m_legend.Alignment = System.Drawing.StringAlignment.Center;
            m_legend.BackColor = System.Drawing.Color.Transparent;
            m_legend.DockedToChartArea = CS_ChartAreaName_Default;
            m_legend.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            m_legend.IsDockedInsideChartArea = false;
            this.Legends.Add(m_legend);
            #endregion 图例
        }

        // 显示提示，并重新定位,xPosition有效
        protected override void UpdateAnnotationByDataPoint(DataPoint point)
        {
            if (null == point)
            {
                Debug.WriteLine("CChartSanility UpdateAnnotationByDataPoint Failed");
                return;
            }
            String prompt = "";
            DateTime dateTimeX = DateTime.FromOADate(point.XValue);
            if (m_serialSanility.Points.Contains(point))
            {
                // 盐度
                prompt = string.Format("盐度：{0:0.00}\n日期：{1}\n时间：{2}", point.YValues[0],
                            dateTimeX.ToString("yyyy-MM-dd"),
                            dateTimeX.ToString("HH:mm:ss"));
            }
            else
            {
                // 就是电导率了
                prompt = string.Format("电导率：{0:0.00}\n日期：{1}\n时间：{2}", point.YValues[0],
                            dateTimeX.ToString("yyyy-MM-dd"),
                            dateTimeX.ToString("HH:mm:ss"));
            }

            m_chartAreaDefault.CursorY.Position = point.YValues[0]; // 重新设置Y的值
            m_chartAreaDefault.CursorX.Position = point.XValue; //重新设置X的值

            // 显示注释
            m_annotation.Text = prompt;
            //m_annotation.X = point.XValue;
            //m_annotation.Y = point.YValues[0];
            m_annotation.AnchorDataPoint = point;
            m_annotation.Visible = true;
        }

        // 同步放大和缩小
        protected override void EH_AxisViewChanged(object sender, ViewEventArgs e)
        {
            // 同步放大
            double iYMin = m_chartAreaDefault.AxisY.Minimum;
            double iYMax = m_chartAreaDefault.AxisY.Maximum;
            double iY2Min = m_chartAreaDefault.AxisY2.Minimum;
            double iY2Max = m_chartAreaDefault.AxisY2.Maximum;
            double yPosition = m_chartAreaDefault.AxisY.ScaleView.Position;
            m_chartAreaDefault.AxisY2.ScaleView.Position = iY2Min + ((yPosition - iYMin) / (iYMax - iYMin)) * (iY2Max - iY2Min);

            double size = m_chartAreaDefault.AxisY.ScaleView.Size;
            m_chartAreaDefault.AxisY2.ScaleView.Size = size * (iY2Max - iY2Min) / (iYMax - iYMin);
        }

        // 重载清空所有数据
        protected override void ClearAllDatas()
        {
            base.ClearAllDatas();
            m_maxDateTime = null;
            m_minDateTime = null;
            m_dMaxConductivity = null;
            m_dMinConductivity = null;
            m_dMaxSanility = null;
            m_dMinSanility = null;
        }

        #endregion 重载
    }
}
