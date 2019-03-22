using Hydrology.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydrology.DBManager.Interface
{
    public interface ICurrentEva : IMultiThread
    { /// <summary>
      /// 异步添加记录
      /// </summary>
      /// <param name="rain"></param>
        void AddNewRow(CEntityRealEva eva);

        /// <summary>
        /// 异步添加新的记录
        /// </summary>
       // /// <param name="rains"></param>
        void AddNewRows(List<CEntityRealEva> evas);

        List<CEntityRealEva> QueryAll();
    }
}
