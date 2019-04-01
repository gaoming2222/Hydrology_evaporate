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
    public class CChartEva : CExChart
    {
        #region 静态常量
        // 数据源中蒸发的列名
        public static readonly string CS_CN_Eva = "eva";

        // 数据源中降雨的列名
        public static readonly string CS_CN_Rain = "Rain";

        // 蒸发的坐标名字
        public static readonly string CS_AsixY_Name = "蒸发(mm)";

        // 降雨的坐标名字
        public static readonly string CS_AsixY2_Name = "降雨(mm)";

        // 图表名字
        public static readonly string CS_Chart_Name = "蒸发降雨过程线";

        // 蒸发线条名字
        public static readonly string CS_Serial_Name_Eva = "Serial_Eva";

        // 降雨线条名字
        public static readonly string CS_Serial_Name_Rain = "Serial_Rain";

        #endregion 静态常量

        private Nullable<decimal> m_dMinEva; //最小的蒸发值,实际值，不是计算后的值
        private Nullable<decimal> m_dMaxEva; //最大的蒸发值，实际值，不是计算后的值

        private Nullable<decimal> m_dMinRain; //最小的降雨，实际值，不是计算后的值
        private Nullable<decimal> m_dMaxRain; //最大的降雨，实际值，不是计算后的值

        private Nullable<DateTime> m_maxDateTime;   //最大的日期
        private Nullable<DateTime> m_minDateTime;   //最小的日期

        private Series m_serialEva;         //蒸发过程线
        private Series m_serialRain;         //降雨过程线

        private Legend m_legend;     //图例

        private MenuItem m_MIEvaSerial; //蒸发
        private MenuItem m_MIRainSerial;  //降雨

        private IHEvaProxy m_proxyHEva;
        private IDEvaProxy m_proxyDEva;

        public CChartEva()
            : base()
        {
            // 设定数据表的列
            base.m_dataTable.Columns.Add(CS_CN_DateTime, typeof(DateTime));
            base.m_dataTable.Columns.Add(CS_CN_Eva, typeof(Decimal));
            base.m_dataTable.Columns.Add(CS_CN_Rain, typeof(Decimal));
        }
        // 外部添加蒸发降雨接口
        public void AddEvas(List<CEntityEva> Evas)
        {
            m_dMinEva = null;
            m_dMaxEva = null;
            foreach (CEntityEva entity in Evas)
            {
                //    if (Eva.Eva > 0 && Eva.Rain > 0)

                // 判断蒸发最大值和最小值
                if (m_dMinEva.HasValue)
                {
                    m_dMinEva = m_dMinEva > entity.Eva ? entity.Eva : m_dMinEva;
                }
                else
                {
                    m_dMinEva = entity.Eva;
                }
                if (m_dMaxEva.HasValue)
                {
                    m_dMaxEva = m_dMaxEva < entity.Eva ? entity.Eva : m_dMaxEva;
                }
                else
                {
                    m_dMaxEva = entity.Eva;
                }
                // 判断降雨的最大值和最小值
                if (m_dMinRain.HasValue)
                {
                    m_dMinRain = m_dMinRain > entity.Rain ? entity.Rain : m_dMinRain;
                }
                else
                {
                    m_dMinRain = entity.Rain;
                }
                if (m_dMaxRain.HasValue)
                {
                    m_dMaxRain = m_dMaxRain < entity.Rain ? entity.Rain : m_dMaxRain;
                }
                else
                {
                    m_dMaxRain = entity.Rain;
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

                if (entity.Eva != -9999 && entity.Rain >= 0)
                {
                    //赋值到内部数据表中
                    m_dataTable.Rows.Add(entity.TimeCollect, entity.Eva, entity.Rain);
                    // m_dataTable.Rows.Add(Eva.TimeCollect, Eva.Eva);
                }
                //  if( Eva.Rain != -9999)
                //{
                //    m_dataTable.Rows.Add(Eva.TimeCollect, Eva.Rain);
                //}


            }
            if (Evas.Count >= 3)
            {
                // 蒸发和降雨最大值和最小值
                decimal offset = 0;
                m_dMaxEva = m_dMaxEva == null ? 0 : m_dMaxEva;
                m_dMinEva = m_dMinEva == null ? 0 : m_dMinEva;
                if (m_dMaxEva != m_dMinEva)
                {
                    offset = (m_dMaxEva.Value - m_dMinEva.Value) * (decimal)0.1;
                }
                else
                {
                    // 如果相等的话
                    offset = (decimal)m_dMaxEva * (decimal)0.1;
                }
                m_chartAreaDefault.AxisY.Maximum = (double)(m_dMaxEva + offset);
                m_chartAreaDefault.AxisY.Minimum = (double)(m_dMinEva - offset);
                m_chartAreaDefault.AxisY.Minimum = m_chartAreaDefault.AxisY.Minimum >= 0 ? m_chartAreaDefault.AxisY.Minimum : 0;
                if (offset == 0)
                {
                    // 人为赋值
                    m_chartAreaDefault.AxisY.Maximum = m_chartAreaDefault.AxisY.Minimum + 10;
                }

                if (m_dMaxRain.HasValue && m_dMinRain.HasValue)
                {
                    if (m_dMaxRain != m_dMinRain)
                    {
                        offset = (m_dMaxRain.Value - m_dMinRain.Value) * (decimal)0.1;
                    }
                    else
                    {
                        offset = (decimal)m_dMaxRain / 2;
                    }
                    m_chartAreaDefault.AxisY2.Maximum = (double)(m_dMaxRain + offset);
                    m_chartAreaDefault.AxisY2.Minimum = (double)(m_dMinRain - offset);
                    m_chartAreaDefault.AxisY2.Minimum = m_chartAreaDefault.AxisY2.Minimum >= 0 ? m_chartAreaDefault.AxisY2.Minimum : 0;

                    if (offset == 0)
                    {
                        m_chartAreaDefault.AxisY2.Maximum = m_chartAreaDefault.AxisY2.Minimum + 10; //人为赋值
                    }
                    m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
                }
                else
                {
                    // 没有降雨数据
                    // 人为降雨最大最小值
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
                    m_dMaxRain = null;
                    m_dMaxEva = null;
                    m_dMinRain = null;
                    m_dMinEva = null;
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
                    m_dMaxRain = null;
                    m_dMaxEva = null;
                    m_dMinRain = null;
                    m_dMinEva = null;
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
            m_proxyHEva = proxy;
        }

        //降雨
        private void EH_MI_RainSerial(object sender, EventArgs e)
        {
            m_MIRainSerial.Checked = !m_MIRainSerial.Checked;
            m_serialRain.Enabled = m_MIRainSerial.Checked;
            //m_serialRain.Enabled = true;
            //m_serialEvaState.Enabled = true;
            if (m_MIRainSerial.Checked && (!m_MIEvaSerial.Checked))
            {
                // 开启右边的滚动条，当且仅当降雨可见的时候
                //m_chartAreaDefault.AxisY2.ScaleView.Zoomable = false;
                //m_chartAreaDefault.AxisY2.ScrollBar.Enabled = true;
                //m_chartAreaDefault.CursorY.AxisType = AxisType.Secondary;
                //m_serialRain.YAxisType = AxisType.Primary;
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
                //m_serialRain.YAxisType = AxisType.Secondary;
                //m_serialEvaState.YAxisType = AxisType.Primary;
            }
            //降雨过程线
            if (m_serialRain.Enabled)
            {
                // 降雨可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
            }
            else
            {
                // 降雨不可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.False;
            }
            //蒸发过程线
            if (m_serialEva.Enabled)
            {
                // 蒸发可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.True;
            }
            else
            {
                // 蒸发不可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.False;
            }
        }

        //蒸发
        private void EH_MI_EvaSerial(object sender, EventArgs e)
        {
            m_MIEvaSerial.Checked = !m_MIEvaSerial.Checked;
            //蒸发
            m_serialEva.Enabled = m_MIEvaSerial.Checked;
            if (m_MIRainSerial.Checked && (!m_MIEvaSerial.Checked))
            {
                // 开启右边的滚动条，当且仅当降雨可见的时候
                m_chartAreaDefault.CursorY.IsUserEnabled = false;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = false;
                //m_chartAreaDefault.CursorY.AxisType = AxisType.Secondary;
                //m_serialRain.YAxisType = AxisType.Primary;
            }
            else
            {
                // 关闭右边的滚动条
                m_chartAreaDefault.CursorY.IsUserEnabled = true;
                m_chartAreaDefault.CursorY.IsUserSelectionEnabled = true;
                //m_chartAreaDefault.AxisY2.ScrollBar.Enabled = false;
                //m_chartAreaDefault.AxisY2.ScaleView.Zoomable = true;
                //m_serialRain.YAxisType = AxisType.Secondary;
                //m_serialEvaState.YAxisType = AxisType.Primary;
            }
            if (m_serialRain.Enabled)
            {
                // 降雨可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.True;
            }
            else
            {
                // 降雨不可见
                m_chartAreaDefault.AxisY2.Enabled = AxisEnabled.False;
            }
            if (m_serialEva.Enabled)
            {
                // 蒸发可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.True;
            }
            else
            {
                // 蒸发不可见
                m_chartAreaDefault.AxisY.Enabled = AxisEnabled.False;
            }
        }


        #region 重载

        // 重新右键菜单
        protected override void InitContextMenu()
        {
            base.InitContextMenu();
            m_MIRainSerial = new MenuItem() { Text = "降雨线" };
            m_MIEvaSerial = new MenuItem() { Text = "蒸发线" };
            base.m_contextMenu.MenuItems.Add(0, m_MIEvaSerial);
            base.m_contextMenu.MenuItems.Add(0, m_MIRainSerial);
            m_MIRainSerial.Checked = true;
            m_MIEvaSerial.Checked = true;

            m_MIEvaSerial.Click += new EventHandler(EH_MI_EvaSerial);
            m_MIRainSerial.Click += new EventHandler(EH_MI_RainSerial);
        }


        // 重写UI,设置XY轴名字
        protected override void InitUI()
        {
            base.InitUI();
            // 设置图表标题
            m_title.Text = CS_Chart_Name;

            // 设置蒸发和降雨格式
            m_chartAreaDefault.AxisY.LabelStyle.Format = "0.00";
            m_chartAreaDefault.AxisY2.LabelStyle.Format = "0.00";

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

            #region 蒸发
            m_serialEva = this.Series.Add(CS_Serial_Name_Eva);
            m_serialEva.Name = "蒸发"; //用来显示图例的
            m_serialEva.ChartArea = CS_ChartAreaName_Default;
            m_serialEva.ChartType = SeriesChartType.Line; //如果点数过多， 画图很慢，初步测试不能超过2000个
            m_serialEva.BorderWidth = 1;
            //m_serialEvaState.Color = Color.FromArgb(22,99,1);
            m_serialEva.Color = Color.Red;
            //m_serialEvaState.BorderColor = Color.FromArgb(120, 147, 190);
            //m_serialEvaState.ShadowColor = Color.FromArgb(64, 0, 0, 0);
            //m_serialEvaState.ShadowOffset = 2;
            //  设置时间类型,对于serial来说
            m_serialEva.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            m_serialEva.IsXValueIndexed = false; // 自己计算X值，以及边界值,否则翻译不出正确的值

            //  绑定数据
            m_serialEva.XValueMember = CS_CN_DateTime;
            m_serialEva.YValueMembers = CS_CN_Eva;

            m_serialEva.YAxisType = AxisType.Primary;
            #endregion 蒸发

            #region 降雨
            m_serialRain = this.Series.Add(CS_Serial_Name_Rain);
            m_serialRain.Name = "降雨"; //用来显示图例的
            m_serialRain.ChartArea = CS_ChartAreaName_Default;
            m_serialRain.ChartType = SeriesChartType.Line; //如果点数过多， 画图很慢，初步测试不能超过2000个
            m_serialRain.BorderWidth = 1;
            //m_serialRain.BorderColor = Color.FromArgb(120, 147, 190);
            m_serialRain.Color = Color.Blue;
            //m_serialRain.ShadowOffset = 2;
            //  设置时间类型,对于serial来说
            m_serialRain.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            m_serialRain.IsXValueIndexed = false; // 自己计算X值，以及边界值,否则翻译不出正确的值

            //  绑定数据
            m_serialRain.XValueMember = CS_CN_DateTime;
            m_serialRain.YValueMembers = CS_CN_Rain;
            m_serialRain.YAxisType = AxisType.Secondary;
            #endregion 降雨

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
                Debug.WriteLine("CChartEva UpdateAnnotationByDataPoint Failed");
                return;
            }
            String prompt = "";
            DateTime dateTimeX = DateTime.FromOADate(point.XValue);
            if (m_serialEva.Points.Contains(point))
            {
                // 蒸发
                prompt = string.Format("蒸发：{0:0.00}\n日期：{1}\n时间：{2}", point.YValues[0],
                            dateTimeX.ToString("yyyy-MM-dd"),
                            dateTimeX.ToString("HH:mm:ss"));
            }
            else
            {
                // 就是降雨了
                prompt = string.Format("降雨：{0:0.00}\n日期：{1}\n时间：{2}", point.YValues[0],
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
            m_dMaxRain = null;
            m_dMinRain = null;
            m_dMaxEva = null;
            m_dMinEva = null;
        }

        #endregion 重载
    }
}
