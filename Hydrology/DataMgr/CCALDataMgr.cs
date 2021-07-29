using Hydrology.DBManager;
using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using Hydrology.Entity.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Hydrology.DataMgr
{
    public class CCALDataMgr
    {
        string[] sqlConStr = new string[4];
        string[] rawDataNew = new string[12];
        string[] hourData = new string[6];
        string[] dayData = new string[8];
        //string stcdForCal;  //用于监测计算的站码
        double Kp = 0.356d;
        double Ke = 1.037d;
        /// <summary>
        /// 蒸发计算
        /// </summary>
        /// <param name="Dictionary<string, string>"></param> 
        /// <param name=""></param>
        public Dictionary<string, string> EvaCal(CEntityEva eva)
        {
            Dictionary<string, string> evaDic = new Dictionary<string, string>();//输出的蒸发计算结果
            string strForInserts = string.Empty;
            rawDataNew[0] = eva.StationID;
            rawDataNew[1] = eva.TimeCollect.ToString();
            rawDataNew[2] = eva.Voltage.ToString();
            rawDataNew[3] = eva.Eva.ToString();
            rawDataNew[4] = eva.Rain.ToString();
            rawDataNew[5] = eva.Temperature.ToString();
            rawDataNew[6] = eva.type;
            rawDataNew[7] = DateTime.Now.ToString();
            string stcdForCal = eva.StationID;
            rawDataNew[8] = eva.kp.ToString();//降雨转换系数
            rawDataNew[9] = eva.ke.ToString();//蒸发转换系数
            rawDataNew[10] = eva.dh.ToString();//人工数据比测初始高度差

            //判断是否为有效数字
            double d1;
            bool isD1 = double.TryParse(rawDataNew[2], out d1);
            double d2;
            bool isD2 = double.TryParse(rawDataNew[3], out d2);
            double d3;
            bool isD3 = double.TryParse(rawDataNew[4], out d3);
            double d4;
            bool isD4 = double.TryParse(rawDataNew[5], out d4);

            if (!isD1 || !isD2 || !isD3 || !isD4 || d1 <= 0.0d || d2 <= 0.0d || d3 <= 0.0d || d4 <= 0.0d)
            {
                return evaDic;
            }

            //==========增加转换系数==============
            string EConvert;
            string PConvert;

            //判断是否有降雨、蒸发转换系数输入，如果有则替换初始转换系数
            if (rawDataNew[8] != "")
            {
                Kp = double.Parse(rawDataNew[8]);
                PConvert = (double.Parse(rawDataNew[4]) * Kp).ToString("F2");
            }
            else
            {
                PConvert = (double.Parse(rawDataNew[4]) * Kp).ToString("F2");
            }

            if (rawDataNew[9] != "")
            {
                Ke = double.Parse(rawDataNew[9]);
                EConvert = (double.Parse(rawDataNew[3]) * Ke).ToString("F2");
            }
            else
            {
                EConvert = (double.Parse(rawDataNew[3]) * Ke).ToString("F2");
            }

            //当出现排水、补水操作时计算小时排水量和补水量
            #region
            decimal pChange = 0.0m;//雨量筒只有排水操作，始终为负
            decimal eChange = 0.0m;//蒸发桶注水时为正、排水时为负
            if (rawDataNew[6] == "PP")//当出现雨量筒排水操作时计算排水量
            {
                DateTime dtTemp = Convert.ToDateTime(rawDataNew[1]);
                //找到原始表中前一条数据，雨量筒数据相减
                string strSqlForOne = "SELECT top 1 * FROM dbo.RawData where dt>='" + dtTemp.AddDays(-1).ToString() + "' and dt<='" + dtTemp.ToString() + "' and stcd='" + stcdForCal + "' order by DT desc";
                DataTable dtForOne = ExecuteDatatable(sqlConStr, strSqlForOne);
                if (dtForOne.Rows.Count == 1)
                {
                    pChange = Convert.ToDecimal(PConvert) - Convert.ToDecimal(dtForOne.Rows[0]["TP"]);
                }
                else
                {
                    return evaDic;
                }
            }

            if (rawDataNew[6] == "PE" || rawDataNew[6] == "ZE")//当出现蒸发桶排、注水操作时计算排水量
            {
                DateTime dtTemp = Convert.ToDateTime(rawDataNew[1]);
                //找到原始表中前一条数据，雨量筒数据相减
                string strSqlForOne = "SELECT top 1 * FROM dbo.RawData where dt>='" + dtTemp.AddDays(-1).ToString() + "' and dt<='" + dtTemp.ToString() + "' and stcd='" + stcdForCal + "' order by DT desc";
                DataTable dtForOne = ExecuteDatatable(sqlConStr, strSqlForOne);
                if (dtForOne.Rows.Count == 1)
                {
                    eChange = Convert.ToDecimal(EConvert) - Convert.ToDecimal(dtForOne.Rows[0]["TE"]);
                }
                else
                {
                    return evaDic;
                }
            }
            #endregion

            //蒸发桶里面的水位需要减去一个高度差dh，结果作为人工数据比对用
            //*******************************需要写进小时表中**********************************
            double compareH = Convert.ToDouble(rawDataNew[3]) - Convert.ToDouble(rawDataNew[10]);
            evaDic.Add("dH", compareH.ToString("F2"));
            //*******************************************数据入库************************************************
            //在原始表中加入转换后的雨量刻度值TP和蒸发量刻度值TE
            //第一次数据入库
            eva.TE = Decimal.Parse(EConvert);
            eva.TP = Decimal.Parse(PConvert);
            eva.pChange = pChange;
            eva.eChange = eChange;
            IEvaProxy evaProxy = CDBDataMgr.Instance.GetEvaProxy();
            evaProxy.AddNewRow(eva);
            System.Threading.Thread.Sleep(200);

            //*******************************************计算时段蒸发量*******************************************
            double hourP = 0.0d;//小时降雨
            double hourE = 0.0d;//小时蒸发
            double hourT = 0.0d;//小时温度
            double hourU = 0.0d;//小时电压
            //=======首先判断是否是整点，整点处理（读取该整点至上个整点的数据，分段计算），否则看上个数据是不是"RE"数据，不处理========
            DateTime time = DateTime.Parse(rawDataNew[1]);
            if (time.Minute == 0 && time.Second == 0)
            {
                //=========按时间顺序读取这个整点至上个整点的数据==========
                string strSqlForHour = "SELECT * FROM dbo.[RawData] where dt>='" + Convert.ToDateTime(rawDataNew[1]).AddHours(-1).ToString() + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' order by cast([STCD] as int),cast([DT] as datetime)";
                DataTable RawHour = ExecuteDatatable(sqlConStr, strSqlForHour);
                int rawHourRows = RawHour.Rows.Count;
                CSystemInfoMgr.Instance.AddInfo("rawDataNew[1]" + rawDataNew[1]);
                CSystemInfoMgr.Instance.AddInfo("rawHourRows" + rawHourRows.ToString("0.00"));
                if (rawHourRows == 0)
                {
                    Console.WriteLine("数据整点格式有误！请检查！");
                    return evaDic;
                }
                //上个整点数据缺失
                else if (rawHourRows == 1)
                #region
                {
                    //计算小时排注水量,上整点数据丢失，用当前这小时的排注水量
                    evaDic.Add("hourEChange", eChange.ToString());
                    //未能检索到上一个时刻的数据，则按距离当前最近的时刻间的均值
                    #region
                    DateTime dtTemp = Convert.ToDateTime(rawDataNew[1]);
                    string strSqlForOne = "SELECT top 2 * FROM dbo.[RawData] where dt>='" + dtTemp.AddDays(-1).ToString() + "' and dt<='" + dtTemp.ToString() + "' and stcd='" + stcdForCal + "' order by DT desc";
                    DataTable dtForOne = ExecuteDatatable(sqlConStr, strSqlForOne);
                    int dtForOneRows = dtForOne.Rows.Count;
                    double hours = 1.0d;
                    double errE = 0;
                    if (dtForOneRows == 1)
                    {
                        hourP = 0.0d;
                        hourE = 0.0d;
                        hourT = (Convert.ToDouble(dtForOne.Rows[0]["T"]));
                        hourU = (Convert.ToDouble(dtForOne.Rows[0]["U"]));
                    }
                    else
                    {
                        //计算采用转换后的刻度值
                        hourP = (Convert.ToDouble(dtForOne.Rows[0]["TP"]) - Convert.ToDouble(dtForOne.Rows[1]["TP"]));
                        hourE = (Convert.ToDouble(dtForOne.Rows[0]["TE"]) - Convert.ToDouble(dtForOne.Rows[1]["TE"]));
                        hourT = (Convert.ToDouble(dtForOne.Rows[0]["T"]) + Convert.ToDouble(dtForOne.Rows[1]["T"])) / 2.0;
                        hourU = (Convert.ToDouble(dtForOne.Rows[0]["U"]) + Convert.ToDouble(dtForOne.Rows[1]["U"])) / 2.0;
                        hourE = hourP - hourE;
                        errE = hourE;
                    }

                    //如果小时雨量筒变化为-1.0mm以下，小时蒸发小于-1.5mm。将ACT设置为“err”,并修改原始数据库中的ACT的值
                    if ((hourP <= -1.0d || hourE < -1.5d) && (eva.type == null || eva.type.Trim() == "") && (Convert.ToString(dtForOne.Rows[1]["ACT"]) == null || Convert.ToString(dtForOne.Rows[1]["ACT"]).Trim() == ""))
                    {
                        CSystemInfoMgr.Instance.AddInfo("人工操作干扰");
                        eva.type = "ERR";
                        string updateSql = "update rawdata set act = 'ER" + errE.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                        evaProxy.UpdateRows(updateSql);
                        hourE = 0;
                        if (hourP <= -1.0d)
                        {
                            hourP = 0;
                        }
                    }

                    if (hourP <= 0.05d)
                    {
                        hourP = 0.0;
                    }

                    if (hourE < 0.0d)
                    {
                        hourE = 0.0;
                    }

                    evaDic.Add("hourP", hourP.ToString("F2"));
                    evaDic.Add("hourE", hourE.ToString("F2"));
                    evaDic.Add("hourT", hourT.ToString("F2"));
                    evaDic.Add("hourU", hourU.ToString("F2"));
                    #endregion
                    //************************** 计算日降雨量 *****************************
                    DateTime theNewDT = Convert.ToDateTime(rawDataNew[1]);
                    DateTime theKeyDT1 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "08:00:00");
                    DateTime theKeyDT2 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "09:00:00");
                    if (theNewDT >= theKeyDT1 && theNewDT < theKeyDT2 && (rawDataNew[6] == null || rawDataNew[6].Trim() == ""))
                    #region
                    {
                        decimal tempE;
                        decimal tempP;
                        decimal p8;
                        decimal p20;
                        //先计算雨量筒、蒸发桶的日排、注水量
                        if (theNewDT >= theKeyDT1 && theNewDT < theKeyDT2)
                        {
                            string strSqlForDayData11 = "select sum(EChange) as dayEChange from Rawdata where dt>'" + theKeyDT1.AddDays(-1).ToString() + "' and dt<='" + theKeyDT1.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                            DataTable dayDT11 = ExecuteDatatable(sqlConStr, strSqlForDayData11);
                            evaDic.Add("dayEChange", dayDT11.Rows[0]["dayEChange"].ToString());
                        }

                        //确定统计的起止时间，读取数据库数据
                        DateTime theBegDT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "09:00:00");//前一天8点
                        DateTime theMidDT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "21:00:00");//前一天20点
                        DateTime theEndDT = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "09:00:00");//今天8点                        
                        string strSqlForDayData = "select stcd,sum(E) as E, sum(P) as P, avg(T) as T from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        string strSqlForDayData1 = "select sum(P) as P8 from data where dt>='" + theMidDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        DataTable dayDT = ExecuteDatatable(sqlConStr, strSqlForDayData);//今天8点到昨天8点日累积降雨和蒸发
                        DataTable dayDT1 = ExecuteDatatable(sqlConStr, strSqlForDayData1);//今天8点到昨天20点累积降雨
                        tempE = Decimal.Parse(dayDT.Rows[0]["E"].ToString()) + decimal.Parse(hourE.ToString());//日蒸发
                        tempP = Decimal.Parse(dayDT.Rows[0]["P"].ToString()) + decimal.Parse(hourP.ToString());//日降雨+8点钟的降雨
                        p8 = Decimal.Parse(dayDT1.Rows[0]["P8"].ToString()) + decimal.Parse(hourP.ToString());//后12小时降雨+8点钟的降雨

                        if (tempP < 0.03m)
                        {
                            tempP = 0.0m;
                        }
                        if (p8 < 0.03m)
                        {
                            p8 = 0.0m;
                        }
                        //判断当天有无排水或者注水操作，如果有则用小时累加值，如果无则取小时累加值和两个8点差值中的最小值                        
                        string strForSumTemp = "select * from RawData where DT>='" + theBegDT.AddHours(-1).ToString() + " ' AND DT<'" + theEndDT.ToString() + " ' and stcd='" + stcdForCal + "'";
                        DataTable dtForDaySumTemp = ExecuteDatatable(sqlConStr, strForSumTemp);
                        int countP = 0;//记录人工操作次数
                        int sumdtForDaySumTempRows = dtForDaySumTemp.Rows.Count;
                        for (int j = 0; j < sumdtForDaySumTempRows; j++)
                        {
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() != "" && dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() != null)
                            {
                                countP = countP + 1;
                            }
                        }
                        //雨量和蒸发值，明天8点减今天8点值，而不是累加值
                        //当没有排注水操作且前一天有八点数据时进行计算
                        DateTime DT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "08:00:00");
                        string strForSum = "SELECT * FROM Data where DT='" + DT.ToString() + " ' and stcd='" + stcdForCal + "'";
                        DataTable Temp = ExecuteDatatable(sqlConStr, strForSum);
                        if (countP == 0 && Temp.Rows.Count != 0)
                        {
                            string strForSum2 = "SELECT a.TE-b.TE as E, b.TP-a.TP as P FROM RawData as a,RawData as b where a.DT='" + theBegDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'and b.stcd='" + stcdForCal + "'";
                            DataTable dtForDaySum2 = ExecuteDatatable(sqlConStr, strForSum2);
                            tempP = decimal.Parse((dtForDaySum2.Rows[0]["P"]).ToString());//24小时降雨值存在temp中
                            decimal E = 0.0m;
                            if (dtForDaySum2.Rows.Count > 0)  //如果取得到昨天八点到今天八点的值
                            {
                                if (tempP < 0.03m)
                                {
                                    tempP = 0.0m;
                                }

                                E = decimal.Parse((dtForDaySum2.Rows[0]["E"]).ToString()) + tempP;
                                if (E > 0)
                                {
                                    tempE = E; // tempE = Math.Min(tempE, E);
                                }
                            }
                        }

                        //用p的值减去p8的值即为p20的值，计算中没有涉及对降雨的修改，只有对蒸发的修改，因此直接相减即可
                        p20 = tempP - p8;//前12小时降雨
                        if (p20 < 0.0m)
                        {
                            p20 = 0.0m;
                        }

                        if (tempE >= 12m)
                        {
                            tempE = 12m;
                        }

                        if (tempE <= 0.0m)
                        {
                            tempE = 0.0m;
                        }

                        //构造dayData数组
                        dayData[0] = dayDT.Rows[0]["STCD"].ToString();
                        dayData[1] = theKeyDT1.AddDays(-1).ToShortDateString();
                        dayData[2] = tempE.ToString("0.00");  //真实蒸发，考虑容器的换算？
                        dayData[3] = tempP.ToString("0.00");
                        dayData[4] = dayDT.Rows[0]["T"].ToString();
                        dayData[5] = p8.ToString("0.00");//存储p8
                        dayData[6] = p20.ToString("0.00");//存储p20
                        dayData[7] = "";
                        evaDic.Add("dayE", dayData[2]);
                        evaDic.Add("dayP", dayData[3]);
                        evaDic.Add("dayT", dayData[4]);
                        evaDic.Add("P8", dayData[5]);
                        evaDic.Add("P20", dayData[6]);
                        #endregion
                    }
                    else
                    {
                        evaDic.Add("dayE", "");
                        evaDic.Add("dayP", "");
                        evaDic.Add("dayT", "");
                        evaDic.Add("P8", "");
                        evaDic.Add("P20", "");
                        evaDic.Add("hourComP", "");
                    }
                    return evaDic;
                }
                #endregion
                else
                #region
                {
                    //最先判断是否有RE字段，如果有则重新界定小时时段检索时间，新的时间段为从最新采集数据时刻到RE之间的时段
                    #region
                    bool hasRE = false;
                    string newDTStr = string.Empty;
                    decimal errE = 0;
                    for (int i = 0; i < rawHourRows; i++)
                    {
                        //判断是否有RE（初始化）字符
                        if (RawHour.Rows[i]["ACT"].ToString().Trim() == "RE")
                        {
                            hasRE = true;
                            newDTStr = RawHour.Rows[i]["DT"].ToString().Trim();
                        }
                    }

                    if (hasRE)
                    {
                        //如果有则重新界定小时时段检索时间，新的时间段为从最新采集数据时刻到RE之间的时段
                        strSqlForHour = "SELECT * FROM dbo.[RawData] where dt>='" + newDTStr + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' order by cast([STCD] as int),cast([DT] as datetime)";
                        //TODO
                        RawHour = ExecuteDatatable(sqlConStr, strSqlForHour);
                        rawHourRows = RawHour.Rows.Count;
                    }
                    #endregion
                    //计算小时蒸发桶排水量，当整点数据都在时用整点数据做差，如果不在则用每次排注水的和
                    string strSqlForHour1 = "SELECT * FROM dbo.[RawData] where dt='" + Convert.ToDateTime(rawDataNew[1]).AddHours(-1).ToString() + "' and stcd='" + stcdForCal + "' order by cast([STCD] as int),cast([DT] as datetime)";
                    DataTable RawHour1 = ExecuteDatatable(sqlConStr, strSqlForHour1);
                    //判断是否有蒸发桶排注水
                    string strSqlForDayDataOne = "select stcd,sum(EChange) as hourEChange,sum(PChange) as hourPChange from RawData where dt>='" + Convert.ToDateTime(rawDataNew[1]).AddHours(-1).ToString() + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' group by stcd";
                    DataTable dayDTOne = ExecuteDatatable(sqlConStr, strSqlForDayDataOne);
                    decimal hourEChange = Convert.ToDecimal(dayDTOne.Rows[0]["hourEChange"]);
                    decimal hourPChange = Convert.ToDecimal(dayDTOne.Rows[0]["hourPChange"]);
                    evaDic.Add("hourEChange", hourEChange.ToString());
                    evaDic.Add("hourPChange", hourPChange.ToString());

                    //首先判断P或ZE的个数，并记录下特征位置
                    List<int> PorZ = new List<int>();
                    List<int> PorZ1 = new List<int>();
                    List<int> PorZ2 = new List<int>();
                    List<int> PorZ3 = new List<int>();
                    for (int i = 0; i < rawHourRows; i++)
                    {
                        if (RawHour.Rows[i]["ACT"].ToString().Trim() == "PP" || RawHour.Rows[i]["ACT"].ToString().Trim() == "PE" || RawHour.Rows[i]["ACT"].ToString().Trim() == "ZE")
                        {
                            PorZ.Add(i);
                        }
                        //如果有pe操作，进行标记
                        if (RawHour.Rows[i]["ACT"].ToString().Trim() == "PE")
                        {
                            PorZ1.Add(i);
                        }
                        //如果有ze操作，进行标记
                        if (RawHour.Rows[i]["ACT"].ToString().Trim() == "ZE")
                        {
                            PorZ2.Add(i);
                        }
                        //如果有PP操作，进行标记
                        if (RawHour.Rows[i]["ACT"].ToString().Trim() == "PP")
                        {
                            PorZ3.Add(i);
                        }
                    }

                    int sumPorZ = PorZ.Count;
                    //1、如果无P或Z，则直接上下时刻相减；                   
                    if (sumPorZ == 0)
                    #region
                    {
                        //CSystemInfoMgr.Instance.AddInfo("TEST3！！！！！");
                        hourE = Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["TE"]) - Convert.ToDouble(RawHour.Rows[0]["TE"]);
                        hourP = Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["TP"]) - Convert.ToDouble(RawHour.Rows[0]["TP"]);
                        hourT = (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["T"]) + Convert.ToDouble(RawHour.Rows[0]["T"])) / 2.0f;
                        hourU = (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["U"]) + Convert.ToDouble(RawHour.Rows[0]["U"])) / 2.0f;
                    }
                    #endregion
                    //2、如果有PP、PE或ZE，则分段计算。PP说明是雨量筒排水，排水期间的降雨量由蒸发桶计算；PE说明是蒸发桶排水，期间降雨量则为真实值；ZE为注水操作。
                    else
                    #region
                    {
                        for (int i = 0; i < sumPorZ; i++)
                        {
                            //每个特征位置需要减两次：（1）是特征位的上一行数据减上一特征位数据；（2）是特征位数据减上一行数据
                            if (i == 0) //如果是第一个特征位，则认为上一特征位为第一行
                            {
                                //第一次减
                                if (PorZ[i] == 0)
                                {
                                    hourE = hourE + Convert.ToDouble(RawHour.Rows[PorZ[i]]["TE"]) - Convert.ToDouble(RawHour.Rows[0]["TE"]);
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["TP"]) - Convert.ToDouble(RawHour.Rows[0]["TP"]);
                                }

                                else
                                {
                                    hourE = hourE + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TE"]) - Convert.ToDouble(RawHour.Rows[0]["TE"]);
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TP"]) - Convert.ToDouble(RawHour.Rows[0]["TP"]);
                                }

                                //第二次减
                                //如果是雨量桶排水，则排水时段雨量就按蒸发筒计算
                                if (RawHour.Rows[PorZ[i]]["ACT"].ToString().Trim() == "PP")
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["TE"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TE"]);
                                }
                                //如果是蒸发桶排水或注水，则排水时段雨量就按雨量筒计算
                                else
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["TP"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TP"]);
                                }
                            }
                            else
                            {
                                //第一次减
                                hourE = hourE + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TE"]) - Convert.ToDouble(RawHour.Rows[PorZ[i - 1]]["TE"]);
                                hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TP"]) - Convert.ToDouble(RawHour.Rows[PorZ[i - 1]]["TP"]);

                                //第二次减
                                //如果是雨量桶排水，则排水时段雨量就按蒸发筒计算
                                if (RawHour.Rows[PorZ[i]]["ACT"].ToString().Trim() == "PP")
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["TE"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TE"]);
                                    //少个特征位置之间的差值
                                }
                                //如果是蒸发桶排水或注水，则排水时段雨量就按雨量筒计算
                                else
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["TP"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TP"]);
                                }
                            }
                        }
                        hourE = hourE + Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["TE"]) - Convert.ToDouble(RawHour.Rows[PorZ[sumPorZ - 1]]["TE"]);
                        hourP = hourP + Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["TP"]) - Convert.ToDouble(RawHour.Rows[PorZ[sumPorZ - 1]]["TP"]);
                        hourT = hourT + (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["T"]) + Convert.ToDouble(RawHour.Rows[0]["T"])) / 2.0f;
                        hourU = hourU + (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["U"]) + Convert.ToDouble(RawHour.Rows[0]["U"])) / 2.0f;
                    }
                    #endregion
                    double dtHours = 1.0f;
                    //构造hourData数组，存储输出信息
                    hourData[0] = RawHour.Rows[rawHourRows - 1]["STCD"].ToString();//站码
                    hourData[1] = RawHour.Rows[rawHourRows - 1]["DT"].ToString();//时间

                    //判断异常操作
                    //如果小时雨量筒变化为-1.0mm以下，小时蒸发大于1.5mm或者小于-1.5mm，并且该小时内没有排注水操作。 将ACT设置为“err”，并将原始数据库中的ACT修改过来
                    #region
                    double hourETest = (hourP - hourE) / dtHours;
                    if ((hourP <= -1.0d || hourETest < -1.5d || hourETest > 1.5d) && sumPorZ == 0)
                    {
                        string updateSql = "";
                        CSystemInfoMgr.Instance.AddInfo("人工操作干扰");
                        //如果上个整点有数据,计算雨量筒人为排注水,降雨量异常，将数据库ACT改为PER
                        if (RawHour1.Rows.Count != 0 && hourP <= -1.0d)
                        {
                            errE = (Decimal)hourP;
                            updateSql = "update rawdata set act = 'PER" + errE.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                        }
                        //如果上个整点有数据,计算蒸发桶排水（蒸发桶异常，雨量筒正常），将数据库ACT改为EER
                        else if (RawHour1.Rows.Count != 0 && hourP > -1.0d && (hourETest < -1.5d || hourETest > 1.5d))
                        {
                            errE = (Decimal)hourETest;
                            updateSql = "update rawdata set act = 'EER" + errE.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                            evaDic["hourEChange"] = errE.ToString();
                        }
                        else
                        {
                            errE = 0;
                            updateSql = "update rawdata set act = 'EER" + errE.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                        }
                        eva.type = "ERR";
                        evaProxy.UpdateRows(updateSql);
                        hourE = 0;
                        if (hourP <= -1.0d)
                        {
                            hourP = 0;
                        }
                    }
                    #endregion
                    //计算真实降雨量
                    #region
                    if (hourP < 0.05f)
                    {
                        hourData[3] = "0.00";  //真实降雨
                        hourP = 0.0f;
                    }
                    else
                    {
                        hourData[3] = (hourP / dtHours).ToString("F2");  //真实降雨
                    }
                    hourP = double.Parse(hourData[3]);
                    #endregion
                    //计算真实蒸发
                    hourE = (hourP - hourE) / dtHours;
                    #region
                    //如果蒸发模式为err或者有re字段，或者蒸发器排水，蒸发直接写0
                    if ((eva.type == "ERR") || hasRE || (PorZ1.Count != 0))
                    {
                        hourE = 0.0d;
                    }
                    //如果蒸发桶有注水操作，该小时蒸发取上一小时蒸发值
                    else if (PorZ2.Count != 0)
                    {
                        string strGetLastE = "SELECT top 1 * FROM dbo.[Data] where dt>='" + Convert.ToDateTime(rawDataNew[1]).AddDays(-1).ToString() + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' order by DT desc";
                        DataTable GetLastE = ExecuteDatatable(sqlConStr, strGetLastE);
                        if (GetLastE.Rows.Count != 0)
                        {
                            hourE = Convert.ToDouble(GetLastE.Rows[0]["E"]);
                        }
                        else
                        {
                            hourE = 0.0d;
                        }
                    }
                    //如果该小时有降雨，且雨量筒有排水“PP”，但无蒸发桶排水标记“PE”，如果蒸发计算大于0.3则按0.3计，蒸发器排水按计算蒸发减0.3计
                    else if (hourP > 0 && PorZ3.Count != 0 && PorZ1.Count == 0 && hourE > 0.3)
                    {
                        double hourEErr = hourE - 0.3;
                        hourE = 0.3d;
                        string updateSqlForE = "update rawdata set act = 'EER" + hourEErr.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                        evaProxy.UpdateRows(updateSqlForE);
                    }
                    else
                    {
                        //未使用ffff
                        hourE = hourE;
                    }

                    //降雨不为0且无其它人工操作和其它操作时,蒸发大于0.3按0.3处理,同时ACT标记ER事件
                    if (hourP != 0.0 && sumPorZ == 0)
                    {
                        if (hourE >= 0.3d)
                        {
                            double hourEErr = hourE - 0.3;
                            hourE = 0.3d;
                            string updateSql = "update rawdata set act = 'ER" + hourEErr.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                            evaProxy.UpdateRows(updateSql);
                        }
                        if (hourE <= -0.3d)
                        {
                            double hourEErr = hourE - 0.3;
                            hourE = -0.3d;
                            string updateSql = "update rawdata set act = 'ER" + hourEErr.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                            evaProxy.UpdateRows(updateSql);
                        }
                    }
                    #endregion

                    //计算蒸发排注水量
                    //如果整点数据在，小时内有蒸发排注水操作，没有人工操作，则按整点数据计算小时蒸发排水量，否则用累加数据
                    if (RawHour1.Rows.Count != 0 && (PorZ1.Count != 0 || PorZ2.Count != 0) && eva.type != "ERR")
                    {
                        //RawHour1里面存储上一个整点数据，RawHour.Rows[rawHourRows - 1]里面存储当前整点数据
                        hourEChange = (decimal)((Convert.ToDecimal(RawHour.Rows[rawHourRows - 1]["TE"]) - Convert.ToDecimal(RawHour1.Rows[0]["TE"])) - Convert.ToDecimal(hourP));
                        evaDic["hourEChange"] = hourEChange.ToString();
                    }
                    hourData[4] = hourT.ToString("F2");
                    hourData[5] = hourU.ToString("F2");
                    evaDic.Add("hourE", hourE.ToString("0.00"));
                    evaDic.Add("hourP", hourP.ToString("0.00"));
                    evaDic.Add("hourT", hourData[4]);
                    evaDic.Add("hourU", hourData[5]);
                    //***************************每天的8点，整理过去一天的日累积降雨量和蒸发量*********************************
                    //***************************每天的8点，整理过去一天的日累积降雨量和蒸发量*********************************
                    //***************************每天的8点，整理过去一天的日累积降雨量和蒸发量*********************************
                    #region
                    DateTime theNewDT = Convert.ToDateTime(rawDataNew[1]);
                    DateTime theKeyDT1 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "08:00:00");
                    DateTime theKeyDT2 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "08:30:00");
                    if (theNewDT >= theKeyDT1 && theNewDT < theKeyDT2)
                    {
                        //先计算蒸发桶的日排、注水量
                        #region
                        string strSqlForDayEChange = "select sum(hourEChange) as dayEChange from data where dt>'" + theKeyDT1.AddDays(-1).ToString() + "' and dt<='" + theKeyDT1.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        DataTable DayEChange = ExecuteDatatable(sqlConStr, strSqlForDayEChange);
                        decimal dayEChange = Convert.ToDecimal(DayEChange.Rows[0]["dayEChange"]) + hourEChange;
                        evaDic.Add("dayEChange", dayEChange.ToString());
                        #endregion
                        //计算日量累加值
                        #region
                        //确定统计的起止时间，读取数据库数据
                        DateTime theBegDT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "09:00:00");//前一天8点
                        DateTime theMidDT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "21:00:00");//前一天20点
                        DateTime theEndDT = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "09:00:00");//今天8点
                        string strSqlForDayData = "select stcd,sum(E) as E, sum(P) as P, avg(T) as T from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        string strSqlForDayData1 = "select sum(P) as P8 from data where dt>='" + theMidDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";

                        DataTable dayDT = ExecuteDatatable(sqlConStr, strSqlForDayData);//今天8点到昨天8点日累积降雨和蒸发
                        DataTable dayDT1 = ExecuteDatatable(sqlConStr, strSqlForDayData1);//今天8点到昨天20点累积降雨
                        decimal tempE = Decimal.Parse(dayDT.Rows[0]["E"].ToString()) + decimal.Parse(hourE.ToString());//日蒸发
                        decimal tempP = Decimal.Parse(dayDT.Rows[0]["P"].ToString()) + decimal.Parse(hourP.ToString());//日降雨+8点钟的降雨
                        decimal p8 = Decimal.Parse(dayDT1.Rows[0]["P8"].ToString()) + decimal.Parse(hourP.ToString());//后12小时降雨+8点钟的降雨

                        //判断前天八点是否有数据，没有数据时a=true，日量只能累加；当有数据时a=false，日量可以前后时段相减
                        bool a = false;
                        DateTime DT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "08:00:00");//前一天8点
                        string strDayData = "select E from data where dt='" + DT.ToString() + "'and stcd='" + stcdForCal + "'";
                        DataTable DT1 = ExecuteDatatable(sqlConStr, strDayData);//今天8点到昨天8点日累积降雨和蒸发
                        if (DT1.Rows.Count == 0)
                        {
                            a = true;
                        }

                        //判断当天各操作次数
                        string strForSumTemp = "select * from RawData where DT>='" + theBegDT.AddHours(-1).ToString() + " ' AND DT<'" + theEndDT.ToString() + " ' and stcd='" + stcdForCal + "'";
                        DataTable dtForDaySumTemp = ExecuteDatatable(sqlConStr, strForSumTemp);
                        int countP = 0;//记录操作次数
                        int countE = 0;//蒸发器排水次数
                        int countPP = 0;//雨量筒排水次数
                        int countER = 0;//记录人工操作次数
                        int countEr = 0;//记录下雨天蒸发大于0.3的次数
                        int sumdtForDaySumTempRows = dtForDaySumTemp.Rows.Count;
                        for (int j = 0; j < sumdtForDaySumTempRows; j++)
                        {
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() != "" && dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() != null)
                            {
                                countP = countP + 1;
                            }
                            //如果有pe或者ZE操作，进行标记
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "ZE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "RE")
                            {
                                countE = countE + 1;
                            }
                            //如果有PP操作，进行标记
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PP")
                            {
                                countPP = countPP + 1;
                            }
                            //如果有ER操作，进行标记
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "ER")
                            {
                                countEr = countEr + 1;
                            }
                        }
                        countER = countP - countE - countPP;

                        //日量计算
                        #region
                        //1、当天没有任何事件，且前一天八点数据存在，日量用8点对8点作差
                        if (countP == 0 && a == false)
                        {
                            string strForSum2 = "SELECT a.TE-b.TE as E, b.TP-a.TP as P FROM RawData as a,RawData as b where a.DT='" + theBegDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'and b.stcd='" + stcdForCal + "'";
                            DataTable dtForDaySum2 = ExecuteDatatable(sqlConStr, strForSum2);
                            tempP = decimal.Parse((dtForDaySum2.Rows[0]["P"]).ToString());//24小时降雨值存在temp中
                            decimal E = 0.0m;
                            if (dtForDaySum2.Rows.Count > 0)  //如果取得到昨天八点到今天八点的值
                            {
                                if (tempP < 0.03m)
                                {
                                    tempP = 0.0m;
                                }
                                E = decimal.Parse((dtForDaySum2.Rows[0]["E"]).ToString()) + tempP;
                                if (E > 0)
                                {
                                    tempE = E;
                                }
                            }
                        }
                        //2、当天有降雨，雨量筒排水,但蒸发桶未排注水且没有人为操作，雨量用累加值，蒸发用8点对8点计算
                        else if (tempP > 0 && countPP > 0 && countE == 0 && countER == 0)
                        {
                            string strForSum2 = "SELECT a.TE-b.TE as E FROM RawData as a,RawData as b where a.DT='" + theBegDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'and b.stcd='" + stcdForCal + "'";
                            DataTable dtForDaySum2 = ExecuteDatatable(sqlConStr, strForSum2);
                            decimal E = 0.0m;
                            if (dtForDaySum2.Rows.Count > 0)  //如果取得到昨天八点到今天八点的值
                            {
                                E = decimal.Parse((dtForDaySum2.Rows[0]["E"]).ToString()) + tempP;
                                if (E > 0)
                                {
                                    tempE = E;
                                }
                            }
                        }
                        //其它全用累加值
                        else
                        {
                            tempE = tempE;
                            tempP = tempP;
                            p8 = p8;
                        }
                        #endregion
                        //用p的值减去p8的值即为p20的值，计算中没有涉及对降雨的修改，只有对蒸发的修改，因此直接相减即可
                        if (tempP < 0.03m)
                        {
                            tempP = 0.0m;
                        }

                        if (p8 < 0.03m)
                        {
                            p8 = 0.0m;
                        }

                        decimal p20 = tempP - p8;//前12小时降雨
                        if (p20 < 0.0m)
                        {
                            p20 = 0.0m;
                        }

                        if (tempE >= 12m)
                        {
                            tempE = 12m;
                        }

                        if (tempE <= 0.0m)
                        {
                            tempE = 0.0m;
                        }

                        //构造dayData数组
                        dayData[0] = dayDT.Rows[0]["STCD"].ToString();
                        dayData[1] = theKeyDT1.AddDays(-1).ToShortDateString();
                        dayData[2] = tempE.ToString("0.00");  //真实蒸发，考虑容器的换算？
                        dayData[3] = tempP.ToString("0.00");
                        dayData[4] = (decimal.Parse(dayDT.Rows[0]["T"].ToString())).ToString("0.00");
                        dayData[5] = p8.ToString("0.00");//存储p8
                        dayData[6] = p20.ToString("0.00");//存储p20
                        dayData[7] = "";
                        evaDic.Add("dayE", dayData[2]);
                        evaDic.Add("dayP", dayData[3]);
                        evaDic.Add("dayT", dayData[4]);
                        evaDic.Add("P8", dayData[5]);
                        evaDic.Add("P20", dayData[6]);
                        #endregion
                    }

                    else
                    {
                        evaDic.Add("dayE", "");
                        evaDic.Add("dayP", "");
                        evaDic.Add("dayT", "");
                        evaDic.Add("P8", "");
                        evaDic.Add("P20", "");
                        evaDic.Add("hourComP", "");
                        evaDic.Add("dayEChange", "");
                    }
                    #endregion
                }
                #endregion
            }
            return evaDic;
        }

        /// <summary>
        /// 每天8点半触发定时器调用timing函数，如果8点到8点半没有数据接收，则计算当天小时降雨和小时蒸发累加值作为日蒸发和日降雨
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public Dictionary<string, string> Timing(String stationIds)
        {
            try
            {
                CSystemInfoMgr.Instance.AddInfo("开始计算");
                //查找8点的数据，如果没有数据则对前一天小时蒸发、降雨进行累加，如果有数据直接返回。
                Dictionary<string, string> evaDic = new Dictionary<string, string>();
                string dt1 = DateTime.Now.ToString();
                DateTime theDT1 = Convert.ToDateTime(Convert.ToDateTime(dt1).ToString("yyyy-MM-dd ") + "08:00:00");
                //DateTime theDT2 = Convert.ToDateTime(Convert.ToDateTime(dt1).ToString("yyyy-MM-dd ") + "08:30:00");
                string strSqlForHour = "SELECT * FROM dbo.DayData where dt='" + theDT1.ToString() + "' order by DT desc";
                DataTable RawHour = ExecuteDatatable(sqlConStr, strSqlForHour);
                int rawHourRows = RawHour.Rows.Count;
                //当查询数据不为0时直接返回
                if (rawHourRows != 0)
                {
                    CSystemInfoMgr.Instance.AddInfo("数据已存在");
                    return null;
                }
                //当查询数据为0时，对一天的降雨、蒸发数据进行累加，作为日降雨、日蒸发数值
                else
                {
                    DateTime theBegDT = Convert.ToDateTime((Convert.ToDateTime(dt1).AddDays(-1)).ToString("yyyy-MM-dd ") + "09:00:00");//前一天8点
                    DateTime theMidDT = Convert.ToDateTime((Convert.ToDateTime(dt1).AddDays(-1)).ToString("yyyy-MM-dd ") + "21:00:00");//前一天20点
                    DateTime theEndDT = Convert.ToDateTime(Convert.ToDateTime(dt1).ToString("yyyy-MM-dd ") + "09:00:00");//今天8点                        
                    string strSqlForDayData = "select stcd,sum(E) as E, sum(P) as P, avg(T) as T from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stationIds + "' group by stcd";
                    string strSqlForDayData1 = "select sum(P) as P8 from data where dt>='" + theMidDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stationIds + "' group by stcd";
                    DataTable dayDT = ExecuteDatatable(sqlConStr, strSqlForDayData);//今天8点到昨天8点日累积降雨和蒸发
                    DataTable dayDT1 = ExecuteDatatable(sqlConStr, strSqlForDayData1);
                    //计算日降雨桶、蒸发桶排注水量
                    string strSqlForDayData2 = "select stcd, sum(EChange) as dayEChange from Rawdata where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stationIds + "' group by stcd";
                    DataTable dayDT2 = ExecuteDatatable(sqlConStr, strSqlForDayData2);

                    if (dayDT2 != null && dayDT2.Rows != null && dayDT2.Rows.Count >= 1)
                    {
                        evaDic.Add("dayEChange", dayDT2.Rows[0]["dayEChange"].ToString());
                    }
                    else
                    {
                        evaDic.Add("dayEChange", "0");
                    }
                    if (dayDT != null && dayDT.Rows != null && dayDT.Rows.Count >= 1)
                    {
                        decimal tempE = Decimal.Parse(dayDT.Rows[0]["E"].ToString());//日蒸发
                        try
                        {
                            CSystemInfoMgr.Instance.AddInfo(tempE.ToString());
                        }
                        catch
                        {
                            CSystemInfoMgr.Instance.AddInfo("err0");
                        }
                        decimal tempP = Decimal.Parse(dayDT.Rows[0]["P"].ToString());//日降雨
                        try
                        {
                            CSystemInfoMgr.Instance.AddInfo(tempP.ToString());
                        }
                        catch
                        {
                            CSystemInfoMgr.Instance.AddInfo("err1");
                        }
                        decimal tempT = Decimal.Parse(dayDT.Rows[0]["T"].ToString());
                        try
                        {
                            CSystemInfoMgr.Instance.AddInfo(tempT.ToString());
                        }
                        catch
                        {
                            CSystemInfoMgr.Instance.AddInfo("err2");
                        }
                        decimal P8 = Decimal.Parse(dayDT1.Rows[0]["P8"].ToString());
                        try
                        {
                            CSystemInfoMgr.Instance.AddInfo(P8.ToString());
                        }
                        catch
                        {
                            CSystemInfoMgr.Instance.AddInfo("err3");
                        }

                        decimal P20 = tempP - P8;
                        evaDic.Add("dH", "0.0");
                        evaDic.Add("hourEChange", "0.0");
                        evaDic.Add("hourP", "0.0");
                        evaDic.Add("hourE", "0.0");
                        evaDic.Add("hourT", "0.0");
                        evaDic.Add("hourU", "0.0");
                        evaDic.Add("dayPChange", "0");

                        if (tempE < 0)
                        {
                            tempE = 0;
                        }
                        if (tempP < 0)
                        {
                            tempP = 0;
                        }
                        evaDic.Add("dayE", tempE.ToString());
                        evaDic.Add("dayP", tempP.ToString());
                        evaDic.Add("dayT", tempT.ToString());
                        evaDic.Add("P8", P8.ToString("0.00"));
                        evaDic.Add("P20", P20.ToString("0.00"));
                        try
                        {
                            CSystemInfoMgr.Instance.AddInfo("计算结果：" + evaDic);
                        }
                        catch
                        {
                            CSystemInfoMgr.Instance.AddInfo("err4");
                        }
                        return evaDic;
                    }
                    else
                    {
                        return evaDic;
                    }

                }
            }
            catch (Exception e)
            {
                CSystemInfoMgr.Instance.AddInfo("8:00数据丢失!" + e.Message);
            }

            return null;
        }

        public void BuShuCal(CEntityEva eva)
        {
            try
            {
                CSystemInfoMgr.Instance.AddInfo("开始8点补数");
                string strForInserts = string.Empty;
                rawDataNew[0] = eva.StationID;//站码
                rawDataNew[1] = eva.TimeCollect.ToString();//时间
                rawDataNew[2] = eva.Temperature.ToString();//温度
                rawDataNew[3] = eva.Eva.ToString();//蒸发刻度
                rawDataNew[4] = eva.Rain.ToString();//降雨刻度
                rawDataNew[5] = eva.kp.ToString();//降雨转换系数
                rawDataNew[6] = eva.ke.ToString();//蒸发转换系数
                String stcdForCal = eva.StationID;

                //判断是否为有效数字
                double d1;
                bool isD1 = double.TryParse(rawDataNew[2], out d1);
                double d2;
                bool isD2 = double.TryParse(rawDataNew[3], out d2);
                double d3;
                bool isD3 = double.TryParse(rawDataNew[4], out d3);
                double d4;
                bool isD4 = double.TryParse(rawDataNew[5], out d4);

                if (!isD1 || !isD2 || !isD3 || !isD4 || d2 < 0.0d || d3 < 0.0d || d4 < 0.0d)
                {
                    return;
                }

                //==========增加转换系数==============
                string EConvert;
                string PConvert;

                //判断是否有降雨、蒸发转换系数输入，如果有则替换初始转换系数
                if (rawDataNew[5] != "")
                {
                    Kp = double.Parse(rawDataNew[5]);
                    PConvert = (double.Parse(rawDataNew[4]) * Kp).ToString("F2");
                }
                else
                {
                    PConvert = (double.Parse(rawDataNew[4]) * Kp).ToString("F2");
                }

                if (rawDataNew[6] != "")
                {
                    Ke = double.Parse(rawDataNew[6]);
                    EConvert = (double.Parse(rawDataNew[3]) * Ke).ToString("F2");
                }
                else
                {
                    EConvert = (double.Parse(rawDataNew[3]) * Ke).ToString("F2");
                }

                //*******************************************数据入库************************************************
                //在原始表中加入转换后的雨量刻度值TP和蒸发量刻度值TE
                eva.TE = Decimal.Parse(EConvert);
                eva.TP = Decimal.Parse(PConvert);
                eva.pChange = 0;
                eva.eChange = 0;

                IEvaProxy evaProxy = CDBDataMgr.Instance.GetEvaProxy();
                evaProxy.AddNewRow(eva);
            }

            catch (Exception e)
            {
                CSystemInfoMgr.Instance.AddInfo("8点补数失败！");
            }

        }

        public void BuShuPZCal(CEntityEva eva)
        {
            try
            {
                CSystemInfoMgr.Instance.AddInfo("开始排注水补数");
                string strForInserts = string.Empty;
                rawDataNew[0] = eva.StationID;//站码
                rawDataNew[1] = eva.TimeCollect.ToString();//时间
                rawDataNew[2] = eva.Temperature.ToString();//温度
                rawDataNew[3] = eva.Eva.ToString();//蒸发刻度
                rawDataNew[4] = eva.Rain.ToString();//降雨刻度
                rawDataNew[5] = eva.kp.ToString();//降雨转换系数
                rawDataNew[6] = eva.ke.ToString();//蒸发转换系数
                rawDataNew[7] = eva.type;//排注水情况 PE：蒸发桶排水，ZE蒸发桶注水，PP雨量筒排水
                String stcdForCal = eva.StationID;

                //判断是否为有效数字
                double d1;
                bool isD1 = double.TryParse(rawDataNew[2], out d1);
                double d2;
                bool isD2 = double.TryParse(rawDataNew[3], out d2);
                double d3;
                bool isD3 = double.TryParse(rawDataNew[4], out d3);
                double d4;
                bool isD4 = double.TryParse(rawDataNew[5], out d4);

                if (!isD1 || !isD2 || !isD3 || !isD4 || d2 < 0.0d || d3 < 0.0d || d4 < 0.0d)
                {
                    return;
                }

                string EConvert;
                string PConvert;
                decimal eChange;
                decimal pChange;

                //判断是否有降雨、蒸发转换系数输入，如果有则替换初始转换系数
                if (rawDataNew[5] != "")
                {
                    Kp = double.Parse(rawDataNew[5]);
                    PConvert = (double.Parse(rawDataNew[4]) * Kp).ToString("F2");
                }
                else
                {
                    PConvert = (double.Parse(rawDataNew[4]) * Kp).ToString("F2");
                }

                if (rawDataNew[6] != "")
                {
                    Ke = double.Parse(rawDataNew[6]);
                    EConvert = (double.Parse(rawDataNew[3]) * Ke).ToString("F2");
                }
                else
                {
                    EConvert = (double.Parse(rawDataNew[3]) * Ke).ToString("F2");
                }
                eva.TE = Decimal.Parse(EConvert);
                eva.TP = Decimal.Parse(PConvert);
                IEvaProxy evaProxy = CDBDataMgr.Instance.GetEvaProxy();
                //*******************************************数据入库************************************************
                //在原始表中加入转换后的雨量刻度值TP和蒸发量刻度值TE
                //如果排注水状态为空值，直接入库
                if (rawDataNew[7] == null || rawDataNew[7] == "")
                {
                    eva.pChange = 0;
                    eva.eChange = 0;
                    evaProxy.AddNewRow(eva);
                    System.Threading.Thread.Sleep(5000);
                }
                //如果为PE找到上一时刻数据，计算蒸发桶排水量为eChange
                else if (rawDataNew[7] == "PE" || rawDataNew[7] == "ZE")
                {
                    DateTime dtTemp = Convert.ToDateTime(rawDataNew[1]);
                    //找到原始表中前一条数据，雨量筒数据相减
                    string strSqlForOne = "SELECT top 1 * FROM dbo.RawData where dt>='" + dtTemp.AddDays(-1).ToString() + "' and dt<='" + dtTemp.ToString() + "' and stcd='" + stcdForCal + "' order by DT desc";
                    DataTable dtForOne = ExecuteDatatable(sqlConStr, strSqlForOne);
                    if (dtForOne.Rows.Count == 1)
                    {
                        eChange = Convert.ToDecimal(EConvert) - Convert.ToDecimal(dtForOne.Rows[0]["TE"]);
                    }
                    else
                    {
                        CSystemInfoMgr.Instance.AddInfo("未找到上时刻数据");
                        return;
                    }
                    eva.pChange = 0;
                    eva.eChange = eChange;

                    evaProxy.AddNewRow(eva);
                    System.Threading.Thread.Sleep(5000);
                }
                else if (rawDataNew[7] == "PP")
                {
                    DateTime dtTemp = Convert.ToDateTime(rawDataNew[1]);
                    //找到原始表中前一条数据，雨量筒数据相减
                    string strSqlForOne = "SELECT top 1 * FROM dbo.RawData where dt>='" + dtTemp.AddDays(-1).ToString() + "' and dt<='" + dtTemp.ToString() + "' and stcd='" + stcdForCal + "' order by DT desc";
                    DataTable dtForOne = ExecuteDatatable(sqlConStr, strSqlForOne);
                    if (dtForOne.Rows.Count == 1)
                    {
                        pChange = Convert.ToDecimal(PConvert) - Convert.ToDecimal(dtForOne.Rows[0]["TP"]);
                    }
                    else
                    {
                        CSystemInfoMgr.Instance.AddInfo("未找到上时刻数据");
                        return;
                    }
                    eva.pChange = pChange;
                    eva.eChange = 0;

                    evaProxy.AddNewRow(eva);
                    System.Threading.Thread.Sleep(5000);
                }
                else
                {
                    CSystemInfoMgr.Instance.AddInfo("排注水输入格式错误");
                    return;
                }

            }
            catch (Exception e)
            {
                CSystemInfoMgr.Instance.AddInfo("排注水补数失败！");
            }

        }

        private DataTable ExecuteDatatable(string[] myConnection, string sql, params SqlParameter[] parameters)
        {

            using (SqlConnection conn = CDBManager.Instance.GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    foreach (SqlParameter parater in parameters)
                    {
                        cmd.Parameters.Add(parater);
                    }
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dataset);
                    return dataset.Tables[0];
                }
            }
        }

        private int ExecuteNonQuery(string[] myConnection, string sql, params SqlParameter[] parameters)
        {
            string connStr = string.Empty;
            connStr = "Server=" + myConnection[0] + ";DataBase=" + myConnection[1] + ";Uid=" + myConnection[2] + ";Pwd=" + myConnection[3];

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    foreach (SqlParameter parater in parameters)
                    {
                        cmd.Parameters.Add(parater);
                    }
                    return cmd.ExecuteNonQuery();
                }
            }
        }

    }
}