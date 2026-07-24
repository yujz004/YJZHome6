using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisionSort_Client.Models;
using VisionSort_Client.ViewModels;   // 引入 ViewModel 命名空间

namespace VisionSort_Client.Views
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();

            // ========== 关键：绑定 ViewModel ==========
            this.DataContext = new HistoryViewModel();

            // 注册行加载事件（用于设置文字颜色）
            RecordsDataGrid.LoadingRow += RecordsDataGrid_LoadingRow;

            // ===== 新增：统计按钮点击事件 =====
            BtnStatistics.Click += BtnStatistics_Click;
        }

        private void RecordsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var record = e.Row.DataContext as InspectionRecord;
            if (record == null) return;

            if (record.Result == "OK")
                e.Row.Foreground = new SolidColorBrush(Colors.Green);
            else if (record.Result == "NG")
                e.Row.Foreground = new SolidColorBrush(Colors.Red);
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = RecordsDataGrid.SelectedItem as InspectionRecord;
            if (selectedItem == null || string.IsNullOrEmpty(selectedItem.ImageSavePath))
            {
                MessageBox.Show("这条记录没有关联的图片。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (System.IO.File.Exists(selectedItem.ImageSavePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(selectedItem.ImageSavePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开图片: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"图片文件不存在: {selectedItem.ImageSavePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStatistics_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new StatisticsWindow();
            statsWindow.Owner = this;
            statsWindow.ShowDialog();
        }
    }
}