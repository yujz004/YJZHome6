using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VisionSort_Client.Models;

namespace VisionSort_Client.Services
{
    public class TcpCommunicationService : ICommunicationService
    {
        private TcpClient _client;
        private NetworkStream _stream;

        // 是否已连接（供界面判断用）
        public bool IsConnected => _client?.Connected ?? false;

        // ---------- 连接服务器 ----------
        public async Task ConnectAsync(string ip, int port)
        {
            // 如果之前有连接，先断开
            DisconnectAsync().Wait();

            _client = new TcpClient();
            // 尝试连接（这里用异步，不卡界面）
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();

            Console.WriteLine($"[TCP] 成功连接到 {ip}:{port}");
        }

        // ---------- 断开连接 ----------
        public async Task DisconnectAsync()
        {
            if (_stream != null)
            {
                await _stream.DisposeAsync();
                _stream = null;
            }
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
            Console.WriteLine("[TCP] 已断开连接");
        }

        // ---------- 发送图片并接收检测结果（核心方法） ----------
        public async Task<DetectionResult> SendImageAsync(byte[] imageData, string productCode, double threshold)
        {
            if (!IsConnected)
            {
                throw new Exception("未连接到服务器，请先点击连接！");
            }

            // 1. 把图片字节 (byte[]) 转换成 Base64 字符串
            string imageBase64 = Convert.ToBase64String(imageData);

            // 2. 构建要发送的 JSON 对象
            var requestObj = new
            {
                cmd = "detect",
                image_base64 = imageBase64,
                product_code = productCode,
                threshold = threshold   // 新增这一行
            };
            string jsonString = JsonSerializer.Serialize(requestObj);

            // 3. 把 JSON 字符串转成字节（UTF-8），末尾加换行符 \n
            byte[] sendBytes = Encoding.UTF8.GetBytes(jsonString + "\n");

            // 4. 通过 NetworkStream 发送出去
            await _stream.WriteAsync(sendBytes, 0, sendBytes.Length);
            await _stream.FlushAsync();

            Console.WriteLine("[TCP] 已发送检测请求，等待 Python 回复...");

            // 5. 接收 Python 返回的数据
            byte[] buffer = new byte[8192];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine($"[TCP] 收到回复: {responseJson}");

            // 6. 把收到的 JSON 字符串解析成我们的 DetectionResult 对象
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            DetectionResult result = JsonSerializer.Deserialize<DetectionResult>(responseJson, options);

            return result;
        }
    }
}