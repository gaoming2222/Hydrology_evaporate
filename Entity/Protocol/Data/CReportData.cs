using System;
using System.Text;

namespace Hydrology.Entity
{
    public class CReportData
    {
        /// <summary>
        /// 数据采集时间
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 水量
        /// </summary>
        public Nullable<Decimal> Water { get; set; }
        /// <summary>
        /// 雨量
        /// </summary>
        public Nullable<Decimal> Rain { get; set; }
        /// <summary>
        /// 电压
        /// </summary>
        public Nullable<Decimal> Voltge { get; set; }
        /// <summary>
        /// 蒸发
        /// </summary>
        public Nullable<Decimal> Evp { get; set; }
        /// <summary>
        /// 蒸发类型
        /// </summary>
        public string EvpType { get; set; }
        /// <summary>
        /// 温度
        /// </summary>
        public Nullable<Decimal> Temperature { get; set; }
    }
}
