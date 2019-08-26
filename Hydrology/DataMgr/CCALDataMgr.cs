
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
        //List<string> rawDataList = new List<string>();
        string stcdForCal;  //用于监测计算的站码
        double Kp = 0.356d;
        double Ke = 1.037d;
        double[] limE = new double[2] { 40, 360 };
        double[] limP = new double[2] { 40, 360 };
        double[] limT = new double[2] { -20, 60 };
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
            stcdForCal = eva.StationID;
            rawDataNew[8] = eva.kp.ToString();//降雨转换系数
            rawDataNew[9] = eva.ke.ToString();//蒸发转换系数
            rawDataNew[10] = eva.dh.ToString();//人工数据比测初始高度差
            string flag = eva.comP.ToString();//是否降雨补偿
            if (flag == "0")
            {
                rawDataNew[11] = "false";
            }
            else
            {
                rawDataNew[11] = "true";
            }

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
            double ConDouE;
            double ConDouP;
            string EConvert;
            string PConvert;


            //判断是否有降雨、蒸发转换系数输入，如果有则替换初始转换系数
            if (rawDataNew[8] != "")
            {
                Kp = double.Parse(rawDataNew[8]);
            }

            if (rawDataNew[9] != "")
            {
                Ke = double.Parse(rawDataNew[9]);
            }


            if (double.TryParse(rawDataNew[3], out ConDouE))//蒸发
            {
                EConvert = (ConDouE * Ke).ToString("F2");
            }
            else
            {
                return evaDic;
            }

            if (double.TryParse(rawDataNew[4], out ConDouP))//降雨
            {
                PConvert = (ConDouP * Kp).ToString("F2");
            }

            else
            {
                return evaDic;
            }

            //当出现排水、补水操作时计算小时排水量和补水量
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
            }


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
            System.Threading.Thread.Sleep(5000);


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
                if (rawHourRows == 0)
                {
                    Console.WriteLine("数据整点格式有误！请检查！");
                    return evaDic;
                }
                else if (rawHourRows == 1)
                #region
                {
                    string strSqlForDayDataOne = "select stcd,sum(EChange) as hourEChange from RawData where dt>='" + Convert.ToDateTime(rawDataNew[1]).AddHours(-1).ToString() + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' group by stcd";
                    DataTable dayDTOne = ExecuteDatatable(sqlConStr, strSqlForDayDataOne);
                    evaDic.Add("hourEChange", dayDTOne.Rows[0]["hourEChange"].ToString());
                    //未能检索到上一个时刻的数据，则按距离当前最近的时刻间的均值
                    DateTime dtTemp = Convert.ToDateTime(rawDataNew[1]);
                    //当一天之内只有两条数据时，小时蒸发、降雨等于日蒸发、降雨，写在小时表和日表中
                    string strSqlForOne1 = "SELECT * FROM dbo.[RawData] where dt>='" + dtTemp.AddDays(-1).ToString() + "' and dt<='" + dtTemp.ToString() + "' and stcd='" + stcdForCal + "' order by DT desc";
                    DataTable dtForOne1 = ExecuteDatatable(sqlConStr, strSqlForOne1);
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
                        hourP = (Convert.ToDouble(dtForOne.Rows[0]["TP"]) - Convert.ToDouble(dtForOne.Rows[1]["TP"])) / hours;
                        hourE = (Convert.ToDouble(dtForOne.Rows[0]["TE"]) - Convert.ToDouble(dtForOne.Rows[1]["TE"])) / hours;
                        errE = hourE;
                        hourT = (Convert.ToDouble(dtForOne.Rows[0]["T"]) + Convert.ToDouble(dtForOne.Rows[1]["T"])) / 2.0;
                        hourU = (Convert.ToDouble(dtForOne.Rows[0]["U"]) + Convert.ToDouble(dtForOne.Rows[1]["U"])) / 2.0;
                        hourE = hourP - hourE;
                    }

                    //如果小时雨量筒变化为-1.5mm以下，小时蒸发大于1.5mm或者小于-1.5mm。将ACT设置为“err”,并修改原始数据库中的ACT的值
                    if (hourP <= -1.5d || hourE > 1.5d || hourE < -1.5d)
                    {
                        eva.type = "ERR";
                        string updateSql = "update rawdata set act = 'ER" + errE.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                        evaProxy.UpdateRows(updateSql);
                    }

                    if (hourP < 0.05d)
                    {
                        hourP = 0.0;
                    }

                    if (hourE < 0.0d)
                    {
                        hourE = 0.0;
                    }

                    //当小时有降雨时，如果小时蒸发大于0.3mm，则令小时蒸发等于0.3mm
                    if (hourP != 0 && hourE > 0.3)
                    {
                        hourE = 0.3;
                    }

                    evaDic.Add("hourP", hourP.ToString("F2"));
                    evaDic.Add("hourE", hourE.ToString("F2"));
                    evaDic.Add("hourT", hourT.ToString("F2"));
                    evaDic.Add("hourU", hourU.ToString("F2"));

                    //************************** 计算日降雨量 *****************************
                    #region
                    DateTime theNewDT = Convert.ToDateTime(rawDataNew[1]);
                    DateTime theKeyDT1 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "08:00:00");
                    DateTime theKeyDT2 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "09:00:00");
                    if (theNewDT >= theKeyDT1 && theNewDT < theKeyDT2 && (rawDataNew[6] == null || rawDataNew[6] == ""))
                    {
                        decimal tempE;
                        decimal tempP;
                        decimal p8;
                        decimal p20;
                        #region
                        //当每天只有两组数据时，小时量=日量
                        if (dtForOne1.Rows.Count == 2)
                        {
                            tempE = decimal.Parse(hourE.ToString());
                            tempP = decimal.Parse(hourP.ToString());
                            p8 = tempP / 2;
                            p20 = p8;
                            if (tempE > 12m)
                            {
                                tempE = 12m;
                            }
                            evaDic.Add("dayE", tempE.ToString("0.00"));
                            evaDic.Add("dayP", tempP.ToString("0.00"));
                            evaDic.Add("dayT", rawDataNew[5]);
                            evaDic.Add("P8", p8.ToString("0.00"));
                            evaDic.Add("P20", p20.ToString("0.00"));
                            evaDic.Add("hourComP", "");
                            evaDic.Add("needCover", "");
                            return evaDic;
                        }
                        #endregion

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

                        //判断降雨小时数
                        #region
                        int hourPNums = 0;//记录降雨小时数
                        int hourP8Nums = 0;//记录p8的12个小时内的降雨小时数
                        string strSqlPNums = "select P from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "'";
                        string strSqlP8Nums = "select P from data where dt>='" + theMidDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "'";
                        //TODO
                        DataTable hourPDT = ExecuteDatatable(sqlConStr, strSqlPNums);
                        DataTable hourP8DT = ExecuteDatatable(sqlConStr, strSqlP8Nums);
                        //8小时的数据还未入库
                        if (hourP > 0)
                        {
                            hourPNums += 1;
                            hourP8Nums += 1;
                        }
                        for (int i = 0; i < hourPDT.Rows.Count; i++)
                        {
                            if (decimal.Parse(hourPDT.Rows[i]["P"].ToString()) != 0m)
                            {
                                hourPNums += 1;
                            }
                        }

                        for (int i = 0; i < hourP8DT.Rows.Count; i++)
                        {
                            if (decimal.Parse(hourP8DT.Rows[i]["P"].ToString()) != 0m)
                            {
                                hourP8Nums += 1;
                            }
                        }
                        #endregion
                        //判断当天有无排水或者注水操作，如果有则用小时累加值，如果无则取小时累加值和两个8点差值中的最小值
                        //*******************************************需要修改*******************************************
                        string strForSumTemp = "select * from RawData where DT>='" + theBegDT.AddHours(-1).ToString() + " ' AND DT<'" + theEndDT.ToString() + " ' and stcd='" + stcdForCal + "'";
                        //TODO
                        DataTable dtForDaySumTemp = ExecuteDatatable(sqlConStr, strForSumTemp);
                        int countP = 0;//记录人工操作次数
                        int sumdtForDaySumTempRows = dtForDaySumTemp.Rows.Count;
                        for (int j = 0; j < sumdtForDaySumTempRows; j++)
                        {
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PP" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "ZE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "RE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "ER")
                            {
                                countP = countP + 1;
                            }
                        }
                        //雨量和蒸发值，明天8点减今天8点值，而不是累加值
                        //当没有排注水操作且前一天有八点数据并且没有降雨时时进行计算
                        DateTime DT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "08:00:00");
                        string strForSum = "SELECT * FROM Data where DT='" + DT.ToString() + " ' and stcd='" + stcdForCal + "'";
                        DataTable Temp = ExecuteDatatable(sqlConStr, strForSum);
                        if (countP == 0 && Temp.Rows.Count != 0 && hourPNums == 0)
                        {
                            string strForSum2 = "SELECT a.TE-b.TE as E, b.TP-a.TP as P FROM RawData as a,RawData as b where a.DT='" + theBegDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'";
                            //string strForSum3 = "SELECT  b.TP-a.TP as P8 FROM RawData as a, RawData as b where a.DT='" + theMidDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'";
                            //TODO
                            DataTable dtForDaySum2 = ExecuteDatatable(sqlConStr, strForSum2);
                            //DataTable dtForDaySum3 = ExecuteDatatable(sqlConStr, strForSum3);
                            tempP = decimal.Parse((dtForDaySum2.Rows[0]["P"]).ToString());//24小时降雨值存在temp中
                            //p8 = decimal.Parse((dtForDaySum3.Rows[0]["P8"]).ToString());//p8重赋值
                            decimal E = 0.0m;
                            if (dtForDaySum2.Rows.Count > 0)  //如果取得到昨天八点到今天八点的值
                            {
                                if (tempP < 0.03m)
                                {
                                    tempP = 0.0m;
                                }
                                //if (p8 < 0.03m)
                                //{
                                //    p8 = 0.0m;
                                //}
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
                        //*****************日降雨补偿********************************
                        int needCover = 0;
                        decimal hourComP = 0m;//小时降雨补偿
                        #region
                        //当补偿系数不为0且降雨超过3个小时，需要进行日降雨补偿
                        if (bool.Parse(rawDataNew[11]) != false && hourPNums > 3)
                        {
                            needCover = 1;
                            decimal sumComP;
                            if (hourPNums >= 13)
                            {
                                sumComP = 0.5m;
                            }
                            sumComP = 0.05m * (hourPNums - 3);//补偿总量        
                            hourComP = sumComP / hourPNums;
                            tempP += sumComP;//对日降雨量进行补偿
                            p8 += hourComP * hourP8Nums;
                            p20 = tempP - p8;
                        }
                        #endregion
                        if (tempE >= 12m)
                        {
                            tempE = 12m;
                        }

                        //构造dayData数组
                        dayData[0] = dayDT.Rows[0]["STCD"].ToString();
                        dayData[1] = theKeyDT1.AddDays(-1).ToShortDateString();
                        dayData[2] = tempE.ToString("0.00");  //真实蒸发，考虑容器的换算？
                        dayData[3] = tempP.ToString("0.00");
                        dayData[4] = dayDT.Rows[0]["T"].ToString();
                        dayData[5] = p8.ToString();//存储p8
                        dayData[6] = p20.ToString();//存储p20
                        dayData[7] = "";

                        evaDic.Add("dayE", dayData[2]);
                        evaDic.Add("dayP", dayData[3]);
                        evaDic.Add("dayT", dayData[4]);
                        evaDic.Add("P8", dayData[5]);
                        evaDic.Add("P20", dayData[6]);
                        evaDic.Add("hourComP", hourComP.ToString());
                        evaDic.Add("needCover", needCover.ToString());//只有拿到这个值为1时，去修改小时表，每个不为0的降雨全部加上hourComP 
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
                        evaDic.Add("needCover", "");
                    }
                    return evaDic;
                }
                #endregion
                else
                #region
                {
                    //最先判断是否有RE字段，如果有则重新界定小时时段检索时间，新的时间段为从最新采集数据时刻到RE之间的时段
                    bool hasRE = false;
                    string newDTStr = string.Empty;
                    double errE = 0;
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

                    //计算小时蒸发桶排水量
                    string strSqlForDayDataOne = "select stcd,sum(EChange) as hourEChange from RawData where dt>='" + Convert.ToDateTime(rawDataNew[1]).AddHours(-1).ToString() + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' group by stcd";
                    DataTable dayDTOne = ExecuteDatatable(sqlConStr, strSqlForDayDataOne);
                    evaDic.Add("hourEChange", dayDTOne.Rows[0]["hourEChange"].ToString());
                    //如果有人工干扰的示数
                    errE = Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["TE"]) - Convert.ToDouble(RawHour.Rows[0]["TE"]);
                    //首先判断P或ZE的个数，并记录下特征位置
                    List<int> PorZ = new List<int>();
                    for (int i = 0; i < rawHourRows; i++)
                    {
                        if (RawHour.Rows[i]["ACT"].ToString().Trim() == "PP" || RawHour.Rows[i]["ACT"].ToString().Trim() == "PE" || RawHour.Rows[i]["ACT"].ToString().Trim() == "ZE")
                        {
                            PorZ.Add(i);
                        }
                    }

                    int sumPorZ = PorZ.Count;

                    //1、如果无P或Z，则直接上下时刻相减；
                    #region
                    if (sumPorZ == 0)
                    {
                        hourE = Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["TE"]) - Convert.ToDouble(RawHour.Rows[0]["TE"]);
                        hourP = Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["TP"]) - Convert.ToDouble(RawHour.Rows[0]["TP"]);
                        hourT = (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["T"]) + Convert.ToDouble(RawHour.Rows[0]["T"])) / 2.0f;
                        hourU = (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["U"]) + Convert.ToDouble(RawHour.Rows[0]["U"])) / 2.0f;
                    }
                    #endregion
                    //2、如果有PP、PE或ZE，则分段计算。PP说明是雨量筒排水，排水期间的降雨量由蒸发桶计算；PE说明是蒸发桶排水，期间降雨量则为真实值；ZE为注水操作。
                    #region
                    else
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

                                hourE = hourE + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TE"]) - Convert.ToDouble(RawHour.Rows[0]["TE"]);
                                hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["TP"]) - Convert.ToDouble(RawHour.Rows[0]["TP"]);

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

                    //如果小时雨量筒变化为-1.5mm以下，小时蒸发大于1.5mm或者小于-1.5mm。将ACT设置为“err”，并将原始数据库中的ACT修改过来
                    if (hourP <= -1.5d || hourE > 1.5d || hourE < -1.5d)
                    {
                        eva.type = "ERR";
                        string updateSql = "update rawdata set act = 'ER" + errE.ToString("0.00") + "' where stcd = '" + rawDataNew[0] + "' and dt = '" + rawDataNew[1] + "'";
                        evaProxy.UpdateRows(updateSql);
                    }

                    double dtHours = 1.0f;
                    //构造hourData数组，存储输出信息
                    hourData[0] = RawHour.Rows[rawHourRows - 1]["STCD"].ToString();//站码
                    hourData[1] = RawHour.Rows[rawHourRows - 1]["DT"].ToString();//时间

                    if (hourP < 0.05f)
                    {
                        hourData[3] = "0.00";  //真实降雨
                        hourP = 0.0f;
                    }
                    else
                    {
                        hourData[3] = (hourP / dtHours).ToString("F2");  //真实降雨
                    }
                    //如果蒸发和降雨为负，则赋值为0
                    if ((hourP - hourE) < 0.0f)
                    {
                        hourData[2] = "0.00";  //真实蒸发
                    }
                    else if ((hourP - hourE) > 1.5f)
                    {
                        hourData[2] = "0.00";  //真实蒸发
                    }
                    else
                    {
                        hourData[2] = ((hourP - hourE) / dtHours).ToString("F2");  //真实蒸发
                    }

                    hourE = double.Parse(hourData[2]);
                    hourP = double.Parse(hourData[3]);

                    if (hourP < 0.05d)
                    {
                        hourP = 0.0;
                    }

                    if (hourE < 0.0d)
                    {
                        hourE = 0.0;
                    }

                    //当小时有降雨时，如果小时蒸发大于0.3mm，则令小时蒸发等于0.3mm
                    if (hourP != 0 && hourE > 0.3)
                    {
                        hourE = 0.3;
                    }

                    hourData[4] = hourT.ToString("F2");
                    hourData[5] = hourU.ToString("F2");
                    evaDic.Add("hourE", hourE.ToString("0.00"));
                    evaDic.Add("hourP", hourP.ToString("0.00"));
                    evaDic.Add("hourT", hourData[4]);
                    evaDic.Add("hourU", hourData[5]);

                    //***************************每天的8点，整理过去一天的日累积降雨量和蒸发量*********************************
                    #region
                    DateTime theNewDT = Convert.ToDateTime(rawDataNew[1]);
                    DateTime theKeyDT1 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "08:00:00");
                    DateTime theKeyDT2 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "08:30:00");
                    //先计算雨量筒、蒸发桶的日排、注水量
                    if (theNewDT >= theKeyDT1 && theNewDT < theKeyDT2)
                    {
                        string strSqlForDayData = "select sum(EChange) as dayEChange from Rawdata where dt>='" + theKeyDT1.AddDays(-1).ToString() + "' and dt<='" + theKeyDT1.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        DataTable dayDT = ExecuteDatatable(sqlConStr, strSqlForDayData);
                        evaDic.Add("dayEChange", dayDT.Rows[0]["dayEChange"].ToString());
                    }
                    #region
                    if (theNewDT >= theKeyDT1 && theNewDT < theKeyDT2 && rawDataNew[6] == null)
                    {
                        //当前一天八点钟没有数据时a=true，日量只能累加；当有数据时a=false，日量可以前后时段相减
                        bool a = false;
                        DateTime DT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "08:00:00");//前一天8点
                        string strDayData = "select E from data where dt='" + DT.ToString() + "'and stcd='" + stcdForCal + "'";
                        DataTable DT1 = ExecuteDatatable(sqlConStr, strDayData);//今天8点到昨天8点日累积降雨和蒸发
                        if (DT1.Rows.Count == 0)
                        {
                            a = true;
                        }
                        //确定统计的起止时间，读取数据库数据
                        DateTime theBegDT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "09:00:00");//前一天8点
                        DateTime theMidDT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "21:00:00");//前一天20点
                        DateTime theEndDT = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "09:00:00");//今天8点
                        string strSqlForDayData = "select stcd,sum(E) as E, sum(P) as P, avg(T) as T from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        string strSqlForDayData1 = "select sum(P) as P8 from data where dt>='" + theMidDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        //TODO
                        DataTable dayDT = ExecuteDatatable(sqlConStr, strSqlForDayData);//今天8点到昨天8点日累积降雨和蒸发
                        DataTable dayDT1 = ExecuteDatatable(sqlConStr, strSqlForDayData1);//今天8点到昨天20点累积降雨

                        decimal tempE = Decimal.Parse(dayDT.Rows[0]["E"].ToString()) + decimal.Parse(hourE.ToString());//日蒸发
                        decimal tempP = Decimal.Parse(dayDT.Rows[0]["P"].ToString()) + decimal.Parse(hourP.ToString());//日降雨+8点钟的降雨
                        decimal p8 = Decimal.Parse(dayDT1.Rows[0]["P8"].ToString()) + decimal.Parse(hourP.ToString());//后12小时降雨+8点钟的降雨

                        if (tempP < 0.03m)
                        {
                            tempP = 0.0m;
                        }
                        if (p8 < 0.03m)
                        {
                            p8 = 0.0m;
                        }

                        //判断降雨小时数
                        #region
                        int hourPNums = 0;//记录降雨小时数
                        int hourP8Nums = 0;//记录p8的12个小时内的降雨小时数
                        string strSqlPNums = "select P from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "'";
                        string strSqlP8Nums = "select P from data where dt>='" + theMidDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "'";
                        //TODO
                        DataTable hourPDT = ExecuteDatatable(sqlConStr, strSqlPNums);
                        DataTable hourP8DT = ExecuteDatatable(sqlConStr, strSqlP8Nums);
                        //8小时的数据还未入库
                        if (hourP > 0)
                        {
                            hourPNums += 1;
                            hourP8Nums += 1;
                        }
                        for (int i = 0; i < hourPDT.Rows.Count; i++)
                        {
                            if (decimal.Parse(hourPDT.Rows[i]["P"].ToString()) != 0m)
                            {
                                hourPNums += 1;
                            }
                        }

                        for (int i = 0; i < hourP8DT.Rows.Count; i++)
                        {
                            if (decimal.Parse(hourP8DT.Rows[i]["P"].ToString()) != 0m)
                            {
                                hourP8Nums += 1;
                            }
                        }
                        #endregion

                        //判断当天有无排水或者注水操作，如果有则用小时累加值，如果无则取小时累加值和两个8点差值中的最小值
                        string strForSumTemp = "select * from RawData where DT>='" + theBegDT.AddHours(-1).ToString() + " ' AND DT<'" + theEndDT.ToString() + " ' and stcd='" + stcdForCal + "'";
                        //TODO
                        DataTable dtForDaySumTemp = ExecuteDatatable(sqlConStr, strForSumTemp);
                        int countP = 0;//记录人工操作次数
                        int sumdtForDaySumTempRows = dtForDaySumTemp.Rows.Count;
                        for (int j = 0; j < sumdtForDaySumTempRows; j++)
                        {
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PP" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "ZE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "RE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "ER")
                            {
                                countP = countP + 1;
                            }
                        }


                        //雨量和蒸发值，明天8点减今天8点值，而不是累加值,如果第一天八点没值则累加
                        if (countP == 0 && a == false && hourPNums == 0)
                        {
                            string strForSum2 = "SELECT a.TE-b.TE as E, b.TP-a.TP as P FROM RawData as a,RawData as b where a.DT='" + theBegDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'";
                            //string strForSum3 = "SELECT  b.TP-a.TP as P8 FROM RawData as a, RawData as b where a.DT='" + theMidDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'";
                            //TODO
                            DataTable dtForDaySum2 = ExecuteDatatable(sqlConStr, strForSum2);
                            //DataTable dtForDaySum3 = ExecuteDatatable(sqlConStr, strForSum3);
                            tempP = decimal.Parse((dtForDaySum2.Rows[0]["P"]).ToString());//24小时降雨值存在temp中
                            //p8 = decimal.Parse((dtForDaySum3.Rows[0]["P8"]).ToString());//p8重赋值
                            //double E1 = 0.0d;
                            //double P2 = 0.0d;
                            decimal E = 0.0m;
                            if (dtForDaySum2.Rows.Count > 0)  //如果取得到昨天八点到今天八点的值
                            {
                                if (tempP < 0.03m)
                                {
                                    tempP = 0.0m;
                                }
                                //if (p8 < 0.03m)
                                //{
                                //    p8 = 0.0m;
                                //}
                                E = decimal.Parse((dtForDaySum2.Rows[0]["E"]).ToString()) + tempP;
                                if (E > 0)
                                {
                                    tempE = E; // tempE = Math.Min(tempE, E);
                                }
                            }
                        }

                        //用p的值减去p8的值即为p20的值，计算中没有涉及对降雨的修改，只有对蒸发的修改，因此直接相减即可
                        decimal p20 = tempP - p8;//前12小时降雨
                        if (p20 < 0.0m)
                        {
                            p20 = 0.0m;
                        }

                        //*****************日降雨补偿********************************
                        #region
                        int needCover = 0;
                        decimal hourComP = 0m;//小时降雨补偿
                        //当补偿系数不为0且降雨超过3个小时，需要进行日降雨补偿
                        if (bool.Parse(rawDataNew[11]) != false && hourPNums > 3)
                        {
                            needCover = 1;
                            decimal sumComP;
                            if (hourPNums >= 13)
                            {
                                sumComP = 0.5m;
                            }
                            sumComP = 0.05m * (hourPNums - 3);//补偿总量        
                            hourComP = sumComP / hourPNums;
                            tempP += sumComP;//对日降雨量进行补偿
                            p8 += hourComP * hourP8Nums;
                            p20 = tempP - p8;
                        }
                        #endregion

                        if (tempE >= 12m)
                        {
                            tempE = 12m;
                        }

                        //构造dayData数组
                        dayData[0] = dayDT.Rows[0]["STCD"].ToString();
                        dayData[1] = theKeyDT1.AddDays(-1).ToShortDateString();
                        dayData[2] = tempE.ToString();  //真实蒸发，考虑容器的换算？
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
                        evaDic.Add("hourComP", hourComP.ToString());
                        evaDic.Add("needCover", needCover.ToString());//只有拿到这个值为1时，去修改小时表，每个不为0的降雨全部加上hourComP 

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
                        evaDic.Add("needCover", "");
                        evaDic.Add("dayEChange", "");
                    }

                    #endregion
                }
            }
            #endregion
            return evaDic;
        }

        /// <summary>
        /// 每天8点半触发定时器调用timing函数，如果8点到8点半没有数据接收，则计算当天小时降雨和小时蒸发累加值作为日蒸发和日降雨
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public Dictionary<string, string> Timing(String stationIds)
        {
            //查找8点到9点之间的数据，如果没有数据则对前一天小时蒸发、降雨进行累加，如果有数据直接返回。
            Dictionary<string, string> evaDic = new Dictionary<string, string>();
            string dt1 = DateTime.Now.ToString();
            DateTime theDT1 = Convert.ToDateTime(Convert.ToDateTime(dt1).ToString("yyyy-MM-dd ") + "08:00:00");
            DateTime theDT2 = Convert.ToDateTime(Convert.ToDateTime(dt1).ToString("yyyy-MM-dd ") + "08:30:00");
            string strSqlForHour = "SELECT * FROM dbo.RawData where dt>='" + theDT1.ToString() + "' and dt<='" + theDT2.ToString() + "' and stcd='" + stationIds + "' order by DT desc";
            DataTable RawHour = ExecuteDatatable(sqlConStr, strSqlForHour);
            int rawHourRows = RawHour.Rows.Count;
            //当查询数据不为0时直接返回
            if (rawHourRows != 0)
            {
                return null;
            }
            //当查询数据为0时，对一天的降雨、蒸发数据进行累加，作为日降雨、日蒸发数值
            else
            {
                DateTime theBegDT = Convert.ToDateTime((Convert.ToDateTime(dt1).AddDays(-1)).ToString("yyyy-MM-dd ") + "09:00:00");//前一天8点
                DateTime theMidDT = Convert.ToDateTime((Convert.ToDateTime(dt1).AddDays(-1)).ToString("yyyy-MM-dd ") + "21:00:00");//前一天20点
                DateTime theEndDT = Convert.ToDateTime(Convert.ToDateTime(dt1).ToString("yyyy-MM-dd ") + "09:00:00");//今天8点                        
                string strSqlForDayData = "select stcd,sum(E) as E, sum(P) as P, T from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stationIds + "' group by stcd,T";
                string strSqlForDayData1 = "select sum(P) as P8 from data where dt>='" + theMidDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stationIds + "' group by stcd";
                DataTable dayDT = ExecuteDatatable(sqlConStr, strSqlForDayData);//今天8点到昨天8点日累积降雨和蒸发
                DataTable dayDT1 = ExecuteDatatable(sqlConStr, strSqlForDayData1);
                //计算日降雨桶、蒸发桶排注水量
                string strSqlForDayData2 = "select stcd,sum(PChange) as dayPChange, sum(EChange) as dayEChange from Rawdata where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                DataTable dayDT2 = ExecuteDatatable(sqlConStr, strSqlForDayData2);

                if (dayDT2 != null && dayDT2.Rows != null && dayDT2.Rows.Count >= 1)
                {
                    evaDic.Add("dayPChange", dayDT2.Rows[0]["dayPChange"].ToString());
                    evaDic.Add("dayEChange", dayDT2.Rows[0]["dayEChange"].ToString());
                }
                else
                {
                    evaDic.Add("dayPChange", "0");
                    evaDic.Add("dayEChange", "0");
                }
                if (dayDT1 != null && dayDT1.Rows != null && dayDT1.Rows.Count >= 1)
                {
                    decimal tempE = Decimal.Parse(dayDT.Rows[0]["E"].ToString());//日蒸发
                    decimal tempP = Decimal.Parse(dayDT.Rows[0]["P"].ToString());//日降雨
                    decimal tempT = Decimal.Parse(dayDT.Rows[0]["T"].ToString());//日降雨
                    decimal P8 = Decimal.Parse(dayDT1.Rows[0]["P8"].ToString());//日降雨
                    decimal P20 = tempP - P8;
                    evaDic.Add("dayE", tempE.ToString());
                    evaDic.Add("dayP", tempP.ToString());
                    evaDic.Add("dayT", tempT.ToString());
                    evaDic.Add("P8", P8.ToString("0.00"));
                    evaDic.Add("P20", P20.ToString("0.00"));
                    return evaDic;
                }

            }
            return null;
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