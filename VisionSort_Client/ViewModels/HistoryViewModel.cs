using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using VisionSort_Client.Models;
using VisionSort_Client.Services;

namespace VisionSort_Client.ViewModels
{
    public class HistoryViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;

        // 界面绑定的记录集合（ObservableCollection 能自动刷新界面）
        private ObservableCollection<InspectionRecord> _records;
        public ObservableCollection<InspectionRecord> Records
        {
            get => _records;
            set { _records = value; OnPropertyChanged(); }
        }

        // 加载状态提示
        private string _statusMessage = "正在加载数据...";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public HistoryViewModel()
        {
            _dataService = new SqlDataService();
            Records = new ObservableCollection<InspectionRecord>();
            LoadDataAsync(); // 构造函数里开始加载数据
        }

        // 异步加载数据
        private async void LoadDataAsync()
        {
            try
            {
                StatusMessage = "⏳ 正在加载历史记录...";
                var list = await _dataService.GetAllRecordsAsync();

                Records.Clear();
                foreach (var item in list)
                {
                    Records.Add(item);
                }

                StatusMessage = $"✅ 加载完成，共 {Records.Count} 条记录";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 加载失败: {ex.Message}";
            }
        }

        // 实现属性通知（INotifyPropertyChanged）
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}