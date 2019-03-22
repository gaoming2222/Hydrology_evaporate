﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydrology.Entity
{
    public class CEntityRealEva
    {
        public CEntityRealEva()
        {
            this.StationType = EStationType.EHydrology; //默认是水文站
            this.ERTDState = ERTDDataState.ENormal; //默认正常
            this.EIChannelType = EChannelType.GPRS; //默认是GPRS
        }

        /// <summary>
        /// 站点编号
        /// </summary>
        public string StrStationID { get; set; }

        /// <summary>
        /// 站点名字
        /// </summary>
        public string StrStationName { get; set; }

        /// <summary>
        /// 站点类型
        /// </summary>
        public EStationType StationType { get; set; }

        /// <summary>
        /// 接收时间
        /// </summary>
        public DateTime TimeReceived { get; set; }

        /// <summary>
        /// 设备采集时间
        /// </summary>
        public DateTime TimeDeviceGained { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public Nullable<Decimal> Temperature { get; set; }

        /// <summary>
        /// 雨量
        /// </summary>
        public Nullable<Decimal> Rain { get; set; }

        /// <summary>
        /// 电压
        /// </summary>
        public Nullable<Decimal> Voltage { get; set; }

        /// <summary>
        /// 蒸发
        /// </summary>
        public Nullable<Decimal> Eva { get; set; }

        /// <summary>
        /// 接收信道
        /// </summary>
        public EChannelType EIChannelType { get; set; }

        /// <summary>
        /// 实时数据的状态，用来显示颜色
        /// </summary>
        public ERTDDataState ERTDState { get; set; }
    }
}
