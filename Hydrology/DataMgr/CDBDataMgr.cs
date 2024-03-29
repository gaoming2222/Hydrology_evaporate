﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Hydrology.DBManager;
using Hydrology.DBManager.DB.SQLServer;
using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using Hydrology.Utils;
using System.Threading;
using System.Windows.Forms;
using Hydrology.Forms;
using System.IO;
using System.Xml;
using Entity;
using Hydrology.Entity.Utils;

namespace Hydrology.DataMgr
{
    class CDBDataMgr
    {
        #region 事件定义
        /// <summary>
        /// 站点基本信息更新消息
        /// </summary>
        public event EventHandler StationUpdated;

        /// <summary>
        /// 分中心基本信息更新消息
        /// </summary>
        public event EventHandler SubCenterUpdated;

        /// <summary>
        /// 串口配置信息更新消息
        /// </summary>
        public event EventHandler SerialPortUpdated;

        /// <summary>
        /// 收到单条实时数据消息
        /// </summary>
        public event EventHandler<CEventSingleArgs<CEntityRealTime>> RecvedRTD;

        /// <summary>
        /// 收到单条实时蒸发数据消息
        /// </summary>
        public event EventHandler<CEventSingleArgs<CEntityRealEva>> RecvedRTD_Eva;

        /// <summary>
        /// 实时数据表清空了事件，发生在切换数据库上面
        /// </summary>
        public event EventHandler RTDCleared;

        /// <summary>
        /// 实时蒸发数据表清空了事件，发生在切换数据库上面
        /// </summary>
        public event EventHandler RTDCleared_Eva;

        #endregion

        #region 单件模式
        private static CDBDataMgr m_sInstance;   //实例指针
        private CDBDataMgr()
        {
            m_iMinutesRange = 0; // 0 表示整点，8点
            m_listCurrentTask = new List<Task>();
            m_mutexTaskList = new Mutex();
            m_bStopServer = false;
        }
        public static CDBDataMgr Instance
        {
            get { return GetInstance(); }
        }
        public static CDBDataMgr GetInstance()
        {
            if (m_sInstance == null)
            {
                m_sInstance = new CDBDataMgr();
            }
            return m_sInstance;
        }
        #endregion ///<单件模式

        #region 数据成员
        private IStationProxy m_proxyStation;
        private ISubCenterProxy m_proxySubCenter;
        private IWaterProxy m_proxyWater;
        private IRainProxy m_proxyRain;
        private IEvaProxy m_proxyEva;
        private IHEvaProxy m_proxyHEva;
        private IDEvaProxy m_proxyDEva;
        private ICurrentEva m_proxyRealEva;
        private ITSRainProxy m_proxyTSRain;
        private ITSWater m_proxyTSWater;
        private ITSVoltage m_proxyTSVoltage;
        // private IFormProxy m_proxyForm;
        private IVoltageProxy m_proxyVoltage;
        private ICurrentDataProxy m_proxyRealtime;
        private ISerialPortProxy m_proxySerialPort;
        private IUserProxy m_proxyUser;
        private IWarningInfoProxy m_proxyWarningInfo;
        private IWaterFlowMapProxy m_proxyWaterFlowMap;
        // private ICommunicationRateProxy m_proxyCommunicationRate;

        //1009gm
        private ISoilDataProxy m_proxySoilData;
        private DateTime calFlag;
        private CCALDataMgr cal;
        private DateTime timeFlag;
        private List<CEntityStation> m_listStations;    //所有站点内存副本
        private List<CEntitySubCenter> m_listSubCenter; //所有分中心内存副本
        private List<CEntitySerialPort> m_listSerialPort;   //所有串口的内存副本
        public Dictionary<string, CEntityStation> m_mapStation;    //站点ID和站点映射
        private Dictionary<string, List<CEntityWaterFlowMap>> m_mapStationWaterFlow; //站点的水位流量线条
        private Dictionary<string, CEntityRealTime> m_mapStationRTD;        //站点实时数据
        private Dictionary<string, CEntityRealEva> m_mapStationRTS;        //站点实时数据
        private Dictionary<string, decimal?> m_mapStationEva;
        private Dictionary<string, decimal?> m_mapStationRain;
        public Dictionary<string, CEntityStation> m_mapGprsStation;    //站点gprs和站点映射

        // 好像没有用了，因为采集时间都是整点的，也就是刚好8点就okay. 
        private int m_iMinutesRange;         //距离8点时刻相差多久为8点数据，默认为30分钟

        private List<Task> m_listCurrentTask; //当前所有的任务列表，便于退出时候等待
        private Mutex m_mutexTaskList; //任务列表互斥量 
        private bool m_bStopServer; // 停止服务

        public static int flag;
        // 日雨量最大差值，由xml文件控制
        private static int dayInterval;
        private static int hourInterval;
        private readonly string CONFIG_PATH = "Config/RainIntervalConfig.xml";
        //private string actStr = "";

        Nullable<decimal> lastDayRain = 0;
        Nullable<decimal> DayRain = 0;
        Nullable<decimal> lastDayEva = 0;
        Nullable<decimal> DayEva = 0;

        private System.Windows.Forms.Timer aTimer = new System.Windows.Forms.Timer()
        {
            Enabled = false,
            Interval = 1 * 60 * 1000// 5分钟
        };

        //  System.Timers.Timer aTimer = new System.Timers.Timer((60 - DateTime.Now.Second) * 1000); 
        #endregion

        #region 公共方法

        public bool Init()
        {
            //todo
            aTimer.Tick += new EventHandler(atimer_Tick);
            aTimer.Enabled = true;
            aTimer.Start();
            // 如果连接失败，则返回false
            if (!CDBManager.Instance.TryToConnection())
            {
                return false;
            }
            timeFlag = DateTime.Now;
            calFlag = DateTime.Now;
            // 此处应该考虑根据配置连接不同的数据库，嘿嘿，从长计议，工厂模式啥啥啥的都可以
            m_proxyStation = new CSQLStation();
            m_proxySubCenter = new CSQLSubCenter();
            m_proxyWater = new CSQLWater();
            m_proxyRain = new CSQLRain();
            m_proxyEva = new CSQLEva();
            m_proxyHEva = new CSQLHEva();
            m_proxyDEva = new CSQLDEva();
            m_proxyRealEva = new CSQLRealEva();
            m_proxyTSRain = new CDBSQLTSRain();
            m_proxyTSWater = new CDBSQLTSWater();
            m_proxyTSVoltage = new CDBSQLTSVoltage();
            //  m_proxyForm = new CSQLForm();
            m_proxyVoltage = new CSQLVoltage();
            m_proxyRealtime = new CSQLCurrenData();
            m_proxySerialPort = new CSQLSerialPort();
            m_proxyUser = new CSQLUser();
            m_proxyWarningInfo = new CSQLWarningInfo();
            m_proxyWaterFlowMap = new CSQLWaterFlowMap();
            // m_proxyCommunicationRate = new CSQLCommunicationRate();
            
            cal = new CCALDataMgr();

            //1009gm
            m_proxySoilData = new CSQLSoilData();

            m_mapStation = new Dictionary<string, CEntityStation>();

            m_mapStationEva = new Dictionary<string, decimal?>();
            m_mapStationRain = new Dictionary<string, decimal?>();

            m_mapGprsStation = new Dictionary<string, CEntityStation>();

            // 读取所有的站点，以及分中心，还有串口
            m_listStations = m_proxyStation.QueryAll();
            m_listSubCenter = m_proxySubCenter.QueryAll();
            m_listSerialPort = m_proxySerialPort.QueryAll();

            // 对站点排序
            m_listStations.Sort();

            // 建立站点映射
            foreach (CEntityStation entity in m_listStations)
            {
                if (entity != null)
                {
                    m_mapStation.Add(entity.StationID, entity);
                }
                try
                {
                    //初始化gprs
                    if (entity.GPRS != "")
                    {
                        m_mapGprsStation.Add(entity.GPRS, entity);
                    }
                    //初始化今日蒸发和今日降雨
                    Decimal? totalEva = 0;
                    Decimal? totalRain = 0;
                    List<CEntityEva> results = m_proxyHEva.QueryForDayEvaList(entity.StationID);
                    if(results != null)
                    {
                        for (int i = 0; i < results.Count; i++)
                        {
                            totalEva = totalEva + results[i].Eva;
                            totalRain = totalRain + results[i].Rain;
                        }
                    }
                    
                    m_mapStationEva[entity.StationID] = totalEva;
                    m_mapStationRain[entity.StationID] = totalRain;

                }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                {
                    MessageBox.Show("站点" + entity.StationID + "GPRS号码" + entity.GPRS + " ");
                }
            }

            //查询当前的蒸发量
            //try
            //{
            //    List<CEntityEva> results = new List<CEntityEva>();
            //    results = m_proxyHEva.QueryForDayEvaList()
            //}
            resetTxt();
            //CreateTable();
            ReadRTDXml();
            ReadRTS();
            // 初始化上次雨量记录
            InitStationRainRecord();
            // 初始化水位
            InitStationLastWaterStage();
            // 初始化水位流量表, 查询数据库
            InitStationFlowMapRecord();
            // 初始化电压
            InitLastVoltageRecord();
            // 如果XML文件中没有读到某个站点的实时数据，新建实时数据
            FillRTDStationNotInMap();
            ReadFromXML();

            // 初始化蒸发参数
            SetEvaConf();

            Protocol.Manager.XmlStationDataSerializer.Instance.DeleteFile();
            Protocol.Manager.XmlStationDataSerializer.Instance.Serialize(m_listStations);

            // 绑定数据库消息
            CDBLog.Instance.NewDBSystemInfo += CSystemInfoMgr.Instance.EHRecvSystemInfo;

            flag = 0;
            return true;
        }

        /// <summary>
        /// 初始化的时候，给实时数据界面发送RTD数据初始化界面
        /// </summary>
        public void SentRTDMsg()
        {
            // 将内存m_mapStationRTD中的上次记录通知界面
            if (null != RecvedRTD && null != m_mapStationRTD)
            {
                foreach (KeyValuePair<string, CEntityRealTime> entity in m_mapStationRTD)
                {
                    // 该字段已经被废弃
                    //if (entity.Value.BIsReceivedValid)
                    //{
                    // 只要数据库有值，就通知页面
                    if ((entity.Value.LastTotalRain.HasValue || entity.Value.LastWaterStage.HasValue))
                    {
                        RecvedRTD.Invoke(this, new CEventSingleArgs<CEntityRealTime>(entity.Value));
                    }
                    //}
                }
            }
        }
        /// <summary>
        /// 初始化的时候，给实时蒸发界面发送蒸发数据初始化界面
        /// </summary>
        public void SentRTDEva()
        {
            // 将内存m_mapStationRTD中的上次记录通知界面
            if (null != RecvedRTD_Eva && null != m_mapStationRTS)
            {
                foreach (KeyValuePair<string, CEntityRealEva> entity in m_mapStationRTS)
                {
                    // 该字段已经被废弃
                    //if (entity.Value.BIsReceivedValid)
                    //{
                    // 只要数据库有值，就通知页面
                    if ((entity.Value.Eva.HasValue))
                    {
                        RecvedRTD_Eva.Invoke(this, new CEventSingleArgs<CEntityRealEva>(entity.Value));
                    }
                    //}
                }
            }
        }
        public IStationProxy GetStationProxy()
        {
            return m_proxyStation;
        }
        public ISubCenterProxy GetSubCenterProxy()
        {
            return m_proxySubCenter;
        }
        public IWaterProxy GetWaterProxy()
        {
            return m_proxyWater;
        }
        public IRainProxy GetRainProxy()
        {
            return m_proxyRain;
        }
        public IVoltageProxy GetVoltageProxy()
        {
            return m_proxyVoltage;
        }
        public ICurrentDataProxy GetRealTimeProxy()
        {
            return m_proxyRealtime;
        }
        public ICurrentEva GetRealEvaProxy()
        {
            return m_proxyRealEva;
        }
        public IEvaProxy GetEvaProxy()
        {
            return m_proxyEva;
        }
        public IHEvaProxy GetHEvaProxy()
        {
            return m_proxyHEva;
        }
        public IDEvaProxy GetDEvaProxy()
        {
            return m_proxyDEva;
        }
        public ISerialPortProxy GetSerialPortProxy()
        {
            return m_proxySerialPort;
        }
        public IUserProxy GetUserProxy()
        {
            return m_proxyUser;
        }
        public IWarningInfoProxy GetWarningInfoProxy()
        {
            return m_proxyWarningInfo;
        }
        public IWaterFlowMapProxy GetWaterFlowMapProxy()
        {
            return m_proxyWaterFlowMap;
        }
        //public ICommunicationRateProxy GetCommunicationRateProxy()
        //{
        //    return m_proxyCommunicationRate;
        //}

        public List<CEntityStation> GetAllStation()
        {
            if (null == m_listStations)
                return new List<CEntityStation>();
            return m_listStations;
        }

        //public List<String> GetAllStationGprs()
        //{
        //    //if (null == m_mapGprsStation)
        //    //    return new List<CEntityStation>();
        //    //return m_mapGprsStation;
        //}

        public List<CEntityStation> GetAllStationData()
        {
            m_listStations = m_proxyStation.QueryAll();
            return m_listStations;
        }

        public List<CEntitySubCenter> GetAllSubCenter()
        {
            if (null == m_listSubCenter)
                return new List<CEntitySubCenter>();
            return m_listSubCenter;
        }
        public List<CEntitySerialPort> GetAllSerialPort()
        {
            if (null == m_listSerialPort)
                return new List<CEntitySerialPort>();
            return m_listSerialPort;
        }
        public List<string> GetAllSerialPortName()
        {
            var result = new List<string>();
            try
            {
                if (m_listSerialPort != null)
                {
                    foreach (var entity in m_listSerialPort)
                    {
                        result.Add("COM" + entity.PortNumber);
                    }
                }
            }
#pragma warning disable CS0168 // 声明了变量“exp”，但从未使用过
            catch (Exception exp) { }
#pragma warning restore CS0168 // 声明了变量“exp”，但从未使用过
            return result;
        }
        public CEntitySerialPort GetSerialPortByPortNumber(int portNumber)
        {
            foreach (var port in m_listSerialPort)
            {
                if (port.PortNumber == portNumber)
                {
                    return port;
                }
            }
            return null;
        }
        public CEntitySubCenter GetSubCenterById(int subcenterID)
        {
            foreach (CEntitySubCenter subcenter in m_listSubCenter)
            {
                if (subcenterID == subcenter.SubCenterID)
                {
                    return subcenter;
                }
            }
            return null; //没找到匹配，返回空
        }
        public CEntitySubCenter GetSubCenterByName(string name)
        {
            foreach (CEntitySubCenter subcenter in m_listSubCenter)
            {
                if (name == subcenter.SubCenterName)
                {
                    return subcenter;
                }
            }
            return null; //没找到匹配，返回空
        }
        public CEntityStation GetStationById(string stationId)
        {
            if (m_mapStation.ContainsKey(stationId))
            {
                return m_mapStation[stationId];
            }
            return null;    //没有找到匹配，返回空
        }

        public CEntityStation GetStationByGprs(string GprsId)
        {
            CEntityStation result = new CEntityStation();
            result = m_proxyStation.QueryByGprs(GprsId);
            return result;

        }

        private void ReadFromXML()
        {

            try
            {
                // 从配置文件中分表时间信息
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(CONFIG_PATH);
                //取根结点
                var root = xmlDoc.DocumentElement;//取到根结点
                //  读取rain结点
                dayInterval = Int32.Parse(xmlDoc.SelectSingleNode("RainIntervalConfig/day").InnerText.ToString().Trim());
                hourInterval = Int32.Parse(xmlDoc.SelectSingleNode("RainIntervalConfig/hour").InnerText.ToString().Trim());
                if (dayInterval <= 0)
                {
                    MessageBox.Show("配置文件'RainIntervalConfig'中数据异常");
                    dayInterval = 500;
                }
                if (hourInterval <= 0)
                {
                    MessageBox.Show("配置文件'RainIntervalConfig'中数据异常");
                    hourInterval = 100;
                }
            }
            catch (Exception ee1)
            {
                Debug.WriteLine(ee1);
                dayInterval = 500;
                hourInterval = 100;
            }

        }

        public string GetComFlagById(string id, EChannelType type)
        {
            string flag = "";
            if (m_mapStation != null && m_mapStation.ContainsKey(id))
            {
                CEntityStation station = m_mapStation[id];
                if (type == EChannelType.GPRS)
                {
                    flag = station.GPRS;
                }
                else if (type == EChannelType.GSM)
                {
                    flag = station.GSM;
                }
                else if (type == EChannelType.BeiDou || type == EChannelType.Beidou500 || type == EChannelType.BeidouNormal)
                {
                    flag = station.BDMemberSatellite;
                }
            }
            return flag;
        }

        public CEntityStation GetStationByGprs_1(string GprsId)
        {
            if (m_mapGprsStation.ContainsKey(GprsId))
            {
                return m_mapGprsStation[GprsId];
            }
            return null;    //没有找到匹配，返回空

        }

        public Dictionary<string, List<CEntityWaterFlowMap>> GetStationWaterFlowMap()
        {
            return m_mapStationWaterFlow;
        }
        /// <summary>
        /// 获取站点最新数据的时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns>如果没有，则返回空</returns>
        public Nullable<DateTime> GetStationLastDateTime(string stationID)
        {
            if (m_mapStationRTD.ContainsKey(stationID))
            {
                // 在判断这个实时数据是否可用
                // if (m_mapStationRTD[stationID].BIsReceivedValid)
                {
                    // 如果可用，返回时间
                    return m_mapStationRTD[stationID].TimeDeviceGained;
                }
            }
            return null;
        }

        public void UpdateAllStation()
        {
            Dictionary<string, CEntityStation> mapStationBefore = m_mapStation;
            Dictionary<string, CEntityRealTime> mapStationRTDBefore = m_mapStationRTD;
            m_listStations = m_proxyStation.QueryAll();
            // 对站点排序
            m_listStations.Sort();
            // 建立站点映射
            m_mapStation = new Dictionary<string, CEntityStation>();
            // 站点实时数据映射
            m_mapStationRTD = new Dictionary<string, CEntityRealTime>();
            // 更新站点数据，以及实时数据
            foreach (CEntityStation entity in m_listStations)
            {
                // 如果以前的map中包含站的数据，最新雨量记录和上次水位的就不需要查询数据库了
                if (mapStationBefore.ContainsKey(entity.StationID))
                {

                    entity.LastDayTime = mapStationBefore[entity.StationID].LastDayTime;
                    entity.LastDayTotalRain = mapStationBefore[entity.StationID].LastDayTotalRain;
                    entity.LastClockSharpTime = mapStationBefore[entity.StationID].LastClockSharpTime;
                    entity.LastTotalRain = mapStationBefore[entity.StationID].LastTotalRain;
                    entity.LastClockSharpTotalRain = mapStationBefore[entity.StationID].LastClockSharpTotalRain;
                    entity.LastWaterStage = mapStationBefore[entity.StationID].LastWaterStage;
                }
                else
                {
                    // 那就是新增的站点，计算数值都为空
                    entity.LastDayTime = null;
                    entity.LastDayTotalRain = null;

                    entity.LastTotalRain = null;
                    entity.LastWaterStage = null;
                    entity.LastClockSharpTime = null;
                    entity.LastClockSharpTotalRain = null;
                }
                m_mapStation.Add(entity.StationID, entity);
            }
            FillRTDStationNotInMap_1(mapStationRTDBefore); //给所有的站点添加map,便于接受数据
            if (StationUpdated != null)
            {
                Protocol.Manager.XmlStationDataSerializer.Instance.DeleteFile();
                Protocol.Manager.XmlStationDataSerializer.Instance.Serialize(m_listStations);
                StationUpdated.Invoke(this, new EventArgs());
            }
        }
        public void UpdateAllSubCenter()
        {
            m_listSubCenter = m_proxySubCenter.QueryAll();
            if (SubCenterUpdated != null)
            {
                SubCenterUpdated.Invoke(this, new EventArgs());
            }
        }
        public void UpdateAllSerialPort()
        {
            m_listSerialPort = m_proxySerialPort.QueryAll();
            if (SerialPortUpdated != null)
            {
                //通知其他订阅者，重新更新串口配置
                SerialPortUpdated.Invoke(this, new EventArgs());
            }
        }
        public void UpdateStationWaterFlowMap()
        {
            InitStationFlowMapRecord();
        }

        /// <summary>
        /// 重新加载数据库
        /// </summary>
        public void ReloadDatabase()
        {
            // 重新加载数据库
            m_proxyStation = new CSQLStation();
            m_proxySubCenter = new CSQLSubCenter();
            m_proxyWater = new CSQLWater();
            m_proxyRain = new CSQLRain();
            m_proxyEva = new CSQLEva();
            m_proxyHEva = new CSQLHEva();
            m_proxyDEva = new CSQLDEva();
            m_proxyRealEva = new CSQLRealEva();
            m_proxyVoltage = new CSQLVoltage();
            m_proxyRealtime = new CSQLCurrenData();
            m_proxySerialPort = new CSQLSerialPort();
            m_proxyUser = new CSQLUser();
            m_proxyWarningInfo = new CSQLWarningInfo();
            m_proxyWaterFlowMap = new CSQLWaterFlowMap();
            // m_proxyCommunicationRate = new CSQLCommunicationRate();

            m_mapStation = new Dictionary<string, CEntityStation>();

            m_iMinutesRange = 30;

            // 读取所有的站点，以及分中心，还有串口
            m_listStations = m_proxyStation.QueryAll();
            m_listSubCenter = m_proxySubCenter.QueryAll();
            m_listSerialPort = m_proxySerialPort.QueryAll();

            // 对站点排序
            m_listStations.Sort();

            // 建立站点映射
            foreach (CEntityStation entity in m_listStations)
            {
                m_mapStation.Add(entity.StationID, entity);
            }
            // 清空实时数据表
            m_mapStationRTD = new Dictionary<string, CEntityRealTime>();
            // 初始化上次雨量记录
            resetTxt();
            InitStationRainRecord();
            // 初始化水位
            InitStationLastWaterStage();
            // 初始化水位流量表
            InitStationFlowMapRecord();
            // 初始化电压记录
            InitLastVoltageRecord();
            // 填满剩下的实时数据表
            FillRTDStationNotInMap();

            Protocol.Manager.XmlStationDataSerializer.Instance.DeleteFile();
            Protocol.Manager.XmlStationDataSerializer.Instance.Serialize(m_listStations);

            // 更新分中心
            if (SubCenterUpdated != null)
            {
                SubCenterUpdated.Invoke(this, new EventArgs());
            }
            // 更新站点
            if (StationUpdated != null)
            {
                StationUpdated.Invoke(this, new EventArgs());
            }
            // 更新串口
            if (SerialPortUpdated != null)
            {
                //通知其他订阅者，重新更新串口配置
                SerialPortUpdated.Invoke(this, new EventArgs());
            }
            // 清空实时数据
            if (RTDCleared != null)
            {
                RTDCleared.Invoke(this, new EventArgs());
            }
            // 清空实时数据
            if (RTDCleared_Eva != null)
            {
                RTDCleared_Eva.Invoke(this, new EventArgs());
            }
            // 最新数据
            SentRTDMsg();
            SentRTDEva();
        }

        /// <summary>
        /// 根据站点ID以及站点类型
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="waterStage"></param>
        /// <returns></returns>
        public Nullable<Decimal> GetWaterFlowByWaterStageAndStation(string stationId, Nullable<decimal> waterStage)
        {
            if (!waterStage.HasValue)
            {
                return null;
            }
            Nullable<Decimal> waterFlow = null;
            stationId = stationId.Trim();
            if (m_mapStationWaterFlow.ContainsKey(stationId))
            {
                // 根据站点雨量计算水位值
                List<CEntityWaterFlowMap> lookup = m_mapStationWaterFlow[stationId];
                waterFlow = CalWateFlowByWaterStage(waterStage.Value, lookup);
            }
            else
            {
                // 遇到未知站点数据类型
                CSystemInfoMgr.Instance.AddInfo
                    (
                        string.Format("GetWaterFlowByWaterStageAndStation Error,未知站点{0}", stationId)
                    );
            }
            return waterFlow;
        }

        /// <summary>
        /// 停止数据库服务，阻塞被调用线程
        /// </summary>
        public void StopDBService()
        {
            try
            {
                m_bStopServer = true; //停止接收任何数据
                // 先等待当前的任务完成,此时不再接受任何其它消息
                while (m_listCurrentTask.Count > 0)
                {
                    Task.WaitAll(m_listCurrentTask.ToArray());
                }


                //m_mutexTaskList.WaitOne();
                //while (m_listCurrentTask.Count > 0)
                {

                    //m_mutexTaskList.ReleaseMutex();
                    //m_listCurrentTask[0].Wait();
                    //Debug.WriteLine("Status:" + m_listCurrentTask[0].Status);
                    //if (m_listCurrentTask[0].Status == TaskStatus.WaitingToRun)
                    //{
                    //m_listCurrentTask[0].
                    //}
                    //m_mutexTaskList.WaitOne();
                    //Thread.Sleep(500);
                }

                m_proxyRain.Close();
                m_proxyWater.Close();
                m_proxyVoltage.Close();
                m_proxyRealtime.Close();
                m_proxyEva.Close();
                m_proxyHEva.Close();
                m_proxyDEva.Close();
                m_proxyRealEva.Close();

                // 告警信息
                m_proxyWarningInfo.Close();
                // 统计报表
                //  m_proxyCommunicationRate.Close();

                // 此时实时数据表中的记录项应该和站点一样多
                // 写入实时数据XML
                List<CEntityRealTime> listRTD = new List<CEntityRealTime>();
                foreach (KeyValuePair<string, CEntityRealTime> entity in m_mapStationRTD)
                {
                    if (m_mapStation.ContainsKey(entity.Key))
                    {
                        if (m_mapStation[entity.Key].LastDataTime.HasValue)
                        {
                            entity.Value.TimeReceived = DateTime.Parse(m_mapStation[entity.Key].LastDataTime.ToString());
                        }

                        //entity.Value.DDayRainFall = m_mapStation[entity.Key]
                        entity.Value.LastDayTime = m_mapStation[entity.Key].LastDayTime;
                        entity.Value.LastDayTotalRain = m_mapStation[entity.Key].LastDayTotalRain;
                        entity.Value.LLastDayTotalRain = m_mapStation[entity.Key].LLastDayTotalRain;
                        entity.Value.LastTotalRain = m_mapStation[entity.Key].LastTotalRain;
                        entity.Value.LastClockSharpTime = m_mapStation[entity.Key].LastClockSharpTime;
                        entity.Value.LastClockSharpRain = m_mapStation[entity.Key].LastClockSharpTotalRain;
                        entity.Value.LastWaterStage = m_mapStation[entity.Key].LastWaterStage;
                        listRTD.Add(entity.Value);
                    }
                }
                CXmlRealTimeDataSerializer.Instance.DeleteFile();
                CXmlRealTimeDataSerializer.Instance.Serialize(listRTD);

                Protocol.Manager.XmlStationDataSerializer.Instance.DeleteFile();
                Protocol.Manager.XmlStationDataSerializer.Instance.Serialize(m_listStations);
                //m_mutexTaskList.ReleaseMutex();
            }
#pragma warning disable CS0168 // 声明了变量“exp”，但从未使用过
            catch (Exception exp) { };
#pragma warning restore CS0168 // 声明了变量“exp”，但从未使用过
        }

        /// <summary>
        /// 收到单条数据
        /// </summary>
        /// <param name="args"></param>
        public void EHRecvStationData(CEventRecvStationDataArgs args)
        {
            NewTask(() => { DealRTDData(args); });
        }

        // 180904 xcj
        /// <summary>
        /// 收到蒸发数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void EHRecvEvaDatas(object sender, CEventRecvStationDatasArgs args)
        {
            NewTask(() => { DealEvaDatas(args); });
        }
        /// <summary>
        /// 收到多条数据记录，包括多条雨量，水量以及电压记录
        /// </summary>
        /// <param name="args"></param>
        public void EHRecvStationDatas(object sender, CEventRecvStationDatasArgs args)
        {

            //m_mutexTaskList.WaitOne();
            NewTask(() => { DealRTDDatas(args); });
            //DealRTDDatas(args);
            //m_mutexTaskList.ReleaseMutex();
        }
        //gm 0331
        public void EHRecvStationTSDatas(object sender, CEventRecvStationDatasArgs args)
        {
            //m_mutexTaskList.WaitOne();
            NewTask(() => { DealRTDTSDatas(args); });
            //DealRTDDatas(args);
            //m_mutexTaskList.ReleaseMutex();
        }

        #endregion ///<公共方法

        #region 帮助方法

        public void SetEvaConf()
        {
            try
            {
                Dictionary<string,string> evaConf = Protocol.Manager.XMLEvaInfo.Instance.DeSerialize();
                EvaConf.kp = decimal.Parse(evaConf["kp"]);
                EvaConf.ke = decimal.Parse(evaConf["ke"]);
                EvaConf.dh = decimal.Parse(evaConf["dh"]);
                EvaConf.comP = bool.Parse(evaConf["comP"]);
            }
            catch (Exception e)
            {
                EvaConf.kp = 1.000m;
                EvaConf.ke = 1.000m;
                EvaConf.dh = 0.000m;
                EvaConf.comP = false;
                MessageBox.Show("未配置蒸发参数！");
            }
        }

        private void InitStationRainRecord()
        {
            // 初始化雨量记录，应该先尝试读取xml文件的，稍候再说
            // 读取所有站点的雨量记录，是否考虑多线程
            for (int i = 0; i < m_listStations.Count; ++i)
            {
                if (m_listStations[i].StationType == EStationType.EHydrology || m_listStations[i].StationType == EStationType.ERainFall)
                {
                    if (m_mapStationRTD.ContainsKey(m_listStations[i].StationID))
                    {
                        // 不需要计算，直接拿XML中的数据
                        m_listStations[i].LastTotalRain = m_mapStationRTD[m_listStations[i].StationID].LastTotalRain;
                        m_listStations[i].LastDayTotalRain = m_mapStationRTD[m_listStations[i].StationID].LastDayTotalRain;
                        m_listStations[i].LastClockSharpTotalRain = m_mapStationRTD[m_listStations[i].StationID].LastClockSharpRain;
                        m_listStations[i].LastDayTime = m_mapStationRTD[m_listStations[i].StationID].LastDayTime;
                        m_listStations[i].LastClockSharpTime = m_mapStationRTD[m_listStations[i].StationID].LastClockSharpTime;
                        continue;
                    }
                    Nullable<Decimal> lastTotalRain = null, lastDayTotalRain = null, llastDayTotalRain = null, lastClockSharpRain = null;
                    Nullable<DateTime> lastDayTime = null;
                    // Nullable<DateTime> llastDayTime = null;
                    Nullable<DateTime> lastClockSharpTime = null;
                    //   Nullable<Decimal> lastPeriodRain = null;
                    Nullable<DateTime> lastDataTime = null;
                    Nullable<EChannelType> lastChannelType = null;
                    Nullable<EMessageType> lastMessageType = null;
                    if (m_proxyRain.GetLastData(ref lastTotalRain, ref lastDataTime, ref lastDayTotalRain, ref llastDayTotalRain, ref lastClockSharpRain, ref lastClockSharpTime, ref lastDayTime, ref lastChannelType, ref lastMessageType, m_listStations[i].StationID))
                    {
                        // 查询成功
                        m_listStations[i].LastTotalRain = lastTotalRain;
                        m_listStations[i].LastDayTotalRain = lastDayTotalRain;
                        m_listStations[i].LLastDayTotalRain = llastDayTotalRain;
                        m_listStations[i].LastClockSharpTotalRain = lastClockSharpRain;
                        // m_listStations[i].LastPeriodRain = lastPeriodRain;
                        if (lastDataTime != null && lastDataTime.HasValue)
                        {
                            m_listStations[i].LastDataTime = lastDataTime;
                        }
                        if (lastDayTime != null && lastDayTime.HasValue)
                        {
                            m_listStations[i].LastDayTime = lastDayTime;
                        }
                        if (lastClockSharpTime != null && lastClockSharpTime.HasValue)
                        {
                            m_listStations[i].LastClockSharpTime = lastClockSharpTime;
                        }
                        if (lastChannelType != null && lastChannelType.HasValue)
                        {
                            m_listStations[i].LastChannelType = lastChannelType;
                        }
                        if (lastMessageType != null && lastMessageType.HasValue)
                        {
                            m_listStations[i].LastMessageType = lastMessageType;
                        }
                    }
                }
            }
        }

        private void InitStationLastWaterStage()
        {
            // 初始化雨量记录，应该先尝试读取xml文件的，稍候再说
            // 读取所有站点的雨量记录，是否考虑多线程
            for (int i = 0; i < m_listStations.Count; ++i)
            {
                if (m_listStations[i].StationType == EStationType.EHydrology || m_listStations[i].StationType == EStationType.ERiverWater)
                {
                    if (m_mapStationRTD.ContainsKey(m_listStations[i].StationID))
                    {
                        // 不需要计算，直接拿XML中的数据
                        m_listStations[i].LastWaterStage = m_mapStationRTD[m_listStations[i].StationID].LastWaterStage;
                        continue;
                    }
                    Nullable<Decimal> lastWaterStage = null;
                    Nullable<Decimal> lastWaterFlow = null;
                    Nullable<DateTime> lastDay = null;
                    Nullable<EChannelType> lastChannelType = null;
                    Nullable<EMessageType> lastMessageType = null;
                    if (m_proxyWater.GetLastData(ref lastWaterStage, ref lastWaterFlow, ref lastDay, ref lastChannelType, ref lastMessageType, m_listStations[i].StationID))
                    {
                        // 查询成功
                        m_listStations[i].LastWaterStage = lastWaterStage;
                        m_listStations[i].LastWaterFlow = lastWaterFlow;
                        if (lastDay != null && lastDay.HasValue)
                        {
                            m_listStations[i].LastDataTime = lastDay;
                        }
                        if (lastChannelType != null && lastChannelType.HasValue)
                        {
                            m_listStations[i].LastChannelType = lastChannelType;
                        }
                        if (lastMessageType != null && lastMessageType.HasValue)
                        {
                            m_listStations[i].LastMessageType = lastMessageType;
                        }
                    }
                }
            }
        }

        private void InitStationFlowMapRecord()
        {
            // 初始化站点水位流量记录，用于计算实时流量
            m_mapStationWaterFlow = new Dictionary<string, List<CEntityWaterFlowMap>>();
            foreach (CEntityStation station in m_listStations)
            {
                List<CEntityWaterFlowMap> listTmp = m_proxyWaterFlowMap.QueryMapsByStationId(station.StationID);
                listTmp.Sort();
                // 添加到记录中
                m_mapStationWaterFlow.Add(station.StationID.Trim(), listTmp);
            }
        }

        /// <summary>
        /// 初始化最一条电压记录
        /// </summary>
        private void InitLastVoltageRecord()
        {
            for (int i = 0; i < m_listStations.Count; ++i)
            {
                if (m_mapStationRTD.ContainsKey(m_listStations[i].StationID))
                {
                    // 不需要计算，直接拿XML中的数据
                    m_listStations[i].LastWaterStage = m_mapStationRTD[m_listStations[i].StationID].LastWaterStage;
                    continue;
                }
                Nullable<Decimal> lastVoltage = null;
                Nullable<DateTime> lastDay = null;
                Nullable<EChannelType> lastChannelType = null;
                Nullable<EMessageType> lastMessageType = null;
                if (m_proxyVoltage.GetLastData(ref lastVoltage, ref lastDay, ref lastChannelType, ref lastMessageType, m_listStations[i].StationID))
                {
                    // 查询成功
                    m_listStations[i].LastVoltage = lastVoltage;
                    if (lastDay != null && lastDay.HasValue)
                    {
                        m_listStations[i].LastDataTime = lastDay;
                    }
                    if (lastChannelType != null && lastChannelType.HasValue)
                    {
                        m_listStations[i].LastChannelType = lastChannelType;
                    }
                    if (lastMessageType != null && lastMessageType.HasValue)
                    {
                        m_listStations[i].LastMessageType = lastMessageType;
                    }
                }
            }
        }

        /// <summary>
        /// 读取XML文件，生成实时数据表，有用的字段是：最近雨量和水位
        /// </summary>
        /// 
        private void resetTxt()
        {
            FileStream fs = new FileStream("numgsm.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            FileStream fs2 = new FileStream("numbd.txt", FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2);
            //开始写入
            sw.Write(0);
            sw2.Write(0);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();

            sw2.Flush();
            //关闭流
            sw2.Close();
            fs2.Close();
        }
        private void CreateTable()
        {
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString();
            string rainName = "rain" + year;
            string waterName = "water" + year;
            string voltageName = "voltage" + year;

            m_proxyRain.createTable(rainName);
            m_proxyVoltage.createTable(voltageName);
            m_proxyWater.createTable(waterName);
        }
        private void ReadRTDXml()
        {
            m_mapStationRTD = new Dictionary<string, CEntityRealTime>();
            // 读取XML,初始化实时数据表
            List<CEntityRealTime> listRTD = CXmlRealTimeDataSerializer.Instance.Deserialize();
            if (null == listRTD)
            {
                return;
            }
            for (int i = 0; i < listRTD.Count; ++i)
            {
                if (m_mapStation.ContainsKey(listRTD[i].StrStationID))
                {
                    // 通知界面
                    m_mapStationRTD.Add(listRTD[i].StrStationID, listRTD[i]);
                }
                else
                {
                    // 位置站点，读取实时数据文件不匹配
                    CSystemInfoMgr.Instance.AddInfo(string.Format("实时数据中站点\"{0}\"在数据库中匹配失败", listRTD[i].StrStationID));
                }
            }

            // 将实时数据表存入数据库中,更新不好做，先删了再加
            m_proxyRealtime.DeleteRows(listRTD);
            m_proxyRealtime.AddNewRows(listRTD);

            //删除实时数据表
            //修改到关闭的时候再删除，以免意外关闭导致xml为空
            //修改为删除，以免非正常退出时，再次启动为前一次数据
            CXmlRealTimeDataSerializer.Instance.DeleteFile();
        }

        private void ReadRTS()
        {
            m_mapStationRTS = new Dictionary<string, CEntityRealEva>();
            // 读取数据库,初始化实时蒸发数据表
            List<CEntityRealEva> listRTS = m_proxyRealEva.QueryAll();
            List<CEntityEva> evaList = m_proxyEva.get4InitEva();
            Dictionary<String, CEntityEva> evaDic = new Dictionary<string, CEntityEva>();
            List<CEntityEva> hEvaList = m_proxyHEva.get4InitEva();
            Dictionary<String, CEntityEva> hEvaDic = new Dictionary<string, CEntityEva>();
            List<CEntityEva> dEvaList = m_proxyDEva.get4InitEva();
            Dictionary<String, CEntityEva> dEvaDic = new Dictionary<string, CEntityEva>();
            if(evaList == null || evaList.Count == 0)
            {
                return;
            }
           
            foreach (CEntityEva eva in evaList)
            {
                evaDic[eva.StationID] = eva;
            }
            if(hEvaList != null && hEvaList.Count > 0)
            {
                foreach (CEntityEva eva in hEvaList)
                {
                    hEvaDic[eva.StationID] = eva;
                }
            }
            if(dEvaList != null && dEvaList.Count > 0)
            {
                foreach (CEntityEva eva in dEvaList)
                {
                    dEvaDic[eva.StationID] = eva;
                }
            }
            
            for(int i = 0;i< evaList.Count; i++)
            {
                CEntityRealEva realEva = new CEntityRealEva();
                realEva.StrStationID = evaList[i].StationID;
                if (!m_mapStation.ContainsKey(evaList[i].StationID))
                {
                    continue;
                }
                realEva.StrStationName = m_mapStation[evaList[i].StationID].StationName;
                if (evaDic.ContainsKey(evaList[i].StationID))
                {
                    realEva.TimeDeviceGained = evaDic[realEva.StrStationID].TimeCollect;
                    realEva.Temperature = evaDic[realEva.StrStationID].Temperature;
                    realEva.RawEva = evaDic[realEva.StrStationID].Eva;
                    realEva.RawRain = evaDic[realEva.StrStationID].Rain;
                    realEva.RawVoltage = evaDic[realEva.StrStationID].Voltage;
                }
                if (hEvaDic.ContainsKey(evaList[i].StationID))
                {
                    realEva.Eva = hEvaDic[realEva.StrStationID].Eva;
                    realEva.Rain = hEvaDic[realEva.StrStationID].Rain;
                }
                if (dEvaDic.ContainsKey(evaList[i].StationID)){
                    realEva.LastDayEva = dEvaDic[realEva.StrStationID].E;
                    realEva.LastDayRain = dEvaDic[realEva.StrStationID].P;
                }
                m_mapStationRTS.Add(realEva.StrStationID, realEva);
                m_mapStation[evaList[i].StationID].LastDayRain = realEva.LastDayRain;
                m_mapStation[evaList[i].StationID].LastDayEva = realEva.LastDayEva;
            }
            //if (null == listRTS)
            //{
            //    return;
            //}
            //for (int i = 0; i < listRTS.Count; ++i)
            //{
            //    if (m_mapStation.ContainsKey(listRTS[i].StrStationID))
            //    {
            //        // 通知界面
            //        m_mapStationRTS.Add(listRTS[i].StrStationID, listRTS[i]);
            //    }
            //    else
            //    {
            //        // 位置站点，读取实时蒸发数据文件不匹配
            //        CSystemInfoMgr.Instance.AddInfo(string.Format("实时蒸发数据中站点\"{0}\"在数据库中匹配失败", listRTS[i].StrStationID));
            //    }
            //}
        }

        /// <summary>
        /// 判断雨量值的状态
        /// </summary>
        /// <param name="rain"></param>
        /// <param name="rtdState"></param>
        private void AssertAndAdjustRainData(CEntityRain rain, ref ERTDDataState rtdState, bool bNotifyWarning = true)
        {
            // 判断雨量信息是否合法，写入系统信息或者告警信息
            CEntityStation station = GetStationById(rain.StationID);
            // 判断是否超过变化值
            rtdState = ERTDDataState.ENormal; //默认正常
            if (station.DRainChange.HasValue)
            {
                // if (rain.PeriodRain > station.DRainChange.Value)
                if (rain.DifferneceRain > station.DRainChange.Value)
                {
                    // 告警信息，和系统信息
                    string info = string.Format("站点（{0}|{1}）雨量变化 {2}超过限制{3}", station.StationID, station.StationName,
                        rain.DifferneceRain.Value.ToString("0.00"), station.DRainChange.Value.ToString("0.00"));
                    if (bNotifyWarning)
                    {
                        CWarningInfoMgr.Instance.AddInfo(info, rain.TimeCollect, EWarningInfoCodeType.ERain, station.StationID);
                        CSystemInfoMgr.Instance.AddInfo(info, rain.TimeCollect, ETextMsgState.EError);
                        if (flag == 0)
                        {
                            flag = 1;
                            
                            info = rain.TimeCollect.ToString() + "\r\n" + "\r\n" + info + "\r\n" + "\r\n" + "是否关闭报警声音？" + "\r\n";
                            
                        }
                    }
                    rtdState = ERTDDataState.EError; // 超过预警值
                }
                //  if (rain.PeriodRain < 0)
                if (rain.DifferneceRain < 0)
                {
                    // 雨量发生了跳变
                    string info = string.Format("站点（{0}|{1}）雨量变化 {2}小于0", station.StationID,
                        station.StationName, rain.DifferneceRain.Value.ToString("0.00"));
                    if (bNotifyWarning)
                    {
                        CWarningInfoMgr.Instance.AddInfo(info, rain.TimeCollect, EWarningInfoCodeType.ERain, station.StationID);
                        CSystemInfoMgr.Instance.AddInfo(info, rain.TimeCollect, ETextMsgState.EError);
                        if (flag == 0)
                        {
                            flag = 1;

                            //MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                            //DialogResult dr = MessageBox.Show(info + " ，是否关闭报警声音?", "雨量报警", messButton);
                            //if (dr == DialogResult.OK)
                            //{
                            //    CVoicePlayer.Instance.Stop();
                            //    flag = 0;
                            //}
                            //MessageBoxForm mbForm = new MessageBoxForm();

                            //string str1 = "雨量报警";
                            //string str2 = info + " ，是否关闭报警声音?";
                            info = rain.TimeCollect.ToString() + "\r\n" + "\r\n" + info + "\r\n" + "\r\n" + "是否关闭报警声音？" + "\r\n";
                            //Thread t = new Thread(new ParameterizedThreadStart(MessageShow));
                            //t.Start(info);

                            ////MessageBoxForm mbForm = new MessageBoxForm();
                            ////mbForm.StartPosition = FormStartPosition.CenterParent;
                            ////mbForm.Text = "雨量报警";
                            ////mbForm.label3.Text = rain.TimeCollect.ToString();
                            ////mbForm.label1.Text = info;
                            ////mbForm.label2.Text = "是否关闭报警声音?";

                            ////mbForm.TopMost = true;
                            ////mbForm.ShowDialog();
                        }

                    }
                    rtdState = ERTDDataState.EError; // 雨量发生跳变
                    // 更改雨量值为0
                    rain.PeriodRain = 0;

                }
            }

        }

        
        /// <summary>
        /// 判断水位值的状态
        /// </summary>
        /// <param name="water"></param>
        /// <param name="rtdState"></param>
        private void AssertWaterData(CEntityWater water, ref ERTDDataState rtdState, ref int status, bool bNotityWarning = true)
        {

            // 判断水量信息是否合法，写入系统信息或者告警信息
            CEntityStation station = GetStationById(water.StationID);
            StringBuilder errinfo = new StringBuilder();
            // 判断是否超过最大值
            rtdState = ERTDDataState.ENormal;
            if (station.DWaterMax.HasValue)
            {
                if (water.WaterStage > station.DWaterMax)
                {
                    errinfo.AppendFormat("水位 {0} 超过最大值 {1} 站点编号：{2}", water.WaterStage.ToString("0.00"), station.DWaterMax.Value.ToString("0.00"), water.StationID);
                    rtdState = ERTDDataState.EError;
                    status = 0;
                }
            }
            // 判断是否低于最小值
            if (station.DWaterMin.HasValue)
            {
                if (water.WaterStage < station.DWaterMin)
                {
                    errinfo.AppendFormat("水位 {0} 低于最小值 {1} 站点编号：{2}", water.WaterStage.ToString("0.00"), station.DWaterMin.Value.ToString("0.00"), water.StationID);
                    rtdState = ERTDDataState.EError;
                    status = 0;
                }
            }

            // 判断是否超过允许变化值,暂时还未考虑好
            if (station.DWaterChange.HasValue && station.LastWaterStage.HasValue)
            {
                Decimal change = water.WaterStage - station.LastWaterStage.Value;
                if (change > station.DWaterChange)
                {
                    errinfo.AppendFormat("水位变化 {0} 超过允许值{1} 站点编号：{2}", change.ToString("0.00"), station.DWaterChange.Value.ToString("0.00"), water.StationID);
                    rtdState = ERTDDataState.EError;
                    status = 0;
                }
            }

            // 更新水位值，便于计算水位变化
            station.LastWaterStage = water.WaterStage;

            // 通知其它页面
            if (!errinfo.ToString().Equals(""))
            {
                if (bNotityWarning)
                {
                    CSystemInfoMgr.Instance.AddInfo(errinfo.ToString(), water.TimeCollect, ETextMsgState.EError);
                    CWarningInfoMgr.Instance.AddInfo(errinfo.ToString(), water.TimeCollect, EWarningInfoCodeType.EWater, station.StationID);
                    if (flag == 0)
                    {
                        flag = 1;
                        //MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                        //DialogResult dr = MessageBox.Show(errinfo + " ，是否关闭报警声音?", "水位报警", messButton);
                        //if (dr == DialogResult.OK)
                        //{
                        //    CVoicePlayer.Instance.Stop();
                        //    flag = 0;
                        //}
                        //string str1="水位报警";
                        //string str2=errinfo + " ，是否关闭报警声音?";
                        //errinfo = water.TimeCollect.ToString() + "\r\n" + errinfo;
                        string info = water.TimeCollect.ToString() + "\r\n" + "\r\n" + errinfo.ToString() + "\r\n" + "\r\n" + "是否关闭报警声音？" + "\r\n";
                        //Thread t = new Thread(new ParameterizedThreadStart(MessageShow_2));
                        //t.Start(info);
                        ////MessageBoxForm mbForm = new MessageBoxForm();
                        ////mbForm.StartPosition = FormStartPosition.CenterParent;
                        ////mbForm.Text = "水位报警";
                        ////mbForm.label3.Text = water.TimeCollect.ToString();
                        ////mbForm.label1.Text = errinfo.ToString();
                        ////mbForm.label2.Text = "是否关闭报警声音?";
                        ////mbForm.TopMost = true;
                        ////mbForm.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// 判断电压值是否小于下限值
        /// </summary>
        /// <param name="voltage"></param>
        /// <param name="rtdState"></param>
        /// <param name="bNotityWarning"></param>
        private void AssertVoltageData(CEntityVoltage voltage, ref ERTDDataState rtdState, ref int status, bool bNotityWarning = true)
        {
            CEntityStation station = GetStationById(voltage.StationID);
            rtdState = ERTDDataState.ENormal;
            if (null == station)
            {
                Debug.WriteLine("AssertVoltageData Failed");
                return;
            }
            if (station.DVoltageMin.HasValue)
            {
                if (voltage.Voltage < (Decimal)station.DVoltageMin.Value)
                {
                    // 小于最小值，报警
                    if (bNotityWarning)
                    {
                        string errinfo = string.Format("站点：{0} 电压值：{1} 低于下限值：{2}", station.StationID,
                        voltage.Voltage, station.DVoltageMin.Value);
                        status = 0;
                        CSystemInfoMgr.Instance.AddInfo(errinfo.ToString(), voltage.TimeCollect, ETextMsgState.EError);
                        CWarningInfoMgr.Instance.AddInfo(errinfo.ToString(), voltage.TimeCollect, EWarningInfoCodeType.EVlotage, station.StationID);
                        if (flag == 0)
                        {
                            flag = 1;
                            //MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                            //DialogResult dr = MessageBox.Show(errinfo + " ，是否关闭报警声音?", "电压报警", messButton);
                            //if (dr == DialogResult.OK)
                            //{
                            //    CVoicePlayer.Instance.Stop();
                            //    flag = 0;
                            //}
                            //MessageBoxForm mbForm = new MessageBoxForm();

                            //string str1 = "水位报警";
                            //string str2 = errinfo + " ，是否关闭报警声音?";
                            string info = voltage.TimeCollect.ToString() + "\r\n" + "\r\n" + errinfo.ToString() + "\r\n" + "\r\n" + "是否关闭报警声音？" + "\r\n";
                            //Thread t = new Thread(new ParameterizedThreadStart(MessageShow_3));
                            //t.Start(info);
                            ////MessageBoxForm mbForm = new MessageBoxForm();
                            ////mbForm.StartPosition = FormStartPosition.CenterParent;
                            ////mbForm.Text = "电压报警";
                            ////mbForm.label3.Text = voltage.TimeCollect.ToString();
                            ////mbForm.label1.Text = errinfo.ToString();
                            ////mbForm.label2.Text = "是否关闭报警声音?";
                            ////mbForm.TopMost = true;
                            ////mbForm.ShowDialog();
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 计算实时数据的状态
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="dPeriodRain"></param>
        /// <param name="dWaterStage"></param>
        /// <returns></returns>
        private ERTDDataState GetDataStatus(string stationId, Nullable<Decimal> dPeriodRain, Nullable<Decimal> dWaterStage)
        {
            ERTDDataState state = ERTDDataState.ENormal;
            int status = 1;
            CEntityRain rain = new CEntityRain();
            rain.StationID = stationId;
            rain.PeriodRain = dPeriodRain;

            AssertAndAdjustRainData(rain, ref state, false);
            if (state != ERTDDataState.ENormal)
            {
                return state;
            }
            if (dWaterStage.HasValue)
            {
                CEntityWater water = new CEntityWater();
                water.StationID = stationId;
                water.WaterStage = dWaterStage.Value;

                AssertWaterData(water, ref state, ref status, false);
                if (state != ERTDDataState.ENormal)
                {
                    return state;
                }
            }
            return state; //此时状态正常
        }

        /// <summary>
        /// 计算时段雨量和日雨量,totalRain需要乘以精度,得到的才是真实雨量值
        /// </summary>
        /// <param name="dRainArruracy"></param>
        /// <param name="lastDayRainDateTime"></param>
        /// <param name="lastDayTotalRain"></param>
        /// <param name="lastTotalRain"></param>
        /// <param name="totalRain"></param>
        /// <param name="datetime"></param>
        /// <param name="dDayRain"></param>
        /// <param name="dPeriodRain"></param>
        private void CalPeriodDayRain(float dRainArruracy, Nullable<DateTime> lastDayRainDateTime,
            Nullable<Decimal> lastDayTotalRain, Nullable<Decimal> lastTotalRain, Decimal totalRain, DateTime datetime,
            ref Nullable<Decimal> dDayRain, ref Nullable<Decimal> dPeriodRain)
        {
            try
            {
                dDayRain = null;
                dPeriodRain = null;
                // 时段雨量
                if (lastTotalRain.HasValue)
                {
                    dPeriodRain = totalRain * (Decimal)dRainArruracy - lastTotalRain.Value;
                }
                if (dPeriodRain < 0)
                {
                    dPeriodRain = 0;
                }

                // 如果时间设置的误差范围内，整点，默认m_iMutesRange为0
                int offset = (datetime.Hour - 8) * 60 + datetime.Minute;
                if (Math.Abs(offset) <= m_iMinutesRange)
                {
                    // 计算日雨量,日期相差一天
                    if (lastDayTotalRain.HasValue && lastDayRainDateTime.HasValue)
                    {
                        TimeSpan timespan = datetime - lastDayRainDateTime.Value;
                        if (1 == timespan.Days)
                        {
                            // 并且日期相差一天，才计算日雨量，否则都为空
                            dDayRain = totalRain * (Decimal)dRainArruracy - lastDayTotalRain;
                        }
                    }
                    if (dDayRain < 0)
                    {
                        dDayRain = 0;
                    }
                    //dPeriodRain = 0; //整点不符合条件，默认日雨量都为0
                }// end of if minutes accepted
            }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
            catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
            {

            }
        }

        // private void CalDifferenceRain(float dRainArruracy, Nullable<DateTime> lastDayRainDateTime,
        //Nullable<Decimal> lastDayTotalRain, Nullable<Decimal> lastTotalRain, Decimal totalRain, DateTime datetime,
        //ref Nullable<Decimal> dDifferenceRain)
        // {
        //     dDifferenceRain = null;
        //     if (lastTotalRain.HasValue)
        //     {
        //         dDifferenceRain = totalRain - lastTotalRain.Value;
        //     }
        //     //dDayRain = null;
        //     //dPeriodRain = null;
        //     //// 时段雨量
        //     //if (lastTotalRain.HasValue)
        //     //{
        //     //    dPeriodRain = totalRain * (Decimal)dRainArruracy - lastTotalRain.Value;
        //     //}

        //     //// 如果时间设置的误差范围内，整点，默认m_iMutesRange为0
        //     //int offset = (datetime.Hour - 8) * 60 + datetime.Minute;
        //     //if (Math.Abs(offset) <= m_iMinutesRange)
        //     //{
        //     //    // 计算日雨量,日期相差一天
        //     //    if (lastDayTotalRain.HasValue && lastDayRainDateTime.HasValue)
        //     //    {
        //     //        TimeSpan timespan = datetime - lastDayRainDateTime.Value;
        //     //        if (1 == timespan.Days)
        //     //        {
        //     //            // 并且日期相差一天，才计算日雨量，否则都为空
        //     //            dDayRain = totalRain * (Decimal)dRainArruracy - lastDayTotalRain;
        //     //        }
        //     //    }
        //     //    //dPeriodRain = 0; //整点不符合条件，默认日雨量都为0
        //     //}// end of if minutes accepted
        // }

        //计算日雨量
        private void CalDayRain(string stationid, float dRainArruracy, Decimal totalRain, DateTime datetime, Nullable<DateTime> LastDayTime,
            Nullable<Decimal> lastDayTotalRain, DateTime tmp_1, ref Nullable<Decimal> dDayRain, ref int status)
        {
            dDayRain = null;
            // 如果时间设置的误差范围内，整点，默认m_iMutesRange为0

            int offset = (datetime.Hour - 8) * 60 + datetime.Minute;
            if (Math.Abs(offset) <= m_iMinutesRange)
            {
                try
                {
                    // 计算日雨量,日期相差一天
                    if (lastDayTotalRain.HasValue && LastDayTime.HasValue)
                    {
                        TimeSpan timespan = datetime - LastDayTime.Value;
                        if (1 == timespan.Days)
                        {
                            // 并且日期相差一天，才计算日雨量，否则都为空
                            dDayRain = totalRain * (Decimal)dRainArruracy - lastDayTotalRain;
                        }
                        else
                        {
                            lastDayTotalRain = m_proxyRain.GetLastDayTotalRain(stationid, tmp_1);
                            if (lastDayTotalRain.HasValue)
                            {
                                dDayRain = totalRain * (Decimal)dRainArruracy - lastDayTotalRain;
                            }
                        }
                    }
                    else
                    {
                        lastDayTotalRain = m_proxyRain.GetLastDayTotalRain(stationid, tmp_1);
                        if (lastDayTotalRain.HasValue)
                        {
                            dDayRain = totalRain * (Decimal)dRainArruracy - lastDayTotalRain;
                        }
                    }
                    if (dDayRain < 0)
                    {
                        if (dayInterval < (10000 * (Decimal)dRainArruracy) - lastDayTotalRain.Value)
                        {
                            dDayRain = 0;
                            status = 0;
                        }
                        else
                        {
                            dDayRain = 10000 * (Decimal)dRainArruracy - lastDayTotalRain.Value + (totalRain * (Decimal)dRainArruracy);
                        }
                    }
                    if (dDayRain > dayInterval)
                    {
                        status = 0;
                    }
                    if (dDayRain == null)
                    {
                        status = 2;
                    }
                }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                {
                    dDayRain = null;
                }

                //dPeriodRain = 0; //整点不符合条件，默认日雨量都为0
            }// end of if minutes accepted
        }
        private void CalDifferenceRain(float dRainArruracy, Decimal totalRain, DateTime datetime, Nullable<Decimal> lastTotalRain, Nullable<Decimal> MaxChange, ref int status, ref Nullable<Decimal> dDifferenceRain)
        {
            try
            {
                dDifferenceRain = null;
                status = 1;
                // 差值雨量
                if (lastTotalRain.HasValue)
                {
                    dDifferenceRain = totalRain * (Decimal)dRainArruracy - lastTotalRain.Value;
                }
                if (dDifferenceRain < 0)
                {
                    MaxChange = MaxChange.HasValue ? MaxChange : (50 / (decimal)dRainArruracy);
                    decimal tmp = (decimal)MaxChange / (decimal)dRainArruracy;
                    if ((10000 - lastTotalRain.Value / (Decimal)dRainArruracy) > tmp)
                    {
                        dDifferenceRain = 0;
                        status = 0;
                    }
                    else
                    {
                        dDifferenceRain = 10000 * (Decimal)dRainArruracy - lastTotalRain.Value + (totalRain * (Decimal)dRainArruracy);
                    }
                }
                if (dDifferenceRain > MaxChange)
                {
                    status = 0;
                }
            }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
            catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
            {

            }
        }

        private void CalPeriodRain(string stationid, float dRainArruracy, Decimal totalRain, DateTime datetime, Nullable<DateTime> lastDataRainDateTime, Nullable<Decimal> lastSharpTotalRain, Nullable<DateTime> lastSharpTime, ref Nullable<Decimal> dPeriodRain, ref int status)
        {
            try
            {
                dPeriodRain = null;
                DateTime tmp = DateTime.Now;
                int periodSpan;

                if (lastSharpTotalRain.HasValue && lastSharpTime.HasValue)
                {
                    TimeSpan timespan = datetime - lastSharpTime.Value;
                    periodSpan = GetPeriodSpan(stationid);
                    if (periodSpan == timespan.Hours)
                    {
                        dPeriodRain = totalRain * (Decimal)dRainArruracy - lastSharpTotalRain;
                    }
                    else
                    {
                        lastSharpTotalRain = m_proxyRain.GetLastClockSharpTotalRain(stationid, datetime);
                        if (lastSharpTotalRain.HasValue)
                        {
                            dPeriodRain = totalRain * (Decimal)dRainArruracy - lastSharpTotalRain;
                            status = 2;
                        }

                    }
                }
                else
                {
                    lastSharpTotalRain = m_proxyRain.GetLastClockSharpTotalRain(stationid, datetime);
                    if (lastSharpTotalRain.HasValue)
                    {
                        dPeriodRain = totalRain * (Decimal)dRainArruracy - lastSharpTotalRain;
                        status = 2;
                    }
                }
                if (dPeriodRain < 0)
                {
                    if (hourInterval < (10000 * (Decimal)dRainArruracy) - lastSharpTotalRain.Value)
                    {
                        dPeriodRain = 0;
                        status = 0;
                    }
                    else
                    {
                        dPeriodRain = 10000 * (Decimal)dRainArruracy - lastSharpTotalRain.Value + (totalRain * (Decimal)dRainArruracy);
                    }
                }
                if (dPeriodRain > hourInterval)
                {
                    status = 0;
                }
            }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
            catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
            {

            }


        }
        private Nullable<Decimal> CalWateFlowByWaterStage(Decimal waterStage, List<CEntityWaterFlowMap> lookup)
        {
            //  Decimal currWaterFlow = 0;
            List<Decimal> currWaterFlow = new List<decimal>();
            List<Decimal> currWaterFlow2 = new List<decimal>();
            // 根据水位计算流量
            Nullable<Decimal> result = null;
            if (lookup.Count <= 1)
            {
                // 只有一个，不能计算，返回空
                if (lookup.Count == 1 && lookup[0].ZR == waterStage)
                {
                    // result = lookup[0].WaterFlow;
                    result = lookup[0].ZR;
                }
            }
            else
            {
                // if (waterStage < lookup[0].WaterStage)
                if (waterStage < lookup[0].ZR)
                {
                    currWaterFlow.Add(lookup[0].Q1);
                    currWaterFlow.Add(lookup[0].Q2);
                    currWaterFlow.Add(lookup[0].Q3);
                    currWaterFlow.Add(lookup[0].Q4);
                    currWaterFlow.Add(lookup[0].Q5);
                    currWaterFlow.Add(lookup[0].Q6);
                    int index = int.Parse(lookup[0].currQ.ToString());

                    currWaterFlow2.Add(lookup[1].Q1);
                    currWaterFlow2.Add(lookup[1].Q2);
                    currWaterFlow2.Add(lookup[1].Q3);
                    currWaterFlow2.Add(lookup[1].Q4);
                    currWaterFlow2.Add(lookup[1].Q5);
                    currWaterFlow2.Add(lookup[1].Q6);

                    int index2 = int.Parse(lookup[1].currQ.ToString());
                    // 位于最小值的前面
                    result = CalPointY(lookup[0].ZR, currWaterFlow[index - 1],
                        lookup[1].ZR, currWaterFlow2[index2 - 1],
                        waterStage);
                }
                else if (waterStage > lookup[lookup.Count - 1].ZR)
                {
                    currWaterFlow2.Add(lookup[lookup.Count - 2].Q1);
                    currWaterFlow2.Add(lookup[lookup.Count - 2].Q2);
                    currWaterFlow2.Add(lookup[lookup.Count - 2].Q3);
                    currWaterFlow2.Add(lookup[lookup.Count - 2].Q4);
                    currWaterFlow2.Add(lookup[lookup.Count - 2].Q5);
                    currWaterFlow2.Add(lookup[lookup.Count - 2].Q6);

                    int index2 = int.Parse(lookup[lookup.Count - 2].currQ.ToString());

                    currWaterFlow.Add(lookup[lookup.Count - 1].Q1);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q2);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q3);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q4);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q5);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q6);
                    int index = int.Parse(lookup[lookup.Count - 1].currQ.ToString());

                    // 位于最大值的后面
                    result = CalPointY(lookup[lookup.Count - 1].ZR, currWaterFlow[index - 1],
                        lookup[lookup.Count - 2].ZR, currWaterFlow2[index2 - 1],
                        waterStage);
                }
                else if (waterStage == lookup[0].ZR)
                {
                    currWaterFlow.Add(lookup[0].Q1);
                    currWaterFlow.Add(lookup[0].Q2);
                    currWaterFlow.Add(lookup[0].Q3);
                    currWaterFlow.Add(lookup[0].Q4);
                    currWaterFlow.Add(lookup[0].Q5);
                    currWaterFlow.Add(lookup[0].Q6);
                    int index = int.Parse(lookup[0].currQ.ToString());
                    // 等于最小值
                    result = currWaterFlow[index - 1];
                }
                else if (waterStage == lookup[lookup.Count - 1].ZR)
                {
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q1);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q2);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q3);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q4);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q5);
                    currWaterFlow.Add(lookup[lookup.Count - 1].Q6);
                    int index = int.Parse(lookup[lookup.Count - 1].currQ.ToString());
                    // 等于最大值
                    result = currWaterFlow[index - 1];
                }
                else
                {
                    #region 位于最小值和最大值区间
                    // 至少两个点，可以计算流量值
                    int indexMin = 0;
                    int indexMax = lookup.Count - 1;
                    int indexMiddle = 0;
                    while (indexMin < (indexMax - 1))
                    {
                        // 二分查找
                        indexMiddle = (int)Math.Ceiling((indexMin + indexMax) / (double)2);
                        if (waterStage > lookup[indexMiddle].ZR)
                        {
                            // 如果大于中间，去中间到右边的部分
                            indexMin = indexMiddle;
                        }
                        else if (waterStage < lookup[indexMiddle].ZR)
                        {
                            // 如果小于中间，取中间到左边的部分
                            indexMax = indexMiddle;
                        }
                        else
                        {
                            // 说明刚好相等,不需要计算，直接返回
                            break;
                        }
                    }
                    if (waterStage == lookup[indexMiddle].ZR)
                    {
                        currWaterFlow.Add(lookup[indexMiddle].Q1);
                        currWaterFlow.Add(lookup[indexMiddle].Q2);
                        currWaterFlow.Add(lookup[indexMiddle].Q3);
                        currWaterFlow.Add(lookup[indexMiddle].Q4);
                        currWaterFlow.Add(lookup[indexMiddle].Q5);
                        currWaterFlow.Add(lookup[indexMiddle].Q6);
                        int index = int.Parse(lookup[indexMiddle].currQ.ToString());
                        // 找到准点匹配，直接返回数值就行
                        result = currWaterFlow[index - 1];
                    }
                    else
                    {

                        currWaterFlow.Add(lookup[indexMin].Q1);
                        currWaterFlow.Add(lookup[indexMin].Q2);
                        currWaterFlow.Add(lookup[indexMin].Q3);
                        currWaterFlow.Add(lookup[indexMin].Q4);
                        currWaterFlow.Add(lookup[indexMin].Q5);
                        currWaterFlow.Add(lookup[indexMin].Q6);
                        int index = int.Parse(lookup[indexMin].currQ.ToString());

                        currWaterFlow2.Add(lookup[indexMax].Q1);
                        currWaterFlow2.Add(lookup[indexMax].Q2);
                        currWaterFlow2.Add(lookup[indexMax].Q3);
                        currWaterFlow2.Add(lookup[indexMax].Q4);
                        currWaterFlow2.Add(lookup[indexMax].Q5);
                        currWaterFlow2.Add(lookup[indexMax].Q6);
                        int index2 = int.Parse(lookup[indexMax].currQ.ToString());
                        // 不是准点匹配，计算中间值
                        result = CalPointY(lookup[indexMin].ZR, currWaterFlow[index - 1],
                            lookup[indexMax].ZR, currWaterFlow2[index2 - 1],
                            waterStage);
                    }
                    #endregion 位于最小值和最大值区间
                }

            }// end of else count > 1
            return result;
        }

        /// <summary>
        /// 根据x1,y1,x2,y2的值，按照一次函数规律，计算x3点的y值
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="y3"></param>
        /// <returns></returns>
        private Decimal CalPointY(Decimal x1, Decimal y1, Decimal x2, Decimal y2, Decimal x3)
        {
            //// 包括x3在x1和x2之间，以及在x1和x2的区间之外
            //Decimal step = (y1 - y2) / (x1 - x2);
            //Decimal result = y1 + step * (x3 - x1);
            //return result;
            // 包括x3在x1和x2之间，以及在x1和x2的区间之外
            Decimal step, result = 0;
            if (x1 - x2 != 0)
            {
                step = (y1 - y2) / (x1 - x2);
                result = y1 + step * (x3 - x1);
            }
            return result;
        }

        //private void FillRTDStationNotInMap()
        //{
        //    for (int i = 0; i < m_listStations.Count; ++i)
        //    {
        //        if (!m_mapStationRTD.ContainsKey(m_listStations[i].StationID))
        //        {
        //            // 如果不包含，新建一个
        //            CEntityRealTime entity = new CEntityRealTime();
        //            entity.StrStationID = m_listStations[i].StationID;
        //            entity.StrStationName = m_listStations[i].StationName;

        //            entity.BIsReceivedValid = false; //不可用, 只是为了保存数据
        //            entity.LastTotalRain = m_listStations[i].LastTotalRain;
        //            entity.LastDayTotalRain = m_listStations[i].LastDayTotalRain;
        //            entity.LastClockSharpRain = m_listStations[i].LastClockSharpTotalRain;
        //            entity.LastClockSharpTime = m_listStations[i].LastClockSharpTime;
        //            entity.LastDayTime = m_listStations[i].LastDayTime;
        //            entity.LastWaterStage = m_listStations[i].LastWaterStage;

        //            entity.DDayRainFall = m_listStations[i].LastDayTotalRain;
        //            entity.DPeriodRain = m_listStations[i].LastPeriodRain;
        //            entity.DWaterYield = m_listStations[i].LastWaterStage;
        //            entity.DWaterFlowFindInTable = m_listStations[i].LastWaterFlow;
        //            // 计算数据状态
        //            entity.ERTDState = GetDataStatus(m_listStations[i].StationID, entity.DPeriodRain, entity.DWaterYield);
        //            if (m_listStations[i].LastDataTime.HasValue)
        //            {
        //                entity.TimeDeviceGained = m_listStations[i].LastDataTime.Value;
        //            }
        //            if (m_listStations[i].LastVoltage.HasValue)
        //            {
        //                entity.Dvoltage = m_listStations[i].LastVoltage.Value;
        //            }

        //            if (m_listStations[i].LastChannelType.HasValue)
        //            {
        //                entity.EIChannelType = m_listStations[i].LastChannelType.Value;
        //            }
        //            else
        //            {
        //                entity.EIChannelType = EChannelType.GPRS; //必须有才能写入
        //            }

        //            if (m_listStations[i].LastMessageType.HasValue)
        //            {
        //                entity.EIMessageType = m_listStations[i].LastMessageType.Value;
        //            }
        //            else
        //            {
        //                entity.EIMessageType = EMessageType.ETimed;
        //            }
        //            entity.EIStationType = m_listStations[i].StationType;
        //            m_mapStationRTD.Add(m_listStations[i].StationID, entity);
        //        }
        //    }
        //}

        /// <summary>
        /// 处理实时数据
        /// </summary>
        /// <param name="args"></param>
        private void FillRTDStationNotInMap()
        {
            // List<CEntityForm> forms = new List<CEntityForm>();
            for (int i = 0; i < m_listStations.Count; ++i)
            {
                // CEntityForm form = new CEntityForm();
                if (!m_mapStationRTD.ContainsKey(m_listStations[i].StationID))
                {
                    // 如果不包含，新建一个
                    CEntityRealTime entity = new CEntityRealTime();
                    entity.StrStationID = m_listStations[i].StationID;
                    entity.StrStationName = m_listStations[i].StationName;
                    entity.BIsReceivedValid = false; //不可用, 只是为了保存数据
                    entity.LastTotalRain = m_listStations[i].LastTotalRain;
                    entity.LastDayTotalRain = m_listStations[i].LastDayTotalRain;
                    entity.LastClockSharpRain = m_listStations[i].LastClockSharpTotalRain;
                    entity.LastClockSharpTime = m_listStations[i].LastClockSharpTime;
                    entity.LastDayTime = m_listStations[i].LastDayTime;
                    entity.LastWaterStage = m_listStations[i].LastWaterStage;
                    if (m_listStations[i].StationType == EStationType.ERainFall || m_listStations[i].StationType == EStationType.EHydrology)
                    {

                        if (m_listStations[i].LastDataTime.HasValue)
                        {
                            if (m_listStations[i].LastDayTotalRain.HasValue && m_listStations[i].LLastDayTotalRain.HasValue)
                            {
                                entity.LastDayRainFall = m_listStations[i].LastDayTotalRain.Value - m_listStations[i].LLastDayTotalRain.Value;
                                if (entity.LastDayRainFall < 0)
                                {
                                    if (m_listStations[i].DRainAccuracy != 0)
                                    {
                                        decimal tmpDiff = 10000 * (decimal)m_listStations[i].DRainAccuracy - m_listStations[i].LLastDayTotalRain.Value + m_listStations[i].LastDayTotalRain.Value;
                                        if (tmpDiff >= dayInterval)
                                        {
                                            entity.LastDayRainFall = 0;
                                        }
                                        else
                                        {
                                            entity.LastDayRainFall = tmpDiff;
                                        }
                                    }
                                    else
                                    {
                                        entity.LastDayRainFall = 0;
                                    }
                                }
                                else if (entity.LastDayRainFall >= dayInterval)
                                {
                                    entity.LastDayRainFall = 0;
                                    entity.ERTDState = ERTDDataState.EError;
                                }
                            }
                            DateTime tmp = new DateTime();
                            //DateTime tmp_1 = new DateTime();
                            if (m_listStations[i].LastDataTime.Value.Hour == 8 && m_listStations[i].LastDataTime.Value.Minute == 0 && m_listStations[i].LastDataTime.Value.Second == 0)
                            {
                                entity.DDayRainFall = 0;
                                if (m_listStations[i].LastClockSharpTime.HasValue)
                                {
                                    tmp = m_listStations[i].LastClockSharpTime.Value;
                                    tmp = tmp.Subtract(new TimeSpan(1, 0, 0));
                                    CEntityRain rain = m_proxyRain.getRainsForInit(m_listStations[i].StationID, tmp);
                                    if (rain != null && m_listStations[i].LastClockSharpTotalRain.HasValue)
                                    {
                                        entity.DPeriodRain = m_listStations[i].LastClockSharpTotalRain - rain.TotalRain;
                                        if (entity.DPeriodRain < 0)
                                        {
                                            if (m_listStations[i].DRainAccuracy != 0)
                                            {
                                                decimal tmpDiff = 10000 * (decimal)m_listStations[i].DRainAccuracy - (decimal)rain.TotalRain + m_listStations[i].LastClockSharpTotalRain.Value;
                                                if (tmpDiff >= hourInterval)
                                                {
                                                    entity.DPeriodRain = 0;
                                                }
                                                else
                                                {
                                                    entity.DPeriodRain = tmpDiff;
                                                }
                                            }
                                            else
                                            {
                                                entity.DPeriodRain = 0;
                                            }
                                        }
                                        else if (entity.DPeriodRain >= hourInterval)
                                        {
                                            entity.DPeriodRain = 0;
                                            entity.ERTDState = ERTDDataState.EError;
                                        }
                                    }
                                }
                            }
                            else if (m_listStations[i].LastDataTime.Value.Minute == 0 && m_listStations[i].LastDataTime.Value.Second == 0)
                            {
                                TimeSpan flag = new TimeSpan(24, 0, 0);
                                if (m_listStations[i].LastDayTime.HasValue && m_listStations[i].LastClockSharpTime.HasValue)
                                {
                                    if (m_listStations[i].LastClockSharpTime - m_listStations[i].LastDayTime <= flag)
                                    {
                                        if (m_listStations[i].LastDayTotalRain.HasValue && m_listStations[i].LastClockSharpTotalRain.HasValue)
                                        {
                                            entity.DDayRainFall = m_listStations[i].LastClockSharpTotalRain - m_listStations[i].LastDayTotalRain;
                                            if (entity.DDayRainFall < 0)
                                            {
                                                if (m_listStations[i].DRainAccuracy != 0)
                                                {
                                                    decimal tmpDiff = 10000 * (decimal)m_listStations[i].DRainAccuracy - m_listStations[i].LastDayTotalRain.Value + m_listStations[i].LastClockSharpTotalRain.Value;
                                                    if (tmpDiff >= dayInterval)
                                                    {
                                                        entity.DDayRainFall = 0;
                                                    }
                                                    else
                                                    {
                                                        entity.DDayRainFall = tmpDiff;
                                                    }
                                                }
                                                else
                                                {
                                                    entity.DDayRainFall = 0;
                                                }
                                            }
                                            else if (entity.DDayRainFall >= dayInterval)
                                            {
                                                entity.DDayRainFall = 0;
                                                entity.ERTDState = ERTDDataState.EError;
                                            }
                                        }

                                    }
                                    else
                                    {
                                        DateTime tmp_2 = m_listStations[i].LastClockSharpTime.Value;
                                        if (m_listStations[i].LastClockSharpTime.Value.Hour < 8)
                                        {

                                            tmp = new DateTime(tmp_2.Year, tmp_2.Month, tmp_2.Day, 8, 0, 0);
                                            tmp = tmp.Subtract(new TimeSpan(24, 0, 0));
                                        }
                                        if (m_listStations[i].LastClockSharpTime.Value.Hour >= 8)
                                        {
                                            tmp = new DateTime(tmp_2.Year, tmp_2.Month, tmp_2.Day, 8, 0, 0);
                                        }
                                        CEntityRain rain = m_proxyRain.getRainsForInit(m_listStations[i].StationID, tmp);
                                        if (rain != null && m_listStations[i].LastClockSharpTotalRain.HasValue)
                                        {
                                            entity.DDayRainFall = m_listStations[i].LastClockSharpTotalRain - rain.TotalRain;

                                        }
                                        //entity.DDayRainFall = m_listStations[i].LastClockSharpTotalRain - rain.TotalRain;
                                    }

                                }
                                if (m_listStations[i].LastClockSharpTime.HasValue)
                                {
                                    tmp = m_listStations[i].LastClockSharpTime.Value;
                                    tmp = tmp.Subtract(new TimeSpan(1, 0, 0));
                                    CEntityRain rain = m_proxyRain.getRainsForInit(m_listStations[i].StationID, tmp);
                                    if (rain != null && m_listStations[i].LastClockSharpTotalRain.HasValue)
                                    {
                                        entity.DPeriodRain = m_listStations[i].LastClockSharpTotalRain - rain.TotalRain;

                                    }
                                }
                            }
                            else
                            {
                                TimeSpan flag = new TimeSpan(24, 0, 0);
                                if (m_listStations[i].LastDayTime.HasValue && m_listStations[i].LastDataTime.HasValue)
                                {
                                    if (m_listStations[i].LastDataTime - m_listStations[i].LastDayTime <= flag)
                                    {
                                        if (m_listStations[i].LastDayTotalRain.HasValue && m_listStations[i].LastTotalRain.HasValue)
                                        {
                                            entity.DDayRainFall = m_listStations[i].LastTotalRain - m_listStations[i].LastDayTotalRain;

                                        }
                                    }
                                    else
                                    {
                                        DateTime tmp_2 = m_listStations[i].LastDataTime.Value;
                                        if (m_listStations[i].LastDataTime.Value.Hour < 8)
                                        {

                                            tmp = new DateTime(tmp_2.Year, tmp_2.Month, tmp_2.Day, 8, 0, 0);
                                            tmp = tmp.Subtract(new TimeSpan(24, 0, 0));
                                        }
                                        if (m_listStations[i].LastDataTime.Value.Hour >= 8)
                                        {
                                            tmp = new DateTime(tmp_2.Year, tmp_2.Month, tmp_2.Day, 8, 0, 0);
                                        }
                                        CEntityRain rain = m_proxyRain.getRainsForInit(m_listStations[i].StationID, tmp);
                                        if (rain != null && m_listStations[i].LastTotalRain.HasValue)
                                        {
                                            entity.DDayRainFall = m_listStations[i].LastTotalRain - rain.TotalRain;

                                        }

                                    }

                                }
                                TimeSpan flag_1 = new TimeSpan(1, 0, 0);
                                if (m_listStations[i].LastClockSharpTime.HasValue && m_listStations[i].LastDataTime.HasValue)
                                {
                                    if (m_listStations[i].LastDataTime - m_listStations[i].LastClockSharpTime <= flag_1)
                                    {
                                        if (m_listStations[i].LastTotalRain.HasValue && m_listStations[i].LastClockSharpTotalRain.HasValue)
                                        {
                                            entity.DPeriodRain = m_listStations[i].LastTotalRain - m_listStations[i].LastClockSharpTotalRain;

                                        }
                                    }
                                    else
                                    {
                                        DateTime tmp_3 = m_listStations[i].LastDataTime.Value;
                                        tmp = new DateTime(tmp_3.Year, tmp_3.Month, tmp_3.Day, tmp_3.Hour, 0, 0);
                                        CEntityRain rain = m_proxyRain.getRainsForInit(m_listStations[i].StationID, tmp);
                                        if (rain != null && m_listStations[i].LastTotalRain.HasValue)
                                        {
                                            entity.DPeriodRain = m_listStations[i].LastTotalRain - rain.TotalRain;

                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (m_listStations[i].StationType == EStationType.ERiverWater || m_listStations[i].StationType == EStationType.EHydrology)
                    {
                        entity.DWaterYield = m_listStations[i].LastWaterStage;
                        entity.DWaterFlowFindInTable = m_listStations[i].LastWaterFlow;
                    }
                    // 计算数据状态
                    entity.ERTDState = GetDataStatus(m_listStations[i].StationID, entity.DPeriodRain, entity.DWaterYield);
                    if (entity.DDayRainFall < 0)
                    {
                        entity.DDayRainFall = 0;
                    }
                    if (entity.DPeriodRain < 0)
                    {
                        entity.DPeriodRain = 0;
                    }
                    if (m_listStations[i].LastDataTime.HasValue)
                    {
                        entity.TimeDeviceGained = m_listStations[i].LastDataTime.Value;
                    }
                    if (m_listStations[i].LastVoltage.HasValue)
                    {
                        entity.Dvoltage = m_listStations[i].LastVoltage.Value;
                    }

                    if (m_listStations[i].LastChannelType.HasValue)
                    {
                        entity.EIChannelType = m_listStations[i].LastChannelType.Value;
                    }
                    else
                    {
                        entity.EIChannelType = EChannelType.GPRS; //必须有才能写入
                    }

                    if (m_listStations[i].LastMessageType.HasValue)
                    {
                        entity.EIMessageType = m_listStations[i].LastMessageType.Value;
                    }
                    else
                    {
                        entity.EIMessageType = EMessageType.ETimed;
                    }
                    entity.EIStationType = m_listStations[i].StationType;
                    m_mapStationRTD.Add(m_listStations[i].StationID, entity);
                    //form.StationID = entity.StrStationID;
                    //form.PeriodRain = entity.DPeriodRain;
                    //form.DayRain = entity.DDayRainFall;
                }
                // forms.Add(form);
            }//END FOR
             // m_proxyRain.AddNewRows(rains);
        }
        private void FillRTDStationNotInMap_1(Dictionary<string, CEntityRealTime> EntityBefore)
        {
            // List<CEntityForm> forms = new List<CEntityForm>();
            for (int i = 0; i < m_listStations.Count; ++i)
            {
                // CEntityForm form = new CEntityForm();
                if (!m_mapStationRTD.ContainsKey(m_listStations[i].StationID))
                {
                    // 如果不包含，新建一个
                    CEntityRealTime entity = new CEntityRealTime();
                    entity.StrStationID = m_listStations[i].StationID;
                    entity.StrStationName = m_listStations[i].StationName;
                    entity.BIsReceivedValid = false; //不可用, 只是为了保存数据
                    entity.LastTotalRain = m_listStations[i].LastTotalRain;
                    entity.LastDayTotalRain = m_listStations[i].LastDayTotalRain;
                    entity.LastClockSharpRain = m_listStations[i].LastClockSharpTotalRain;
                    entity.LastClockSharpTime = m_listStations[i].LastClockSharpTime;
                    entity.LastDayTime = m_listStations[i].LastDayTime;
                    entity.LastWaterStage = m_listStations[i].LastWaterStage;
                    if (m_listStations[i].StationType == EStationType.ERainFall || m_listStations[i].StationType == EStationType.EHydrology)
                    {
                        if (EntityBefore.ContainsKey(m_listStations[i].StationID))
                        {
                            entity.DDayRainFall = EntityBefore[m_listStations[i].StationID].DDayRainFall;
                            entity.DPeriodRain = EntityBefore[m_listStations[i].StationID].DPeriodRain;
                            // entity.TimeReceived = EntityBefore[m_listStations[i].StationID].TimeReceived;
                        }
                        else
                        {
                            entity.DDayRainFall = null;
                            entity.DPeriodRain = null;
                        }
                    }
                    if (m_listStations[i].StationType == EStationType.ERiverWater || m_listStations[i].StationType == EStationType.EHydrology)
                    {
                        entity.DWaterYield = m_listStations[i].LastWaterStage;
                        entity.DWaterFlowFindInTable = m_listStations[i].LastWaterFlow;
                    }
                    // 计算数据状态
                    entity.ERTDState = GetDataStatus(m_listStations[i].StationID, entity.DPeriodRain, entity.DWaterYield);
                    if (EntityBefore.ContainsKey(m_listStations[i].StationID))
                    {
                        entity.TimeDeviceGained = EntityBefore[m_listStations[i].StationID].TimeDeviceGained;
                    }
                    else
                    {
                        entity.TimeDeviceGained = DateTime.Now;
                    }

                    if (EntityBefore.ContainsKey(m_listStations[i].StationID))
                    {
                        entity.Dvoltage = EntityBefore[m_listStations[i].StationID].Dvoltage;
                    }
                    else
                    {
                        entity.Dvoltage = 0; ;
                    }

                    if (EntityBefore.ContainsKey(m_listStations[i].StationID))
                    {
                        entity.EIChannelType = EntityBefore[m_listStations[i].StationID].EIChannelType;
                    }
                    else
                    {
                        // entity.EIChannelType = EChannelType.GPRS; //必须有才能写入
                        if (m_listStations[i].LastChannelType.HasValue)
                        {
                            entity.EIChannelType = m_listStations[i].LastChannelType.Value;
                        }
                        else
                        {
                            entity.EIChannelType = EChannelType.GPRS;
                        }
                    }

                    if (EntityBefore.ContainsKey(m_listStations[i].StationID))
                    {
                        entity.EIMessageType = EntityBefore[m_listStations[i].StationID].EIMessageType;
                    }
                    else
                    {
                        if (m_listStations[i].LastMessageType.HasValue)
                        {
                            entity.EIMessageType = m_listStations[i].LastMessageType.Value;
                        }
                        else
                        {
                            entity.EIMessageType = EMessageType.ETimed;
                        }
                    }
                    entity.EIStationType = m_listStations[i].StationType;
                    m_mapStationRTD.Add(m_listStations[i].StationID, entity);
                    //form.StationID = entity.StrStationID;
                    //form.PeriodRain = entity.DPeriodRain;
                    //form.DayRain = entity.DDayRainFall;
                }
                // forms.Add(form);
            }//END FOR
             // m_proxyRain.AddNewRows(rains);
        }
        // gm 0331
        private void DealRTDTSDatas(CEventRecvStationDatasArgs args)
        {
            int tmpDataCount = args.Datas.Count;
            if (tmpDataCount <= 0)
            {
                // 数据为空
                CSystemInfoMgr.Instance.AddInfo("收到空的数据记录项目");
                return;
            }
            CEntityStation station = GetStationById(args.StrStationID);
            //station
            if (null == station)
            {
                Debug.WriteLine("站点配置不正确，数据库没有站点{0}的配置", args.StrStationID);
                return;
            }
            #region 雨量表
            if (args.EStationType == EStationType.EHydrology || args.EStationType == EStationType.ERainFall)
            {
                List<CEntityTSRain> rains = new List<CEntityTSRain>();
                foreach (CSingleStationData data in args.Datas)
                {
                    // 是否和上一条时间一致, 就丢失当条数据
                    if (m_mapStationRTD[station.StationID].TimeDeviceGained == data.DataTime)
                    {
                        Debug.WriteLine("drop");
                        continue;
                    }
                    CEntityTSRain rain = new CEntityTSRain();
                    rain.StationID = args.StrStationID;
                    rain.TimeCollect = data.DataTime;
                    rain.TimeRecieved = args.RecvDataTime;
                    rain.TotalRain = data.TotalRain * (Decimal)station.DRainAccuracy;
                    rain.MessageType = args.EMessageType;
                    rain.ChannelType = args.EChannelType;
                    rains.Add(rain);

                }
                m_proxyTSRain.AddNewRows(rains);//添加数据库
                //添加代码
            }
            #endregion 雨量表

            #region 水位表
            if (args.EStationType == EStationType.EHydrology || args.EStationType == EStationType.ERiverWater)
            {
                List<CEntityTSWater> listWater = new List<CEntityTSWater>();
                foreach (CSingleStationData data in args.Datas)
                {
                    // 是否和上一条时间一致, 就丢失当条数据
                    if (m_mapStationRTD[station.StationID].TimeDeviceGained == data.DataTime)
                    {
                        Debug.WriteLine("drop");
                        continue;
                    }
                    CEntityTSWater water = new CEntityTSWater();
                    water.StationID = station.StationID;
                    water.TimeCollect = data.DataTime;
                    water.TimeRecieved = args.RecvDataTime;
                    if (station.DWaterBase.HasValue)
                    {
                        // 减去水位基值
                        // water.WaterStage = data.WaterStage.Value - station.DWaterBase.Value;
                        //1105gm
                        water.WaterStage = data.WaterStage.Value + station.DWaterBase.Value;
                    }
                    else
                    {
                        water.WaterStage = data.WaterStage.Value;
                    }
                    water.ChannelType = args.EChannelType;
                    water.MessageType = args.EMessageType;
                    listWater.Add(water);
                }
                //插入数据库
                //daima
                m_proxyTSWater.AddNewRows(listWater);

            }

            #endregion 水位表

            #region 电压表
            List<CEntityTSVoltage> listVoltages = new List<CEntityTSVoltage>();
            foreach (CSingleStationData data in args.Datas)
            {
                // 是否和上一条时间一致, 就丢失当条数据
                if (m_mapStationRTD[station.StationID].TimeDeviceGained == data.DataTime)
                {
                    Debug.WriteLine("drop");
                    continue;
                }
                CEntityTSVoltage voltage = new CEntityTSVoltage();
                voltage.StationID = station.StationID;
                voltage.TimeCollect = data.DataTime;
                voltage.TimeRecieved = args.RecvDataTime;
                voltage.Voltage = (Decimal)data.Voltage;
                voltage.ChannelType = args.EChannelType;
                voltage.MessageType = args.EMessageType;
                if (voltage.Voltage > 0)
                {
                    listVoltages.Add(voltage);
                }

            }
            // 加入数据库
            //代码

            m_proxyTSVoltage.AddNewRows(listVoltages);
            #endregion 电压表
        }

        private void DealEvaDatas(CEventRecvStationDatasArgs args)
        {
            try
            {
                Dictionary<string, string> cDic = new Dictionary<string, string>();
                int tmpDataCount = args.Datas.Count;
                if (tmpDataCount <= 0)
                {
                    // 数据为空
                    CSystemInfoMgr.Instance.AddInfo("收到空的数据记录项目");
                    return;
                }
                // 生成实时数据CEntityRTD
                CEntityStation station = GetStationById(args.StrStationID);
                //station
                if (null == station)
                {
                    Debug.WriteLine("站点配置不正确，数据库没有站点{0}的配置", args.StrStationID);
                    return;
                }
                #region 蒸发表
                List<CEntityEva> HEvas = new List<CEntityEva>();
                List<CEntityEva> DEvas = new List<CEntityEva>();
                
                foreach (CSingleStationData data in args.Datas)
                {
                    // 是否和上一条时间一致, 就丢失当条数据
                 
                    if (m_mapStationRTD[station.StationID].TimeDeviceGained == data.DataTime)
                    {
                        Debug.WriteLine("drop");
                        continue;
                    }

                    //将接收时间更新为最新时间
                    m_mapStationRTD[station.StationID].TimeDeviceGained = data.DataTime;
                    

                    if (data.Eva == null || data.Temp == null)
                    {
                        continue;
                    }

                    CEntityEva Eva = new CEntityEva();
                    Eva.StationID = args.StrStationID;
                    Eva.Eva = data.Eva;
                    Eva.TE = data.Eva;
                    Eva.TimeCollect = data.DataTime;
                    Eva.Temperature = data.Temp;
                    Eva.Rain = data.TotalRain;
                    Eva.TP = data.TotalRain;
                    Eva.Voltage = data.Voltage;
                    Eva.type = data.EvpType;
                    Eva.kp = station.DWaterMin;
                    Eva.ke = station.DWaterMax;
                    Eva.dh = station.DWaterChange;
                    Eva.maxE = station.DRainChange;
                    Eva.comP = 0;

                    CEntityEva DEva = new CEntityEva();
                    DEva.StationID = args.StrStationID;
                    DEva.Eva = data.Eva;
                    DEva.TE = data.Eva;
                    DEva.TimeCollect = data.DataTime;
                    DEva.Temperature = data.Temp;
                    DEva.Rain = data.TotalRain;
                    DEva.TP = data.TotalRain;
                    DEva.Voltage = data.Voltage;
                    DEva.type = data.EvpType;

                    //TimeSpan ts1 = new TimeSpan(DateTime.Now.Ticks);
                    //TimeSpan ts2 = new TimeSpan(timeFlag.Ticks);
                    //TimeSpan ts = ts1.Subtract(ts2).Duration();
                    //int seconds = ts.Seconds;
                    //if(seconds > 5 || seconds < -5)
                    //{
                    //    Thread.Sleep(5 * 1000);
                    //    CSystemInfoMgr.Instance.AddInfo("调用函数开始前休眠5S！");
                    //}
                    CSystemInfoMgr.Instance.AddInfo("调用函数开始！");
                    if(System.Math.Abs((DateTime.Now.Subtract(calFlag)).Seconds) < 1)
                    {
                        CSystemInfoMgr.Instance.AddInfo("延迟1S调用！");
                        Thread.Sleep(1 * 1000);
                    }
                    cDic = cal.EvaCal(Eva);
                    calFlag = DateTime.Now;
                    if (cDic.Count == 0)
                    {
                        continue;
                    }
                    if (cDic.ContainsKey("hourE"))
                    {

                        if (cDic["hourE"] != "")
                        {
                            Eva.Eva = decimal.Parse(cDic["hourE"]);
                            Eva.Rain = decimal.Parse(cDic["hourP"]);
                            Eva.Temperature = decimal.Parse(cDic["hourT"]);
                            Eva.Voltage = decimal.Parse(cDic["hourU"]);
                            Eva.DH = decimal.Parse(cDic["dH"]);
                            Eva.hourEChange = decimal.Parse(cDic["hourEChange"]);
                            m_mapStationEva[args.StrStationID] = m_mapStationEva[args.StrStationID] + decimal.Parse(cDic["hourE"]);
                            m_mapStationRain[args.StrStationID] = m_mapStationRain[args.StrStationID] + decimal.Parse(cDic["hourP"]);
                            HEvas.Add(Eva);
                        }
                    }
                    if (cDic.ContainsKey("dayE"))
                    {
                        if (cDic["dayE"] != "")
                        {
                            DEva.Eva = decimal.Parse(cDic["dayE"]);
                            DEva.Rain = decimal.Parse(cDic["dayP"]);
                            DEva.Temperature = decimal.Parse(cDic["dayT"]);
                            DEva.P8 = decimal.Parse(cDic["P8"]);
                            DEva.P20 = decimal.Parse(cDic["P20"]);
                            DEva.dayEChange = decimal.Parse(cDic["dayEChange"]);
                            m_mapStationEva[args.StrStationID] = 0;
                            m_mapStationRain[args.StrStationID] = 0;
                            lastDayEva = decimal.Parse(cDic["dayE"]);
                            lastDayRain = decimal.Parse(cDic["dayP"]);
                            station.LastDayEva = decimal.Parse(cDic["dayE"]);
                            station.LastDayRain = decimal.Parse(cDic["dayP"]); 

                            DEvas.Add(DEva);
                        }
                    }
                }
                if (HEvas.Count > 0)
                {
                    m_proxyHEva.AddNewRows(HEvas);
                }
                if (DEvas.Count > 0)
                {
                    m_proxyDEva.AddNewRows(DEvas);
                }
                #endregion

                #region 实时蒸发表
                if(args.Datas[tmpDataCount - 1].EvpType != null 
                    && args.Datas[tmpDataCount - 1].EvpType != ""
                    && args.Datas[tmpDataCount - 1].EvpType.ToString().Length >= 2)
                {
                    station.act = args.Datas[tmpDataCount - 1].EvpType;
                }
                CEntityRealEva realtime = new CEntityRealEva();
                if (!cDic.ContainsKey("hourE"))
                {
                    realtime.StrStationID = station.StationID;
                    realtime.StationType = station.StationType;
                    realtime.StrStationName = station.StationName;
                   
                    //所有数据为空
                    realtime.LastDayRain = null;
                    realtime.LastDayEva = null;
                    realtime.DayRain = null;
                    realtime.DayEva = null;
                    realtime.Eva = null;
                    realtime.Rain = null;
                    if(args.Datas[tmpDataCount - 1].Temp.HasValue)
                    {
                        realtime.Temperature = args.Datas[tmpDataCount - 1].Temp.Value;
                    }
                    else
                    {
                        realtime.Temperature = null;
                    }
                    
                    realtime.Voltage =null;
                    realtime.DH =null;

                    realtime.TimeReceived = args.RecvDataTime;
                    realtime.TimeDeviceGained = args.Datas[tmpDataCount - 1].DataTime; //采集时间
                    realtime.RawEva = args.Datas[tmpDataCount - 1].Eva;
                    realtime.RawRain = args.Datas[tmpDataCount - 1].TotalRain;
                    realtime.RawVoltage = args.Datas[tmpDataCount - 1].Voltage;

                    //realtime.LastDayRain = lastDayRain;
                    //realtime.LastDayEva = lastDayEva;
                    realtime.LastDayRain = station.LastDayRain;
                    realtime.LastDayEva = station.LastDayEva;
                    realtime.DayRain = m_mapStationRain[args.StrStationID];
                    realtime.DayEva = m_mapStationEva[args.StrStationID];
                    //if(args.Datas[tmpDataCount - 1].EvpType != null && (args.Datas[tmpDataCount - 1].EvpType.ToString().Length >= 2)){
                    //    realtime.act = args.Datas[tmpDataCount - 1].EvpType;
                    //}
                    realtime.act = station.act;
                    realtime.evaPZ = "-";
                    //realtime.act = args.Datas[tmpDataCount - 1].EvpType;

                    // 发消息，通知界面更新
                    if (RecvedRTD_Eva != null)
                    {
                        Task.Factory.StartNew(() => { RecvedRTD_Eva.Invoke(this, new CEventSingleArgs<CEntityRealEva>(realtime)); });
                    }

                    m_mapStationRTS[station.StationID] = realtime;

                    //m_proxyRealEva.AddNewRow(realtime);
                }
                else
                {
                    realtime.StrStationID = station.StationID;
                    realtime.StationType = station.StationType;
                    realtime.StrStationName = station.StationName;
                    //TODO
                    //realtime.LastDayRain = lastDayRain;
                    //realtime.LastDayEva = lastDayEva;
                    realtime.LastDayRain = station.LastDayRain;
                    realtime.LastDayEva = station.LastDayEva;
                    realtime.DayRain = m_mapStationRain[args.StrStationID]; 
                    realtime.DayEva = m_mapStationEva[args.StrStationID];
                    //realtime.EIChannelType = args.EChannelType;
                    realtime.Eva = Decimal.Parse(cDic["hourE"]);
                    realtime.Rain = Decimal.Parse(cDic["hourP"]);
                    realtime.Temperature = Decimal.Parse(cDic["hourT"]);
                    realtime.Voltage = Decimal.Parse(cDic["hourU"]);
                    realtime.DH = decimal.Parse(cDic["dH"]);
                    realtime.TimeReceived = args.RecvDataTime;
                    realtime.TimeDeviceGained = args.Datas[tmpDataCount - 1].DataTime; //采集时间
                    realtime.RawEva = args.Datas[tmpDataCount - 1].Eva;
                    realtime.RawRain = args.Datas[tmpDataCount - 1].TotalRain;
                    realtime.RawVoltage = args.Datas[tmpDataCount - 1].Voltage;
                    //realtime.act = args.Datas[tmpDataCount - 1].EvpType;
                    realtime.act = station.act;
                    if (cDic.ContainsKey("dayEChange") && (cDic["dayEChange"] != ""))
                    {
                        realtime.evaPZ = cDic["dayEChange"];
                    }
                    else
                    {
                        realtime.evaPZ = "-";
                    }
                   
                    // 发消息，通知界面更新
                    if (RecvedRTD_Eva != null)
                    {
                        Task.Factory.StartNew(() => { RecvedRTD_Eva.Invoke(this, new CEventSingleArgs<CEntityRealEva>(realtime)); });
                    }

                    m_mapStationRTS[station.StationID] = realtime;

                    m_proxyRealEva.AddNewRow(realtime);

                    //将今日，昨日，前日数据发送到设备TODO
                    //1.查询前日和昨日的蒸发和降雨
                    try
                    {
                        List<CEntityEva> evaList = new List<CEntityEva>();
                        string aaaa = "0000";
                        string bbbb = "0000";
                        string cccc = "0000";
                        string ddd = "000";
                        string eee = "000";
                        string fff = "000";
                        if (realtime.TimeDeviceGained.Hour == 8)
                        {
                            DateTime strtTime = new DateTime(realtime.TimeDeviceGained.AddDays(-1).Year, realtime.TimeDeviceGained.AddDays(-1).Month, realtime.TimeDeviceGained.AddDays(-1).Day, 7, 0, 0);
                            DateTime endTime = new DateTime(realtime.TimeDeviceGained.Year, realtime.TimeDeviceGained.Month, realtime.TimeDeviceGained.Day, 7, 0, 0);
                            evaList = getEvaByTime(realtime.StrStationID, strtTime, endTime);
                            if (evaList != null && evaList.Count == 1)
                            {
                                aaaa = ((Math.Round(evaList[0].P.Value, 1)) * 10).ToString("0000");
                                ddd = ((Math.Round(evaList[0].E.Value, 1)) * 10).ToString("000");
                                bbbb = (Math.Round(realtime.LastDayRain.Value, 1) * 10).ToString("0000");
                                eee = (Math.Round(realtime.LastDayEva.Value, 1) * 10).ToString("000");
                                cccc = "0000";
                                fff = "000";
                            }
                        }
                        else if (DateTime.Now.Hour > 8)
                        {
                            DateTime strtTime = new DateTime(realtime.TimeDeviceGained.AddDays(-1).Year, realtime.TimeDeviceGained.AddDays(-1).Month, realtime.TimeDeviceGained.AddDays(-1).Day, 7, 0, 0);
                            DateTime endTime = new DateTime(realtime.TimeDeviceGained.Year, realtime.TimeDeviceGained.Month, realtime.TimeDeviceGained.Day, 9, 0, 0);
                            evaList = getEvaByTime(realtime.StrStationID, strtTime, endTime);
                            if (evaList != null && evaList.Count == 2)
                            {
                                //decimal a = Math.Round(evaList[0].Rain.Value, 1);
                                aaaa = ((Math.Round(evaList[0].P.Value, 1)) * 10).ToString("0000");
                                ddd = ((Math.Round(evaList[0].E.Value, 1)) * 10).ToString("000");
                                bbbb = ((Math.Round(evaList[1].P.Value, 1)) * 10).ToString("0000");
                                eee = ((Math.Round(evaList[1].E.Value, 1)) * 10).ToString("000");
                                if (realtime.DayRain.HasValue && realtime.DayEva.HasValue)
                                {
                                    cccc = (Math.Round(realtime.DayRain.Value, 1) * 10).ToString("0000");
                                    fff = (Math.Round(realtime.DayEva.Value, 1) * 10).ToString("000");
                                }
                                else
                                {
                                    cccc = "0000";
                                    fff = "000";
                                }
                            }
                        }
                        else if (DateTime.Now.Hour < 8)
                        {
                            DateTime strtTime = new DateTime(realtime.TimeDeviceGained.AddDays(-2).Year, realtime.TimeDeviceGained.AddDays(-2).Month, realtime.TimeDeviceGained.AddDays(-2).Day, 7, 0, 0);
                            DateTime endTime = new DateTime(realtime.TimeDeviceGained.AddDays(-1).Year, realtime.TimeDeviceGained.AddDays(-1).Month, realtime.TimeDeviceGained.AddDays(-1).Day, 7, 0, 0);
                            evaList = getEvaByTime(realtime.StrStationID, strtTime, endTime);
                            if (evaList != null && evaList.Count == 2)
                            {
                                aaaa = ((Math.Round(evaList[0].P.Value, 1)) * 10).ToString("0000");
                                ddd = ((Math.Round(evaList[0].E.Value, 1)) * 10).ToString("000");
                                bbbb = ((Math.Round(evaList[1].P.Value, 1)) * 10).ToString("0000");
                                eee = ((Math.Round(evaList[1].E.Value, 1)) * 10).ToString("000");
                                if (realtime.DayRain.HasValue && realtime.DayEva.HasValue)
                                {
                                    cccc = (Math.Round(realtime.DayRain.Value, 1) * 10).ToString("0000");
                                    fff = (Math.Round(realtime.DayEva.Value, 1) * 10).ToString("000");
                                }
                                else
                                {
                                    cccc = "0000";
                                    fff = "000";
                                }
                            }
                        }
                        //at+cpbw=4,”aaaabbbbccccdddeeefff”，129,”nn”
                        string day = realtime.TimeDeviceGained.Day.ToString("00");
                        StringBuilder sb = new StringBuilder();
                        sb.Append("at+");//AT指令头
                        sb.Append("cpbw=4,\"");
                        sb.Append(aaaa + bbbb + cccc + ddd + eee + fff);
                        sb.Append("\",129,");
                        sb.Append("\"");
                        sb.Append(day);
                        sb.Append("\"");
                        sb.Append("\r\n");
                        CSystemInfoMgr.Instance.AddInfo(sb.ToString());
                        CPortDataMgr.Instance.SendHDMsg(station.GPRS, station.StationID, sb.ToString(), EChannelType.GPRS);
                    }
                    catch(Exception e) { }
                    
                }
                

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        

        private void DealRTDDatas(CEventRecvStationDatasArgs args)
        {
            try
            {

                Nullable<Decimal> tmpPeriodRain = null;
                Nullable<Decimal> tmpDayRain = null;
                Nullable<Decimal> tmpDifferenceRain = null;
                Nullable<Decimal> tmpWaterFlow = null;
                ERTDDataState tmpRTDRainDataState = ERTDDataState.ENormal;
                ERTDDataState tmpRTDWaterDataState = ERTDDataState.ENormal;
                ERTDDataState tmpRTDVoltageDataState = ERTDDataState.ENormal;
                int tmpDataCount = args.Datas.Count;
                if (tmpDataCount <= 0)
                {
                    // 数据为空
                    CSystemInfoMgr.Instance.AddInfo("收到空的数据记录项目");
                    return;
                }
                // 生成实时数据CEntityRTD
                CEntityStation station = GetStationById(args.StrStationID);
                //station
                if (null == station)
                {
                    Debug.WriteLine("站点配置不正确，数据库没有站点{0}的配置", args.StrStationID);
                    return;
                }
                // 更新水量表，雨量表以及电压表
                #region 雨量表
                if (args.EStationType == EStationType.EHydrology || args.EStationType == EStationType.ERainFall || args.EStationType == EStationType.EReservoir)
                {
                    List<CEntityRain> rains = new List<CEntityRain>();
                    foreach (CSingleStationData data in args.Datas)
                    {
                        // 是否和上一条时间一致, 就丢失当条数据
                        if (m_mapStationRTD[station.StationID].TimeDeviceGained == data.DataTime)
                        {
                            Debug.WriteLine("drop");
                            continue;
                        }
                        //station.LastDayTime= data.DataTime

                        if (args.Datas.Count != 1)
                        {
                            m_mapStationRTD[station.StationID].TimeDeviceGained = data.DataTime;
                        }
                        if (data.TotalRain < 0)
                        {
                            continue;
                        }
                        int year = data.DataTime.Year;
                        int month = data.DataTime.Month;
                        int day = data.DataTime.Day;
                        CEntityRain rain = new CEntityRain();
                        rain.BState = 1;
                        int status = 1;

                        int hour = data.DataTime.Hour;
                        int minute = data.DataTime.Minute;
                        int second = data.DataTime.Second;

                        // 中游局定时报处理
                        if (data.DataTime.Minute == 05)
                        {
                            station.LastTotalRain = station.LastClockSharpTotalRain;
                        }

                        if (data.TotalRain.HasValue)
                        {
                            CalDifferenceRain(station.DRainAccuracy, data.TotalRain.Value, data.DataTime, station.LastTotalRain, station.DRainChange, ref status, ref tmpDifferenceRain);
                            station.LastTotalRain = data.TotalRain.Value * (decimal)station.DRainAccuracy;
                        }
                        if (status == 0)
                        {
                            rain.BState = 0;
                        }

                        station.LastDataTime = data.DataTime;
                        if (data.TotalRain.ToString() != "")
                        {
                            if (hour == 8 && minute == 0 && second == 0)
                            {
                                DateTime tmp = new DateTime(year, month, day, 8, 0, 0);
                                DateTime tmp_1 = tmp.Subtract(new TimeSpan(24, 0, 0));
                                //station.LastDayTime = tmp.Subtract(new TimeSpan(24, 0, 0));
                                //station.LastDayTotalRain = m_proxyRain.GetLastDayTotalRain(station.StationID, DateTime.Parse(station.LastDayTime.ToString()));
                                if (data.TotalRain.HasValue)
                                {
                                    CalDayRain(station.StationID, station.DRainAccuracy, data.TotalRain.Value, data.DataTime, station.LastDayTime, station.LastDayTotalRain, tmp_1, ref tmpDayRain, ref status);
                                }
                                //更新
                                if (status == 2 && rain.BState != 0)
                                {
                                    rain.BState = 2;
                                }
                                station.LLastDayTotalRain = station.LastDayTotalRain;
                                station.LastDayTotalRain = data.TotalRain.Value * (decimal)station.DRainAccuracy;
                                station.LastDayTime = tmp;
                            }
                            if ((minute + second) == 0)
                            {
                                //station.LastClockSharpTotalRain = m_proxyRain.GetLastClockSharpTotalRain(station.StationID, data.DataTime);
                                if (data.TotalRain.HasValue)
                                {
                                    CalPeriodRain(station.StationID, station.DRainAccuracy, data.TotalRain.Value, data.DataTime, station.LastDataTime, station.LastClockSharpTotalRain, station.LastClockSharpTime, ref tmpPeriodRain, ref status);
                                }
                                if (status == 2 && rain.BState != 0)
                                {
                                    rain.BState = 2;
                                }
                                station.LastClockSharpTotalRain = data.TotalRain.Value * (decimal)station.DRainAccuracy;
                                station.LastClockSharpTime = data.DataTime;
                            }

                        }
                        rain.StationID = station.StationID;
                        rain.TimeCollect = data.DataTime;
                        rain.TimeRecieved = args.RecvDataTime;
                        rain.PeriodRain = data.DataTime.Minute == 0 ? tmpPeriodRain : null;
                        rain.DayRain = tmpDayRain;
                        rain.DifferneceRain = tmpDifferenceRain;
                        rain.TotalRain = data.TotalRain * (Decimal)station.DRainAccuracy;
                        rain.MessageType = args.EMessageType;
                        rain.ChannelType = EChannelType.None;
                        AssertAndAdjustRainData(rain, ref tmpRTDRainDataState);
                        //if (status == 1)
                        //{
                        //    rain.BState = 1;
                        //}else
                        //{
                        //   rain.BState = 0;
                        // }
                        //rain.BState = 1;
                        rain.DifferneceRain = rain.DifferneceRain.HasValue ? (rain.DifferneceRain < 0 ? 0 : rain.DifferneceRain) : null;
                        rain.TotalRain = rain.TotalRain.HasValue ? (rain.TotalRain < 0 ? 0 : rain.TotalRain) : null;
                        rain.DayRain = rain.DayRain.HasValue ? (rain.DayRain < 0 ? 0 : rain.DayRain) : null;
                        rain.PeriodRain = rain.PeriodRain.HasValue ? (rain.PeriodRain < 0 ? 0 : rain.PeriodRain) : null;
                        rains.Add(rain);
                        // 更新站点信息，便于下次计算日雨量和时段雨量
                        station.LastTotalRain = rain.TotalRain;
                        // 如果时间设置的误差范围内
                        int offset = (data.DataTime.Hour - 8) * 60 + data.DataTime.Minute;

                        //if (Math.Abs(offset) <= m_iMinutesRange)
                        //{
                        //    station.LastDayTime = data.DataTime;
                        //    station.LastDayTotalRain = rain.TotalRain;
                        //}
                    }
                    m_proxyRain.AddNewRows(rains); //写入数据库
                                                   //NewTask(() => { foreach (CEntityRain rain in rains) { AssertRainData(rain); } });
                                                   //foreach (CEntityRain rain in rains) { AssertRainData(rain, ref tmpRTDDataState); }
                                                   //Task task = new Task(() => { foreach (CEntityRain rain in rains) { AssertRainData(rain); } });
                                                   //task.Start();

                }
                #endregion 雨量表

                #region 水位表
                if (args.EStationType == EStationType.EHydrology || args.EStationType == EStationType.ERiverWater || args.EStationType == EStationType.EReservoir)
                {
                    List<CEntityWater> listWater = new List<CEntityWater>();
                    foreach (CSingleStationData data in args.Datas)
                    {
                        // 是否和上一条时间一致, 就丢失当条数据
                        if (m_mapStationRTD[station.StationID].TimeDeviceGained == data.DataTime)
                        {
                            Debug.WriteLine("drop");
                            continue;
                        }
                        if (args.Datas.Count != 1)
                        {
                            m_mapStationRTD[station.StationID].TimeDeviceGained = data.DataTime;
                        }
                        if (data.WaterStage == -200 || data.WaterStage == -20000)
                        {
                            continue;
                        }
                        CEntityWater water = new CEntityWater();
                        water.StationID = station.StationID;
                        water.TimeCollect = data.DataTime;
                        water.TimeRecieved = args.RecvDataTime;
                        if (station.DWaterBase.HasValue)
                        {
                            // 减去水位基值
                            // water.WaterStage = data.WaterStage.Value - station.DWaterBase.Value;
                            //1105gm
                            water.WaterStage = data.WaterStage.Value + station.DWaterBase.Value;
                        }
                        else
                        {
                            water.WaterStage = data.WaterStage.Value;
                        }
                        //tmpWaterFlow = GetWaterFlowByWaterStageAndStation(args.StrStationID, data.WaterStage.Value);
                        tmpWaterFlow = GetWaterFlowByWaterStageAndStation(args.StrStationID, water.WaterStage);
                        water.WaterFlow = tmpWaterFlow; //此处需要计算的
                        //water.ChannelType = args.EChannelType;
                        water.ChannelType = EChannelType.None;
                        water.MessageType = args.EMessageType;
                        int status = 1;
                        AssertWaterData(water, ref tmpRTDWaterDataState, ref status);
                        water.state = status;
                        listWater.Add(water);
                    }
                    m_proxyWater.AddNewRows(listWater);
                }
                #endregion 水量表

                #region 电压表
                List<CEntityVoltage> listVoltages = new List<CEntityVoltage>();
                foreach (CSingleStationData data in args.Datas)
                {
                    // 是否和上一条时间一致, 就丢失当条数据
                    if (m_mapStationRTD[station.StationID].TimeDeviceGained == data.DataTime)
                    {
                        Debug.WriteLine("drop");
                        continue;
                    }
                    if (args.Datas.Count != 1)
                    {
                        m_mapStationRTD[station.StationID].TimeDeviceGained = data.DataTime;
                    }
                    if (data.Voltage <= 0)
                    {
                        continue;
                    }
                    CEntityVoltage voltage = new CEntityVoltage();
                    voltage.StationID = station.StationID;
                    voltage.TimeCollect = data.DataTime;
                    voltage.TimeRecieved = args.RecvDataTime;
                    voltage.Voltage = (Decimal)data.Voltage;
                    voltage.ChannelType = EChannelType.None;
                    voltage.MessageType = args.EMessageType;
                    int status = 1;
                    AssertVoltageData(voltage, ref tmpRTDVoltageDataState, ref status);
                    voltage.state = status;
                    listVoltages.Add(voltage);
                }
                m_proxyVoltage.AddNewRows(listVoltages);
                #endregion 电压表

                #region 实时数据表
                CEntityRealTime realtime = new CEntityRealTime();
                realtime.StrStationID = station.StationID;
                realtime.EIChannelType = args.EChannelType;
                realtime.EIMessageType = args.EMessageType;
                realtime.EIStationType = args.EStationType;
                realtime.StrStationName = station.StationName;
                realtime.StrPort = args.StrSerialPort;
                realtime.TimeReceived = args.RecvDataTime;
                if (args.Datas[tmpDataCount - 1].Voltage > 0)
                {
                    realtime.Dvoltage = args.Datas[tmpDataCount - 1].Voltage;   //电压
                }
                realtime.TimeDeviceGained = args.Datas[tmpDataCount - 1].DataTime; //采集时间
                if (realtime.EIStationType == EStationType.ERainFall || realtime.EIStationType == EStationType.EHydrology)
                {
                    if (args.Datas[tmpDataCount - 1].TotalRain >= 0)
                    {
                        if (station.LastDayTotalRain.HasValue && station.LLastDayTotalRain.HasValue)
                        {
                            realtime.LastDayRainFall = station.LastDayTotalRain.Value - station.LLastDayTotalRain.Value;
                            if (realtime.LastDayRainFall < 0)
                            {
                                if (station.DRainAccuracy != 0)
                                {
                                    decimal tmpDiff = 10000 * (decimal)station.DRainAccuracy - station.LLastDayTotalRain.Value + station.LastDayTotalRain.Value;
                                    if (tmpDiff >= dayInterval)
                                    {
                                        realtime.LastDayRainFall = 0;
                                    }
                                    else
                                    {
                                        realtime.LastDayRainFall = tmpDiff;
                                    }
                                }
                                else
                                {
                                    realtime.LastDayRainFall = 0;
                                }
                            }
                            else if (realtime.LastDayRainFall >= dayInterval)
                            {
                                realtime.LastDayRainFall = 0;
                                realtime.ERTDState = ERTDDataState.EError;
                            }
                        }

                        //realtime.DDayRainFall = tmpDayRain; //保存的是最有一次计算的结果
                        // CalPeriodRain(station.StationID, station.DRainAccuracy, data.TotalRain.Value, data.DataTime, station.LastDataTime, station.LastClockSharpTotalRain, station.LastClockSharpTime, ref tmpPeriodRain, ref status);
                        if (args.Datas[tmpDataCount - 1].DataTime.Minute + args.Datas[tmpDataCount - 1].DataTime.Second == 0)
                        {
                            realtime.DPeriodRain = tmpPeriodRain;
                        }
                        else
                        {
                            realtime.DPeriodRain = calRainForRealTimePeriodRain(station.StationID, args.Datas[tmpDataCount - 1].TotalRain, args.Datas[tmpDataCount - 1].DataTime, station.DRainAccuracy, station.LastClockSharpTotalRain, station.LastClockSharpTime);
                        }
                        if (realtime.DPeriodRain < 0)
                        {
                            realtime.DPeriodRain = null;
                        }
                        //realtime.DPeriodRain = tmpPeriodRain; //保存的是最有一次计算的结果
                        if (args.Datas[tmpDataCount - 1].DataTime.Hour == 8 && args.Datas[tmpDataCount - 1].DataTime.Minute == 0 && args.Datas[tmpDataCount - 1].DataTime.Second == 0)
                        {
                            // realtime.DDayRainFall = tmpDayRain;
                            realtime.DDayRainFall = 0;
                        }
                        else
                        {
                            DateTime tmpOld = new DateTime();
                            DateTime tmp = new DateTime();
                            if (args.Datas[tmpDataCount - 1].DataTime.Hour > 8)
                            {
                                tmpOld = args.Datas[tmpDataCount - 1].DataTime;
                                tmp = new DateTime(tmpOld.Year, tmpOld.Month, tmpOld.Day, 8, 0, 0);
                            }
                            else
                            {
                                tmpOld = args.Datas[tmpDataCount - 1].DataTime;
                                tmp = new DateTime(tmpOld.Year, tmpOld.Month, tmpOld.Day, 8, 0, 0);
                                tmp = tmp.Subtract(new TimeSpan(24, 0, 0));
                            }
                            //   DateTime tmpUse = tmp.Subtract(new TimeSpan(24, 0, 0));
                            realtime.DDayRainFall = calRainForRealTimeDayRain(station.StationID, args.Datas[tmpDataCount - 1].TotalRain, args.Datas[tmpDataCount - 1].DataTime, station.DRainAccuracy, station.LastDayTotalRain, station.LastDayTime, tmp);
                        }
                        if (realtime.DDayRainFall < 0)
                        {
                            realtime.DDayRainFall = null;
                        }
                    }
                }
                if (realtime.EIStationType == EStationType.ERiverWater || realtime.EIStationType == EStationType.EHydrology || realtime.EIStationType == EStationType.EReservoir)
                {
                    if (args.Datas[tmpDataCount - 1].WaterStage != -20000)
                    {
                        realtime.DWaterYield = args.Datas[tmpDataCount - 1].WaterStage; //水位

                        if (station.DWaterBase.HasValue)
                        {
                            //  减去水位基值
                            //realtime.DWaterYield -= station.DWaterBase.Value;
                            //2017_03
                            realtime.DWaterYield += station.DWaterBase.Value;
                        }

                        realtime.DWaterFlowFindInTable = tmpWaterFlow; //相应流量
                    }
                }


                if (tmpRTDRainDataState == ERTDDataState.EError
                    || tmpRTDWaterDataState == ERTDDataState.EError
                    || tmpRTDVoltageDataState == ERTDDataState.EError)
                {
                    realtime.ERTDState = ERTDDataState.EError;
                }
                else
                {
                    realtime.ERTDState = ERTDDataState.ENormal;
                }

                UpdateRTDState(realtime);

                // 发消息，通知界面更新
                if (RecvedRTD != null)
                {
                    Task.Factory.StartNew(() => { RecvedRTD.Invoke(this, new CEventSingleArgs<CEntityRealTime>(realtime)); });
                }
                // 更新实时内存副本
                m_mapStationRTD[args.StrStationID] = realtime;
                // 写入实时信息表
                m_proxyRealtime.AddNewRow(realtime);
            }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
            catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
            {

            }

            #endregion 实时数据表
            // RefreshCommunicationRecord(station, args);
        }
        #region 计算实时数据表
        private Nullable<decimal> calRainForRealTimePeriodRain(string stationid, Nullable<decimal> totalrain, DateTime tm, float accuracy, Nullable<decimal> lastSharpClockTotalRain, Nullable<DateTime> lastSharpClockTime)
        {
            Nullable<decimal> result = null;
            if (totalrain.HasValue && lastSharpClockTotalRain.HasValue)
            {
                TimeSpan timespan = tm - (DateTime)lastSharpClockTime;
                if (1 > timespan.Hours)
                {
                    result = totalrain.Value * (decimal)accuracy - lastSharpClockTotalRain;
                }
                else
                {
                    DateTime time = new DateTime(tm.Year, tm.Month, tm.Day, tm.Hour, 0, 0);
                    DateTime time_1 = time.AddHours(1);
                    //查询数据库
                    Nullable<decimal> lastSharpTotalRain = m_proxyRain.GetLastClockSharpTotalRain(stationid, time_1);
                    if (lastSharpTotalRain.HasValue)
                    {
                        result = totalrain.Value * (decimal)accuracy - lastSharpTotalRain;
                    }
                }
            }
            else
            {
                // int hour = tm.Hour + 1;
                DateTime time = new DateTime(tm.Year, tm.Month, tm.Day, tm.Hour, 0, 0);
                DateTime time_1 = time.AddHours(1);
                //查询数据库
                Nullable<decimal> lastSharpTotalRain = m_proxyRain.GetLastClockSharpTotalRain(stationid, time_1);
                if (totalrain.HasValue && lastSharpClockTotalRain.HasValue)
                {
                    result = totalrain.Value * (decimal)accuracy - lastSharpTotalRain;
                }
            }
            return result;
        }
        private Nullable<decimal> calRainForRealTimeDayRain(string stationid, Nullable<decimal> totalrain, DateTime tm, float accuracy, Nullable<decimal> lastDayRain, Nullable<DateTime> lastDayTime, DateTime tmp)
        {
            Nullable<decimal> result = null;
            if (totalrain.HasValue && lastDayRain.HasValue)
            {
                TimeSpan timespan = tm - lastDayTime.Value;
                if (1 > timespan.Days)
                {
                    result = totalrain.Value * (decimal)accuracy - lastDayRain;
                }
                else
                {
                    decimal? lastDayTotalRain = m_proxyRain.GetLastDayTotalRain(stationid, tmp);
                    if (lastDayTotalRain.HasValue)
                    {
                        result = totalrain.Value * (decimal)accuracy - lastDayTotalRain;
                    }
                }
            }
            else
            {
                decimal? lastDayTotalRain = m_proxyRain.GetLastDayTotalRain(stationid, tmp);
                if (totalrain.HasValue && lastDayTotalRain.HasValue)
                {
                    result = totalrain.Value * (decimal)accuracy - lastDayTotalRain;
                }

            }
            return result;
        }
        #endregion

        private void DealRTDData(CEventRecvStationDataArgs args)
        {
            try
            {
                //Debug.WriteLine(string.Format("#收到站点{0}实时数据", args.StrStationID));
                // 生成实时数据CEntityRTD
                Nullable<Decimal> tmpPeriodRain = null;
                Nullable<Decimal> tmpDayRain = null;
                Nullable<Decimal> tmpDifferenceRain = null;
                Nullable<Decimal> tmpWaterFlow = null;
                ERTDDataState tmpRTDRainDataState = ERTDDataState.ENormal;
                ERTDDataState tmpRTDWaterDataState = ERTDDataState.ENormal;
                ERTDDataState tmpRTDVoltageDataState = ERTDDataState.ENormal;
                CEntityStation station = GetStationById(args.StrStationID);
                if (null == station)
                {
                    Debug.WriteLine("站点配置不正确，数据库没有站点{0}的配置", args.StrStationID);
                    return;
                }
                // 是否和上一条时间一致, 就丢失当条数据
                if (m_mapStationRTD[station.StationID].TimeDeviceGained == args.DataTime)
                {
                    Debug.WriteLine("drop");
                    return;
                }
                CEntityRealTime realtime = new CEntityRealTime();
                realtime.StrStationID = station.StationID;
                realtime.EIChannelType = args.EChannelType;
                realtime.EIMessageType = args.EMessageType;
                // 判断站点类型是否一致
                if (args.EStationType != station.StationType)
                {
                    Debug.WriteLine("实时数据中站点{0}的类型与数据库中的类型配置不正确", args.StrStationID);
                    CSystemInfoMgr.Instance.AddInfo(string.Format("实时数据中站点{0}的类型与数据库中的类型配置不正确", args.StrStationID), DateTime.Now, ETextMsgState.EError);
                    //return; //也不计算
                }
                realtime.EIStationType = args.EStationType;
                realtime.StrStationName = station.StationName;
                realtime.Dvoltage = args.Voltage;
                realtime.StrPort = args.StrSerialPort;
                realtime.DWaterYield = args.WaterStage; //水位，可能为空
                if (station.DWaterBase.HasValue)
                {
                    //  减去水位基值
                    //realtime.DWaterYield -= station.DWaterBase.Value;
                    //2017_03
                    realtime.DWaterYield += station.DWaterBase.Value;
                }
                tmpWaterFlow = GetWaterFlowByWaterStageAndStation(args.StrStationID, args.WaterStage);
                realtime.DWaterFlowFindInTable = tmpWaterFlow;
                realtime.TimeDeviceGained = args.DataTime;
                realtime.TimeReceived = args.RecvDataTime;
                // 如果是雨量站或水文站，计算时段雨量和日雨量
                if (EStationType.ERainFall == args.EStationType || EStationType.EHydrology == args.EStationType)
                {
                    // 时段雨量
                    // 计算时段雨量和日雨量
                    CalPeriodDayRain(station.DRainAccuracy, station.LastDayTime, station.LastDayTotalRain, station.LastTotalRain,
                    args.TotalRain.Value, args.DataTime,
                    ref tmpDayRain, ref tmpPeriodRain);
                    //CalDifferenceRain(station.DRainAccuracy, station.LastDayTime, station.LastDayTotalRain, station.LastTotalRain,
                    //   args.TotalRain.Value, args.DataTime,
                    //   ref tmpDifferenceRain);
                    realtime.DDayRainFall = tmpPeriodRain;
                    realtime.DPeriodRain = tmpPeriodRain;
                    realtime.DDifferenceRain = tmpDifferenceRain;
                    // 更新累计雨量，便于下次使用
                    if ((args.TotalRain * (Decimal)station.DRainAccuracy >= station.LastTotalRain))
                    {
                        station.LastTotalRain = args.TotalRain * (Decimal)station.DRainAccuracy;
                    }
                    else if (((DateTime)GetStationLastDateTime(args.StrStationID)).AddDays(15) <= args.DataTime)
                    {
                        station.LastTotalRain = args.TotalRain * (Decimal)station.DRainAccuracy;
                    }
                    else if ((args.TotalRain * (Decimal)station.DRainAccuracy + station.LastTotalRain >= 9000) &&
                        (station.LastTotalRain - args.TotalRain * (Decimal)station.DRainAccuracy >= 9000))
                    {
                        station.LastTotalRain = args.TotalRain * (Decimal)station.DRainAccuracy;
                    }

                    // 如果时间设置的误差范围内
                    int offset = (args.DataTime.Hour - 8) * 60 + args.DataTime.Minute;

                    if (station.LastDayTotalRain.HasValue && station.LLastDayTotalRain.HasValue)
                    {
                        realtime.LastDayRainFall = station.LastDayTotalRain.Value - station.LLastDayTotalRain.Value;
                    }
                    //if (Math.Abs(offset) <= m_iMinutesRange)
                    //{
                    //    // 更新日雨量，便于下次使用
                    //    station.LastDayTime = args.DataTime;
                    //    station.LastDayTotalRain = args.TotalRain.Value * (Decimal)station.DRainAccuracy;
                    //}

                }// end of if rain of hydrology


                // 更新水量表，雨量表以及电压表
                if (args.EStationType == EStationType.EHydrology || args.EStationType == EStationType.ERainFall)
                {
                    CEntityRain rain = new CEntityRain();
                    rain.StationID = station.StationID;
                    rain.TimeCollect = args.DataTime;
                    rain.TimeRecieved = args.RecvDataTime;
                    rain.PeriodRain = realtime.DPeriodRain;
                    rain.DayRain = realtime.DDayRainFall;
                    rain.TotalRain = args.TotalRain * (Decimal)station.DRainAccuracy;
                    rain.MessageType = args.EMessageType;
                    rain.ChannelType = args.EChannelType;
                    AssertAndAdjustRainData(rain, ref tmpRTDRainDataState);
                    if (tmpRTDRainDataState == ERTDDataState.EError)
                    {
                        rain.BState = 0;
                    }
                    else
                    {
                        rain.BState = 1;
                    }
                    m_proxyRain.AddNewRow(rain);

                }

                if ((args.EStationType == EStationType.EHydrology || args.EStationType == EStationType.ERiverWater) && (args.WaterStage != -20000))
                {
                    CEntityWater water = new CEntityWater();
                    water.StationID = station.StationID;
                    water.TimeCollect = args.DataTime;
                    water.TimeRecieved = args.RecvDataTime;
                    if (realtime.DWaterYield.HasValue)
                    {
                        water.WaterStage = realtime.DWaterYield.Value;
                    }
                    else
                    {
                        // 没有水位值？开玩笑吧？
                        water.WaterStage = 0;
                    }
                    water.WaterFlow = tmpWaterFlow;    //此处需要计算的
                    water.ChannelType = args.EChannelType;
                    water.MessageType = args.EMessageType;
                    if (tmpRTDWaterDataState == ERTDDataState.EError)
                    {
                        water.state = 0;
                    }
                    else
                    {
                        water.state = 1;
                    }
                    int status2 = water.state;
                    AssertWaterData(water, ref tmpRTDWaterDataState, ref status2);
                    m_proxyWater.AddNewRow(water);
                }
                CEntityVoltage voltage = new CEntityVoltage();
                voltage.StationID = station.StationID;
                voltage.TimeCollect = args.DataTime;
                voltage.TimeRecieved = args.RecvDataTime;
                voltage.Voltage = args.Voltage;
                voltage.ChannelType = args.EChannelType;
                voltage.MessageType = args.EMessageType;
                if (tmpRTDVoltageDataState == ERTDDataState.EError)
                {
                    voltage.state = 0;
                }
                else
                {
                    voltage.state = 1;
                }
                int status = voltage.state;
                AssertVoltageData(voltage, ref tmpRTDVoltageDataState, ref status);
                m_proxyVoltage.AddNewRow(voltage);


                // 更新状态值
                if (tmpRTDRainDataState == ERTDDataState.EError
                    || tmpRTDWaterDataState == ERTDDataState.EError
                    || tmpRTDVoltageDataState == ERTDDataState.EError)
                {
                    realtime.ERTDState = ERTDDataState.EError;
                }
                else
                {
                    realtime.ERTDState = ERTDDataState.ENormal;
                }
                UpdateRTDState(realtime);
                // 发消息，通知界面更新
                if (RecvedRTD != null)
                {
                    RecvedRTD.Invoke(this, new CEventSingleArgs<CEntityRealTime>(realtime));
                }
                // 更新实时内存副本
                m_mapStationRTD[args.StrStationID] = realtime;

                // 写入实时信息表
                m_proxyRealtime.AddNewRow(realtime);

                //RefreshCommunicationRecord(station, args);
            }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
            catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
            {

            }

        }

        // 当前数据库任务完成的通知
        private void TaskEndAction(Task task)
        {
            m_mutexTaskList.WaitOne();
            if (m_listCurrentTask.Contains(task))
            {
                //task.Wait();
                m_listCurrentTask.Remove(task);
                //Debug.WriteLine(string.Format("处理数据线程结束{0},状态{1}", m_listCurrentTask.Count, task.Status));
                Debug.Write("|");
            }
            m_mutexTaskList.ReleaseMutex();
        }

        /// <summary>
        /// 开启一个线程，使用任务模式完成某件事情
        /// </summary>
        /// <param name="action"></param>
        private void NewTask(Action action)
        {
            if (m_bStopServer)
            {
                // 不再接受任务
                Debug.WriteLine("drop new task");
                return;
            }
            m_mutexTaskList.WaitOne();
            //Task task = new Task(action);
            Task task = Task.Factory.StartNew(action);
            m_listCurrentTask.Add(task);
            //task.Start();
            task.ContinueWith(this.TaskEndAction, TaskContinuationOptions.OnlyOnRanToCompletion);
            m_mutexTaskList.ReleaseMutex();
        }

        /// <summary>
        /// 根据延迟，更新站点状态值,已废弃，自动更新，一个半小时以内的数据OKAY, 2个半小时以后更新
        /// </summary>
        /// <param name="entity"></param>
        private void UpdateRTDState(CEntityRealTime entity)
        {
            /*
            if (entity.ERTDState == ERTDDataState.ENormal)
            {
                // 只有正常的情况下，才计算延迟
                TimeSpan span = entity.TimeReceived - entity.TimeDeviceGained;
                int offset = Math.Abs(span.Minutes);
                if (offset > 5 && offset < 10)
                {
                    entity.ERTDState = ERTDDataState.EWarning;
                }
                else if (offset > 10)
                {
                    // 红色显示
                    entity.ERTDState = ERTDDataState.EError;
                }
            }*/
            // 延迟计算放到CDataGridViewRTD中计算了

        }

        #endregion

        //180903xcj
        public int GetPeriodSpan(string id)
        {
            string reportInterval = m_listStations.Find(i => i.StationID == id).Reportinterval;
            int periodSpan = 24 / (int.Parse(reportInterval));
            return periodSpan;
        }


        //1009gm
        public List<CEntityVoltage> GetVoltageForRateTable(CEntityStation station, DateTime data)
        {
            List<CEntityVoltage> results = new List<CEntityVoltage>();
            results = m_proxyVoltage.QueryForRateTable(station, data);
            return results;
        }
        //获取报讯段次
        public string GetIntervalForRateTable(CEntityStation station)
        {
            string reportInterval = "24";
            List<CEntityStation> tempResults = new List<CEntityStation>();
            tempResults = m_proxyStation.QueryAll();
            string stationid = station.StationID;
            for (int i = 0; i < tempResults.Count; i++)
            {
                if (stationid == tempResults[i].StationID)
                {
                    reportInterval = tempResults[i].Reportinterval;
                    break;
                }
            }
            return reportInterval;
        }
        public List<CEntityVoltage> GetVoltageForRateMonthTable(CEntityStation station, DateTime startTime, DateTime endTime)
        {
            List<CEntityVoltage> results = new List<CEntityVoltage>();
            results = m_proxyVoltage.QueryForRateMonthTable(station, startTime, endTime);
            return results;
        }
        public List<CEntityRain> GetRainsForTable(string StationId, DateTime date)
        {
            List<CEntityRain> results = new List<CEntityRain>();
            results = m_proxyRain.QueryAccTimeAndStation(StationId, date);
            return results;
        }
        public List<CEntityRain> GetRainsForYearTable(string StationId, DateTime date)
        {
            List<CEntityRain> results = new List<CEntityRain>();
            //results = m_proxyRain.QueryAccTimeAndStation(StationId, date);
            results = m_proxyRain.QueryForYearTable(StationId, date);
            return results;
        }
        public List<CEntityRain> GetMRaainsForTable(string StationId, DateTime date)
        {
            List<CEntityRain> results = new List<CEntityRain>();
            results = m_proxyRain.QueryForMonthTable(StationId, date);
            return results;
        }
        //public List<CEntityRain> GetDRaainsForTable(string StationId, DateTime date)
        //{
        //    List<CEntityRain> results = new List<CEntityRain>();
        //    results = m_proxyRain.QueryForMonthTable(StationId, date);
        //    return results;
        //}
        public List<CEntitySoilData> GetMSoilsForTable(string StationId, DateTime date)
        {
            List<CEntitySoilData> results = new List<CEntitySoilData>();
            results = m_proxySoilData.QueryForMonthTable(StationId, date);
            return results;
        }

        //gm添加
        public List<CEntityWater> GetWaterForTable(string station, DateTime date)
        {
            List<CEntityWater> results = new List<CEntityWater>();
            results = m_proxyWater.QueryA(station, date);
            return results;
        }
        public List<CEntityWater> getWaterForYearTable(string station, DateTime date)
        {
            List<CEntityWater> results = new List<CEntityWater>();
            results = m_proxyWater.QueryForYear(station, date);
            return results;
        }

        //gm2018-12-24
        public List<CEntityWater> GetWaterByTime(string station, DateTime start,DateTime end)
        {
            List<CEntityWater> results = new List<CEntityWater>();
            results = m_proxyWater.QueryByTime(station, start, end);
            return results;
        }
        public List<CEntityRain> getListRainByTime(string station, DateTime start, DateTime end)
        {
            List<CEntityRain> results = new List<CEntityRain>();
            results = m_proxyRain.getListRainByTime(station, start, end);
            return results;
        }

        public List<CEntityRainAndWater> getRainAndWaterList(string station, DateTime start, DateTime end)
        {
            List<CEntityRainAndWater> results = new List<CEntityRainAndWater>();
            results = m_proxyRain.getRainAndWaterByTime(station, start, end);
            return results;
        }

        public List<CEntityEva> getEvaByTime(string station, DateTime start, DateTime end)
        {
            List<CEntityEva> results = new List<CEntityEva>();
            results = m_proxyDEva.getEvabyTime(station, start, end);
            return results;
        }
        /// <summary>
        /// 获取小时表数据
        /// </summary>
        /// <param name="station"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<CEntityEva> QueryForHourEvaList4Table(string station, DateTime start, DateTime end)
        {
            List<CEntityEva> results = new List<CEntityEva>();
            results = m_proxyHEva.QueryForHourEvaList4Table(station, start, end);
            return results;
        }

        public List<CEntityEva> getSpEvaByTime(string station, DateTime start, DateTime end)
        {
            List<CEntityEva> results = new List<CEntityEva>();
            results = m_proxyEva.getEvabyTime(station, start, end);
            return results;
        }

        #region 雨量表定时更新
        //System.Timers.Timer aTimer = new System.Timers.Timer();
        //        private void atimer_Tick(object sender, EventArgs e)
        //        {

        //            if (DateTime.Now.Hour == 3 && DateTime.Now.Minute >= 10 && DateTime.Now.Minute < 15)
        //            {
        //                try
        //                {
        //                    Nullable<Decimal> lastTotalRain = null;
        //                    CEntityRain LastSharpMes = new CEntityRain();
        //                    CEntityRain LastDayMes = new CEntityRain();
        //                    List<string> updateStation = new List<string>();
        //                    //更新雨量表
        //                    DateTime dt = DateTime.Now;
        //                    DateTime start = dt.Subtract(new TimeSpan(24, 0, 0));
        //                    updateStation = m_proxyRain.getUpdateStations(start, dt);
        //                    for (int k = 0; k < updateStation.Count; k++)
        //                    {
        //                        DateTime tmp = new DateTime(dt.Year, dt.Month, dt.Day, 8, 0, 0);
        //                        DateTime tmp8 = tmp.Subtract(new TimeSpan(48, 0, 0));
        //                        //CEntityStation station = m_proxyStation.QueryById(updateStation[k]);
        //                        CEntityStation station = m_proxyStation.QueryById(updateStation[k]);
        //                        if (station != null)
        //                        {
        //                            lastTotalRain = m_proxyRain.GetLastRain(updateStation[k], start).TotalRain;
        //                            LastSharpMes = m_proxyRain.GetLastSharpRain(updateStation[k], start);
        //                            LastDayMes = m_proxyRain.GetLastDayRain(updateStation[k], tmp8);
        //                            Nullable<Decimal> lastSharpTotalRain = LastSharpMes.TotalRain;
        //                            Nullable<DateTime> lastSharpTotalTime = LastSharpMes.TimeCollect;
        //                            Nullable<Decimal> lastDayTotalRain = LastDayMes.TotalRain;
        //                            Nullable<DateTime> lastDayTime = LastDayMes.TimeCollect;
        //                            int startIndex = 0;
        //                            int status = 1;
        //                            List<CEntityRain> rains = m_proxyRain.getListRainForUpdate(updateStation[k], start, dt);
        //                            for (int i = 0; i < rains.Count; i++)
        //                            {
        //                                if ((rains[i].TimeCollect.Minute + rains[i].TimeCollect.Second) == 0)
        //                                {
        //                                    //rains[i].PeriodRain = CalPeriodRain_1(station.DRainAccuracy, rains[i].TotalRain, rains[i].TimeCollect, lastSharpTotalRain);
        //                                    rains[i].PeriodRain = CalPeriodRain_2(station.StationID, station.DRainAccuracy, rains[i].TotalRain, rains[i].TimeCollect, lastSharpTotalRain, lastSharpTotalTime);
        //                                    lastSharpTotalTime = rains[i].TimeCollect;
        //                                    lastSharpTotalRain = rains[i].TotalRain;
        //                                }
        //                                if (rains[i].TimeCollect.Hour == 8)
        //                                {
        //                                    rains[i].DayRain = CalDayRain_2(station.StationID, station.DRainAccuracy, rains[i].TotalRain, rains[i].TimeCollect, lastDayTotalRain, lastDayTime);
        //                                    lastDayTotalRain = rains[i].TotalRain;//可以不更新
        //                                    lastDayTime = rains[i].TimeCollect;
        //                                }
        //                                rains[i].DifferneceRain = CalDifferenceRain_1(station.DRainAccuracy, rains[i].TotalRain, lastTotalRain, station.DRainChange, ref status);
        //                                rains[i].BState = status;
        //                                lastTotalRain = rains[i].TotalRain;
        //                            }
        //                            //可以所有的站点批量搞？？
        //                            m_proxyRain.UpdateRows_1(rains, startIndex);
        //                        }
        //                    }
        //                }
        //#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
        //                catch (Exception ex)
        //#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
        //                {
        //                    Console.WriteLine("夜间更新报错！");
        //                }


        //            }
        //            //将rains更新到数据库

        //        }
        private void atimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Hour == 8 && DateTime.Now.Minute == 30)
            //if (true)
            {
                CSystemInfoMgr.Instance.AddInfo("人工计算日量开始");
                //1.获取所有站点ID
                List<String> stationIds = new List<string>();
                if (m_listStations != null && m_listStations.Count > 0)
                {
                    for (int i = 0; i < m_listStations.Count; i++)
                    {
                        if (m_listStations[i].StationType == EStationType.EEva)
                        {
                            stationIds.Add(m_listStations[i].StationID);
                        }
                    }
                }
                //2.定时调用
                for (int i = 0; i <stationIds.Count; i++)
                {
                    CSystemInfoMgr.Instance.AddInfo(stationIds[i]);
                    CEntityStation station = GetStationById(stationIds[i]);
                    Dictionary<string, string> ret = cal.Timing(stationIds[i]);
                    List<CEntityEva> dEvaList = new List<CEntityEva>(); 
                    if(ret == null || ret.Count == 0)
                    {
                        continue;
                    }
                    if (ret.ContainsKey("dayE"))
                    {
                        if (ret["dayE"] != "")
                        {
                            //更新首页显示缓存数据
                            m_mapStationEva[stationIds[i]] = 0;
                            m_mapStationRain[stationIds[i]] = 0;
                            station.LastDayEva = decimal.Parse(ret["dayE"]);
                            station.LastDayRain = decimal.Parse(ret["dayP"]);
                            CEntityEva DEva = new CEntityEva();
                            DEva.StationID = stationIds[i];
                            DEva.TimeCollect = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);
                            DEva.Eva = decimal.Parse(ret["dayE"]);
                            DEva.Rain = decimal.Parse(ret["dayP"]);
                            DEva.Temperature = decimal.Parse(ret["dayT"]);
                            DEva.P8 = decimal.Parse(ret["P8"]);
                            DEva.P20 = decimal.Parse(ret["P20"]);
                            DEva.dayPChange = decimal.Parse(ret["dayPChange"]);
                            DEva.dayEChange = decimal.Parse(ret["dayEChange"]);
                            dEvaList.Add(DEva);
                        }
                    }
                    //3.初始化并保存
                    if (dEvaList.Count > 0)
                    {
                        CSystemInfoMgr.Instance.AddInfo("人工计算日量存储");
                        m_proxyDEva.AddNewRows(dEvaList);
                    }
                }
                //将昨日降雨更新为最新的
                
            

            }
        }

        private Nullable<Decimal> CalDifferenceRain_1(float dRainArruracy, Nullable<Decimal> totalRain, Nullable<Decimal> lastTotalRain, Nullable<Decimal> MaxChange, ref int status)
        {
            Nullable<Decimal> dDifferenceRain = null;
            status = 1;
            // 差值雨量
            if (lastTotalRain.HasValue)
            {
                dDifferenceRain = totalRain.HasValue ? totalRain : 0 - lastTotalRain.Value;
            }
            if (dDifferenceRain < 0)
            {
                decimal tmp = (decimal)MaxChange / (decimal)dRainArruracy;
                if ((10000 - lastTotalRain.Value / (Decimal)dRainArruracy) > tmp)
                {
                    dDifferenceRain = 0;
                    status = 0;
                }
                else
                {
                    dDifferenceRain = 10000 * (Decimal)dRainArruracy - lastTotalRain.Value + (totalRain.HasValue ? totalRain : 0);
                }
            }
            if (dDifferenceRain < 0)
            {
                dDifferenceRain = 0;
            }
            return dDifferenceRain;
        }

        private Nullable<Decimal> CalPeriodRain_1(float dRainArruracy, Nullable<Decimal> totalRain, DateTime datetime, Nullable<Decimal> lastSharpTotalRain)
        {
            Nullable<Decimal> dPeriodRain = null;
            DateTime tmp = DateTime.Now;

            //时段雨量
            if ((datetime.Minute + datetime.Second == 0))
            {
                dPeriodRain = totalRain.HasValue ? totalRain : 0 - lastSharpTotalRain;
            }
            if (dPeriodRain < 0)
            {
                dPeriodRain = 0;
            }
            return dPeriodRain;
        }

        private Nullable<Decimal> CalPeriodRain_2(string stationid, float dRainArruracy, Decimal? totalRain, DateTime datetime, Nullable<Decimal> lastSharpTotalRain, Nullable<DateTime> lastSharpTime)
        {
            Nullable<Decimal> dPeriodRain = null;
            DateTime tmp = DateTime.Now;
            if (lastSharpTotalRain.HasValue && lastSharpTime.HasValue)
            {
                TimeSpan timespan = datetime - lastSharpTime.Value;
                if (1 == timespan.Hours)
                {
                    dPeriodRain = totalRain.HasValue ? totalRain : 0 - lastSharpTotalRain;
                }
                else
                {
                    lastSharpTotalRain = m_proxyRain.GetLastClockSharpTotalRain(stationid, datetime);
                    if (lastSharpTotalRain.HasValue)
                    {
                        dPeriodRain = totalRain.HasValue ? totalRain : 0 - lastSharpTotalRain;
                    }

                }
            }
            if (dPeriodRain < 0)
            {
                return null;
            }
            return dPeriodRain;

        }

        private Nullable<Decimal> CalDayRain_1(float dRainArruracy, Nullable<Decimal> totalRain, DateTime datetime, Nullable<Decimal> lastDayTotalRain)
        {
            Nullable<Decimal> dDayRain = null;
            // 如果时间设置的误差范围内，整点，默认m_iMutesRange为0
            int offset = (datetime.Hour - 8) * 60 + datetime.Minute;
            if (Math.Abs(offset) <= m_iMinutesRange)
            {
                // 计算日雨量,日期相差一天
                if (lastDayTotalRain.HasValue)
                {  // 并且日期相差一天，才计算日雨量，否则都为空
                    dDayRain = totalRain.HasValue ? totalRain : 0 - lastDayTotalRain;
                }
                //dPeriodRain = 0; //整点不符合条件，默认日雨量都为0
            }// end of if minutes accepted

            if (dDayRain < 0)
            {
                return 0;
            }
            return dDayRain;
        }

        private Nullable<Decimal> CalDayRain_2(string stationid, float dRainArruracy, Nullable<Decimal> totalRain, DateTime datetime, Nullable<Decimal> lastDayTotalRain, Nullable<DateTime> LastDayTime)
        {
            Nullable<Decimal> dDayRain = null;
            // 如果时间设置的误差范围内，整点，默认m_iMutesRange为0

            int offset = (datetime.Hour - 8) * 60 + datetime.Minute;
            if (Math.Abs(offset) <= m_iMinutesRange)
            {
                // 计算日雨量,日期相差一天
                if (lastDayTotalRain.HasValue && LastDayTime.HasValue)
                {
                    TimeSpan timespan = datetime - LastDayTime.Value;
                    if (1 == timespan.Days)
                    {
                        // 并且日期相差一天，才计算日雨量，否则都为空
                        dDayRain = totalRain.HasValue ? totalRain : 0 - lastDayTotalRain;
                    }
                    else
                    {
                        //lastDayTotalRain = m_proxyRain.GetLastDayRain(stationid, LastDayTime.Value).TotalRain;
                        dDayRain = totalRain.HasValue ? totalRain : 0 - lastDayTotalRain;

                    }
                }
                //dPeriodRain = 0; //整点不符合条件，默认日雨量都为0
            }// end of if minutes accepted
            if (dDayRain < 0)
            {
                return 0;
            }
            return dDayRain;
        }

        #endregion

       
        

    } // end of class
}
