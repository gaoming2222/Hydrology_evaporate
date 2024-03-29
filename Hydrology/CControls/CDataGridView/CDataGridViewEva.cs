﻿using Hydrology.DataMgr;
using Hydrology.DBManager;
using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using Hydrology.Forms;
using Hydrology.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Hydrology.CControls
{
    /// <summary>
    /// 蒸发显示表格控件，包括编辑和非编辑模式
    /// </summary>
    class CDataGridViewEva : CExDataGridView
    {
        #region 静态常量
        public static readonly string CS_Delete = "删除";
        public static readonly string CS_StationID = "站号";
        public static readonly string CS_StationName = "站名";
        public static readonly string CS_TimeCollected = "采集时间";
        public static readonly string CS_Eva = "蒸发(mm)";
        public static readonly string CS_Rain = "雨量(mm)";
        public static readonly string CS_Voltage = "电压(V)";
        public static readonly string CS_Temp = "温度值(℃)";
        public static readonly string CS_EvaPZ = "蒸发桶排注水(mm)";
        public static readonly string CS_DH = "蒸发小时排注水量(mm)";
        public static readonly string CS_DDH = "蒸发排注水量(mm)";
        public static readonly string CS_P8 = "8点-20点雨量和(mm)";
        public static readonly string CS_P20 = "20点-8点雨量和(mm)";
        public static readonly string CS_RawEva = "蒸发器示数(mm)";
        public static readonly string CS_RawEvaF = "蒸发器换算后示数(mm)";
        public static readonly string CS_RawRain = "雨量筒示数(mm)";
        public static readonly string CS_RawRainF = "雨量筒换算后示数(mm)";
        public static readonly string CS_eleNeed = "电测针读数(mm)";
        public static readonly string CS_TimeFormat = "yyy-MM-dd HH:mm:ss";
        public static readonly string CS_RawACT = "排注水操作";
        public static readonly string CS_RawContent = "数据说明";
        #endregion  ///<STATIC_STRING

        #region 数据成员
        private bool m_bIsEditable; //编辑模式，默认非编辑模式
        private bool m_bIsHEva; //表格类型，默认为小时表
        private bool m_isREva;
        private List<CEntityEva> m_listUpdated; //更新的蒸发记录
        private List<long> m_listDeleteSanilities;    //删除的蒸发记
        private List<String> m_listDeleteSanilities_StationId;    //删除的蒸发记录
        private List<String> m_listDeleteSanilities_StationDate;    //删除的蒸发记录
        private List<CEntityEva> m_listAddedEva;   //新增的蒸发记录

        // 查询相关信息
        private IHEvaProxy m_proxyHEva;   //蒸发小时表的操作接口
        private IDEvaProxy m_proxyDEva;   //蒸发日表的操作接口
        private IEvaProxy m_proxyEva;     //蒸发原始表操作接口
        private string m_strStaionId;            //查询的测站ID
        private DateTime m_dateTimeStart;   //查询的起点日期
        private DateTime m_dateTimeEnd;     //查询的起点日期

        // 导出到Excel表格
        private ToolStripMenuItem m_menuItemExportToExcel;  //导出到Excel表格
        #endregion ///<DATA_MEMBER

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
            get { return m_bIsHEva; }
            set { SetREva(value); }
        }

        #endregion ///<PROPERTY

        #region 公共方法
        public CDataGridViewEva()
            : base()
        {
            // 设定标题栏,默认有个隐藏列,默认非编辑模式
            this.Header = new string[]
            {
                CS_StationID,CS_StationName,CS_TimeCollected, CS_Eva, CS_Rain ,CS_Temp, CS_Voltage,CS_DH
            };
            //base.HideColomns = new int[] { 7 };

            // 设置一页的数量
            this.PageRowCount = CDBParams.GetInstance().UIPageRowCount;

            // 初始化成员变量
            m_bIsEditable = false; // 默认是非编辑模式
            m_bIsHEva = true;  // 默认是小时表
            m_isREva = false;
            m_listUpdated = new List<CEntityEva>();
            m_listDeleteSanilities = new List<long>();
            m_listDeleteSanilities_StationId = new List<String>();
            m_listDeleteSanilities_StationDate = new List<String>();
            m_listAddedEva = new List<CEntityEva>();

            //初始话原始蒸发信息表
            m_proxyEva = CDBDataMgr.GetInstance().GetEvaProxy();

        }

        /// <summary>
        /// 初始化数据来源，绑定与数据库的数据
        /// </summary>
        /// <param name="proxy"></param>
        public void InitDataSource(IHEvaProxy proxy)
        {
            m_proxyHEva = proxy;
        }

        /// <summary>
        /// 初始化数据来源，绑定与数据库的数据
        /// </summary>
        /// <param name="proxy"></param>
        public void InitDataSource(IDEvaProxy proxy)
        {
            m_proxyDEva = proxy;
        }

        // 设置显示的雨量记录
        public void SetEva(List<CEntityEva> listEva)
        {
            // 清空所有数据,是否一定要这样？好像可以考虑其它方式
            base.m_dataTable.Rows.Clear();
            // 判断状态值
            List<string[]> newRows = new List<string[]>();
            List<EDataState> states = new List<EDataState>();
            
            if (m_bIsHEva)
            {
                if (!m_bIsEditable)
                {
                    string[] newRow;
                    // 只读模式
                    for (int i = 0; i < listEva.Count; ++i)
                    {
                        EDataState state = EDataState.ENormal; //默认所有数据都是正常的
                        string strStationName = "";
                        string strStationId = "";
                        CEntityStation station = CDBDataMgr.Instance.GetStationById(listEva[i].StationID);
                        if (null != station)
                        {

                            strStationName = station.StationName;
                            strStationId = station.StationID;
                        }
                        string dh = "--";
                        if(listEva[i].hourEChange != null)
                        {
                            dh = listEva[i].hourEChange.ToString();
                        }
                        newRow = new string[]
                        {
                        strStationId,
                        strStationName,/*站名*/
                        listEva[i].TimeCollect.ToString(CS_TimeFormat), /*采集时间*/
                        listEva[i].Eva.ToString(), /*蒸发*/
                        listEva[i].Rain.ToString(), /*雨量*/
                        listEva[i].Temperature.ToString(), /*温度*/
                        listEva[i].Voltage.ToString(), /*电压*/
                        dh /*高度差*/
                        };

                        newRows.Add(newRow);
                        states.Add(state);
                    }
                    // 添加到集合的数据表中
                    base.AddRowRange(newRows, states);
                }
                else
                {
                    string[] newRow;
                    // 编辑模式，需要将更新的数据和删除的数据，与当前数据进行合并
                    for (int i = 0; i < listEva.Count; ++i)
                    {
                        EDataState state = EDataState.ENormal; //默认所有数据都是正常的
                        string strStationName = "";
                        CEntityStation station = CDBDataMgr.Instance.GetStationById(listEva[i].StationID);
                        if (null != station)
                        {
                            strStationName = station.StationName;
                        }

                        newRow = new string[]
                        {
                         "False", /*未选中*/
                        m_strStaionId,
                        strStationName,/*站名*/
                        listEva[i].TimeCollect.ToString(CS_TimeFormat), /*采集时间*/
                        listEva[i].Eva.ToString(), /*蒸发*/
                        listEva[i].Rain.ToString(), /*雨量*/
                        listEva[i].Temperature.ToString(), /*温度*/
                        listEva[i].Voltage.ToString() /*电压*/
                        //listEva[i].DH.ToString() /*高度差*/
                        };

                        newRows.Add(newRow);
                        states.Add(state);

                    }
                    // 添加到集合的数据表中
                    base.AddRowRange(newRows, states);
                }
            }
            else
            {
                if (!m_bIsEditable)
                {
                    string[] newRow;
                    // 只读模式
                    for (int i = 0; i < listEva.Count; ++i)
                    {
                        EDataState state = EDataState.ENormal; //默认所有数据都是正常的
                        string strStationName = "";
                        string strStationId = "";
                        CEntityStation station = CDBDataMgr.Instance.GetStationById(listEva[i].StationID);
                        String evaF = "--";
                        if (listEva[i].E.HasValue)
                        {
                            evaF = ((Decimal)(listEva[i].E.Value * station.DWaterMax)).ToString("0.00");
                        }
                        if (null != station)
                        {

                            strStationName = station.StationName;
                            strStationId = station.StationID;
                        }
                        string content = "温度正常";
                        if(listEva[i].Temperature < 4)
                        {
                            content = "温度异常";
                        }
                        newRow = new string[]
                        {
                        strStationId,
                        strStationName,/*站名*/
                        listEva[i].TimeCollect.AddDays(-1).Year.ToString() + "年" + listEva[i].TimeCollect.AddDays(-1).Month.ToString() + "月"  + (listEva[i].TimeCollect.AddDays(-1).Day).ToString() + "日",
                        listEva[i].Eva.ToString(), /*蒸发*/
                        evaF,
                        listEva[i].Rain.ToString(), /*雨量*/

                        listEva[i].Temperature.ToString(), /*温度*/
                        listEva[i].P8.ToString(), /*8-20*/
                        listEva[i].P20.ToString(), /*20-8*/
                        listEva[i].dayEChange.ToString(),
                        content
                        };

                        newRows.Add(newRow);
                        states.Add(state);
                    }
                    // 添加到集合的数据表中
                    base.AddRowRange(newRows, states);
                }
                else
                {
                    string[] newRow;
                    // 编辑模式，需要将更新的数据和删除的数据，与当前数据进行合并
                    for (int i = 0; i < listEva.Count; ++i)
                    {
                        EDataState state = EDataState.ENormal; //默认所有数据都是正常的
                        string strStationName = "";
                        CEntityStation station = CDBDataMgr.Instance.GetStationById(listEva[i].StationID);
                        String evaF = "--";
                        if (listEva[i].E.HasValue)
                        {
                            evaF = ((Decimal)(listEva[i].E.Value * station.DWaterMax)).ToString("0.00");
                        }
                        if (null != station)
                        {
                            strStationName = station.StationName;
                        }

                        newRow = new string[]
                        {
                         "False", /*未选中*/
                        m_strStaionId,
                        strStationName,/*站名*/
                        //listEva[i].TimeCollect.ToString(CS_TimeFormat), /*采集时间*/
                        listEva[i].TimeCollect.AddDays(-1).Year.ToString() + "年" + listEva[i].TimeCollect.AddDays(-1).Month.ToString() + "月"  + (listEva[i].TimeCollect.AddDays(-1).Day).ToString() + "日",
                        listEva[i].Eva.ToString(), /*蒸发*/
                        //evaF,
                        listEva[i].Rain.ToString(), /*雨量*/
                        listEva[i].dayEChange.ToString()
                        //listEva[i].Temperature.ToString(), /*温度*/
                        //listEva[i].P8.ToString(), /*8-20*/
                        //listEva[i].P20.ToString(), /*20-8*/
                        //listEva[i].dayEChange.ToString()
                        };

                        newRows.Add(newRow);
                        states.Add(state);

                    }
                    // 添加到集合的数据表中
                    base.AddRowRange(newRows, states);
                }
            }
        }



        public void SetREva(List<CEntityEva> listEva)
        {
            base.m_dataTable.Rows.Clear();
            List<string[]> newRows = new List<string[]>();
            List<EDataState> states = new List<EDataState>();
            if (!m_bIsEditable)
            {
                string[] newRow;
                // 只读模式
                for (int i = 0; i < listEva.Count; ++i)
                {
                    EDataState state = EDataState.ENormal; //默认所有数据都是正常的
                    string strStationName = "";
                    string strStationId = "";
                    CEntityStation station = CDBDataMgr.Instance.GetStationById(listEva[i].StationID);
                    if (null != station)
                    {

                        strStationName = station.StationName;
                        strStationId = station.StationID;
                    }
                    decimal? evaF = listEva[i].Eva * station.DWaterMax;
                    decimal? rainF = listEva[i].Rain * station.DWaterMin;
                    decimal? neddF = listEva[i].Eva - station.DWaterChange;
                    string act = "--";
                    if (listEva[i] != null)
                    {
                        if (listEva[i].act.ToString().Contains("PP"))
                        {
                            act = "雨量筒排水";
                        }
                        if (listEva[i].act.ToString().Contains("PE"))
                        {
                            act = "蒸发器排水";
                        }
                        if (listEva[i].act.ToString().Contains("ZE"))
                        {
                            act = "蒸发器补水";
                        }
                        if (listEva[i].act.ToString().Contains("EER"))
                        {
                            act = "排水";
                        }
                    }
                    newRow = new string[]
                    {
                        strStationId,
                        strStationName,/*站名*/
                        listEva[i].TimeCollect.ToString(CS_TimeFormat), /*采集时间*/
                        listEva[i].Eva.ToString(), /*蒸发*/
                        Math.Round((Double)evaF,2).ToString(),
                        listEva[i].Rain.ToString(), /*雨量*/
                        Math.Round((Double)rainF,2).ToString(),
                        Math.Round((Double)neddF,2).ToString("0.00"),
                        listEva[i].Temperature.ToString(), /*温度*/
                        //listEva[i].Voltage.ToString(), /*电压*/
                        act /*蒸发模式*/
                    };

                    newRows.Add(newRow);
                    states.Add(state);
                }
                // 添加到集合的数据表中
                base.AddRowRange(newRows, states);
            }
        }

        // 设置查询条件
        public bool SetFilter(string strStationId, DateTime timeStart, DateTime timeEnd,bool isRawData)
        {
            ClearAllState();
            m_strStaionId = strStationId;
            m_dateTimeStart = timeStart;
            m_dateTimeEnd = timeEnd;
            if (isRawData)
            {
                //TODO
                m_proxyEva.SetFilter(strStationId, timeStart, timeEnd,true);
                if (-1 == m_proxyEva.GetPageCount())
                {
                    // 查询失败
                    MessageBox.Show("数据库忙，查询失败，请稍后再试！");
                    return false;
                }
                else
                {
                    // 并查询数据，显示第一页
                    this.OnMenuFirstPage(this, null);
                    base.TotalPageCount = m_proxyHEva.GetPageCount();
                    base.TotalRowCount = m_proxyHEva.GetRowCount();
                    SetREva(m_proxyEva.GetPageData(1, false));
                    return true;
                }
            }
            if (m_bIsHEva)
            {
                m_proxyHEva.SetFilter(strStationId, timeStart, timeEnd);
                if (-1 == m_proxyHEva.GetPageCount())
                {
                    // 查询失败
                    MessageBox.Show("数据库忙，查询失败，请稍后再试！");
                    return false;
                }
                else
                {
                    // 并查询数据，显示第一页
                    this.OnMenuFirstPage(this, null);
                    base.TotalPageCount = m_proxyHEva.GetPageCount();
                    base.TotalRowCount = m_proxyHEva.GetRowCount();
                    SetEva(m_proxyHEva.GetPageData(1, false));
                    return true;
                }
            }
            else
            {
                m_proxyDEva.SetFilter(strStationId, timeStart, timeEnd);
                m_proxyEva.SetFilter(strStationId, timeStart, timeEnd,true);
                if (-1 == m_proxyDEva.GetPageCount())
                {
                    // 查询失败
                    MessageBox.Show("数据库忙，查询失败，请稍后再试！");
                    return false;
                }
                else
                {
                    // 并查询数据，显示第一页
                    this.OnMenuFirstPage(this, null);
                    base.TotalPageCount = m_proxyDEva.GetPageCount();
                    base.TotalRowCount = m_proxyDEva.GetRowCount();
                    List<CEntityEva> dEvaList = m_proxyDEva.GetPageData(1, false);
                    List<CEntityEva> evaList = m_proxyEva.GetPageData(1, false);
                    for(int i = 0;i< dEvaList.Count; i++)
                    {
                        for(int j = 0;j< evaList.Count; j++)
                        {
                            Console.WriteLine("!!!" + i + "!!!" + j);
                            if (evaList[j].TimeCollect.Equals(dEvaList[i].TimeCollect)){
                                dEvaList[i].E = evaList[j].Eva;
                                break;
                            }
                            
                        }
                    }
                    //SetEva(m_proxyDEva.GetPageData(1, false));
                    SetEva(dEvaList);
                    return true;
                }
            }
        }


        public bool SetFilter_1(string strStationId, DateTime timeStart, DateTime timeEnd, bool isRawData)
        {
            ClearAllState();
            m_strStaionId = strStationId;
            m_dateTimeStart = timeStart;
            m_dateTimeEnd = timeEnd;
            if (isRawData)
            {
                //TODO
                m_proxyEva.SetFilter(strStationId, timeStart.AddHours(-1), timeEnd, true);
                if (-1 == m_proxyEva.GetPageCount())
                {
                    // 查询失败
                    MessageBox.Show("数据库忙，查询失败，请稍后再试！");
                    return false;
                }
                else
                {
                    // 并查询数据，显示第一页
                    this.OnMenuFirstPage(this, null);
                    base.TotalPageCount = m_proxyEva.GetPageCount();
                    base.TotalRowCount = m_proxyEva.GetRowCount();
                    List<CEntityEva> evaList = new List<CEntityEva>();
                    List<CEntityEva> eva8List = new List<CEntityEva>();
                    evaList = m_proxyEva.GetPageData(1, false);
                    if(evaList == null)
                    {
                        return false;
                    }
                    for (int i = 0; i < evaList.Count; i++)
                    {
                        //如果是8：00数据
                        if(evaList[i].TimeCollect.Hour == 8 && evaList[i].TimeCollect.Minute == 0)
                        {
                            eva8List.Add(evaList[i]);
                        }
                        else if(evaList[i].act != null && (evaList[i].act == "PP" || evaList[i].act == "PE" || evaList[i].act == "ZE") )
                        {
                            eva8List.Add(evaList[i]);
                            if (i <= evaList.Count - 2)
                            {
                                eva8List.Add(evaList[i + 1]);
                            }
                        }
                    }
                    base.TotalRowCount = eva8List.Count;
                    SetREva(eva8List);
                    return true;
                }
            }
            return true;
            
        }

        // 添加电压记录
        public void AddEva(CEntityEva entity)
        {
            m_listAddedEva.Add(entity);
        }

        // 保存数据
        public override bool DoSave()
        {
            if (this.IsCurrentCellInEditMode)
            {
                //MessageBox.Show("请完成当前的编辑");
                this.EndEdit();
                //return false;
            }
            //base.DoSave();
            // 更新
            GetUpdatedData();
            if (m_listAddedEva.Count > 0 || m_listUpdated.Count > 0 || m_listDeleteSanilities_StationId.Count > 0)
            {
                bool result = true;
                // 增加
                if (m_listAddedEva.Count > 0)
                {
                    //直接添加，不需要等待1分钟
                    if (m_bIsHEva)
                    {
                        m_proxyHEva.AddNewRows_DataModify(m_listAddedEva);
                    }
                    else
                    {
                        m_proxyDEva.AddNewRows_DataModify(m_listAddedEva);
                    }
                    m_listAddedEva.Clear();
                }
                // 修改
                if (m_listUpdated.Count > 0)
                {
                    if (m_bIsHEva)
                    {
                        m_proxyHEva.AddNewRows_DataModify(m_listUpdated);
                    }
                    else
                    {
                        //1.根据时间获取原始数据
                        foreach(CEntityEva item in m_listUpdated)
                        {
                            string stationid = item.StationID;
                            DateTime timeCollect = item.TimeCollect;
                            CEntityEva eva = m_proxyDEva.GetEva1ByTime(stationid, timeCollect);

                            if (eva.dayEChange.HasValue)
                            {
                                //item.dayEChange = eva.dayEChange + (eva.P - item.Rain) + (item.Eva - eva.E);
                                item.dayEChange = item.dayEChange;
                            }
                        }
                        //2.更新数据
                        m_proxyDEva.UpdateRows(m_listUpdated);

                    }
                    m_listUpdated.Clear();
                }
                // 删除
                if (m_listDeleteSanilities_StationId.Count > 0)
                {
                    if (m_bIsHEva)
                    {
                        result = result && m_proxyHEva.DeleteRows(m_listDeleteSanilities_StationId, m_listDeleteSanilities_StationDate);
                    }
                    else
                    {
                        result = result && m_proxyDEva.DeleteRows(m_listDeleteSanilities_StationId, m_listDeleteSanilities_StationDate);

                    }
                    m_listDeleteSanilities.Clear();
                }
                if (result)
                {
                    //MessageBox.Show("保存成功，新增记录稍有延迟");
                }
                else
                {
                    // 保存失败
                    //MessageBox.Show("保存失败");
                    return false;
                }
                if (m_bIsHEva)
                {
                    SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage, true));
                }
                else
                {
                    SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage, true));
                }
            }
            else
            {
                //MessageBox.Show("没有任何修改，无需保存");
            }
            return true;
        }

        // 判断当前是否有修改尚未保存
        public bool IsModifiedUnSaved()
        {
            if (this.IsCurrentCellInEditMode)
            {
                //MessageBox.Show("请完成当前的编辑");
                this.EndEdit();
                //return false;
            }
            if (m_listAddedEva.Count > 0 || base.m_listMaskedDeletedRows.Count > 0 || base.m_listEditedRows.Count > 0)
            {
                return true;
            }
            return false;
        }

        public void SetEditable(bool bEditable)
        {
            m_bIsEditable = bEditable;
            if (m_bIsEditable)
            {
                //this.Header = new string[]
                //{
                //    CS_Delete,CS_StationID,CS_StationName,CS_TimeCollected, CS_Eva, CS_Rain, CS_Temp, CS_Voltage
                //};
                this.Header = new string[]
                {
                     CS_Delete,CS_StationID,CS_StationName,CS_TimeCollected, CS_Eva,CS_Rain,CS_DDH
                };

                //开启编辑模式,设置可编辑列

                DataGridViewCheckBoxColumn deleteCol = new DataGridViewCheckBoxColumn();
                base.SetColumnEditStyle(0, deleteCol);


                // 蒸发编辑列
                DataGridViewNumericUpDownColumn Eva = new DataGridViewNumericUpDownColumn()
                {
                    Minimum = 0,
                    Maximum = 65537,
                    DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                };
                base.SetColumnEditStyle(4, Eva);

                DataGridViewNumericUpDownColumn Rain = new DataGridViewNumericUpDownColumn()
                {
                    Minimum = 0,
                    Maximum = 65537,
                    DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                };
                base.SetColumnEditStyle(5, Rain);

                //蒸发排注水量可编辑
                DataGridViewNumericUpDownColumn DDH = new DataGridViewNumericUpDownColumn()
                {
                    Minimum = -9999,
                    Maximum = 65537,
                    DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                };
                base.SetColumnEditStyle(6, DDH);
                //// 雨量编辑列
                //DataGridViewNumericUpDownColumn Rain = new DataGridViewNumericUpDownColumn()
                //{
                //    Minimum = 0,
                //    Maximum = 65537,
                //    DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                //};
                //base.SetColumnEditStyle(5, Rain);

                //// 温度编辑列
                //DataGridViewNumericUpDownColumn Temp = new DataGridViewNumericUpDownColumn()
                //{
                //    Minimum = 0,
                //    Maximum = 65537,
                //    DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                //};
                //base.SetColumnEditStyle(6, Temp);

                //// 电压编辑列
                //DataGridViewNumericUpDownColumn Vol = new DataGridViewNumericUpDownColumn()
                //{
                //    Minimum = 0,
                //    Maximum = 65537,
                //    DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                //};
                //base.SetColumnEditStyle(7, Vol);

                //// 高度差编辑列
                //DataGridViewNumericUpDownColumn DH = new DataGridViewNumericUpDownColumn()
                //{
                //    Minimum = 0,
                //    Maximum = 65537,
                //    DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                //};
                //base.SetColumnEditStyle(8, DH);

            }
            else
            {
                //this.Columns[2].Width = 125;
                //this.Columns[5].Width = 125;
            }
        }

        public void SetREva(bool bIsREva)
        {
            if (bIsREva)
            {
                if (!m_bIsEditable)
                {
                    //  是原始蒸发表
                    this.Header = new string[]
                    {
                        CS_StationID,CS_StationName,CS_TimeCollected, CS_RawEva,CS_RawEvaF, CS_RawRain,CS_RawRainF,CS_eleNeed, CS_Temp, CS_RawACT

                    };
                    //base.HideColomns = new int[] { };
                }
            }
            
        }

        public void SetHEva(bool bIsHEva)
        {
            m_bIsHEva = bIsHEva;

            if (!m_bIsHEva)
            {
                if (!m_bIsEditable)
                {
                    //  是日蒸发表
                    this.Header = new string[]
                    {
                        CS_StationID,CS_StationName,CS_TimeCollected, CS_Eva,CS_RawEvaF,CS_Rain, CS_Temp, CS_P8, CS_P20,CS_EvaPZ,CS_RawContent
                    };
                }
                else
                {
                    
                    this.Header = new string[]
                    {
                        CS_Delete,CS_StationID,CS_StationName,CS_TimeCollected, CS_Eva, CS_EvaPZ
                    };

                    //开启编辑模式,设置可编辑列

                    DataGridViewCheckBoxColumn deleteCol = new DataGridViewCheckBoxColumn();
                    base.SetColumnEditStyle(0, deleteCol);

                    //// 设置采集时间编辑列
                    //CalendarColumn collectionCol = new CalendarColumn();
                    //base.SetColumnEditStyle(2, collectionCol);

                    // 蒸发编辑列
                    DataGridViewNumericUpDownColumn Eva = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(4, Eva);

                    // 雨量编辑列
                    DataGridViewNumericUpDownColumn Rain = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(5, Rain);

                    // 温度编辑列
                    DataGridViewNumericUpDownColumn Temp = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(6, Temp);

                    // 电压编辑列
                    DataGridViewNumericUpDownColumn Vol = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(7, Vol);

                    // 电压编辑列
                    DataGridViewNumericUpDownColumn P8 = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(8, P8);

                    // 电压编辑列
                    DataGridViewNumericUpDownColumn P20 = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(9, P20);

                }
            }
            else
            {
                if (!m_bIsEditable)
                {
                    //  是日蒸发表
                    this.Header = new string[]
                    {
                        CS_StationID,CS_StationName,CS_TimeCollected, CS_Eva, CS_Rain, CS_Temp, CS_Voltage,CS_DH
                    };
                    //base.HideColomns = new int[] { 7 };
                }
                else
                {
                    this.Header = new string[]
                    {
                        CS_Delete,CS_StationID,CS_StationName,CS_TimeCollected, CS_Eva, CS_Rain, CS_Temp, CS_Voltage
                    };
                    //base.HideColomns = new int[] { };

                    //开启编辑模式,设置可编辑列

                    DataGridViewCheckBoxColumn deleteCol = new DataGridViewCheckBoxColumn();
                    base.SetColumnEditStyle(0, deleteCol);

                    //// 设置采集时间编辑列
                    //CalendarColumn collectionCol = new CalendarColumn();
                    //base.SetColumnEditStyle(2, collectionCol);

                    // 蒸发编辑列
                    DataGridViewNumericUpDownColumn Eva = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(4, Eva);

                    // 雨量编辑列
                    DataGridViewNumericUpDownColumn Rain = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(5, Rain);

                    // 温度编辑列
                    DataGridViewNumericUpDownColumn Temp = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(6, Temp);

                    // 电压编辑列
                    DataGridViewNumericUpDownColumn Vol = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(7, Vol);

                    // 高度差编辑列
                    DataGridViewNumericUpDownColumn DH = new DataGridViewNumericUpDownColumn()
                    {
                        Minimum = 0,
                        Maximum = 65537,
                        DecimalPlaces = 3 /*好像是设置小数点后面的位数*/

                    };
                    base.SetColumnEditStyle(8, DH);
                }
            }
        }

        #endregion ///< PUBLIC_METHOD

        #region 事件处理
        private void EH_MI_ExportToExcel_Click(object sender, EventArgs e)
        {
            // 弹出对话框，并导出到Excel文件
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Excel文件(*.xls)|*.xls|所有文件(*.*)|*.*";
            if (DialogResult.OK == dlg.ShowDialog())
            {
                // 保存到Excel表格中
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add(CS_StationID);
                dataTable.Columns.Add(CS_StationName, typeof(string));
                dataTable.Columns.Add(CS_Eva, typeof(string));
                dataTable.Columns.Add(CS_Rain, typeof(string));
                dataTable.Columns.Add(CS_Temp, typeof(string));
                if (m_bIsHEva)
                {
                    dataTable.Columns.Add(CS_Voltage, typeof(string));
                    dataTable.Columns.Add(CS_DH, typeof(string));
                }
                else
                {
                    dataTable.Columns.Add(CS_P8, typeof(string));
                    dataTable.Columns.Add(CS_P20, typeof(string));
                }
                dataTable.Columns.Add(CS_TimeCollected, typeof(DateTime));
                // 逐页读取数据
                for (int i = 0; i < m_iTotalPage; ++i)
                {
                    List<CEntityEva> tmpSanilities = m_proxyHEva.GetPageData(i + 1, false);
                    foreach (CEntityEva Eva in tmpSanilities)
                    {
                        // 赋值到dataTable中去
                        DataRow row = dataTable.NewRow();
                        // row[CS_EvaID] = Eva.EvaID;
                        row[CS_StationID] = Eva.StationID;
                        row[CS_StationName] = CDBDataMgr.Instance.GetStationById(Eva.StationID).StationName;
                        if (Eva.Eva != -9999)
                        {
                            row[CS_Eva] = Eva.Eva;
                        }
                        else
                        {
                            row[CS_Eva] = "";
                        }
                        if (Eva.Rain != -9999)
                        {
                            row[CS_Rain] = Eva.Rain;
                        }
                        else
                        {
                            row[CS_Rain] = "";
                        }
                        if (Eva.Temperature != -9999)
                        {
                            row[CS_Temp] = Eva.Temperature;
                        }
                        else
                        {
                            row[CS_Temp] = "";
                        }
                        if (m_bIsHEva)
                        {
                            if (Eva.Voltage != -9999)
                            {
                                row[CS_Voltage] = Eva.Voltage;
                            }
                            else
                            {
                                row[CS_Voltage] = "";
                            }
                            if (Eva.DH != -9999)
                            {
                                row[CS_DH] = Eva.DH;
                            }
                            else
                            {
                                row[CS_DH] = "";
                            }
                        }
                        else
                        {
                            if (Eva.P8 != -9999)
                            {
                                row[CS_P8] = Eva.P8;
                            }
                            else
                            {
                                row[CS_P8] = "";
                            }
                            if (Eva.P20 != -9999)
                            {
                                row[CS_P20] = Eva.P20;
                            }
                            else
                            {
                                row[CS_P20] = "";
                            }
                        }
                        row[CS_TimeCollected] = Eva.TimeCollect;
                        dataTable.Rows.Add(row);
                    }
                }
                // 显示提示框
                CMessageBox box = new CMessageBox() { MessageInfo = "正在导出表格，请稍候" };
                box.ShowDialog(this);
                if (CExcelExport.ExportToExcelWrapper(dataTable, dlg.FileName, "蒸发雨量表"))
                {
                    //box.Invoke((Action)delegate { box.Close(); });
                    box.CloseDialog();
                    MessageBox.Show(string.Format("导出成功,保存在文件\"{0}\"中", dlg.FileName));
                }
                else
                {
                    //box.Invoke((Action)delegate { box.Close(); });
                    box.CloseDialog();
                    MessageBox.Show("导出失败");
                }
            }//end of if dialog okay
        }
        #endregion 事件处理

        #region 重载
        // 重写上一页事件
        protected override void OnMenuPreviousPage(object sender, EventArgs e)
        {
            // 获取当前修改的日期
            // GetUpdatedData(); //换页修改数据丢失问题
            if (this.IsModifiedUnSaved())
            {
                DialogResult result = MessageBox.Show("当前页所做修改尚未保存，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (DialogResult.Cancel == result)
                {
                    //取消的话，不换页码
                }
                else if (DialogResult.Yes == result)
                {
                    // 保存当前修改
                    if (!DoSave())
                    {
                        // 如果保存失败，不允许退出
                        MessageBox.Show("保存失败,请检查数据库连接及其配置");
                        return;
                    }
                    MessageBox.Show("保存成功");
                    // 保存成功，换页
                    if (m_bIsHEva)
                    {
                        SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage - 1, true));
                    }
                    else
                    {
                        SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage - 1, true));
                    }
                    base.OnMenuPreviousPage(sender, e);
                }
                else if (DialogResult.No == result)
                {
                    //不保存，直接换页，直接退出
                    //清楚所有状态位
                    base.ClearAllState();
                    if (m_bIsHEva)
                    {
                        SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage - 1, false));
                    }
                    else
                    {
                        SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage - 1, false));
                    }
                    base.OnMenuPreviousPage(sender, e);
                }
            }
            else
            {
                // 没有修改，直接换页
                if (m_bIsHEva)
                {
                    SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage - 1, false));
                }
                else
                {
                    SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage - 1, false));
                }
                base.OnMenuPreviousPage(sender, e);
            }

        }

        // 重写下一页事件
        protected override void OnMenuNextPage(object sender, EventArgs e)
        {
            // GetUpdatedData(); //换页数据丢失问题
            if (this.IsModifiedUnSaved())
            {
                DialogResult result = MessageBox.Show("当前页所做修改尚未保存，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (DialogResult.Cancel == result)
                {
                    //取消的话，不换页码
                }
                else if (DialogResult.Yes == result)
                {
                    // 保存当前修改
                    if (!DoSave())
                    {
                        // 如果保存失败，不允许退出
                        MessageBox.Show("保存失败,请检查数据库连接及其配置");
                        return;
                    }
                    MessageBox.Show("保存成功");
                    // 保存成功，换页
                    if (m_bIsHEva)
                    {
                        SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage + 1, true));
                    }
                    else
                    {
                        SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage + 1, true));
                    }
                    base.OnMenuNextPage(sender, e);
                }
                else if (DialogResult.No == result)
                {
                    //不保存，直接换页，直接退出
                    //清楚所有状态位
                    base.ClearAllState();
                    if (m_bIsHEva)
                    {
                        SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage + 1, true));
                    }
                    else
                    {
                        SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage + 1, true));
                    }
                    base.OnMenuNextPage(sender, e);
                }
            }
            else
            {
                // 没有修改，直接换页
                if (m_bIsHEva)
                {
                    SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage + 1, true));
                }
                else
                {
                    SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage + 1, true));
                }
                base.OnMenuNextPage(sender, e);
            }
        }

        /// <summary>
        /// 重写首页事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMenuFirstPage(object sender, EventArgs e)
        {
            if (this.IsModifiedUnSaved())
            {
                DialogResult result = MessageBox.Show("当前页所做修改尚未保存，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (DialogResult.Cancel == result)
                {
                    //取消的话，不换页码
                }
                else if (DialogResult.Yes == result)
                {
                    // 保存当前修改
                    if (!DoSave())
                    {
                        // 如果保存失败，不允许退出
                        MessageBox.Show("保存失败,请检查数据库连接及其配置");
                        return;
                    }
                    MessageBox.Show("保存成功");
                    // 保存成功，换页
                    base.OnMenuFirstPage(sender, e);
                    if (m_bIsHEva)
                    {
                        SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage, true));
                    }
                    else
                    {
                        SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage, true));
                    }
                    this.UpdateDataToUI();
                }
                else if (DialogResult.No == result)
                {
                    //不保存，直接换页，直接退出
                    //清楚所有状态位
                    base.ClearAllState();
                    base.OnMenuFirstPage(sender, e);
                    if (m_bIsHEva)
                    {
                        SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage, true));
                    }
                    else
                    {
                        SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage, true));
                    }
                    this.UpdateDataToUI();
                }
            }
            else
            {
                // 没有修改，直接换页
                base.OnMenuFirstPage(sender, e);
                if (m_bIsHEva)
                {
                    SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage, true));
                }
                else
                {
                    SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage, true));
                }
                this.UpdateDataToUI();
            }

        }

        /// <summary>
        /// 重写尾页事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMenuLastPage(object sender, EventArgs e)
        {
            if (this.IsModifiedUnSaved())
            {
                DialogResult result = MessageBox.Show("当前页所做修改尚未保存，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (DialogResult.Cancel == result)
                {
                    //取消的话，不换页码
                }
                else if (DialogResult.Yes == result)
                {
                    // 保存当前修改
                    if (!DoSave())
                    {
                        // 如果保存失败，不允许退出
                        MessageBox.Show("保存失败,请检查数据库连接及其配置");
                        return;
                    }
                    else
                    {
                        MessageBox.Show("保存成功");
                        // 保存成功，换页
                        base.OnMenuFirstPage(sender, e);
                        if (m_bIsHEva)
                        {
                            SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage, true));
                        }
                        else
                        {
                            SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage, true));
                        }
                        this.UpdateDataToUI();
                    }
                }
                else if (DialogResult.No == result)
                {
                    //不保存，直接换页，直接退出
                    //清楚所有状态位
                    base.ClearAllState();
                    base.OnMenuLastPage(sender, e);
                    if (m_bIsHEva)
                    {
                        SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage, true));
                    }
                    else
                    {
                        SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage, true));
                    }
                    this.UpdateDataToUI();
                }
            }
            else
            {
                // 没有修改，直接换页
                base.OnMenuLastPage(sender, e);
                if (m_bIsHEva)
                {
                    SetEva(m_proxyHEva.GetPageData(base.m_iCurrentPage, true));
                }
                else
                {
                    SetEva(m_proxyDEva.GetPageData(base.m_iCurrentPage, true));
                }
                this.UpdateDataToUI();
            }
        }

        // 重写Cell值改变事件
        protected override void EHValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                int iPreValue = this.FirstDisplayedScrollingRowIndex;
                if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Delete)
                {
                    if (base.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Equals("True"))
                    {
                        // 删除项
                        base.MarkRowDeletedOrNot(e.RowIndex, true);
                    }
                    else
                    {
                        base.MarkRowDeletedOrNot(e.RowIndex, false);
                    }
                }
                base.EHValueChanged(sender, e);
                base.UpdateDataToUI();
                FocusOnRow(iPreValue, false);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex) { }
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
        }

        // 重写双击事件
        protected override void OnCellMouseDoubleClick(DataGridViewCellMouseEventArgs e)
        {
            try
            {
                int iPreValue = this.FirstDisplayedScrollingRowIndex;
                if (base.m_listMaskedDeletedRows.Contains(e.RowIndex))
                {
                    if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Delete)
                    {
                        //开启编辑
                        base.OnCellMouseDoubleClick(e);
                    }
                    else
                    {
                        //不编辑
                    }
                }
                else
                {
                    // 开启编辑
                    base.OnCellMouseDoubleClick(e);
                }
                FocusOnRow(iPreValue, false);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex) { }
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
        }

        // 单击事件
        protected override void OnCellClick(DataGridViewCellEventArgs e)
        {
            try
            {
                int iPreValue = this.FirstDisplayedScrollingRowIndex;
                if (base.m_listMaskedDeletedRows.Contains(e.RowIndex))
                {
                    if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Delete)
                    {
                        //开启编辑
                        base.OnCellClick(e);
                    }
                    else
                    {
                        //不编辑
                    }
                }
                else
                {
                    // 开启编辑
                    base.OnCellClick(e);
                }
                FocusOnRow(iPreValue, false);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex) { }
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
        }

        protected override void InitContextMenu()
        {
            base.InitContextMenu();
            ToolStripSeparator seperator = new ToolStripSeparator();
            m_menuItemExportToExcel = new ToolStripMenuItem("导出Excel...");

            base.m_contextMenu.Items.Add(seperator);
            base.m_contextMenu.Items.Add(m_menuItemExportToExcel);

            // 绑定事件
            m_menuItemExportToExcel.Click += new EventHandler(EH_MI_ExportToExcel_Click);
        }

        protected override void OnSizeChanged(object sender, EventArgs e)
        {
            base.OnSizeChanged(sender, e);
        }

        protected override void ClearAllState()
        {
            base.ClearAllState();
            m_listAddedEva.Clear();
        }
        #endregion ///<OVERWRITE

        #region 帮助方法
        // 生成更新过的数据列表
        private void GetUpdatedData()
        {
            // 如果标记为删除的就不需要再更新了
            List<int> listUpdatedRows = new List<int>();
            for (int i = 0; i < m_listEditedRows.Count; ++i)
            {
                if (!m_listMaskedDeletedRows.Contains(m_listEditedRows[i]))
                {
                    // 如果不在删除列中，则需要更新
                    listUpdatedRows.Add(m_listEditedRows[i]);
                }
            }
            // 获取更新过的数据
            for (int i = 0; i < listUpdatedRows.Count; ++i)
            {
                CEntityEva Eva = new CEntityEva();
                Eva.StationID = m_strStaionId;
                Eva.TimeCollect = DateTime.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_TimeCollected].Value.ToString()).AddDays(1).AddHours(8);
                Eva.Rain = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_Rain].Value.ToString());
                Eva.Eva = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_Eva].Value.ToString());
                Eva.dayEChange = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_DDH].Value.ToString());
                //Eva.Temperature = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_Temp].Value.ToString());
                //if (m_bIsHEva)
                //{
                //    Eva.Voltage = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_Voltage].Value.ToString());
                //    Eva.DH = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_DH].Value.ToString());
                //}
                //else
                //{
                //    Eva.P8 = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_P8].Value.ToString());
                //    Eva.P20 = Decimal.Parse(base.Rows[listUpdatedRows[i]].Cells[CS_P20].Value.ToString());
                //}

                m_listUpdated.Add(Eva);
            }
            // 获取删除过的数据
            for (int i = 0; i < base.m_listMaskedDeletedRows.Count; ++i)
            {
                // m_listDeleteSanilities.Add(long.Parse(base.Rows[m_listMaskedDeletedRows[i]].Cells[CS_EvaID].Value.ToString()));
                DateTime tmp = DateTime.Parse(base.Rows[m_listMaskedDeletedRows[i]].Cells[CS_TimeCollected].Value.ToString()).AddDays(1).AddHours(8);
             
                m_listDeleteSanilities_StationId.Add(base.Rows[m_listMaskedDeletedRows[i]].Cells[CS_StationID].Value.ToString());
                m_listDeleteSanilities_StationDate.Add(tmp.ToString());
            }
            m_listEditedRows.Clear();   //清空此次记录
            m_listMaskedDeletedRows.Clear(); //清空标记为删除的记录
        }
        #endregion 帮助方法

    }
}
