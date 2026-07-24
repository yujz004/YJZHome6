using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionSort_Client.Models;

namespace VisionSort_Client.Services
{
    public interface ICommunicationService
    {
        Task ConnectAsync(string ip, int port);
        Task DisconnectAsync();
        Task<DetectionResult> SendImageAsync(byte[] imageData, string productCode, double threshold);

        bool IsConnected { get; }
    }
}
