using Hydrology.DBManager.DB.SQLServer;
using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Hydrology.CControls
{
    public class CChartVoltage : CExChart
    {
        #region 静态常量
        // 数据源中温度的列名
        public static readonly string CS_CN_Temp = "Temp";

        // 数据源中电压的列名
        public static readonly string CS_CN_Voltage = "Voltage";

        // 温度的坐标名字
        //public static readonly string CS_AsixY_Name = "温度(℃)";

        // 电压的坐标名字
        public static readonly string CS_AsixY2_Name = "电压(V)";

        // 图表名字
        public static readonly string CS_Chart_Name = "电压过程线";

        // 温度线条名字
        public static readonly string CS_Serial_Name_Temp = "Serial_Temp";

        // 电压线条名字
        public static readonly string CS_Serial_Name_Voltage = "Serial_Voltage";

        #endregion 静态常量

        private Nullable<decimal> m_dMinTemp; //最小的温度值,实际值，不是计算后的值
        private Nullable<decimal> m_dMaxTemp; //最大的温度值，实际值，不是计算后的值

        private Nullable<decimal> m_dMinVoltage; //最小的电压，实际值，不是计算后的值
        private Nullable<decimal> m_dMaxVoltage; //最大的电压，实际值，不是计算后的值

        private Nullable<DateTime> m_maxDateTime;   //最大的日期
        private Nullable<DateTime> m_minDateTime;   //最小的日期

        private Series m_serialTemp;         //温度过程线
        private Series m_serialVoltage;         //电压过程线

        private Legend m_legend;     //图例

        private MenuItem m_MITempSerial; //温度
        private MenuItem m_MIVoltageSerial;  //电压

        private IHEvaProxy m_proxyHEva;
        private IDEvaProxy m_proxyDEva;

        public CChartVoltage()
            : base()
        {
            // 设定数据表的列
            base.m_dataTable.Columns.Add(CS_CN_DateTime, typeof(DateTime));
            base.m_dataTable.Columns.Add(CS_CN_Temp, typeof(Decimal));
            base.m_dataTable.Columns.Add(CS_CN_Voltage, typeof(Decimal));
        }
        // 外部添加温度电压接口
        public void AddEvas(List<CEntityEva> Evas)
        {
            m_dMinTemp = null;
            m_dMaxTemp = null;
            foreach (CEntityEva entity in Evas)
            {
                //    if (Eva.Eva > 0 && Eva.Voltage > 0)

                // 判断温度最大值和最小值
                if (m_dMinTemp.HasValue)
                {
                    m_dMinTemp = m_dMinTemp > entity.Temperature ? entity.Temperature : m_dMinTemp;
                }
                else
                {
                    m_dMinTemp = entity.Temperature;
                }
                if (m_dMaxTemp.HasValue)
                {
                    m_dMaxTemp = m_dMaxTemp < entity.Temperature ? entity.Temperature : m_dMaxTemp;
                }
                else
                {
                    m_dMaxTemp = entity.Temperature;
                }
                // 判断电压的最大值和最小值
                if (m_dMinVoltage.HasValue)
                {
                    m_dMinVoltage = m_dMinVoltage > entity.Voltage ? entity.Voltage : m_dMinVoltage;
                }
                else
                {
                    m_dMinVoltage = entity.Voltage;
                }
                if (m_dMaxVoltage.HasValue)
                {
                    m_dMaxVoltage = m_dMaxVoltage < entity.Voltage ? entity.Voltage : m_dMaxVoltage;
                }
                else
                {
                    m_dMaxVoltage = entity.Voltage;
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

                if (entity.Temperature != -9999 && entity.Voltage >= 0)
                {
                    //赋值到内部数据表中
                    m_dataTable.Rows.Add(entity.TimeCollect, entity.Temperature, entity.Voltage);
                    // m_dataTable.Rows.Add(Eva.TimeCollect, Eva.Eva);
                }
                //  if( Eva.Voltage != -9999)
                //{
                //    m_dataTable.Rows.Add(Eva.TimeCollect, Eva.Voltage);
                //}


            }
            if (Evas.Count >= 3)
            {
                // 温度和电压最大值和最小值
                decimal offset = 0;
                m_dMaxTemp = m_dMaxTemp == null ? 0 : m_dMaxTemp;
                m_dMinTemp = m_dMinTemp == null ? 0 : m_dMinTemp;
                if (m_dMaxTemp != m_dMinTemp)
                {
                    offset = (m_dMaxTemp.Value - m_dMinTemp.Value) * (decimal)0.1;
                }
                else
                {
                    // 如果相等的话
                    offset = (decimal)m_dMaxTemp * (decimal)0.1;
                }
                m_chartAreaDefault.AxisY.Maximum = (double)(m_dMaxTemp + offset);
                m_chartAreaDefault.AxisY.Minimum = (double)(m_dMinTemp - offset);
                m_chartAreaDefault.AxisY.Minimum = m_chartAreaDefault.AxisY.Minimum >= 0 ? m_chartAreaDefault.AxisY.Minimum : 0;
                if (offset == 0)
                {
                    // 人为赋值
                    m_chartAreaDefault.AxisY.Maximum = m_chartAreaDefault.AxisY.Minimum + 10;
                }

                if (m_dMaxVoltage.HasValue && m_dMinVoltage.HasValue)
                {
                    if (m_dMaxVoltage != m_dMinVoltage)
                    {
                        offset = (m_dMaxVoltage.Value - m_dMinVoltage.Value) * (decimal)0.1;
                    }
                    else
                    {
                        offset = (decimal)m_dMaxVoltage / 2;
                    }
                    m_chartAreaDefault.AxisY2.Maximum = (double)(m_dMaxVoltage + offset);
                    m_chartAreaDefault.AxisY2.Minimum = (double)(m_dMinVoltage - offset);
                    m_chartAreaDefault.AxisY2.Minimum = m_chartAreaDefault.AxisY2.Minimum >= 0 ? m_chartAreaDefault.AxisY2.Minimum : 0;

                    if (offset == 0)
                    {
                        m_chartAreaDefault.AxisY2.Maximum = m_chartAreaDefault.AxisY2.Minimum + 10; //人为赋值
                    }
                    m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
                }
                else
                {
                    // 没有电压数据
                    // 人为电压最大最小值
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
            m_proxyHEva = new CSQLHEva();
            if (TimeSelect)
            {
                m_proxyHEva.SetFilter(iStationId, timeStart, timeEnd);
                if (-1 == m_proxyHEva.GetPageCount())
                {
                    // 查询失败
                    // MessageBox.Show("数据库忙，查询失败，请稍后再试！");
                    return false;
                }
                else
                {
                    // 并查询数据，显示第一页
                    m_dMaxVoltage = null;
                    m_dMaxTemp = null;
                    m_dMinVoltage = null;
                    m_dMinTemp = null;
                    int iTotalPage = m_proxyHEva.GetPageCount();
                    int rowcount = m_proxyHEva.GetRowCount();
                    if (rowcount > CI_Chart_Max_Count)
                    {
                        // 数据量太大，退出绘图
                        MessageBox.Show("查询结果集太大，自动退出绘图");
                        return false;
                    }
                    for (int i = 0; i < iTotalPage; ++i)
                    {
                        // 查询所有的数据
                        this.AddEvas(m_proxyHEva.GetPageData(i + 1, false));
                    }
                    return true;
                }
            }
            else
            {
                m_proxyDEva.SetFilter(iStationId, timeStart, timeEnd);
                if (-1 == m_proxyDEva.GetPageCount())
                {
                    // 查询失败
                    // MessageBox.Show("数据库忙，查询失败，请稍后再试！");
                    return false;
                }
                else
                {
                    // 并查询数据，显示第一页
                    m_dMaxVoltage = null;
                    m_dMaxTemp = null;
                    m_dMinVoltage = null;
                    m_dMinTemp = null;
                    int iTotalPage = m_proxyDEva.GetPageCount();
                    int rowcount = m_proxyDEva.GetRowCount();
                    if (rowcount > CI_Chart_Max_Count)
                    {
                        // 数据量太大，退出绘图
                        MessageBox.Show("查询结果集太大，自动退出绘图");
                        return false;
                    }
                    for (int i = 0; i < iTotalPage; ++i)
                    {
                        // 查询所有的数据
                        this.AddEvas(m_proxyDEva.GetPageData(i + 1, false));
                    }
                    return true;
                }
            }
        }

        public void InitDataSource(IDEvaProxy proxy)
        {
            m_proxyDEva = proxy;
        }

        public void InitDataSource(IHEvaProxy proxy)
        {
            m_proxyHEva = new CSQLHEva();
        }

        //电压
        private void EH_MI_VoltageSerial(object sender, EventArgs e)
        {
            m_MIVoltageSerial.Checked = !m_MIVoltageSerial.Checked;
            m_serialVoltage.Enabled = m_MIVoltageSerial.Checked;
            //m_serialVoltage.Enabled = true;
            //m_serialEvaState.Enabled = true;
            if (m_MIVoltageSerial.Checked && (!m_MITempSerial.Checked))
            {
                // 开启右边的滚动条，当且仅当电压可见的时候
                //m_chartAreaDefault.AxisY2.ScaleView.Zoomable = false;
                //m_chartAreaDefault.AxisY2.ScrollBar.Enabled = true;
                //m_chartAreaDefault.CursorY.AxisType = AxisType.Secondary;
                //m_serialVoltage.YAxisType = AxisType.Primary;
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
                //m_serialVoltage.YAxisType = AxisType.Secondary;
                //m_serialEvaState.YAxisType = AxisType.Primary;
            }
            //电压过程线
            if (m_serialVoltage.Enabled)
            {
                // 电压可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
            }
            else
            {
                // 电压不可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.False;
            }
            //温度过程线
            if (m_serialTemp.Enabled)
            {
                // 温度可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.True;
            }
            else
            {
                // 温度不可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.False;
            }
        }

        //温度
        private void EH_MI_EvaSerial(object sender, EventArgs e)
        {
            m_MITempSerial.Checked = !m_MITempSerial.Checked;
            //温度
            m_serialTemp.Enabled = false;
            if (m_MIVoltageSerial.Checked && (!m_MITempSerial.Checked))
            {
                // 开启右边的滚动条，当且仅当电压可见的时候
                m_chartAreaDefault.CursorY.IsUserEnabled = false;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = false;
                //m_chartAreaDefault.CursorY.AxisType = AxisType.Secondary;
                //m_serialVoltage.YAxisType = AxisType.Primary;
            }
            else
            {
                // 关闭右边的滚动条
                m_chartAreaDefault.CursorY.IsUserEnabled = true;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = true;
                //m_chartAreaDefault.AxisY2.ScrollBar.Enabled = false;
                //m_chartAreaDefault.AxisY2.ScaleView.Zoomable = true;
                //m_serialVoltage.YAxisType = AxisType.Secondary;
                //m_serialEvaState.YAxisType = AxisType.Primary;
            }
            if (m_serialVoltage.Enabled)
            {
                // 电压可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
            }
            else
            {
                // 电压不可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.False;
            }
            if (m_serialTemp.Enabled)
            {
                // 温度可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.True;
            }
            else
            {
                // 温度不可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.False;
            }
        }


        #region 重载

        // 重新右键菜单
        protected override void InitContextMenu()
        {
            base.InitContextMenu();
            m_MIVoltageSerial = new MenuItem() { Text = "电压线" };
            m_MITempSerial = new MenuItem() { Text = "温度线" };
            base.m_contextMenu.MenuItems.Add(0, m_MITempSerial);
            base.m_contextMenu.MenuItems.Add(0, m_MIVoltageSerial);
            m_MIVoltageSerial.Checked = true;
            m_MITempSerial.Checked = true;

            m_MITempSerial.Click += new EventHandler(EH_MI_EvaSerial);
            m_MIVoltageSerial.Click += new EventHandler(EH_MI_VoltageSerial);
        }


        // 重写UI,设置XY轴名字
        protected override void InitUI()
        {
            base.InitUI();
            // 设置图表标题
            m_title.Text = CS_Chart_Name;

            // 设置温度和电压格式
            m_chartAreaDefault.AxisY.LabelStyle.Format = "0.00";
            m_chartAreaDefault.AxisY2.LabelStyle.Format = "0.00";

            // m_chartAreaDefault.AxisX.Title = CS_Asix_DateTime; //不显示名字
            //m_chartAreaDefault.AxisY.Title = CS_AsixY_Name;
            m_chartAreaDefault.AxisY2.Title = CS_AsixY2_Name;
            m_chartAreaDefault.AxisY.IsStartedFromZero = true;
            m_chartAreaDefault.AxisY2.IsStartedFromZero = true;

            m_chartAreaDefault.AxisX.TextOrientation = TextOrientation.Horizontal;
            m_chartAreaDefault.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            //m_chartAreaDefault.AxisX.a
            m_chartAreaDefault.AxisX.LabelStyle.Format = "MM-dd HH";
            m_chartAreaDefault.AxisX.LabelStyle.Angle = 90;


            #region 电压
            m_serialVoltage = this.Series.Add(CS_Serial_Name_Voltage);
            m_serialVoltage.Name = "电压"; //用来显示图例的
            m_serialVoltage.ChartArea = CS_ChartAreaName_Default;
            m_serialVoltage.ChartType = SeriesChartType.Line; //如果点数过多， 画图很慢，初步测试不能超过2000个
            m_serialVoltage.BorderWidth = 1;
            //m_serialVoltage.BorderColor = Color.FromArgb(120, 147, 190);
            m_serialVoltage.Color = Color.Blue;
            //m_serialVoltage.ShadowOffset = 2;
            //  设置时间类型,对于serial来说
            m_serialVoltage.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            m_serialVoltage.IsXValueIndexed = false; // 自己计算X值，以及边界值,否则翻译不出正确的值

            //  绑定数据
            m_serialVoltage.XValueMember = CS_CN_DateTime;
            m_serialVoltage.YValueMembers = CS_CN_Voltage;
            m_serialVoltage.YAxisType = AxisType.Secondary;
            #endregion 电压

            #region 温度
            //m_serialTemp = this.Series.Add(CS_Serial_Name_Temp);
            //m_serialTemp.Name = "温度"; //用来显示图例的
            //m_serialTemp.ChartArea = CS_ChartAreaName_Default;
            //m_serialTemp.ChartType = SeriesChartType.Line; //如果点数过多， 画图很慢，初步测试不能超过2000个
            //m_serialTemp.BorderWidth = 1;
            ////m_serialEvaState.Color = Color.FromArgb(22,99,1);
            //m_serialTemp.Color = Color.Red;
            ////m_serialEvaState.BorderColor = Color.FromArgb(120, 147, 190);
            ////m_serialEvaState.ShadowColor = Color.FromArgb(64, 0, 0, 0);
            ////m_serialEvaState.ShadowOffset = 2;
            ////  设置时间类型,对于serial来说
            //m_serialTemp.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            //m_serialTemp.IsXValueIndexed = false; // 自己计算X值，以及边界值,否则翻译不出正确的值

            ////  绑定数据
            //m_serialTemp.XValueMember = CS_CN_DateTime;
            //m_serialTemp.YValueMembers = CS_CN_Temp;

            //m_serialTemp.YAxisType = AxisType.Primary;
            #endregion 温度

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
                Debug.WriteLine("CChartTemp UpdateAnnotationByDataPoint Failed");
                return;
            }
            String prompt = "";
            DateTime dateTimeX = DateTime.FromOADate(point.XValue);
            if (false)
            {
                // 温度
                prompt = string.Format("温度：{0:0.00}\n日期：{1}\n时间：{2}", point.YValues[0],
                            dateTimeX.ToString("yyyy-MM-dd"),
                            dateTimeX.ToString("HH:mm:ss"));
            }
            else
            {
                // 就是电压了
                prompt = string.Format("电压：{0:0.00}\n日期：{1}\n时间：{2}", point.YValues[0],
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
            m_dMaxVoltage = null;
            m_dMinVoltage = null;
            m_dMaxTemp = null;
            m_dMinTemp = null;
        }

        #endregion 重载
    }
}