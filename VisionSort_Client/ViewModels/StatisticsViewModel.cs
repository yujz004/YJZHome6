using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VisionSort_Client.Services;

namespace VisionSort_Client.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        private SeriesCollection _productASeries;
        private SeriesCollection _productBSeries;

        public SeriesCollection ProductASeries
        {
            get => _productASeries;
            set { _productASeries = value; OnPropertyChanged(); }
        }

        public SeriesCollection ProductBSeries
        {
            get => _productBSeries;
            set { _productBSeries = value; OnPropertyChanged(); }
        }

        public async Task LoadDataAsync()
        {
            var dataService = new SqlDataService();
            var records = await dataService.GetAllRecordsAsync();

            // 统计产品A（注意：如果数据库中的编号是中文，请将 "ProductA" 改为 "产品A"）
            int a_ok = records.Count(r => r.ProductCode == "ProductA" && r.Result == "OK");
            int a_ng = records.Count(r => r.ProductCode == "ProductA" && r.Result == "NG");

            // 统计产品B
            int b_ok = records.Count(r => r.ProductCode == "ProductB" && r.Result == "OK");
            int b_ng = records.Count(r => r.ProductCode == "ProductB" && r.Result == "NG");

            // 构建饼图数据
            ProductASeries = new SeriesCollection
            {
                new PieSeries { Title = "OK", Values = new ChartValues<int> { a_ok }, DataLabels = true, LabelPoint = point => $"{point.Participation:P1}" },
                new PieSeries { Title = "NG", Values = new ChartValues<int> { a_ng }, DataLabels = true, LabelPoint = point => $"{point.Participation:P1}" }
            };

            ProductBSeries = new SeriesCollection
            {
                new PieSeries { Title = "OK", Values = new ChartValues<int> { b_ok }, DataLabels = true, LabelPoint = point => $"{point.Participation:P1}" },
                new PieSeries { Title = "NG", Values = new ChartValues<int> { b_ng }, DataLabels = true, LabelPoint = point => $"{point.Participation:P1}" }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}