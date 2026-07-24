using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VisionSort_Client.ViewModels;

namespace VisionSort_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 关键：创建 ViewModel 实例，并告诉窗口“数据来源是它”
            MainWindowViewModel viewModel = new MainWindowViewModel();

            // 把界面的各个元素和 ViewModel 的属性绑定起来
            this.DataContext = viewModel;

            // 手动绑定：把界面上的文本框和 ViewModel 的属性连起来（也可以用 XAML 绑定，但为了新手易懂，我们偷懒用代码绑定）
            // 其实更好的方式是在 XAML 里写 Binding，但为了避免你搞混，我们直接用代码赋值+事件更新。
            // 但是为了让上面 ViewModel 的属性变化自动更新界面，我们要在 ViewModel 里实现了 INotifyPropertyChanged，
            // 并且界面控件要订阅通知。简单起见，我这里教你一种“极简手写绑定”：

            // 监听 ViewModel 的属性变化事件，手动更新界面控件
            viewModel.PropertyChanged += (sender, e) =>
            {
                // 当 ViewModel 的 StatusText 变化时，更新界面上的 TxtStatus
                if (e.PropertyName == nameof(viewModel.StatusText))
                    TxtStatus.Text = viewModel.StatusText;
                else if (e.PropertyName == nameof(viewModel.TotalCount))
                    TxtTotal.Text = $"总数：{viewModel.TotalCount}";
                else if (e.PropertyName == nameof(viewModel.OKCount))
                    TxtOK.Text = $"良品：{viewModel.OKCount}";
                else if (e.PropertyName == nameof(viewModel.NGCount))
                    TxtNG.Text = $"不良品：{viewModel.NGCount}";
                else if (e.PropertyName == nameof(viewModel.Rate))
                    TxtRate.Text = $"良率：{viewModel.Rate}";
            };

            // 给界面按钮绑定点击事件（调用 ViewModel 的命令）
            BtnConnect.Click += (s, e) => viewModel.ConnectCommand.Execute(null);
            BtnDetect.Click += (s, e) => viewModel.DetectCommand.Execute(null);
            BtnHistory.Click += (s, e) =>
            {
                var historyWindow = new Views.HistoryWindow();
                historyWindow.Owner = this; // 设置所有者，防止主窗口被挡住
                historyWindow.ShowDialog(); // 以模态对话框显示
            };

            BtnSettings.Click += (s, e) =>
            {
                var settingsWindow = new Views.SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            };

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            viewModel?.StopPreview();
            //viewModel?.Dispose(); // 如果实现了 IDisposable，可以调用
        }

    }
}