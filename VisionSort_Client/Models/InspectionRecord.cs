using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionSort_Client.Models
{
    public class InspectionRecord
    {
        public int RecordID { get; set; }
        public string ProductCode { get; set; }
        public string Result { get; set; }
        public string DefectType { get; set; }
        public decimal Confidence { get; set; }
        public int? DefectX { get; set; }
        public int? DefectY { get; set; }
        public int? DefectW { get; set; }
        public int? DefectH { get; set; }
        public string ImageSavePath { get; set; }
        public DateTime InspTime { get; set; }
    }
}
