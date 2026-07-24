using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VisionSort_Client.Helpers;
using VisionSort_Client.Models;
using VisionSort_Client.Services;


namespace VisionSort_Client.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {


        // ---------- 声明事件（这是让界面刷新的关键） ----------
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        // ---------- 服务引用（我们暂时用模拟的，不用连真实的Python） ----------
        private readonly ICommunicationService _commService;

        private string _serverIP = "127.0.0.1";
        private int _serverPort = 8888;

        private ICameraService _cameraService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPreviewRunning = false;

        private readonly IDataService _dataService;

        // 产品显示名称 与 内部英文编号 的映射
        private readonly Dictionary<string, string> _productCodeMap = new Dictionary<string, string>
        {
            { "产品A", "ProductA" },
            { "产品B", "ProductB" },
            { "产品C", "ProductC" }
        };



        // ---------- 构造函数（项目启动时自动调用） ----------
        public MainWindowViewModel()
        {

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();

                _serverIP = config["TcpSettings:PythonServerIP"] ?? "127.0.0.1";
                _serverPort = int.Parse(config["TcpSettings:PythonServerPort"] ?? "8888");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配置文件失败，使用默认值: {ex.Message}");
            }

            _commService = new TcpCommunicationService();
            _cameraService = new CameraService();
            _dataService = new SqlDataService();  // 新增
            ConnectCommand = new RelayCommand(async (p) => await ExecuteConnectAsync(p));
            DetectCommand = new RelayCommand(async (p) => await ExecuteDetectAsync(p));

            // 先 new 一个通信服务（虽然是假的，但我们后面让它返回假数据）
            _commService = new TcpCommunicationService();
            _cameraService = new CameraService();

            // 把“连接”和“检测”两个按钮的命令绑定到下面的方法上
            ConnectCommand = new RelayCommand(async (p) => await ExecuteConnectAsync(p));
            DetectCommand = new RelayCommand(async (p) => await ExecuteDetectAsync(p));

           
        }

        // ---------- 界面按钮绑定的命令（供 XAML 调用） ----------
        public ICommand ConnectCommand { get; }
        public ICommand DetectCommand { get; }

        // ---------- 界面显示的属性（会实时反映到界面） ----------

        private BitmapSource _currentFrame;
        public BitmapSource CurrentFrame
        {
            get => _currentFrame;
            set { _currentFrame = value; OnPropertyChanged(); }
        }

        private string _statusText = "⚪ 未连接";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private string _selectedProductCode = "产品A";
        public string SelectedProductCode
        {
            get => _selectedProductCode;
            set { _selectedProductCode = value; OnPropertyChanged(); }
        }

        private int _totalCount = 0;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
        }

        private int _okCount = 0;
        public int OKCount
        {
            get => _okCount;
            set { _okCount = value; OnPropertyChanged(); }
        }

        private int _ngCount = 0;
        public int NGCount
        {
            get => _ngCount;
            set { _ngCount = value; OnPropertyChanged(); }
        }

        public string Rate => TotalCount == 0 ? "0%" : $"{((double)OKCount / TotalCount * 100):F1}%";

        // 为了简化，我们让良率属性也刷新（当 TotalCount 或 OKCount 变化时）
        // 注意：上面 Rate 没有 set，所以当计数变化时，我们要手动通知界面刷新一次
        private void UpdateRate() => OnPropertyChanged(nameof(Rate));

        // ---------- 连接按钮的执行逻辑 ----------
        private async Task ExecuteConnectAsync(object parameter)
        {
            try
            {
                StatusText = "⏳ 正在连接...";
                await _commService.ConnectAsync(_serverIP, _serverPort);

                // 连接成功后打开摄像头
                _cameraService.Open(0);  // 0 代表默认摄像头
                StartPreview();

                StatusText = "🟢 已连接 (真实)";
            }
            catch (Exception ex)
            {
                StatusText = $"🔴 连接失败: {ex.Message}";
            }
        }

        // ---------- 检测按钮的执行逻辑 ----------
        private async Task ExecuteDetectAsync(object parameter)
        {
            if (!_commService.IsConnected)
            {
                StatusText = "🔴 请先点击“连接服务器”！";
                return;
            }

            StatusText = "⏳ 检测中...";

            try
            {
                // ---------- 1. 从摄像头抓取一帧 ----------
                var bitmap = _cameraService.CaptureFrame();
                if (bitmap == null)
                {
                    StatusText = "❌ 抓取画面失败";
                    return;
                }

                // ---------- 2. 将 Bitmap 转为 byte[] (JPG格式) 发给 Python ----------
                byte[] imageData;
                using (var ms = new System.IO.MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageData = ms.ToArray();
                }

                // 读取最新的阈值设置
                var settingsService = new Services.SettingsService();
                var settings = settingsService.LoadSettings();
                double threshold = settings.DetectThreshold;

                // 把阈值传给 Python
                // 获取显示名称，然后映射为英文编号
                string displayName = SelectedProductCode;
                string actualProductCode = _productCodeMap.ContainsKey(displayName) ? _productCodeMap[displayName] : displayName;
                
                var result = await _commService.SendImageAsync(imageData, actualProductCode, threshold);

                // ---------- 4. 处理返回结果（关键：画框） ----------
                // 先在界面上更新统计数字
                TotalCount++;
                if (result.Result == "OK")
                {
                    OKCount++;
                    StatusText = "✅ 检测结果：合格 (OK)";
                }
                else
                {
                    NGCount++;
                    StatusText = $"❌ 检测结果：不合格 (NG) - {result.DefectType}";

                    // 如果 Python 返回了缺陷位置（location），就在图片上画红色矩形
                    if (result.Location != null)
                    {
                        var loc = result.Location;
                        using (var g = System.Drawing.Graphics.FromImage(bitmap))
                        {
                            // 红色边框，粗细为 3 像素
                            using (var pen = new System.Drawing.Pen(System.Drawing.Color.Red, 3))
                            {
                                g.DrawRectangle(pen, loc.X, loc.Y, loc.W, loc.H);
                            }
                        }
                    }
                }
                UpdateRate();

                // ---------- 6. 保存记录到数据库 ----------
                // 生成唯一的图片文件名（避免覆盖）
                string imageFileName = $"NG_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
                string imageFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HistoryImages");
                if (!System.IO.Directory.Exists(imageFolder))
                    System.IO.Directory.CreateDirectory(imageFolder);

                string imageFullPath = System.IO.Path.Combine(imageFolder, imageFileName);

                // 保存带红框的图片到硬盘（如果有红框的话）
                bitmap.Save(imageFullPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                // 获取当前选中的产品编号（与发送给 Python 的一致）
                

                // 如果你之前做了映射，用映射后的值，否则直接用 SelectedProductCode
                var record = new InspectionRecord
                {
                    ProductCode = actualProductCode,  // ← 改成动态获取
                    Result = result.Result,
                    DefectType = result.DefectType ?? "未知",
                    Confidence = (decimal)result.Confidence,
                    DefectX = result.Location?.X ?? 0,
                    DefectY = result.Location?.Y ?? 0,
                    DefectW = result.Location?.W ?? 0,
                    DefectH = result.Location?.H ?? 0,
                    ImageSavePath = imageFullPath,
                    InspTime = DateTime.Now
                };

                // 异步保存（不阻塞界面）
                Task.Run(() => _dataService.SaveRecord(record));

                // ---------- 5. 将处理后的图片（可能带有红色矩形）显示到界面 ----------
                // 把 Bitmap 转换成 WPF 能显示的 BitmapSource
                var bitmapSource = ConvertBitmapToBitmapSource(bitmap);
                CurrentFrame = bitmapSource;

                // 释放 Bitmap 资源
                bitmap.Dispose();
            }
            catch (Exception ex)
            {
                StatusText = $"❌ 检测异常: {ex.Message}";
            }
        }

        private void StartPreview()
        {
            if (_isPreviewRunning) return;
            _cancellationTokenSource = new CancellationTokenSource();
            _isPreviewRunning = true;

            Task.Run(() =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var bitmap = _cameraService.CaptureFrame();
                        if (bitmap != null)
                        {
                            // 必须在 UI 线程更新 CurrentFrame
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                CurrentFrame = ConvertBitmapToBitmapSource(bitmap);
                            });
                            bitmap.Dispose();
                        }
                        else
                        {
                            // 如果抓取失败，适当延时
                            Thread.Sleep(100);
                        }
                        Thread.Sleep(50); // 约 20 FPS
                    }
                    catch (Exception ex)
                    {
                        // 记录日志，避免崩溃
                        Console.WriteLine($"预览异常: {ex.Message}");
                        break;
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void StopPreview()
        {
            _isPreviewRunning = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        // 辅助方法：Bitmap -> BitmapSource
        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            using (var ms = new System.IO.MemoryStream())
            {
                // 将 Bitmap 保存为 BMP 格式到内存流
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Position = 0;

                // 使用 WPF 的 BmpBitmapDecoder 解码（必须在 UI 线程调用）
                var decoder = new BmpBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var source = decoder.Frames[0];
                // 冻结后可跨线程访问（但这里是 UI 线程，保险起见）
                source.Freeze();
                return source;
            }
        }


    }
}
