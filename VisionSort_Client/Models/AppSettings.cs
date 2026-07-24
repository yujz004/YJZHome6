using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionSort_Client.Models
{
    public class AppSettings
    {
        public int CameraIndex { get; set; } = 0;
        public double DetectThreshold { get; set; } = 0.05;
        public string PythonServerIP { get; set; } = "127.0.0.1";
        public int PythonServerPort { get; set; } = 8888;
    }
}
