using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionSort_Client.Services
{
    public interface ICameraService : IDisposable
    {
        bool IsOpened { get; }
        void Open(int deviceIndex = 0);
        void Close();
        Bitmap CaptureFrame(); // 捕获一帧为 Bitmap
    }
}
