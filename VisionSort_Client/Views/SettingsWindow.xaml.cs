using System.Windows;
using VisionSort_Client.Models;
using VisionSort_Client.Services;

namespace VisionSort_Client.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private AppSettings _currentSettings;

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            LoadSettings();
            BtnSave.Click += (s, e) => SaveSettings();
            BtnCancel.Click += (s, e) => this.Close();
        }

        private void LoadSettings()
        {
            _currentSettings = _settingsService.LoadSettings();
            TxtThreshold.Text = _currentSettings.DetectThreshold.ToString();
            TxtCameraIndex.Text = _currentSettings.CameraIndex.ToString();
        }

        private void SaveSettings()
        {
            // 简单验证输入
            if (!double.TryParse(TxtThreshold.Text, out double threshold) || threshold < 0 || threshold > 1)
            {
                MessageBox.Show("阈值请输入 0~1 之间的数字。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtCameraIndex.Text, out int cameraIndex) || cameraIndex < 0)
            {
                MessageBox.Show("摄像头索引请输入非负整数。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _currentSettings.DetectThreshold = threshold;
            _currentSettings.CameraIndex = cameraIndex;
            _settingsService.SaveSettings(_currentSettings);

            MessageBox.Show("设置已保存，重启程序后生效。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}