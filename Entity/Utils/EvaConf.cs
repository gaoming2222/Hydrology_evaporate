using System.Xml.Serialization;

namespace Hydrology.Entity.Utils
{
    [XmlRoot("Root")]
    public class EvaConf
    {
        public EvaConf()
        {
            Kp = 1;
            Ke = 1;
            Dh = 0;
            ComP = false;
        }

        public EvaConf(decimal kp, decimal ke, decimal dh, bool comP)
        {
            Kp = kp;
            Ke = ke;
            Dh = dh;
            ComP = comP;
        }

        // 降雨转换系数
        [XmlElement("kp")]
        private static decimal kp;

        // 降雨转换系数
        [XmlElement("ke")]
        private static decimal ke;

        // 降雨转换系数
        [XmlElement("dh")]
        private static decimal dh;

        // 降雨转换系数
        [XmlElement("comP")]
        private static bool comP;

        public static decimal Kp { get => kp; set => kp = value; }
        public static decimal Ke { get => ke; set => ke = value; }
        public static decimal Dh { get => dh; set => dh = value; }
        public static bool ComP { get => comP; set => comP = value; }
    }
}
