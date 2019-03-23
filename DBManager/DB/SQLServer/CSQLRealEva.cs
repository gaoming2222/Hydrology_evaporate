using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Hydrology.DBManager.DB.SQLServer
{
    public class CSQLRealEva : CSQLBase, ICurrentEva
    {
        #region 静态常量
        private const string CT_EntityName = "CEntityRealEva";   //  数据库表Eva实体类
        public static readonly string CT_TableName = "CurrentEva";      //数据库中蒸发表的名字
        public static readonly string CN_StationId = "StationID";   //站点ID
        public static readonly string CN_CName = "CName";   //站点名字
        public static readonly string CN_CType = "CType";   //站点类型
        public static readonly string CN_DataTime = "DataTime";    //数据的采集时间
        public static readonly string CN_Temp = "T";  //温度
        public static readonly string CN_Eva = "E";  //蒸发值
        public static readonly string CN_Rain = "P";  //降雨
        public static readonly string CN_State = "CurrentState";    //实时数据状态
        #endregion

        #region 成员变量

        public System.Timers.Timer m_addTimer_1;
        #endregion 

        public CSQLRealEva()
            : base()
        {
            // 为数据表添加列
            m_tableDataAdded.Columns.Add(CN_StationId);
            m_tableDataAdded.Columns.Add(CN_CName);
            m_tableDataAdded.Columns.Add(CN_CType);
            m_tableDataAdded.Columns.Add(CN_DataTime);
            
            m_tableDataAdded.Columns.Add(CN_Temp);
            m_tableDataAdded.Columns.Add(CN_Eva);
            m_tableDataAdded.Columns.Add(CN_Rain);

            m_tableDataAdded.Columns.Add(CN_State);

            // 初始化互斥量
            m_mutexWriteToDB = CDBMutex.Mutex_TB_RealEva;

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
            //清空内存表的所有内容，把内容复制到临时表tmp中
            DataTable tmp = dt.Copy();
            m_tableDataAdded.Rows.Clear();

            // 释放内存表的互斥量
            m_mutexDataTable.ReleaseMutex();

            try
            {
                //将临时表中的内容写入数据库
                string connstr = CDBManager.Instance.GetConnectionString();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connstr, SqlBulkCopyOptions.FireTriggers))
                {
                    // 蒸发表有插入触发器，如果遇到重复记录，则更新为当前的最新记录
                    //bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.DestinationTableName = CSQLRealEva.CT_TableName;
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_CName, CN_CName);
                    bulkCopy.ColumnMappings.Add(CN_CType, CN_CType);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    bulkCopy.ColumnMappings.Add(CN_Eva, CN_Eva);
                    bulkCopy.ColumnMappings.Add(CN_Temp, CN_Temp);
                    bulkCopy.ColumnMappings.Add(CN_Rain, CN_Rain);
                    bulkCopy.ColumnMappings.Add(CN_State, CN_State);

                    try
                    {
                        bulkCopy.WriteToServer(tmp);
                        Debug.WriteLine("###{0} :add {1} lines to CurrentEva db", DateTime.Now, tmp.Rows.Count);
                        CDBLog.Instance.AddInfo(string.Format("添加{0}行到实时蒸发表", tmp.Rows.Count));
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

        protected override bool AddDataToDB()
        {
            // 然后获取内存表的访问权
            m_mutexDataTable.WaitOne();

            if (m_tableDataAdded.Rows.Count <= 0)
            {
                m_mutexDataTable.ReleaseMutex();
                return true;
            }
            //清空内存表的所有内容，把内容复制到临时表tmp中
            DataTable tmp = m_tableDataAdded.Copy();
            m_tableDataAdded.Rows.Clear();

            // 释放内存表的互斥量
            m_mutexDataTable.ReleaseMutex();

            // 先获取对数据库的唯一访问权
            m_mutexWriteToDB.WaitOne();

            try
            {
                //将临时表中的内容写入数据库
                string connstr = CDBManager.Instance.GetConnectionString();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connstr, SqlBulkCopyOptions.FireTriggers))
                {
                    // 蒸发表有插入触发器，如果遇到重复记录，则更新为当前的最新记录
                    bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;

                    bulkCopy.DestinationTableName = CSQLRealEva.CT_TableName;
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_CName, CN_CName);
                    bulkCopy.ColumnMappings.Add(CN_CType, CN_CType);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    bulkCopy.ColumnMappings.Add(CN_Eva, CN_Eva);
                    bulkCopy.ColumnMappings.Add(CN_Temp, CN_Temp);
                    bulkCopy.ColumnMappings.Add(CN_Rain, CN_Rain);
                    bulkCopy.ColumnMappings.Add(CN_State, CN_State);

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
            Debug.WriteLine("###{0} :add {1} lines to CurrentEva db", DateTime.Now, tmp.Rows.Count);
            CDBLog.Instance.AddInfo(string.Format("添加{0}行到实时蒸发表", tmp.Rows.Count));
            m_mutexWriteToDB.ReleaseMutex();
            return true;
        }

        public void AddNewRows(List<CEntityRealEva> sanilities)
        {
            // 直接写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityRealEva Eva in sanilities)
            {
                DataRow row = m_tableDataAdded.NewRow();
                row[CN_StationId] = Eva.StrStationID;
                row[CN_CName] = Eva.StrStationName;
                row[CN_CType] = Eva.StationType;
                row[CN_DataTime] = Eva.TimeDeviceGained.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_Temp] = Eva.Temperature;
                row[CN_Eva] = Eva.Eva;
                row[CN_Rain] = Eva.Rain;
                row[CN_State] = Eva.ERTDState.ToString();
                m_tableDataAdded.Rows.Add(row);
            }
            NewTask(() => { AddDataToDB(); });

            m_mutexDataTable.ReleaseMutex();

        }

        public void AddNewRow(CEntityRealEva Eva)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量

            DataRow row = m_tableDataAdded.NewRow();
            row[CN_StationId] = Eva.StrStationID;
            row[CN_CName] = Eva.StrStationName;
            row[CN_CType] = Eva.StationType;
            row[CN_DataTime] = Eva.TimeDeviceGained.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            row[CN_Temp] = Eva.Temperature;
            row[CN_Eva] = Eva.Eva;
            row[CN_Rain] = Eva.Rain;
            row[CN_State] = Eva.ERTDState.ToString();
            //row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(Eva.ChannelType);
            m_tableDataAdded.Rows.Add(row);

            if (m_tableDataAdded.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
            {
                // 如果超过最大值，写入数据库
                NewTask(() => { InsertSqlBulk(m_tableDataAdded); });
            }
            else
            {
                // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
                m_addTimer_1.Start();
            }
            m_mutexDataTable.ReleaseMutex();
        }

        public List<CEntityRealEva> QueryAll()
        {
            string sql = " select * from " + CT_TableName;
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            // 构建结果集
            List<CEntityRealEva> results = new List<CEntityRealEva>();
            //dataTableTmp.Rows.Count
            for (int rowid = 0; rowid < dataTableTmp.Rows.Count; ++rowid)
            {
                CEntityRealEva realtime = new CEntityRealEva();
                realtime.StrStationID = dataTableTmp.Rows[rowid][CN_StationId].ToString();
                realtime.StrStationName = dataTableTmp.Rows[rowid][CN_CName].ToString();
                realtime.StationType = CEnumHelper.DBRTStrToStationType(dataTableTmp.Rows[rowid][CN_CType].ToString());
                realtime.TimeDeviceGained = DateTime.Parse(dataTableTmp.Rows[rowid][CN_DataTime].ToString());
                if (!dataTableTmp.Rows[rowid][CN_Temp].ToString().Equals(""))
                {
                    realtime.Temperature = Decimal.Parse(dataTableTmp.Rows[rowid][CN_Temp].ToString());
                }
                if (!dataTableTmp.Rows[rowid][CN_Rain].ToString().Equals(""))
                {
                    realtime.Rain = Decimal.Parse(dataTableTmp.Rows[rowid][CN_Rain].ToString());
                }
                if (!dataTableTmp.Rows[rowid][CN_Eva].ToString().Equals(""))
                {
                    realtime.Eva = Decimal.Parse(dataTableTmp.Rows[rowid][CN_Eva].ToString());
                }

                realtime.ERTDState = CEnumHelper.DBStrToState(dataTableTmp.Rows[rowid][CN_State].ToString());
                results.Add(realtime);
            }
            return results;
        }
    }
}
