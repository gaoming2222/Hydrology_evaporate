using Entity;
using Hydrology.DBManager.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBManager.Interface
{
    public interface IStationCorrsProxy : IMultiThread
    {
        List<StationCorrs> QueryA();
    }
}
