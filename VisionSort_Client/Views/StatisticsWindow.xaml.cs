using System.Windows;
using VisionSort_Client.ViewModels;

namespace VisionSort_Client.Views
{
    public partial class StatisticsWindow : Window
    {
        private StatisticsViewModel _viewModel;

        public StatisticsWindow()
        {
            InitializeComponent();
            // 窗口加载时开始异步加载数据
            this.Loaded += async (s, e) =>
            {
                _viewModel = new StatisticsViewModel();
                this.DataContext = _viewModel;
                await _viewModel.LoadDataAsync();  // 异步加载，不阻塞UI
            };
        }
    }
}