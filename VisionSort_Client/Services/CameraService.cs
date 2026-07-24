using OpenCvSharp;
using OpenCvSharp.Extensions;  // 新增：为了使用 ToBitmap()
using System;
using System.Drawing;

namespace VisionSort_Client.Services
{
    public class CameraService : ICameraService
    {
        private VideoCapture? _capture;   // 可空类型
        private Mat _frame = new Mat();

        public bool IsOpened => _capture?.IsOpened() ?? false;

        public void Open(int deviceIndex = 0)
        {
            if (_capture != null) Close();
            _capture = new VideoCapture(deviceIndex);
            if (!_capture.IsOpened())
                throw new Exception("无法打开摄像头，请检查设备连接");
        }

        public void Close()
        {
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
        }

        public Bitmap? CaptureFrame()   // 允许返回 null
        {
            if (!IsOpened) throw new Exception("摄像头未打开");
            if (_capture.Read(_frame))
            {
                // 检查图像是否有效
                if (_frame.Rows == 0 || _frame.Cols == 0)
                    return null;

                // 直接转换为 System.Drawing.Bitmap（依赖扩展包）
                return _frame.ToBitmap();
            }
            return null;
        }

        public void Dispose() => Close();
    }
}