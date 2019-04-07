﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using Hydrology.Entity;
using Hydrology.DBManager.Interface;
using Hydrology.DBManager;
using Hydrology.Entity.Utils;

namespace Hydrology.DataMgr
{
    public class CCALDataMgr
    {
        string[] sqlConStr = new string[4];
        string[] rawDataNew = new string[12];
        string[] hourData = new string[6];
        string[] dayData = new string[6];
        //List<string> rawDataList = new List<string>();
        string stcdForCal;  //用于监测计算的站码
        double Kp = 0.356d;
        double Ke = 1.037d;
        double ELimit = 0.5d;  //小时蒸发大于0.5mm，则认为为错误数据
        double Plimit = 0.2d;  //日降雨量小于0.2mm，则认为无降雨
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
            //int sumRawDataRows = rawDataList.Count;
            string strForInserts = string.Empty;
            //rawDataNew[0] = myDic["StationID"];
            rawDataNew[0] = eva.StationID;
            //rawDataNew[1] = myDic["Time"];
            rawDataNew[1] = eva.TimeCollect.ToString();
            //rawDataNew[2] = myDic["Voltge"];
            rawDataNew[2] = eva.Voltage.ToString();
            //rawDataNew[3] = myDic["Evp"];
            rawDataNew[3] = eva.Eva.ToString();
            //rawDataNew[4] = myDic["Rain"];
            rawDataNew[4] = eva.Rain.ToString();
            //rawDataNew[5] = myDic["Temperature"];
            rawDataNew[5] = eva.Temperature.ToString();
            //rawDataNew[6] = myDic["EvpType"];
            rawDataNew[6] = eva.type;
            rawDataNew[7] = DateTime.Now.ToString();
            stcdForCal = eva.StationID;



            rawDataNew[8] = EvaConf.kp.ToString();//降雨转换系数
            rawDataNew[9] = EvaConf.ke.ToString();//蒸发转换系数
            rawDataNew[10] = EvaConf.dh.ToString();//人工数据比测初始高度差
            rawDataNew[11] = EvaConf.comP.ToString();//是否降雨补偿


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


            //蒸发桶里面的水位需要减去一个高度差dh，结果作为人工数据比对用
            //*******************************需要写进原始表中**********************************
            double compareH = Convert.ToDouble(rawDataNew[3]) - Convert.ToDouble(rawDataNew[10]);



            //*******************************************数据入库************************************************
            //将原始表中的降雨量修改为PConvert，蒸发量修改为EConvert
            //第一次数据入库
            eva.Eva = Decimal.Parse(EConvert);
            eva.Rain = Decimal.Parse(PConvert);
            IEvaProxy evaProxy = CDBDataMgr.Instance.GetEvaProxy();
            evaProxy.AddNewRow(eva);
            System.Threading.Thread.Sleep(5000);
            //TODO sql语句
            //*******************************************需要修改*******************************************
            //string strSqlForInquire = "SELECT top 1 * FROM dbo.[RawData]";
            //DataTable dtReadSql = ExecuteDatatable(sqlConStr, strSqlForInquire);
            //*******************************************需要修改*******************************************




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
                //*******************************************需要修改*******************************************
                string strSqlForHour = "SELECT * FROM dbo.[RawData] where dt>='" + Convert.ToDateTime(rawDataNew[1]).AddHours(-1).ToString() + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' order by cast([STCD] as int),cast([DT] as datetime)";
                //TODO List
                DataTable RawHour = ExecuteDatatable(sqlConStr, strSqlForHour);
                //*******************************************需要修改*******************************************

                int rawHourRows = RawHour.Rows.Count;

                if (rawHourRows == 0)
                {
                    Console.WriteLine("数据整点格式有误！请检查！");
                    return evaDic;
                }

                else if (rawHourRows == 1)
                #region
                {
                    //未能检索到上一个时刻的数据，则按距离当前最近的时刻间的均值
                    DateTime dtTemp = Convert.ToDateTime(rawDataNew[1]);
                    //*******************************************需要修改*******************************************
                    string strSqlForOne = "SELECT top 2 * FROM dbo.[RawData] where dt>='" + dtTemp.AddDays(-1).ToString() + "' and dt<='" + dtTemp.ToString() + "' and stcd='" + stcdForCal + "' order by DT desc";
                    //TODO
                    DataTable dtForOne = ExecuteDatatable(sqlConStr, strSqlForOne);
                    //*******************************************需要修改*******************************************
                    int dtForOneRows = dtForOne.Rows.Count;
                    double hours = 1.0d;
                    if (dtForOneRows == 1)
                    {
                        hourP = 0.0d;
                        hourE = 0.0d;
                        hourT = (Convert.ToDouble(dtForOne.Rows[0]["T"]));
                        hourU = (Convert.ToDouble(dtForOne.Rows[0]["U"]));
                    }
                    else
                    {
                        hourP = (Convert.ToDouble(dtForOne.Rows[0]["P"]) - Convert.ToDouble(dtForOne.Rows[1]["P"])) / hours;
                        hourE = (Convert.ToDouble(dtForOne.Rows[0]["E"]) - Convert.ToDouble(dtForOne.Rows[1]["E"])) / hours;
                        hourT = (Convert.ToDouble(dtForOne.Rows[0]["T"]) + Convert.ToDouble(dtForOne.Rows[1]["T"])) / 2.0;
                        hourU = (Convert.ToDouble(dtForOne.Rows[0]["U"]) + Convert.ToDouble(dtForOne.Rows[1]["U"])) / 2.0;
                        hourE = hourP - hourE;
                    }

                    if (hourP < 0.05d)
                    {
                        hourP = 0.0;
                    }

                    if (hourE < 0.0d)
                    {
                        hourE = 0.0;
                    }
                    if (hourE > 2.0d)
                    {
                        hourE = 0.0;
                    }
                    evaDic.Add("hourP", hourP.ToString("F2"));
                    evaDic.Add("hourE", hourE.ToString("F2"));
                    evaDic.Add("hourT", hourT.ToString("F2"));
                    evaDic.Add("hourU", hourU.ToString("F2"));
                }
                #endregion
                else
                #region
                {
                    //最先判断是否有RE字段，如果有则重新界定小时时段检索时间，新的时间段为从最新采集数据时刻到RE之间的时段
                    bool hasRE = false;
                    string newDTStr = string.Empty;
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
                        //*******************************************需要修改*******************************************
                        strSqlForHour = "SELECT * FROM dbo.[RawData] where dt>='" + newDTStr + "' and dt<='" + rawDataNew[1] + "' and stcd='" + stcdForCal + "' order by cast([STCD] as int),cast([DT] as datetime)";
                        //TODO
                        RawHour = ExecuteDatatable(sqlConStr, strSqlForHour);
                        //*******************************************需要修改*******************************************
                        rawHourRows = RawHour.Rows.Count;
                    }

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
                        hourE = Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["E"]) - Convert.ToDouble(RawHour.Rows[0]["E"]);
                        hourP = Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["P"]) - Convert.ToDouble(RawHour.Rows[0]["P"]);
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
                                hourE = hourE + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["E"]) - Convert.ToDouble(RawHour.Rows[0]["E"]);
                                hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["P"]) - Convert.ToDouble(RawHour.Rows[0]["P"]);

                                //第二次减
                                //如果是雨量桶排水，则排水时段雨量就按蒸发筒计算
                                if (RawHour.Rows[PorZ[i]]["ACT"].ToString().Trim() == "PP")
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["E"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["E"]);
                                }
                                //如果是蒸发桶排水或注水，则排水时段雨量就按雨量筒计算
                                else
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["P"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["P"]);
                                }
                            }
                            else
                            {
                                //第一次减
                                hourE = hourE + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["E"]) - Convert.ToDouble(RawHour.Rows[PorZ[i - 1]]["E"]);
                                hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["P"]) - Convert.ToDouble(RawHour.Rows[PorZ[i - 1]]["P"]);

                                //第二次减
                                //如果是雨量桶排水，则排水时段雨量就按蒸发筒计算
                                if (RawHour.Rows[PorZ[i]]["ACT"].ToString().Trim() == "PP")
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["E"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["E"]);

                                    //少个特征位置之间的差值
                                }
                                //如果是蒸发桶排水或注水，则排水时段雨量就按雨量筒计算
                                else
                                {
                                    hourE = hourE + 0.0f;
                                    hourP = hourP + Convert.ToDouble(RawHour.Rows[PorZ[i]]["P"]) - Convert.ToDouble(RawHour.Rows[PorZ[i] - 1]["P"]);
                                }
                            }
                        }
                        hourE = hourE + Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["E"]) - Convert.ToDouble(RawHour.Rows[PorZ[sumPorZ - 1]]["E"]);
                        hourP = hourP + Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["P"]) - Convert.ToDouble(RawHour.Rows[PorZ[sumPorZ - 1]]["P"]);
                        hourT = hourT + (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["T"]) + Convert.ToDouble(RawHour.Rows[0]["T"])) / 2.0f;
                        hourU = hourU + (Convert.ToDouble(RawHour.Rows[rawHourRows - 1]["U"]) + Convert.ToDouble(RawHour.Rows[0]["U"])) / 2.0f;
                    }
                    #endregion

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
                    else if ((hourP - hourE) > 2.0f)
                    {
                        hourData[2] = "0.00";  //真实蒸发
                    }
                    else
                    {
                        hourData[2] = ((hourP - hourE) / dtHours).ToString("F2");  //真实蒸发
                    }
                    hourData[4] = hourT.ToString("F2");
                    hourData[5] = hourU.ToString("F2");

                    //evaDic.Add("STCD", hourData[0]);
                    //evaDic.Add("DT", hourData[1]);
                    evaDic.Add("hourE", hourData[2]);
                    evaDic.Add("hourP", hourData[3]);
                    evaDic.Add("hourT", hourData[4]);
                    evaDic.Add("hourU", hourData[5]);
                    //***************************每天的8点，整理过去一天的日累积降雨量和蒸发量*********************************
                    #region
                    int hourPNums = 0;//记录降雨小时数
                    DateTime theNewDT = Convert.ToDateTime(rawDataNew[1]);
                    DateTime theKeyDT1 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "08:00:00");
                    DateTime theKeyDT2 = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "09:00:00");
                    #region
                    if (theNewDT >= theKeyDT1 && theNewDT < theKeyDT2 && rawDataNew[6] == null)
                    {
                        //确定统计的起止时间，读取数据库数据
                        DateTime theBegDT = Convert.ToDateTime((Convert.ToDateTime(rawDataNew[1]).AddDays(-1)).ToString("yyyy-MM-dd ") + "09:00:00");
                        DateTime theEndDT = Convert.ToDateTime(Convert.ToDateTime(rawDataNew[1]).ToString("yyyy-MM-dd ") + "09:00:00");
                        //*******************************************需要修改*******************************************
                        string strSqlForDayData = "select stcd,sum(E) as E, sum(P) as P, avg(T) as T from data where dt>='" + theBegDT.ToString() + "' and dt<'" + theEndDT.ToString() + "' and stcd='" + stcdForCal + "' group by stcd";
                        //TODO
                        DataTable dayDT = ExecuteDatatable(sqlConStr, strSqlForDayData);
                        //*******************************************需要修改*******************************************

                        //判断当天有无排水或者注水操作，如果有则用小时累加值，如果无则取小时累加值和两个8点差值中的最小值
                        //*******************************************需要修改*******************************************
                        string strForSumTemp = "select * from RawData where DT>='" + theBegDT.AddHours(-1).ToString() + " ' AND DT<'" + theEndDT.ToString() + " ' and stcd='" + stcdForCal + "'";
                        //TODO
                        DataTable dtForDaySumTemp = ExecuteDatatable(sqlConStr, strForSumTemp);
                        //*******************************************需要修改*******************************************

                        int countP = 0;
                        int sumdtForDaySumTempRows = dayDT.Rows.Count;
                        for (int j = 0; j < sumdtForDaySumTempRows; j++)
                        {
                            if (Math.Abs(Decimal.Parse(dtForDaySumTemp.Rows[j + 1]["P"].ToString()) - Decimal.Parse(dtForDaySumTemp.Rows[j]["P"].ToString())) > 0.05m)
                            {
                                hourPNums += 1;
                            }
                            if (dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PP" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "PE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "ZE" || dtForDaySumTemp.Rows[j]["ACT"].ToString().Trim() == "RE")
                            {
                                countP = countP + 1;
                            }
                        }

                        //雨量和蒸发值，明天8点减今天8点值，而不是累加值
                        if (countP == 0)
                        {
                            //*******************************************需要修改*******************************************
                            string strForSum2 = "SELECT a.E-b.E as E, b.P-a.P as P FROM [dbo].[RawData] as a,[dbo].[RawData] as b where a.DT='" + theBegDT.AddHours(-1).ToString() + " ' AND b.DT = '" + theEndDT.AddHours(-1).ToString() + " ' and a.stcd='" + stcdForCal + "'";
                            //TODO
                            DataTable dtForDaySum2 = ExecuteDatatable(sqlConStr, strForSum2);
                            //*******************************************需要修改*******************************************

                            double E1 = 0.0d;
                            double P2 = 0.0d;
                            double E2 = 0.0d;
                            if (dtForDaySum2.Rows.Count > 0)  //如果取得到昨天八点到今天八点的值
                            {
                                E1 = Convert.ToDouble(dayDT.Rows[0]["E"]);
                                P2 = Convert.ToDouble(dtForDaySum2.Rows[0]["P"]);
                                if (P2 < 0.03d)
                                {
                                    P2 = 0.0d;
                                }
                                E2 = Convert.ToDouble(dtForDaySum2.Rows[0]["E"]) + P2;

                                if (E2 <= 0)
                                {
                                    dayDT.Rows[0]["E"] = E1;
                                }
                                else
                                {
                                    dayDT.Rows[0]["E"] = Math.Min(E1, E2);
                                }
                            }
                        }

                        decimal tempP = Decimal.Parse(dayDT.Rows[0]["P"].ToString());

                        //*****************日降雨补偿********************************
                        //判断日降雨的小时数，小于3不补偿，最大为13
                        if (hourPNums > 3 && hourPNums <= 13)
                        {
                            tempP = Decimal.Parse(dayDT.Rows[0]["P"].ToString()) + Convert.ToDecimal(rawDataNew[11]) * 0.05m * (hourPNums - 3);
                        }


                        //==================写入日累积值，并触发网页程序=====================
                        //构造dayData数组
                        dayData[0] = dayDT.Rows[0]["STCD"].ToString();
                        dayData[1] = theKeyDT1.AddDays(-1).ToShortDateString();
                        dayData[2] = dayDT.Rows[0]["E"].ToString();  //真实蒸发，考虑容器的换算？
                        dayData[3] = tempP.ToString();
                        dayData[4] = dayDT.Rows[0]["T"].ToString();
                        dayData[5] = "";



                        evaDic.Add("dayE", dayData[2]);
                        evaDic.Add("dayP", dayData[3]);
                        evaDic.Add("dayT", dayData[4]);
                        #endregion
                    }
                    else
                    {
                        evaDic.Add("dayE", "");
                        evaDic.Add("dayP", "");
                        evaDic.Add("dayT", "");
                    }

                    #endregion
                }
            }
            #endregion
            return evaDic;
        }

        private DataTable ExecuteDatatable(string[] myConnection, string sql, params SqlParameter[] parameters)
        {
            //string connStr = string.Empty;
            //connStr = "Server=" + myConnection[0] + ";DataBase=" + myConnection[1] + ";Uid=" + myConnection[2] + ";Pwd=" + myConnection[3];
            //SqlConnection conn = new SqlConnection(connStr);
            //conn.Open();
            //SqlCommand cmd = new SqlCommand(sql);
            //SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            //DataSet dataSet = new DataSet();
            //adapter.Fill(dataSet);
            //return dataSet.Tables[0];

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
