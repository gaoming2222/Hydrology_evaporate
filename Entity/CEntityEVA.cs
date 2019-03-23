using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydrology.Entity
{
    public class CEntityEva
    {
        #region PROPERTY
        
        /// <summary>
        ///  测站中心的ID
        /// </summary>
        public string StationID { get; set; }

        /// <summary>
        ///  数据值的采集时间
        /// </summary>
        public DateTime TimeCollect { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public Nullable<Decimal> Temperature { get; set; }

        /// <summary>
        /// 电压
        /// </summary>
        public Nullable<Decimal> Voltage { get; set; }

        /// <summary>
        /// 蒸发值
        /// </summary>
        public Nullable<Decimal> Eva { get; set; }

        /// <summary>
        /// 雨量
        /// </summary>
        public Nullable<Decimal> Rain { get; set; }

        /// <summary>
        /// 蒸发模式
        /// </summary>
        public string type { get; set; }

        #endregion
    }
}
