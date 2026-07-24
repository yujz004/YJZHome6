using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionSort_Client.Models
{
    public class DetectionResult
    {
        public string Status { get; set; }        // "success" / "error"
        public string Result { get; set; }        // "OK" / "NG"
        public double Confidence { get; set; }
        public string DefectType { get; set; }
        public Location Location { get; set; }
        public string Timestamp { get; set; }
    }

    public class Location
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }
}
