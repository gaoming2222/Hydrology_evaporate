using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Hydrology.CControls;
using Hydrology.Entity;
using Hydrology.DataMgr;
using System.Globalization;
using System.IO;
using Hydrology.DBManager.Interface;
using Hydrology.DBManager.DB.SQLServer;
using Entity;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;

namespace Hydrology.Forms
{
    public partial class TextForm : Form
    {
        #region 静态常量
        private static readonly string CS_Subcenter_All = "所有分中心";
        #endregion 静态常量

        #region 方法变量
        private IWaterProxy m_proxyWater;
        private IRainProxy m_proxyRain;
        #endregion

        public TextForm()
        {
            m_proxyWater = new CSQLWater();
            m_proxyRain = new CSQLRain();
            InitializeComponent();
            InitUI();
        }

        private void InitUI()
        {
            this.SuspendLayout();
            this.checkBox1.CheckedChanged += new EventHandler(EHCheckAllChanged);
            InitDate();
            InitSubCenter();
            InitModels();
        }
        // 获取表格类型
        private string getType()
        {
            string result = "";
            //foreach (var item in TableType.Controls)
            //{
            //    if (item is RadioButton)
            //    {
            //        RadioButton radioButton = item as RadioButton;
            //        if (radioButton.Checked)
            //        {
            //            result = radioButton.Text.Trim();
            //        }
            //    }
            //}
            return result;
        }
        private void InitDate()
        {
            //DateTimer.CustomFormat = "yyyy年MM月";
            ExldateTime.Value = DateTime.Now;
        }
        /// <summary>
        /// 初始化分中心信息
        /// </summary>
        private void InitSubCenter()
        {
            // 初始化分中心
            List<CEntitySubCenter> subcenter = CDBDataMgr.Instance.GetAllSubCenter();
            SubCenter.Items.Add(CS_Subcenter_All);
            for (int i = 0; i < subcenter.Count; ++i)
            {
                SubCenter.Items.Add(subcenter[i].SubCenterName);
            }
            this.SubCenter.SelectedIndex = 0;
            List<CEntityStation> iniStations = getStations(SubCenter.Text);
            List<CEntitySoilStation> iniSoilStations = getSoilStations(SubCenter.Text);
            InitStations(iniStations);
            InitSoilStations(iniSoilStations);

        }

        private void InitModels()
        {
            List<String> modelsName = new List<string>();
            modelsName = getNameInFile(@"models");
            for (int i = 0; i < modelsName.Count; ++i)
            {
                models.Items.Add(modelsName[i].Split('\\')[1]);
            }
            this.models.SelectedIndex = 0;
        }
        private List<CEntityStation> getStations(string subCenter)
        {
            List<CEntityStation> stations = new List<CEntityStation>();
            if (SubCenter.Text == CS_Subcenter_All)
            {
                // 统计所有站点的畅通率
                stations = CDBDataMgr.Instance.GetAllStation();
            }
            else
            {
                // 统计某个分中心的畅通率
                List<CEntityStation> stationsAll = CDBDataMgr.Instance.GetAllStation();
                CEntitySubCenter centerName = CDBDataMgr.Instance.GetSubCenterByName(subCenter);
                for (int i = 0; i < stationsAll.Count; ++i)
                {
                    if (stationsAll[i].SubCenterID == centerName.SubCenterID)
                    {
                        // 该测站在分中心内，添加到要查询的列表中
                        stations.Add(stationsAll[i]);
                    }
                }
            }
            return stations;
        }
        private List<CEntitySoilStation> getSoilStations(string subCenter)
        {
            List<CEntitySoilStation> stations = new List<CEntitySoilStation>();
            if (SubCenter.Text == CS_Subcenter_All)
            {
                // 统计所有的站点
                stations = CDBSoilDataMgr.Instance.GetAllSoilStation();
            }
            else
            {
                // 统计某个中心下的所有站点
                List<CEntitySoilStation> stationsAll = CDBSoilDataMgr.Instance.GetAllSoilStation();
                CEntitySubCenter centerName = CDBDataMgr.Instance.GetSubCenterByName(subCenter);
                for (int i = 0; i < stationsAll.Count; ++i)
                {
                    if (stationsAll[i].SubCenterID == centerName.SubCenterID)
                    {
                        // 该测站在分中心内，添加到要查询的列表中
                        stations.Add(stationsAll[i]);
                    }
                }
            }
            return stations;
        }
        private void InitStations(List<CEntityStation> stations)
        {
            this.StationSelect.Items.Clear();
            //this.StationSelect.Items.Add("所有站点");
            for (int i = 0; i < stations.Count; i++)
            {
                this.StationSelect.Items.Add(stations[i].StationID + "|" + stations[i].StationName);
            }
        }
        private void InitSoilStations(List<CEntitySoilStation> stations)
        {
            if (stations.Count > 0)
            {
                //this.stationSelect.Items.Clear();
                for (int i = 0; i < stations.Count; i++)
                {
                    this.StationSelect.Items.Add(stations[i].StationID + "|" + stations[i].StationName);
                }
                if (this.StationSelect.Items.Count > 0)
                {
                    this.StationSelect.SetItemChecked(0, true);
                }
            }
        }

        private void search_Click(object sender, EventArgs e)
        {
            //获取 radioButton 的table类型值；
            string tableType = "";
            //foreach (var item in TableType.Controls)
            //{
            //    if (item is RadioButton)
            //    {
            //        RadioButton radioButton = item as RadioButton;
            //        if (radioButton.Checked)
            //        {
            //            tableType = radioButton.Text.Trim();
            //        }
            //    }
            //}
            // 获取分中心
            string subcenterName = SubCenter.Text;
            //获取时间  date.value;
            // 获取站点信息 默认为全部站点
            List<string> stationSelected = new List<string>();

            int flagInt = 0;
            for (int i = 0; i < StationSelect.Items.Count; i++)
            {
                if (StationSelect.GetItemChecked(i))
                {
                    flagInt++;
                }
            }
            if (flagInt == 0)
            {
                for (int i = 0; i < StationSelect.Items.Count; i++)
                {
                    stationSelected.Add(StationSelect.GetItemText(StationSelect.Items[i]));
                }
            }
            else
            {
                for (int i = 0; i < StationSelect.Items.Count; i++)
                {
                    if (StationSelect.GetItemChecked(i))
                    {
                        stationSelected.Add(StationSelect.GetItemText(StationSelect.Items[i]));
                    }
                }
            }

            //DateTime dt1 = DateTimer.Value;
            //DateTime dt = dt1.Date;
            string type = getType();

            if (type.Equals("普通文本"))
            {
                CMessageBox box = new CMessageBox();
                box.MessageInfo = "正在查询数据库";
                box.ShowDialog(this);
                this.Enabled = false;
                this.Enabled = true;
                box.CloseDialog();
            }
            if (type.Equals("中澳格式"))
            {

                CMessageBox box = new CMessageBox();
                box.MessageInfo = "正在查询数据库";
                box.ShowDialog(this);
                this.Enabled = false;
                this.Enabled = true;
                box.CloseDialog();
            }
            if (type.Equals("墒    情"))
            {
                CMessageBox box = new CMessageBox();
                box.MessageInfo = "正在查询数据库";
                box.ShowDialog(this);
                this.Enabled = false;
                this.Enabled = true;
                box.CloseDialog();
            }
            this.Enabled = true;
        }

        private void export_Click(object sender, EventArgs e)
        {
            //DateTime startTimeTmp = DateTimer.Value;
            //DateTime endTimeTmp = endDateTime.Value;
            //DateTime startTime = new DateTime(startTimeTmp.Year, startTimeTmp.Month, startTimeTmp.Day, 0, 0, 0);
            //DateTime endTime = new DateTime(endTimeTmp.Year, endTimeTmp.Month, endTimeTmp.Day, 0, 0, 0);

            DateTime ExlTimeTmp = ExldateTime.Value;
            DateTime ExlTimeStrt = new DateTime(ExlTimeTmp.Year, ExlTimeTmp.Month, 1, 0, 0, 0);
            DateTime ExlTimeEnd = new DateTime(ExlTimeTmp.Year, ExlTimeTmp.Month+1, 1, 0, 0, 0);
            string dte = ExlTimeTmp.Year.ToString() + "年" + ExlTimeTmp.Month.ToString();
            string month = ExlTimeTmp.Month.ToString();

            // 获取是雨量还是水位类型
            string type = getType();
            // 获取被选择的站点
            // 获取站点信息 默认为全部站点
            List<string> stationSelected = new List<string>();
            
            int flagInt = 0;
            for (int i = 0; i < StationSelect.Items.Count; i++)
            {
                if (StationSelect.GetItemChecked(i))
                {
                    flagInt++;
                }
            }
            if (flagInt == 0)
            {
                for (int i = 0; i < StationSelect.Items.Count; i++)
                {
                    stationSelected.Add(StationSelect.GetItemText(StationSelect.Items[i]));
                }
            }
            else
            {
                for (int i = 0; i < StationSelect.Items.Count; i++)
                {
                    if (StationSelect.GetItemChecked(i))
                    {
                        stationSelected.Add(StationSelect.GetItemText(StationSelect.Items[i]));
                    }
                }
            }
            #region 导出蒸发雨量月表
            string sourcePath = @"models/" + models.Text;
            if(models.Text == "降水量、蒸发量月报表.xls")
            {
                string errMsg = "";
                for (int i = 0; i < stationSelected.Count; i++)
                {
                    string stationName = stationSelected[i].Split('|')[1].Trim();
                    string stationid = stationSelected[i].Split('|')[0].Trim() ;
                    string prefixName = stationSelected[i].Split('|')[1] + stationSelected[i].Split('|')[0] + "站" + dte.Substring(0,5) + "降水量、蒸发量月报表.xls";
                    string fileName = "ZfExcels/" + prefixName;
                     //string targetPath = @"ZfExcels/" + stationSelected[i].Split('|')[0] + stationSelected[i].Split('|')[1] + ExlTimeTmp.ToString() + "降水量、蒸发量月报表.xls";
                    string targetPath = @fileName;
                    Dictionary<String,Object> result =  getAndSetExcelValueZf(sourcePath, targetPath,stationid,stationName, dte, ExlTimeStrt, ExlTimeEnd);
                    if(result["ERR"].ToString() != "0")
                    {
                        errMsg = errMsg + result["ID"].ToString() + result["ERR"].ToString() + "/r/n";
                    }
                }
                if(errMsg != "" && errMsg.Length > 0)
                {
                    MessageBox.Show(errMsg);
                }
                else
                {
                    MessageBox.Show("Excel导出成功！");
                }

            }
            if(models.Text == "蒸发观测记录表.xls")
            {
                string errMsg = "";
                for (int i = 0; i < stationSelected.Count; i++)
                {
                    string stationName = stationSelected[i].Split('|')[1].Trim();
                    string stationid = stationSelected[i].Split('|')[0].Trim();
                    string prefixName = stationSelected[i].Split('|')[1] + stationSelected[i].Split('|')[0] + "站" + dte + "蒸发观测记录表.xls";
                    string fileName = "GcExcels/" + prefixName;
                    string targetPath = @fileName;
                    Dictionary<string, object> result = new Dictionary<string, object>();

                    result = getAndSetExcelValueGc(sourcePath, targetPath, stationid, stationName, dte, ExlTimeStrt, ExlTimeEnd);
                    if (errMsg != "" && errMsg.Length > 0)
                    {
                        MessageBox.Show(errMsg);
                    }
                    else
                    {
                        MessageBox.Show("Excel导出成功！");
                    }
                }
            }
            if (models.Text == "蒸发观测记录表.xls")
            {

            }
            if (models.Text == "蒸发观测记录表.txt")
            {
                string errMsg = "";
                for (int i = 0; i < stationSelected.Count; i++)
                {
                    DateTime strtTime = new DateTime(ExlTimeTmp.Year, 1, 2, 0, 0, 0);
                    DateTime endTime = new DateTime(ExlTimeTmp.Year+1, 1, 2, 0, 0, 0);
                    string stationName = stationSelected[i].Split('|')[1].Trim();
                    string stationid = stationSelected[i].Split('|')[0].Trim();
                    string prefixName = stationSelected[i].Split('|')[1] + stationSelected[i].Split('|')[0] + "站" + dte + "南方片年数据.txt";
                    string fileName = "ZfExcels/" + prefixName;
                    //string targetPath = @"ZfExcels/" + stationSelected[i].Split('|')[0] + stationSelected[i].Split('|')[1] + ExlTimeTmp.ToString() + "降水量、蒸发量月报表.xls";
                    string targetPath = @fileName;
                    Dictionary<String, Object> result = setYearTxtZf(sourcePath, targetPath, stationid, stationName, dte, strtTime, endTime);
                    if (result["ERR"].ToString() != "0")
                    {
                        errMsg = errMsg + result["ID"].ToString() + result["ERR"].ToString() + "/r/n";
                    }
                }
                if (errMsg != "" && errMsg.Length > 0)
                {
                    MessageBox.Show(errMsg);
                }
                else
                {
                    MessageBox.Show("Excel导出成功！");
                }
            }
            #endregion
                
        }

        private void center_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<CEntityStation> iniStations = getStations(SubCenter.Text);
            InitStations(iniStations);
            List<CEntitySoilStation> iniSoilStations = getSoilStations(SubCenter.Text);
            InitSoilStations(iniSoilStations);
        }
        private void TableTypeChanged(object sender, EventArgs e)
        {
            string type = getType();
            
        }

        private void EHCheckAllChanged(object sender, EventArgs e)
        {
            if (checkBox1.CheckState == CheckState.Checked)
            {
                // 全选
                for (int i = 0; i < StationSelect.Items.Count; i++)
                {
                    this.StationSelect.SetItemChecked(i, true);
                }

            }
            else
            {
                // 全不选
                for (int i = 0; i < StationSelect.Items.Count; i++)
                {
                    this.StationSelect.SetItemChecked(i, false);
                }

            }
        }

        #region 帮助方法
        public int getDayOfYear(DateTime date)
        {
            DateTime startDate = new DateTime(DateTime.Now.Year, 1, 1,0,0,0);
            int dayOfYear = (date - startDate).Days + 1 ;
            return dayOfYear;
        }

        public void exportTxt(List<string> list, string txtFile)
        {
            FileStream fs = new FileStream(txtFile, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            try
            {
                sw.Flush();
                // 使用StreamWriter来往文件中写入内容 
                sw.BaseStream.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < list.Count; i++)
                {
                    sw.WriteLine(list[i]);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("导出水位数据失败11");
            }
            finally
            {
                //关闭此文件t 
                sw.Flush();
                sw.Close();
                fs.Close();
            }
        }

        #endregion


        #region NPOI文件操作

        /// <summary>
        /// 读取并修改Excel的内容
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Dictionary<String,Object> getAndSetExcelValueZf(String sourcePath,String resultPaht,string stationid,string stationName,string dte, DateTime strtTime, DateTime endTime)
        {
            
            Dictionary<String, Object> result = new Dictionary<string, object>();
            result["ERR"] = "0";
            //查询获取所需数据
            List<CEntityEva> evaList = new List<CEntityEva>();
            evaList = CDBDataMgr.GetInstance().getEvaByTime(stationid, strtTime, endTime);
            if(evaList == null || evaList.Count == 0)
            {
                result["ERR"] = "数据库无数据";
                result["ID"] = stationid;
                return result;
            }
            #region 计算平均值 最大值 最小值等
            Dictionary<string, CEntityEva> exlData = new Dictionary<string, CEntityEva>();
            Decimal totalP = 0;
            Decimal totalE = 0;
            Decimal maxP = 0;
            int totalePDays = 0;
            string maxPDay = " 日";
            Decimal maxE = 0;
            string maxEDay = " 日";
            Decimal minE = 9999;
            string minEDay = " 日";

            foreach(CEntityEva eva in evaList)
            {
                exlData[eva.TimeCollect.Day.ToString()] = eva;
                if (eva.P.HasValue)
                {
                    totalP = totalP + eva.P.Value;
                    if(eva.P.Value > 0)
                    {
                        totalePDays = totalePDays + 1;
                    }
                    
                    if(eva.P.Value > maxP)
                    {
                        maxP = eva.P.Value;
                        maxPDay = eva.TimeCollect.Day.ToString() + "日";
                    }
                    
                }
                if (eva.E.HasValue)
                {
                    totalE = totalE + eva.E.Value;
                    if(eva.E.Value > maxE)
                    {
                        maxE = eva.E.Value;
                        maxEDay = eva.TimeCollect.Day.ToString() + "日";
                    }
                    if(eva.E.Value < minE)
                    {
                        minE = eva.E.Value;
                        minEDay = eva.TimeCollect.Day.ToString() + "日";
                        if(minE < 0)
                        {
                            minE = 0;
                        }
                    }
                }
            }
            #endregion

            try
            {
                FileStream fs = File.OpenRead(sourcePath);
                //把文件内容写到工作簿中，然后关闭文件
                //XSSFWorkbook workbook = new XSSFWorkbook(file);
                //HSSFWorkbook workbook = new HSSFWorkbook(file)
                IWorkbook workbook = new HSSFWorkbook(fs);
                fs.Close();
                //获取第一个sheet
                ISheet sheet = workbook.GetSheetAt(0);

                //获取蒸发日表数据
                #region 降水量、蒸发量月报表
                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    #region 表头 表尾
                    if (i == 0)
                    {
                        //A1 站名 + 时间
                        foreach (ICell cell in sheet.GetRow(i).Cells)
                        {
                            /*
                             * Excel数据Cell有不同的类型，当我们试图从一个数字类型的Cell读取出一个字符串并写入数据库时，就会出现Cannot get a text value from a numeric cell的异常错误。
                             * 解决办法：先设置Cell的类型，然后就可以把纯数字作为String类型读进来了
                             */
                            string a = cell.StringCellValue;
                            int iiii = cell.ColumnIndex;
                            if (cell.ColumnIndex == 0)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(stationName + "站" + dte);
                            }
                        }

                        continue;
                    }
                    else if (i == 1)
                    {
                        //F1 站名
                        continue;
                    }
                    else if (i == 2)
                    {
                        //F1 站名
                        continue;
                    }
                    else if (i == 3)
                    {
                        //F1 站名
                        continue;
                    }
                    else if (i == 35)
                    {
                        foreach (ICell cell in sheet.GetRow(i).Cells)
                        {
                            if (cell.ColumnIndex == 4)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(totalP.ToString());
                            }
                            if (cell.ColumnIndex == 10)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(totalE.ToString());
                            }
                        }
                        continue;
                    }
                    else if (i == 36)
                    {
                        foreach (ICell cell in sheet.GetRow(i).Cells)
                        {
                            if (cell.ColumnIndex == 4)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(totalePDays.ToString() + "天");
                            }
                            if (cell.ColumnIndex == 10)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(maxE.ToString());
                            }
                            if (cell.ColumnIndex == 12)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(maxEDay);
                            }
                        }

                        continue;
                    }
                    else if (i == 37)
                    {
                        foreach (ICell cell in sheet.GetRow(i).Cells)
                        {
                            if (cell.ColumnIndex == 4)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(maxP.ToString());
                            }
                            if (cell.ColumnIndex == 6)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(maxPDay);
                            }
                            if (cell.ColumnIndex == 10)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(minE.ToString());
                            }
                            if (cell.ColumnIndex == 12)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(minEDay);
                            }
                        }
                        continue;
                    }
                    else if (i == 38)
                    {

                        continue;
                    }
                    #endregion
                    //如果包含该日8：00的数据，则取出
                    CEntityEva tmp = new CEntityEva();
                    if (exlData.ContainsKey((i - 3).ToString()))
                    {
                        tmp = exlData[(i - 3).ToString()];
                    }
                    else
                    {
                        continue;
                    }
                    #region 蒸发数据
                    foreach (ICell cell in sheet.GetRow(i).Cells)
                    {
                        /*
                         * Excel数据Cell有不同的类型，当我们试图从一个数字类型的Cell读取出一个字符串并写入数据库时，就会出现Cannot get a text value from a numeric cell的异常错误。
                         * 解决办法：先设置Cell的类型，然后就可以把纯数字作为String类型读进来了
                         */
                        if (cell.ColumnIndex == 1)
                        {
                            if(tmp.P20 != null && tmp.P20.ToString() != "")
                            {
                                if(tmp.P20.Value > 0)
                                {
                                    cell.SetCellType(CellType.String);
                                    cell.SetCellValue(tmp.P20.Value.ToString("0.0"));
                                }
                                
                            }
                            
                        }
                        if (cell.ColumnIndex == 5)
                        {
                            if (tmp.P8 != null && tmp.P8.ToString() != "")
                            {
                                if(tmp.P8.Value > 0)
                                {
                                    cell.SetCellType(CellType.String);
                                    cell.SetCellValue(tmp.P8.Value.ToString("0.0"));
                                }
                                
                            }
                            
                        }
                        if (cell.ColumnIndex == 7)
                        {
                            if(tmp.P !=  null && tmp.P.ToString() != "")
                            {
                                if(tmp.P > 0)
                                {
                                    cell.SetCellType(CellType.String);
                                    cell.SetCellValue(tmp.P.Value.ToString("0.0"));
                                }
                            }
                            
                        }
                        if (cell.ColumnIndex == 10)
                        {
                            if(tmp.E != null && tmp.E.ToString() != "")
                            {
                                if(tmp.E > 0)
                                {
                                    cell.SetCellType(CellType.String);
                                    cell.SetCellValue(tmp.E.Value.ToString("0.0"));
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion
                FileStream fs2 = File.Create(resultPaht);
                workbook.Write(fs2);
                fs2.Close();
                
            }catch(Exception e)
            {
                result["ERR"] = e.Message;
                result["ID"] = stationid;
                return result;
            }
            return result;

        }

        /// <summary>
        /// 观测月表导出
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="resultPaht"></param>
        /// <param name="stationid"></param>
        /// <param name="stationName"></param>
        /// <param name="dte"></param>
        /// <param name="strtTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public Dictionary<String, Object> getAndSetExcelValueGc(String sourcePath, String resultPaht, string stationid, string stationName, string dte, DateTime strtTime, DateTime endTime)
        {
            Dictionary<String, Object> result = new Dictionary<string, object>();
            result["ERR"] = "0";
            Dictionary<string, CEntityEva> dataDic = new Dictionary<string, CEntityEva>();
            decimal totalEva = 0;
            decimal maxEva = 0;
            decimal minEva = 100;
            DateTime maxDate = DateTime.Now;
            DateTime minDate = DateTime.Now;
            try
            {
                //1.根据站点ID获取站点获取站点基本信息
                CEntityStation station = CDBDataMgr.GetInstance().GetStationById(stationid);
                
                //2.查询原始数据、小时数据、日数据
                endTime = endTime.AddDays(1);
                //2.1查询获取小时数据
                List<CEntityEva> hourEvaList = new List<CEntityEva>();
                hourEvaList = CDBDataMgr.GetInstance().QueryForHourEvaList4Table(stationid, strtTime, endTime);

                //2.2查询日数据
                List<CEntityEva> evaList = new List<CEntityEva>();
                evaList = CDBDataMgr.GetInstance().getEvaByTime(stationid, strtTime, endTime);

                //2.3查询原始数据
                List<CEntityEva> evaSpList = new List<CEntityEva>();
                evaSpList = CDBDataMgr.GetInstance().getSpEvaByTime(stationid, strtTime, endTime);

                if (evaList == null || evaList.Count == 0)
                {
                    result["ERR"] = "数据库无数据";
                    result["ID"] = stationid;
                    return result;
                }
                //3 组合数据 计算数据
                
                for (int i = 1; i <= 31; i++)
                {
                    CEntityEva tmp = new CEntityEva();
                    //8点小时数据
                    foreach (CEntityEva item in hourEvaList)
                    {
                        if (item.TimeCollect.Day == i)
                        {
                            tmp.StationID = item.StationID;
                            tmp.TimeCollect = item.TimeCollect;
                            tmp.DH = item.DH;
                            break;
                        }
                    }
                    //日数据
                    foreach (CEntityEva item in evaList)
                    {

                        if (item.TimeCollect.AddDays(-1).Day == i)
                        {
                            totalEva = totalEva + item.E.Value;
                            if (item.E.Value > maxEva)
                            {
                                maxEva = item.E.Value;
                                maxDate = item.TimeCollect;
                            }
                            if (item.E.Value < minEva)
                            {
                                minEva = item.E.Value;
                                minDate = item.TimeCollect;
                            }
                            tmp.E = item.E;
                            tmp.P = item.P;
                            tmp.dayEChange = item.dayEChange;
                        }
                    }
                    //原始数据
                    if (evaSpList != null && evaSpList.Count > 0)
                    {
                        foreach (CEntityEva item in evaSpList)
                        {

                            if (item.TimeCollect.Day == i)
                            {
                                tmp.act = item.act;
                            }
                        }
                    }

                    dataDic[i.ToString()] = tmp;
                }
            }catch(Exception ee)
            {
                result["ERR"] = ee.Message;
            }
            
            
            //写入数据库
            try
            {
                FileStream fs = File.OpenRead(sourcePath);
                //把文件内容写到工作簿中，然后关闭文件
                //XSSFWorkbook workbook = new XSSFWorkbook(file);
                //HSSFWorkbook workbook = new HSSFWorkbook(file)
                IWorkbook workbook = new HSSFWorkbook(fs);
                fs.Close();
                //获取第一个sheet
                ISheet sheet = workbook.GetSheetAt(0);

                //获取蒸发日表数据
                #region 降水量、蒸发量月报表
                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    if (i == 0)
                    {
                        //A1 站名 + 时间
                        foreach (ICell cell in sheet.GetRow(i).Cells)
                        {
                            /*
                             * Excel数据Cell有不同的类型，当我们试图从一个数字类型的Cell读取出一个字符串并写入数据库时，就会出现Cannot get a text value from a numeric cell的异常错误。
                             * 解决办法：先设置Cell的类型，然后就可以把纯数字作为String类型读进来了
                             */
                            if (cell.ColumnIndex == 5)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(stationName + "站");
                            }
                        }
                        continue;
                    }

                    if (i == 1)
                    {
                        //A1 站名 + 时间
                        foreach (ICell cell in sheet.GetRow(i).Cells)
                        {
                            /*
                             * Excel数据Cell有不同的类型，当我们试图从一个数字类型的Cell读取出一个字符串并写入数据库时，就会出现Cannot get a text value from a numeric cell的异常错误。
                             * 解决办法：先设置Cell的类型，然后就可以把纯数字作为String类型读进来了
                             */
                            
                            if (cell.ColumnIndex == 13)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(dte + "月");
                            }
                        }
                        continue;
                    }
                    for(int j = 5; j <= 35; j++)
                    {
                        if (i == j)
                        {
                            CEntityEva evaData = dataDic[(j - 4).ToString()];
                            foreach (ICell cell in sheet.GetRow(i).Cells)
                            {
                                //填写8：00
                                if (cell.ColumnIndex == 1)
                                {
                                    cell.SetCellType(CellType.String);
                                    cell.SetCellValue("08:00");
                                }

                                //填写加水前
                                if (cell.ColumnIndex == 2)
                                {
                                    if (evaData.DH != null && evaData.DH.ToString() != "")
                                    {
                                        cell.SetCellType(CellType.String);
                                        cell.SetCellValue(evaData.DH.Value.ToString("0.0"));
                                    }
                                }
                                if (cell.ColumnIndex == 3)
                                {
                                    if (evaData.DH != null && evaData.DH.ToString() != "")
                                    {
                                        cell.SetCellType(CellType.String);
                                        cell.SetCellValue(evaData.DH.Value.ToString("0.0"));
                                    }
                                }
                                if (cell.ColumnIndex == 4)
                                {
                                    if (evaData.DH != null && evaData.DH.ToString() != "")
                                    {
                                        cell.SetCellType(CellType.String);
                                        cell.SetCellValue(evaData.DH.Value.ToString("0.0"));
                                    }
                                }
                                //填写加水后
                                if (cell.ColumnIndex == 5)
                                {
                                    if (evaData.act != null && evaData.act.ToString() != "")
                                    {
                                        if (evaData.act.Trim().StartsWith("ER"))
                                        {
                                            cell.SetCellType(CellType.String);
                                            cell.SetCellValue(evaData.act.ToString().Substring(2));
                                        }
                                        
                                    }
                                }

                                if (cell.ColumnIndex == 6)
                                {
                                    if (evaData.act != null && evaData.act.ToString() != "")
                                    {
                                        if (evaData.act.Trim().StartsWith("ER"))
                                        {
                                            cell.SetCellType(CellType.String);
                                            cell.SetCellValue(evaData.act.ToString().Substring(2));
                                        }
                                    }
                                }
                                if (cell.ColumnIndex == 7)
                                {
                                    if (evaData.act != null && evaData.act.ToString() != "")
                                    {
                                        if (evaData.act.Trim().StartsWith("ER"))
                                        {
                                            cell.SetCellType(CellType.String);
                                            cell.SetCellValue(evaData.act.ToString().Substring(2));
                                        }
                                        
                                    }
                                }
                                if(cell.ColumnIndex == 10)
                                {
                                    if (evaData.dayEChange != null && evaData.dayEChange.ToString() != "")
                                    {
                                        cell.SetCellType(CellType.String);
                                        cell.SetCellValue(evaData.dayEChange.Value.ToString("0.0"));
                                    }
                                }

                                if (cell.ColumnIndex == 12)
                                {
                                    if (evaData.P != null && evaData.P.ToString() != "" && evaData.P > 0)
                                    {
                                        cell.SetCellType(CellType.String);
                                        cell.SetCellValue(evaData.P.Value.ToString("0.0"));
                                    }
                                }

                                if (cell.ColumnIndex == 13)
                                {
                                    if (evaData.E != null && evaData.E.ToString() != "" && evaData.E > 0)
                                    {
                                        cell.SetCellType(CellType.String);
                                        cell.SetCellValue(evaData.E.Value.ToString("0.0"));
                                    }
                                }

                                if (cell.ColumnIndex == 14)
                                {
                                    if (evaData.act != null && evaData.act.ToString() != "")
                                    {
                                        if (evaData.act.Contains("ER"))
                                        {
                                            cell.SetCellType(CellType.String);
                                            cell.SetCellValue(evaData.dayEChange.Value.ToString("0.0"));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (i == 36)
                    {
                        //A1 站名 + 时间
                        foreach (ICell cell in sheet.GetRow(i).Cells)
                        {
                            if (cell.ColumnIndex == 6)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(totalEva.ToString());
                            }
                            if (cell.ColumnIndex == 10)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(maxEva.ToString() + "  " + maxDate.Day.ToString() + "日");
                            }
                            if (cell.ColumnIndex == 14)
                            {
                                cell.SetCellType(CellType.String);
                                cell.SetCellValue(minEva.ToString() + "  " + minDate.Day.ToString() + "日");
                            }
                        }
                        continue;
                    }

                }
                #endregion
                FileStream fs2 = File.Create(resultPaht);
                workbook.Write(fs2);
                fs2.Close();

            }
            catch (Exception e)
            {
                result["ERR"] = e.Message;
                result["ID"] = stationid;
                return result;
            }
            return result;

        }


        public Dictionary<String, Object> setYearTxtZf(String sourcePath, String resultPaht, string stationid, string stationName, string dte, DateTime strtTime, DateTime endTime)
        {
            Dictionary<String, Object> result = new Dictionary<string, object>();
            result["ERR"] = "0";
            //查询获取所需数据
            List<CEntityEva> evaList = new List<CEntityEva>();
            evaList = CDBDataMgr.GetInstance().getEvaByTime(stationid, strtTime, endTime);
            if (evaList == null || evaList.Count == 0)
            {
                result["ERR"] = "数据库无数据";
                result["ID"] = stationid;
                return result;
            }
            FileStream fs;
            StreamWriter sw;
            try
            {
                
                //1.判断文件是否存在，如果存在则直接写入，不存在则先创建文件
                if (!System.IO.File.Exists(resultPaht))
                {
                    //没有则创建这个文件
                    fs = new FileStream(resultPaht, FileMode.Create, FileAccess.Write);//创建写入文件                //设置文件属性为隐藏
                    sw = new StreamWriter(fs);
                }
                else
                {
                    fs = new FileStream(resultPaht, FileMode.Open, FileAccess.Write);
                    sw = new StreamWriter(fs);
                }
                //2.遍历获取到的蒸发数据
                Dictionary<int, CEntityEva> evaDic = new Dictionary<int, CEntityEva>();
                List<CEntityEva> allData = new List<CEntityEva>();
                DateTime time1 = new DateTime(strtTime.Year, 1, 2, 8, 0, 0);
                DateTime time2 = new DateTime(strtTime.Year + 1, 1, 1, 8, 0, 0);
                //2.1将list放入Dic,方便查找
                for (int i = 0;i< evaList.Count; i++)
                {
                    
                    evaDic.Add((evaList[i].TimeCollect.Month * 31 + evaList[i].TimeCollect.Day), evaList[i]);
                }
                //变量1-31日，变量1-12月，在DIC中查找相关值
                for(int day = 1;day <= 31; day++)
                {
                    StringBuilder data = new StringBuilder( day.ToString() + ",");
                    for(int month = 1;month <= 12; month++)
                    {
                        if(evaDic.ContainsKey(month * 31 + day) && evaDic[month * 31 + day].E.HasValue)
                        {
                            data.Append(evaDic[month * 31 + day].E.Value.ToString("0.0"));
                        }
                        data.Append(",,");
                    }
                    data.Append(",");
                    sw.WriteLine(data.ToString());//开始写入值
                }
                
                //2.3
                sw.Close();
                fs.Close();
            }
            catch(Exception e)
            {

            }
            return result;
        }

        public List<String> getNameInFile(String sourcePath)
        {
            List<String> result = new List<string>();
            string[] files = Directory.GetFiles(sourcePath, "*.*");
            foreach(String name in files)
            {
                result.Add(name);
            }
            return result;
        }

        #endregion



    }


    
}
