using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Hydrology.DBManager;
using Hydrology.Entity;
using Hydrology.DataMgr;
using Hydrology.Forms;

namespace Hydrology.CControls
{
    /// <summary>
    /// 实时数据的表格控件，包含表头以及内容
    /// </summary>
    class CDataGridViewEvaRTD : CExDataGridView
    {
        #region 静态常量
        public static readonly string CS_StationID = "站点编号";
        public static readonly string CS_StationName = "站名";
        public static readonly string CS_StationType = "站点类型";
        public static readonly string CS_TimeCollected = "采集时间";
        public static readonly string CS_TimeReceived = "接收时间";

        public static readonly string CS_LastDayRain = "昨日降雨(mm)";
        public static readonly string CS_LastDayEva = "昨日蒸发(mm)";
        public static readonly string CS_DayRain= "当前降雨(mm)";
        public static readonly string CS_DayEva = "当前蒸发(mm)";

        public static readonly string CS_Eva = "时段蒸发(mm)";
        public static readonly string CS_Rain = "时段雨量(mm)";
        public static readonly string CS_Temp = "蒸发器水温(℃)";
        public static readonly string CS_RawE = "蒸发器液位(mm)";
        public static readonly string CS_RawP = "雨量器液位(mm)";
        //public static readonly string CS_RawT = "蒸发器水温";
        public static readonly string CS_RawV = "电池电压(V)";
        public static readonly string CS_ACT = "排注水操作";
        public static readonly string CS_evaPZ = "蒸发桶排水(mm)";
        public static readonly string CS_DH = "高度差(mm)";
        public static readonly string CS_NullUIStr = "---";
        #endregion ///<STATIC_STRING

        #region 数据成员
        private ToolStripMenuItem m_itemWarningFirst;
        private ToolStripMenuItem m_itemNormalFirst;
        private ToolStripMenuItem m_itemRecoverDefault; //恢复默认

        private List<CEntityRealEva> m_listEntityRTD;  //与表格内容一致的RTD内容
        #endregion

        public CDataGridViewEvaRTD()
            : base()
        {
            m_listEntityRTD = new List<CEntityRealEva>();
            // 设定标题栏,默认有个隐藏列
            this.Header = new string[] 
            {
                CS_StationName,  CS_StationID,CS_StationType, CS_TimeCollected,
                CS_TimeReceived,CS_LastDayRain,CS_LastDayEva,CS_DayRain,CS_DayEva,
                CS_Eva, CS_Rain, CS_Temp,
                CS_RawE,CS_RawP,CS_RawV,CS_ACT,CS_evaPZ,
                CS_DH
            };
            // 隐藏延迟列，串口列
            base.HideColomns = new int[] { 2,4,16,17 };
            // 设置一页的数量
            this.PageRowCount = CDBParams.GetInstance().UIPageRowCount;
            //this.PageRowCount = 300;   //  默认一页显示数量

            //DataGridViewNumericUpDownColumn stationId = new DataGridViewNumericUpDownColumn()
            //{
            //    Minimum = 0,
            //    Maximum = 65537,
            //    DecimalPlaces = 0 /*好像是设置小数点后面的位数*/

            //};
            //this.SetColumnEditStyle(1, stationId);

            // 绑定消息，用于按照ID排序
            //this.SortCompare += new DataGridViewSortCompareEventHandler(this.EHSortCompare);
            //this.CellValueNeeded += new DataGridViewCellValueEventHandler(this.EHCellValueNeeded);
            //this.sort

            // 设置数据类型
            //this.Columns[CS_StationId].ValueType = typeof(int);
            // m_dataTable.Columns[CS_StationId].DataType = System.Type.GetType("System.Int32");
        }

        public void RecalculateHeaderSize()
        {
            this.Columns[3].Width = 180; //删除列宽度为20
        }
        // 添加一条实时记录
        public void AddRTD(CEntityRealEva entity)
        {
            base.AddRow(GetUIShowStringList(entity).ToArray(), EDataState.ENormal);
            m_listEntityRTD.Add(entity);
        }

        /// <summary>
        /// 更新最新记录，根据stationId的唯一性
        /// </summary>
        /// <param name="entity"></param>
        public bool UpdateRTD(CEntityRealEva entity)
        {
            // 先找到ID所在的行
            m_mutexDataTable.WaitOne();
            for (int i = 0; i < m_dataTable.Rows.Count; ++i)
            {
                if (m_dataTable.Rows[i][CS_StationID].ToString().Trim() == entity.StrStationID.Trim())
                {
                    // 找到匹配，直接更新并退出
                    // this.Invoke(new Action(() => { m_bindingSource.ResumeBinding(); }), null);
                    //this.DataSource = m_bindingSource;
                    m_mutexDataTable.ReleaseMutex();
                    return base.UpdateRowData(i,
                        GetUIShowStringList(entity).ToArray(),
                        EDataState.ENormal);
                }

            }//end of for
            //this.DataSource = m_bindingSource;
            for (int i = 0; i < m_listEntityRTD.Count; ++i)
            {
                if (m_listEntityRTD[i].StrStationID.Trim() == entity.StrStationID.Trim())
                {
                    //找到匹配更新
                    m_listEntityRTD[i] = entity;
                    break;
                }
            }
            m_mutexDataTable.ReleaseMutex();
            return false;
        }

        /// <summary>
        /// 刷新实时数据超时的状态值，超过1个半小时，为黄色，超过2个半小时为红色
        /// </summary>
        /// <returns></returns>
        public bool RefreshRTDTimeOutStatus()
        {
            base.m_mutexDataTable.WaitOne();
            List<int> updateRowIndex = new List<int>();
            List<EDataState> updateStatus = new List<EDataState>();
            for (int i = 0; i < m_dataTable.Rows.Count; ++i)
            {
                DateTime time = DateTime.Parse(m_dataTable.Rows[i][CS_TimeCollected].ToString());
                EDataState state = GetDataStatus(time);
                if (state == EDataState.EError || state == EDataState.EWarning)
                {
                    // 只有显示红色或者黄色才更新，否则不变
                    updateRowIndex.Add(i);
                    updateStatus.Add(state);
                }
            }
            base.m_mutexDataTable.ReleaseMutex();
            return base.UpdateRowRangeStatus(updateRowIndex, updateStatus);
        }

        /// <summary>
        /// 根据表格的内容生成实时数据列表
        /// </summary>
        /// <returns></returns>
        public List<CEntityRealEva> GetRTDList()
        {
            List<CEntityRealEva> result = new List<CEntityRealEva>(m_listEntityRTD.ToArray());
            return result;
        }

        #region OVERRIDE
        // 初始化菜单项
        protected override void InitContextMenu()
        {
            base.InitContextMenu();

            // 添加警告优先，正常优先，以及筛选器选项
            m_itemWarningFirst = new ToolStripMenuItem() { Text = "警告优先" };
            m_itemNormalFirst = new ToolStripMenuItem() { Text = "正常优先" };
            m_itemRecoverDefault = new ToolStripMenuItem() { Text = "恢复默认" };
            // ToolStripMenuItem itemFilter = new ToolStripMenuItem() { Text = "筛选条件..." };

            ToolStripSeparator seperator = new ToolStripSeparator();
            m_contextMenu.Items.Add(seperator);
            m_contextMenu.Items.Add(m_itemWarningFirst);
            m_contextMenu.Items.Add(m_itemNormalFirst);
            m_contextMenu.Items.Add(m_itemRecoverDefault);
            seperator = new ToolStripSeparator();
            //m_contextMenu.Items.Add(seperator);
            //m_contextMenu.Items.Add(itemFilter);

            // 绑定消息
            m_itemWarningFirst.Click += EHWarningFirst;
            m_itemNormalFirst.Click += EHNormalFirst;
            m_itemRecoverDefault.Click += EHRecoverDefault;
        }
        // 重写下一页事件
        protected override void OnMenuNextPage(object sender, EventArgs e)
        {
            //base.OnMenuNextPage(sender, e);
            //MessageBox.Show("next page in RTD");
            //base.m_iCurrentPage += 1;
            //由于内存分页，并不需要将iCurrentPage加1
            base.OnMenuNextPage(sender, e);
            RefreshRowVisiable();
            //this.Update();
        }
        // 重写上一页事件
        protected override void OnMenuPreviousPage(object sender, EventArgs e)
        {
            //base.m_iCurrentPage -= 1;
            base.OnMenuPreviousPage(sender, e);
            RefreshRowVisiable();
        }
        /// <summary>
        /// 重写首页事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMenuFirstPage(object sender, EventArgs e)
        {
            base.OnMenuFirstPage(sender, e);
            RefreshRowVisiable();
        }

        /// <summary>
        /// 重写尾页事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMenuLastPage(object sender, EventArgs e)
        {
            base.OnMenuLastPage(sender, e);
            RefreshRowVisiable();
        }
        // 重写绑定完后的数据响应时间
        protected override void ExDataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // 计算总页数
            base.m_iTotalPage = (int)Math.Ceiling((this.Rows.Count / (double)m_iPageRowCount));
            // 刷新显示的页码
            RefreshRowVisiable();
            // 隐藏状态列,并计算总页数
            base.ExDataBindingComplete(sender, e);
        }

        // 重写格式化数据
        protected override void ExCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // 绘制颜色
            base.ExCellFormatting(sender, e);
            // 更改行号，也可以设置成显示其他信息
            DataGridViewRow CurrentRow = this.Rows[e.RowIndex];
            CurrentRow.HeaderCell.Value = Convert.ToString(e.RowIndex + 1);
        }

        protected override void OnSizeChanged(object sender, EventArgs e)
        {
            base.OnSizeChanged(sender, e);
            this.Columns[3].Width = 125;
        }
        // 重写双击事件
        protected override void OnCellMouseDoubleClick(DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
                {
                    if (base.m_arrayStrHeader[e.ColumnIndex] == CS_StationID || base.m_arrayStrHeader[e.ColumnIndex] == CS_StationName ||
                        base.m_arrayStrHeader[e.ColumnIndex] == CS_Rain || base.m_arrayStrHeader[e.ColumnIndex] == CS_Eva)
                    {

                        //获取当前单元格的行号和列号
                        int rowIndex = e.RowIndex;
                        string stationid = this.Rows[rowIndex].Cells[CS_StationID].Value.ToString();
                        string stationname = this.Rows[rowIndex].Cells[CS_StationName].Value.ToString();
                        CStationDataMgrForm form = new CStationDataMgrForm()
                        {
                            Editable = false
                        };
                        if (form != null)
                        {
                            form.cmbStation.Text = string.Format("({0,-8}|{1})", stationid, stationname);
                            form.cmbQueryInfo.SelectedIndex = 0;
                            form.cmb_TimeSelect.SelectedIndex = 0;
                            form.dptTimeStart.Value = DateTime.Now.AddDays(-1);
                            form.dptTimeEnd.Value = DateTime.Now;
                            //form.btnQuery.PerformClick();
                            form.queryData();
                            form.ShowDialog();
                        }
                    }
                    if (base.m_arrayStrHeader[e.ColumnIndex] == CS_DayEva || base.m_arrayStrHeader[e.ColumnIndex] == CS_DayRain ||
                        base.m_arrayStrHeader[e.ColumnIndex] == CS_LastDayEva || base.m_arrayStrHeader[e.ColumnIndex] == CS_LastDayRain)
                    {

                        //获取当前单元格的行号和列号
                        int rowIndex = e.RowIndex;
                        string stationid = this.Rows[rowIndex].Cells[CS_StationID].Value.ToString();
                        string stationname = this.Rows[rowIndex].Cells[CS_StationName].Value.ToString();
                        CStationDataMgrForm form = new CStationDataMgrForm()
                        {
                            Editable = false
                        };
                        if (form != null)
                        {
                            form.cmbStation.Text = string.Format("({0,-8}|{1})", stationid, stationname);
                            form.cmbQueryInfo.SelectedIndex = 0;
                            form.cmb_TimeSelect.SelectedIndex = 1;
                            form.dptTimeStart.Value = DateTime.Now.AddDays(-30);
                            form.dptTimeEnd.Value = DateTime.Now;
                            //form.btnQuery.PerformClick();
                            form.queryData();
                            form.ShowDialog();
                        }
                    }
                    //双击进入水温查询
                    if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Temp)
                    {

                        //获取当前单元格的行号和列号
                        int rowIndex = e.RowIndex;
                        string stationid = this.Rows[rowIndex].Cells[CS_StationID].Value.ToString();
                        string stationname = this.Rows[rowIndex].Cells[CS_StationName].Value.ToString();
                        CStationDataMgrForm form = new CStationDataMgrForm()
                        {
                            Editable = false
                        };
                        if (form != null)
                        {
                            form.cmbStation.Text = string.Format("({0,-8}|{1})", stationid, stationname);
                            form.cmbQueryInfo.SelectedIndex = 2;
                            form.cmb_TimeSelect.SelectedIndex = 0;
                            form.dptTimeStart.Value = DateTime.Now.AddDays(-1);
                            form.dptTimeEnd.Value = DateTime.Now;
                            //form.btnQuery.PerformClick();
                            form.queryData();
                            form.ShowDialog();
                        }
                    }

                    //双击进入电压查询
                    if (base.m_arrayStrHeader[e.ColumnIndex] == CS_RawV)
                    {

                        //获取当前单元格的行号和列号
                        int rowIndex = e.RowIndex;
                        string stationid = this.Rows[rowIndex].Cells[CS_StationID].Value.ToString();
                        string stationname = this.Rows[rowIndex].Cells[CS_StationName].Value.ToString();
                        CStationDataMgrForm form = new CStationDataMgrForm()
                        {
                            Editable = false
                        };
                        if (form != null)
                        {
                            form.cmbStation.Text = string.Format("({0,-8}|{1})", stationid, stationname);
                            form.cmbQueryInfo.SelectedIndex = 1;
                            form.cmb_TimeSelect.SelectedIndex = 0;
                            form.dptTimeStart.Value = DateTime.Now.AddDays(-1);
                            form.dptTimeEnd.Value = DateTime.Now;
                            //form.btnQuery.PerformClick();
                            form.queryData();
                            form.ShowDialog();
                        }
                    }
                    //双击进入原始数据查询
                    if(base.m_arrayStrHeader[e.ColumnIndex] == CS_RawE 
                        || base.m_arrayStrHeader[e.ColumnIndex] == CS_RawP)
                    {
                        //获取当前单元格的行号和列号
                        int rowIndex = e.RowIndex;
                        string stationid = this.Rows[rowIndex].Cells[CS_StationID].Value.ToString();
                        string stationname = this.Rows[rowIndex].Cells[CS_StationName].Value.ToString();
                        CStationDataMgrForm form = new CStationDataMgrForm()
                        {
                            Editable = false
                        };
                        if (form != null)
                        {
                            form.cmbStation.Text = string.Format("({0,-8}|{1})", stationid, stationname);
                            form.cmbQueryInfo.SelectedIndex = 0;
                            form.cmb_ViewStyle.SelectedIndex = 2;
                            form.cmb_TimeSelect.SelectedIndex = 2;
                            form.dptTimeStart.Value = DateTime.Now.AddDays(-1);
                            form.dptTimeEnd.Value = DateTime.Now;
                            //form.btnQuery.PerformClick();
                            form.queryData();
                            form.ShowDialog();
                        }
                    }
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
        }

        #endregion

        #region 事件处理
        private void EHNormalFirst(object sender, EventArgs e)
        {
            //正常优先
            m_itemNormalFirst.Checked = true;
            m_itemWarningFirst.Checked = false;
            m_itemRecoverDefault.Checked = false;
            if (this.Columns.Count > 0)
            {
                // 排序
                this.Sort(this.Columns[S_C_State], System.ComponentModel.ListSortDirection.Ascending);
            }
        }
        private void EHWarningFirst(object sender, EventArgs e)
        {
            //警告优先
            m_itemNormalFirst.Checked = false;
            m_itemWarningFirst.Checked = true;
            m_itemRecoverDefault.Checked = false;
            if (this.Columns.Count > 0)
            {
                // 排序
                this.Sort(this.Columns[S_C_State], System.ComponentModel.ListSortDirection.Descending);
            }
        }
        private void EHRecoverDefault(object sender, EventArgs e)
        {
            //恢复默认
            m_itemNormalFirst.Checked = false;
            m_itemWarningFirst.Checked = false;
            m_itemRecoverDefault.Checked = true;
            if (this.Columns.Count > 0)
            {
                // 排序
                this.Sort(this.Columns[CS_StationID], System.ComponentModel.ListSortDirection.Ascending);
                //DataRow[] rows = base.m_dataTable.Select(string.Format("{0} LIKE '*'", CS_StationId), string.Format("Convert({0},'System.Int32') ASC", CS_StationId));
            }
        }
        //private void EHSortCompare(object sender, DataGridViewSortCompareEventArgs e)
        //{
        //     // 用来处理站点排序
        //    if (e.Column.HeaderText == CS_StationId)
        //    {
        //        e.SortResult = int.Parse(e.CellValue1.ToString()).CompareTo(int.Parse(e.CellValue2.ToString()));
        //        e.Handled = true;
        //    }
        //}

        //private void EHCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        //{
        //    if (e.ColumnIndex == this.Columns[CS_StationId].Index)
        //    {
        //        int parsedValue = 0;
        //        if (!int.TryParse(e.Value.ToString(), out parsedValue))
        //        {
        //            if (int.TryParse(this[e.ColumnIndex, e.RowIndex].Value.ToString(), out parsedValue))
        //                e.Value = parsedValue;
        //            else
        //                e.Value = 0;
        //        }
        //    }
        //}
        #endregion  事件处理

        #region 帮助方法
        // 刷新当前行显示的记录
        private void RefreshRowVisiable()
        {
            if (BindingContext == null)
            {
                return;
            }
            CurrencyManager cm = (CurrencyManager)BindingContext[this.DataSource];
            cm.SuspendBinding(); //挂起数据绑定
            // 控制显示行数,内存中分页
            int startRow = (base.m_iCurrentPage - 1) * base.m_iPageRowCount;
            int maxRow = Math.Min(startRow + base.m_iPageRowCount, this.Rows.Count);
            for (int i = 0; i < this.Rows.Count; ++i)
            {
                if (i >= startRow && i < maxRow)
                {
                    // 显示该条记录
                    this.Rows[i].Visible = true;
                }
                else
                {
                    // 隐藏这条记录
                    this.Rows[i].Visible = false;
                }
            }
            // 恢复数据绑定
            cm.ResumeBinding();
        }

        // 计算当前数值的状态信息
        private CExDataGridView.EDataState GetDataStatus(CEntityRealTime entity)
        {
            // 看是否设定最大值，以及最小值，还有变化值之类的东东啦
            //             CExDataGridView.EDataState state = CExDataGridView.EDataState.ENormal;
            //             TimeSpan span = entity.TimeReceived - entity.TimeDeviceGained;
            //             int offset = Math.Abs(span.Minutes);
            //             if (offset > 5 && offset < 10)
            //             {
            //                 state = CExDataGridView.EDataState.EWarning;
            //             }
            //             else if (offset > 10)
            //             {
            //                 state = CExDataGridView.EDataState.EError;
            //             }
            //             return state;
            CExDataGridView.EDataState state = GetDataStatus(entity.TimeDeviceGained);
            if (state == EDataState.ENormal)
            {
                // 只有没超时的时候，才会显示实时数据的状态
                switch (entity.ERTDState)
                {
                    case ERTDDataState.EError: state = CExDataGridView.EDataState.EPink; break;
                    case ERTDDataState.ENormal: state = CExDataGridView.EDataState.ENormal; break;
                    case ERTDDataState.EWarning: state = CExDataGridView.EDataState.EWarning; break;
                }
            }
            return state;
        }

        /// <summary>
        /// 根据实时数据的时间计算数据的显示状态,一个半小时以内为黄色，2个半小时以上为红色，其余为正常
        /// </summary>
        /// <param name="timeCollect"></param>
        /// <returns></returns>
        private CExDataGridView.EDataState GetDataStatus(DateTime timeCollect)
        {
            // 只有正常的情况下，才计算延迟
            TimeSpan span = DateTime.Now - timeCollect;
            double offset = Math.Abs(span.TotalMinutes);
            if (offset > 90 && offset < 150)
            {
                // 黄色显示
                return CExDataGridView.EDataState.EWarning;
            }
            else if (offset > 150)
            {
                // 红色显示
                return CExDataGridView.EDataState.EError;
            }
            return CExDataGridView.EDataState.ENormal;
        }

        private List<string> GetUIShowStringList(CEntityRealEva entity)
        {
            List<string> result = new List<string>();
            result.Add(entity.StrStationName);
            result.Add(entity.StrStationID);
           
            //result.Add(CEnumHelper.StationTypeToUIStr(entity.StationType));
            result.Add("蒸发站");
            result.Add(entity.TimeDeviceGained.ToString());
            result.Add(entity.TimeReceived.ToString());

            result.Add(entity.LastDayRain >= 0 ? entity.LastDayRain.Value.ToString() : CS_NullUIStr);
            result.Add(entity.LastDayEva >= 0 ? entity.LastDayEva.Value.ToString() : CS_NullUIStr);
            result.Add(entity.DayRain >= 0 ? entity.DayRain.Value.ToString() : CS_NullUIStr);
            result.Add(entity.DayEva >= 0 ? entity.DayEva.Value.ToString() : CS_NullUIStr);

            result.Add(entity.Eva >= 0 ? entity.Eva.Value.ToString() : CS_NullUIStr);
            result.Add(entity.Rain.HasValue ? entity.Rain.ToString() : CS_NullUIStr);
            result.Add(entity.Temperature.HasValue ? entity.Temperature.ToString() : CS_NullUIStr);
            result.Add(entity.RawEva >= 0 ? entity.RawEva.Value.ToString() : CS_NullUIStr);
            result.Add(entity.RawRain >= 0 ? entity.RawRain.Value.ToString() : CS_NullUIStr);
            result.Add(entity.RawVoltage >= 0 ? entity.RawVoltage.Value.ToString() : CS_NullUIStr);
            if(entity.act == null)
            {
                result.Add(CS_NullUIStr);
            }
            else if(entity.act.Contains("PP"))
            {
                result.Add("雨量筒排水" + entity.TimeDeviceGained.ToString());
            }else if (entity.act.Contains("PE"))
            {
                result.Add("蒸发器排水" + entity.TimeDeviceGained.ToString());
            }else if (entity.act.Contains("ZE"))
            {
                result.Add("蒸发器补水" + entity.TimeDeviceGained.ToString());
            }
            else if (entity.act.Contains("ER"))
            {
                result.Add("异常扰动" + entity.TimeDeviceGained.ToString());
            }
            else
            {
                result.Add(CS_NullUIStr);
            }
            if(entity.evaPZ != null && entity.evaPZ != "")
            {
                result.Add(entity.evaPZ.ToString());
            }
            else
            {
                result.Add("-");
            }
            result.Add(entity.DH.HasValue ? entity.DH.ToString() : CS_NullUIStr);
            return result;
        }
        #endregion ///<HELP_METHOD
    }
}
