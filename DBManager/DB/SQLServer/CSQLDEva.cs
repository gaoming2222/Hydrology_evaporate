﻿using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace Hydrology.DBManager.DB.SQLServer
{
    public class CSQLDEva : CSQLBase, IDEvaProxy
    {
        #region 静态常量
        private const string CT_EntityName = "CEntityEva";   //  数据库表Eva实体类
        public static readonly string CT_TableName = "DayData";      //数据库中蒸发初始表的名字
        public static readonly string CN_StationId = "STCD";   //站点ID
        public static readonly string CN_DataTime = "DT";    //数据的采集时间
        public static readonly string CN_Temp = "T";  //温度
        public static readonly string CN_Eva = "E";  //蒸发值
        public static readonly string CN_Rain = "P";  //降雨
        public static readonly string CN_Rain8 = "P8";   //8点到20点的降雨之和
        public static readonly string CN_Rain20 = "P20";   //20点到8点的降雨之和
        //public static readonly string CN_dayPChange = "dayPChange";
        public static readonly string CN_dayEChange = "dayEChange";
        #endregion

        #region 成员变量

        private List<long> m_listDelRows;            // 删除蒸发记录的链表
        private List<CEntityEva> m_listUpdateRows; // 更新蒸发记录的链表

        private string m_strStaionId;       //需要查询的测站
        private DateTime m_startTime;  //查询起始时间
        private DateTime m_endTime;    //查询结束时间
        private bool m_TimeSelect;
        private string TimeSelectString
        {
            get
            {
                if (m_TimeSelect == false)
                {
                    return "";
                }
                else
                {
                    return "convert(VARCHAR," + CN_DataTime + ",120) LIKE '%00:00%' and ";
                }
            }
        }

        public System.Timers.Timer m_addTimer_1;
        #endregion 

        public CSQLDEva()
            : base()
        {
            m_listDelRows = new List<long>();
            m_listUpdateRows = new List<CEntityEva>();
            // 为数据表添加列
            m_tableDataAdded.Columns.Add(CN_StationId);
            m_tableDataAdded.Columns.Add(CN_DataTime);

            m_tableDataAdded.Columns.Add(CN_Temp);
            m_tableDataAdded.Columns.Add(CN_Eva);
            m_tableDataAdded.Columns.Add(CN_Rain);
            m_tableDataAdded.Columns.Add(CN_Rain8);
            m_tableDataAdded.Columns.Add(CN_Rain20);
            //m_tableDataAdded.Columns.Add(CN_dayPChange);
            m_tableDataAdded.Columns.Add(CN_dayEChange);

            //m_tableDataAdded.Columns.Add(CN_TransType);

            // 分页查询相关
            m_strStaionId = null;

            // 初始化互斥量
            m_mutexWriteToDB = CDBMutex.Mutex_TB_DEva;

            m_addTimer_1 = new System.Timers.Timer();
            m_addTimer_1.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer_1);
            m_addTimer_1.Enabled = false;
            m_addTimer_1.Interval = CDBParams.GetInstance().AddToDbDelay;
        }

        /// <summary>
        /// 定时器事件
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected virtual void EHTimer_1(object source, System.Timers.ElapsedEventArgs e)
        {
            //定时器事件，将所有的记录都写入数据库
            m_addTimer_1.Stop();  //停止定时器
            m_dateTimePreAddTime = DateTime.Now;
            //将数据写入数据库
            NewTask(() => { InsertSqlBulk(m_tableDataAdded); });
        }

        private void InsertSqlBulk(DataTable dt)
        {
            // 然后获取内存表的访问权
            m_mutexDataTable.WaitOne();

            if (dt.Rows.Count <= 0)
            {
                m_mutexDataTable.ReleaseMutex();
                return;
            }
            //清空内存表的所有内容，把内容复制到临日表tmp中
            DataTable tmp = dt.Copy();
            m_tableDataAdded.Rows.Clear();

            // 释放内存表的互斥量
            m_mutexDataTable.ReleaseMutex();

            try
            {
                //将临日表中的内容写入数据库
                string connstr = CDBManager.Instance.GetConnectionString();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connstr, SqlBulkCopyOptions.FireTriggers))
                {
                    // 蒸发表有插入触发器，如果遇到重复记录，则更新为当前的最新记录
                    //bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.DestinationTableName = CSQLDEva.CT_TableName;
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    bulkCopy.ColumnMappings.Add(CN_Eva, CN_Eva);
                    bulkCopy.ColumnMappings.Add(CN_Temp, CN_Temp);
                    bulkCopy.ColumnMappings.Add(CN_Rain, CN_Rain);
                    bulkCopy.ColumnMappings.Add(CN_Rain8, CN_Rain8);
                    bulkCopy.ColumnMappings.Add(CN_Rain20, CN_Rain20);
                    //bulkCopy.ColumnMappings.Add(CN_dayPChange, CN_dayPChange);
                    bulkCopy.ColumnMappings.Add(CN_dayEChange, CN_dayEChange);

                    try
                    {
                        bulkCopy.WriteToServer(tmp);
                        Debug.WriteLine("###{0} :add {1} lines to DEva db", DateTime.Now, tmp.Rows.Count);
                        CDBLog.Instance.AddInfo(string.Format("添加{0}行到蒸发日表", tmp.Rows.Count));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        //如果出现异常，SqlBulkCopy 会使数据库回滚，所有Table中的记录都不会插入到数据库中，
                        //此时，把Table折半插入，先插入一半，再插入一半。如此递归，直到只有一行时，如果插入异常，则返回。
                        if (tmp.Rows.Count == 1)
                            return;
                        int middle = tmp.Rows.Count / 2;
                        DataTable table = tmp.Clone();
                        for (int i = 0; i < middle; i++)
                            table.ImportRow(tmp.Rows[i]);

                        InsertSqlBulk(table);

                        table.Clear();
                        for (int i = middle; i < tmp.Rows.Count; i++)
                            table.ImportRow(tmp.Rows[i]);
                        InsertSqlBulk(table);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return;
            }

            return;
        }

        public void AddNewRow(CEntityEva Eva)
        {
            throw new NotImplementedException();
        }

        public void AddNewRows(List<CEntityEva> sanilities)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityEva Eva in sanilities)
            {
                DataRow row = m_tableDataAdded.NewRow();
                row[CN_StationId] = Eva.StationID;
                row[CN_DataTime] = Eva.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_Temp] = Eva.Temperature;
                row[CN_Eva] = Eva.Eva;
                row[CN_Rain] = Eva.Rain;
                row[CN_Rain8] = Eva.P8;
                row[CN_Rain20] = Eva.P20;
                //row[CN_dayPChange] = Eva.dayPChange;
                row[CN_dayEChange] = Eva.dayEChange;
                m_tableDataAdded.Rows.Add(row);
            }
            //TODO
            NewTask(() => { InsertSqlBulk(m_tableDataAdded); });
            //if (m_tableDataAdded.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
            //{
            //    // 如果超过最大值，写入数据库
            //    NewTask(() => { InsertSqlBulk(m_tableDataAdded); });
            //}
            //else
            //{
            //    // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
            //    m_addTimer_1.Start();
            //}
            m_mutexDataTable.ReleaseMutex();
        }

        public void AddNewRows_DataModify(List<CEntityEva> sanilities)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityEva Eva in sanilities)
            {
                DataRow row = m_tableDataAdded.NewRow();
                row[CN_StationId] = Eva.StationID;
                row[CN_DataTime] = Eva.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_Temp] = Eva.Temperature;
                row[CN_Eva] = Eva.Eva;
                row[CN_Rain] = Eva.Rain;
                row[CN_Rain8] = Eva.P8;
                row[CN_Rain20] = Eva.P20;
                //row[CN_dayPChange] = Eva.P20;
                row[CN_dayEChange] = Eva.dayEChange;
                m_tableDataAdded.Rows.Add(row);

            }
            // 如果超过最大值，写入数据库
            NewTask(() => { AddDataToDB(); });

            m_mutexDataTable.ReleaseMutex();
        }

        // 根据当前条件查询统计数据
        private void DoCountQuery()
        {
            string sql = "select count(*) count from " + CT_TableName + " " +
                "where " + CN_StationId + " = " + m_strStaionId + " " +
                "and " + TimeSelectString + CN_DataTime + "  between " + DateTimeToDBStr(m_startTime) +
                 "and " + DateTimeToDBStr(m_endTime);
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                m_iRowCount = Int32.Parse((dataTableTmp.Rows[0])[0].ToString());
                m_iPageCount = (int)Math.Ceiling((double)m_iRowCount / CDBParams.GetInstance().UIPageRowCount); //向上取整
            }
            catch (System.Exception ex)
            {
                // 超时异常等等
                Debug.WriteLine(ex.ToString());
            }

        }

        public List<CEntityEva> GetPageData(int pageIndex, bool irefresh)
        {
            if (pageIndex <= 0 || m_startTime == null || m_endTime == null || m_strStaionId == null)
            {
                return new List<CEntityEva>();
            }
            // 获取某一页的数据，判断所需页面是否在内存中有值
            int startIndex = (pageIndex - 1) * CDBParams.GetInstance().UIPageRowCount + 1;
            int key = (int)(startIndex / CDBParams.GetInstance().DBPageRowCount) + 1; //对应于数据库中的索引
            int startRow = startIndex - (key - 1) * CDBParams.GetInstance().DBPageRowCount - 1;
            Debug.WriteLine("Eva startIndex;{0} key:{1} startRow:{2}", startIndex, key, startRow);
            // 判断MAP中是否有值
            if (m_mapDataTable.ContainsKey(key) && !irefresh)
            {
                // 从内存中读取
                return CopyDataToList(key, startRow);
            }
            else if (m_mapDataTable.ContainsKey(key) && irefresh)
            {
                m_mapDataTable.Remove(key);
                // 从数据库中查询
                int topcount = key * CDBParams.GetInstance().DBPageRowCount;
                int rowidmim = topcount - CDBParams.GetInstance().DBPageRowCount;
                string sql = " select * from ( " +
                    "select top " + topcount.ToString() + " row_number() over( order by " + CN_DataTime + " ) as " + CN_RowId + ",* " +
                    "from " + CT_TableName + " " +
                    "where " + CN_StationId + "=" + m_strStaionId.ToString() + " " +
                    "and " + TimeSelectString + CN_DataTime + " between " + DateTimeToDBStr(m_startTime) +
                    "and " + DateTimeToDBStr(m_endTime) +
                    ") as tmp1 " +
                    "where " + CN_RowId + ">" + rowidmim.ToString() +
                    " order by " + CN_DataTime + " DESC";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                m_mapDataTable.Add(key, dataTableTmp);
                return CopyDataToList(key, startRow);
            }
            else
            {
                // 从数据库中查询
                int topcount = key * CDBParams.GetInstance().DBPageRowCount;
                int rowidmim = topcount - CDBParams.GetInstance().DBPageRowCount;
                string sql = " select * from ( " +
                    "select top " + topcount.ToString() + " row_number() over( order by " + CN_DataTime + " ) as " + CN_RowId + ",* " +
                    "from " + CT_TableName + " " +
                    "where " + CN_StationId + "=" + m_strStaionId.ToString() + " " +
                    "and " + TimeSelectString + CN_DataTime + " between " + DateTimeToDBStr(m_startTime) +
                    "and " + DateTimeToDBStr(m_endTime) +
                    ") as tmp1 " +
                    "where " + CN_RowId + ">" + rowidmim.ToString() +
                    " order by " + CN_DataTime + " DESC";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                m_mapDataTable.Add(key, dataTableTmp);
                return CopyDataToList(key, startRow);
            }
        }

        // 将Map中由key指定的DataTable,从startRow开始返回界面最大行数的集合
        private List<CEntityEva> CopyDataToList(int key, int startRow)
        {
            List<CEntityEva> result = new List<CEntityEva>();
            // 取最小值 ，保证不越界
            int endRow = Math.Min(m_mapDataTable[key].Rows.Count, startRow + CDBParams.GetInstance().UIPageRowCount);
            DataTable table = m_mapDataTable[key];
            for (; startRow < endRow; ++startRow)
            {
                CEntityEva Eva = new CEntityEva();
                Eva.StationID = table.Rows[startRow][CN_StationId].ToString();
                Eva.TimeCollect = DateTime.Parse(table.Rows[startRow][CN_DataTime].ToString());
                if (!table.Rows[startRow][CN_Temp].ToString().Equals(""))
                {
                    Eva.Temperature = Decimal.Parse(table.Rows[startRow][CN_Temp].ToString());

                }
                if (!table.Rows[startRow][CN_Eva].ToString().Equals(""))
                {
                    Eva.Eva = Decimal.Parse(table.Rows[startRow][CN_Eva].ToString());

                }
                if (!table.Rows[startRow][CN_Rain].ToString().Equals(""))
                {
                    Eva.Rain = Decimal.Parse(table.Rows[startRow][CN_Rain].ToString());
                }
                if (!table.Rows[startRow][CN_Rain8].ToString().Equals(""))
                {
                    Eva.P8 = Decimal.Parse(table.Rows[startRow][CN_Rain8].ToString());
                }
                if (!table.Rows[startRow][CN_Rain20].ToString().Equals(""))
                {
                    Eva.P20 = Decimal.Parse(table.Rows[startRow][CN_Rain20].ToString());
                }
                //if (!table.Rows[startRow][CN_dayPChange].ToString().Equals(""))
                //{
                //    Eva.dayPChange = Decimal.Parse(table.Rows[startRow][CN_dayPChange].ToString());
                //}
                if (!table.Rows[startRow][CN_dayEChange].ToString().Equals(""))
                {
                    Eva.dayEChange = Decimal.Parse(table.Rows[startRow][CN_dayEChange].ToString());
                }
                result.Add(Eva);
            }
            return result;
        }

        public void SetFilter(string stationId, DateTime timeStart, DateTime timeEnd)
        {
            // 设置查询条件
            if (null == m_strStaionId)
            {
                // 第一次查询
                m_iRowCount = -1;
                m_iPageCount = -1;
                m_strStaionId = stationId;
                m_startTime = timeStart;
                m_endTime = timeEnd;
            }
            else
            {
                // 不是第一次查询
                if (stationId != m_strStaionId || timeStart != m_startTime || timeEnd != m_endTime)
                {
                    m_iRowCount = -1;
                    m_iPageCount = -1;
                    m_mapDataTable.Clear(); //清空上次查询缓存
                }
                m_strStaionId = stationId;
                m_startTime = timeStart;
                m_endTime = timeEnd;;
            }
        }

        // 恢复初始状态
        private void ResetAll()
        {
            m_mutexDataTable.WaitOne();
            m_iPageCount = -1;
            m_mapDataTable.Clear(); //清空所有记录
            m_mutexDataTable.ReleaseMutex();
        }

        public int GetPageCount()
        {
            if (-1 == m_iPageCount)
            {
                DoCountQuery();
            }
            return m_iPageCount;
        }

        public int GetRowCount()
        {
            if (-1 == m_iPageCount)
            {
                DoCountQuery();
            }
            return m_iRowCount;
        }

        protected override bool AddDataToDB()
        {
            // 然后获取内存表的访问权
            m_mutexDataTable.WaitOne();

            if (m_tableDataAdded.Rows.Count <= 0)
            {
                m_mutexDataTable.ReleaseMutex();
                return true;
            }
            //清空内存表的所有内容，把内容复制到临日表tmp中
            DataTable tmp = m_tableDataAdded.Copy();
            m_tableDataAdded.Rows.Clear();

            // 释放内存表的互斥量
            m_mutexDataTable.ReleaseMutex();

            // 先获取对数据库的唯一访问权
            m_mutexWriteToDB.WaitOne();

            try
            {
                //将临日表中的内容写入数据库
                string connstr = CDBManager.Instance.GetConnectionString();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connstr, SqlBulkCopyOptions.FireTriggers))
                {
                    // 蒸发表有插入触发器，如果遇到重复记录，则更新为当前的最新记录
                    bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;

                    bulkCopy.DestinationTableName = CSQLDEva.CT_TableName;
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    bulkCopy.ColumnMappings.Add(CN_Eva, CN_Eva);
                    bulkCopy.ColumnMappings.Add(CN_Temp, CN_Temp);
                    bulkCopy.ColumnMappings.Add(CN_Rain, CN_Rain);
                    bulkCopy.ColumnMappings.Add(CN_Rain8, CN_Rain8);
                    bulkCopy.ColumnMappings.Add(CN_Rain20, CN_Rain20);

                    try
                    {
                        bulkCopy.WriteToServer(tmp);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }

                }
                //conn.Close();   //关闭连接
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                m_mutexWriteToDB.ReleaseMutex();
                return false;
            }
            Debug.WriteLine("###{0} :add {1} lines to DEva db", DateTime.Now, tmp.Rows.Count);
            CDBLog.Instance.AddInfo(string.Format("添加{0}行到蒸发日表", tmp.Rows.Count));
            m_mutexWriteToDB.ReleaseMutex();
            return true;
        }

        public bool DeleteRows(List<String> Evas_StationId, List<String> Evas_StationDate)
        {
            // 删除某条雨量记录
            StringBuilder sql = new StringBuilder();
            int currentBatchCount = 0;
            for (int i = 0; i < Evas_StationId.Count; i++)
            {
                ++currentBatchCount;
                sql.AppendFormat("delete from {0} where {1}={2} and {3}='{4}';",
                    CT_TableName,
                    CN_StationId, Evas_StationId[i].ToString(),
                    CN_DataTime, Evas_StationDate[i].ToString()
                );
                if (currentBatchCount >= CDBParams.GetInstance().UpdateBufferMax)
                {
                    // 更新数据库
                    if (!this.ExecuteSQLCommand(sql.ToString()))
                    {
                        // 保存失败
                        return false;
                    }
                    sql.Clear(); //清除以前的所有命令
                    currentBatchCount = 0;
                }
            }
            // 如何考虑线程同异步
            if (!ExecuteSQLCommand(sql.ToString()))
            {
                return false;
            }
            ResetAll();
            return true;
        }
        /// <summary>
        /// 根据站点ID和时间查询
        /// </summary>
        /// <param name="stationid"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<CEntityEva> getEvabyTime(string stationid, DateTime start, DateTime end)
        {
            List<CEntityEva> evaList = new List<CEntityEva>();
            String sql = "select * from " + CT_TableName + " where STCD=" + stationid + " and DT between '" + start + "'and '" + end + "' order by DT;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int flag = dataTableTemp.Rows.Count;
            if (flag == 0)
            {
                return null;
            }
            else
            {
                for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
                {
                    CEntityEva eva = new CEntityEva();
                    eva.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    eva.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    if(dataTableTemp.Rows[rowid][CN_Eva] != null && dataTableTemp.Rows[rowid][CN_Eva].ToString() != "")
                    {
                        eva.E = decimal.Parse(dataTableTemp.Rows[rowid][CN_Eva].ToString());
                    }
                    else
                    {
                        eva.E = null;
                    }
                    if(dataTableTemp.Rows[rowid][CN_Rain] != null && dataTableTemp.Rows[rowid][CN_Rain].ToString() != "")
                    {
                        eva.P = decimal.Parse(dataTableTemp.Rows[rowid][CN_Rain].ToString());
                    }
                    else
                    {
                        eva.P = null;
                    }
                    if (dataTableTemp.Rows[rowid][CN_Rain8] != null && dataTableTemp.Rows[rowid][CN_Rain8].ToString() != "")
                    {
                        eva.P8 = decimal.Parse(dataTableTemp.Rows[rowid][CN_Rain8].ToString());
                    }
                    else
                    {
                        eva.P8 = null;
                    }
                    if(dataTableTemp.Rows[rowid][CN_Rain20] != null && dataTableTemp.Rows[rowid][CN_Rain20].ToString()!="")
                    {
                        eva.P20 = decimal.Parse(dataTableTemp.Rows[rowid][CN_Rain20].ToString());
                    }
                    else
                    {
                        eva.P20 = null;
                    }

                    if (dataTableTemp.Rows[rowid][CN_dayEChange] != null && dataTableTemp.Rows[rowid][CN_dayEChange].ToString() != "")
                    {
                        eva.dayEChange = decimal.Parse(dataTableTemp.Rows[rowid][CN_dayEChange].ToString());
                    }
                    else
                    {
                        eva.dayEChange = null;
                    }
                    evaList.Add(eva);
                }
            }
            return evaList;
        }

        public List<CEntityEva> get4InitEva()
        {
            List<CEntityEva> result = new List<CEntityEva>();
            String sql = "select t.* from (select DayData.*, row_number() over(partition by stcd order by dt desc) rn from DayData) t where rn <= 1; ";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int flag = dataTableTemp.Rows.Count;
            if (flag == 0)
            {
                return null;
            }
            else
            {
                for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
                {
                    CEntityEva eva = new CEntityEva();
                    eva.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    eva.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    if (dataTableTemp.Rows[rowid][CN_Eva] != null && dataTableTemp.Rows[rowid][CN_Eva].ToString() != "")
                    {
                        eva.E = decimal.Parse(dataTableTemp.Rows[rowid][CN_Eva].ToString());
                    }
                    else
                    {
                        eva.E = null;
                    }
                    if (dataTableTemp.Rows[rowid][CN_Rain] != null && dataTableTemp.Rows[rowid][CN_Rain].ToString() != "")
                    {
                        eva.P = decimal.Parse(dataTableTemp.Rows[rowid][CN_Rain].ToString());
                    }
                    else
                    {
                        eva.P = null;
                    }
                    result.Add(eva);
                }
            }

            return result;
        }
    }
}
