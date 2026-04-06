using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MLAH_LogAnalyzer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UC_Com_Docking : UserControl
    {


        public event Action<DateTime> ChartClicked;
        public UC_Com_Docking()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 부모 뷰에서 호출하는 차트 갱신 메서드
        /// </summary>
        public void RefreshCharts(
            ObservableCollection<CommunicationDataOutput> data1,
            ObservableCollection<CommunicationDataOutput> data2,
            ObservableCollection<CommunicationDataOutput> data3)
        {
            // 1. 데이터 바인딩
            if (data1 != null) ScoreChart1.DataSource = data1;
            if (data2 != null) ScoreChart2.DataSource = data2;
            if (data3 != null) ScoreChart3.DataSource = data3;

            // 2. 범위 적용
            ApplyChartRange(ScoreChart1, data1);
            ApplyChartRange(ScoreChart2, data2);
            ApplyChartRange(ScoreChart3, data3);
        }

        private void ApplyChartRange(ChartControl chart, ObservableCollection<CommunicationDataOutput> data)
        {
            if (chart == null || chart.Diagram is not XYDiagram2D diagram || diagram.ActualAxisX == null || diagram.ActualAxisY == null)
                return;

            // [방어 1] 차트가 그려지기 전(Unloaded)이거나 화면에 안 보일 때 무리한 범위 계산 시도 방지
            if (!chart.IsLoaded) return;

            DateTime minTime;
            DateTime maxTime;

            if (data == null || !data.Any())
            {
                minTime = DateTime.Now;
                maxTime = minTime.AddSeconds(1);
            }
            else
            {
                minTime = DateTime.MaxValue;
                maxTime = DateTime.MinValue;
                foreach (var dp in data)
                {
                    if (dp.Timestamp < minTime) minTime = dp.Timestamp;
                    if (dp.Timestamp > maxTime) maxTime = dp.Timestamp;
                }
                if (maxTime <= minTime) maxTime = minTime.AddSeconds(1);
            }

            // [방어 2] 업데이트 중 렌더링 충돌 방지
            chart.BeginInit();
            try
            {
                // X축 설정 (강제 new 할당 없이 기존 객체 활용)
                if (diagram.ActualAxisX.WholeRange != null)
                    diagram.ActualAxisX.WholeRange.SetMinMaxValues(minTime, maxTime);
                if (diagram.ActualAxisX.VisualRange != null)
                    diagram.ActualAxisX.VisualRange.SetMinMaxValues(minTime, maxTime);

                // Y축 설정
                if (diagram.ActualAxisY.WholeRange != null)
                    diagram.ActualAxisY.WholeRange.SetMinMaxValues(-1.5, 1.5);
                if (diagram.ActualAxisY.VisualRange != null)
                    diagram.ActualAxisY.VisualRange.SetMinMaxValues(-0.5, 1.5);

                diagram.ActualAxisY.CustomLabels.Clear();
                diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(0, "Fail"));
                diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(1, "Success"));
            }
            finally
            {
                // 업데이트 완료
                chart.EndInit();
            }
        }

        private void ScoreChart_BoundDataChanged(object sender, RoutedEventArgs e)
        {
            var chart = sender as ChartControl;
            if (chart == null || chart.Diagram == null) return;

            foreach (var series in chart.Diagram.Series)
            {
                if (series is LineSeries2D lineSeries)
                {
                    uint uavId = 0;
                    // 차트 이름으로 ID 구분 (ScoreChart1 -> 4, ScoreChart2 -> 5, ScoreChart3 -> 6)
                    if (chart.Name == "ScoreChart1") uavId = 4;
                    else if (chart.Name == "ScoreChart2") uavId = 5;
                    else if (chart.Name == "ScoreChart3") uavId = 6;

                    SolidColorBrush brush = Brushes.Gray;
                    switch (uavId)
                    {
                        case 4: brush = Brushes.Blue; break;
                        case 5: brush = Brushes.Red; break;
                        case 6: brush = Brushes.LimeGreen; break;
                    }

                    if (lineSeries.LineStyle == null) lineSeries.LineStyle = new LineStyle();
                    //lineSeries.LineStyle.Brush = brush;
                    lineSeries.LineStyle.Thickness = 2;

                    lineSeries.MarkerModel = new CircleMarker2DModel();
                    lineSeries.MarkerVisible = true;
                    //lineSeries.MarkerStyle.Fill = Brushes.White;
                    //lineSeries.MarkerStyle.Stroke = brush;
                    //lineSeries.MarkerStyle.StrokeThickness = 2;
                }
            }
        }

        private void ScoreChart_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            // 현재 이벤트가 발생한 차트 확인
            var chart = sender as ChartControl;
            string uavPrefix = "UAV";

            // 차트 이름에 따라 접두사 결정
            if (chart != null)
            {
                if (chart.Name == "ScoreChart1") uavPrefix = "UAV 1";
                else if (chart.Name == "ScoreChart2") uavPrefix = "UAV 2";
                else if (chart.Name == "ScoreChart3") uavPrefix = "UAV 3";
            }

            foreach (var element in e.CrosshairElements)
            {
                if (element.SeriesPoint != null)
                {
                    // 1. 현재 값(Value) 가져오기
                    double val = element.SeriesPoint.Value;

                    // 2. 값에 따라 텍스트 결정
                    // (정확한 비교를 위해 반올림 혹은 오차 범위 허용)
                    string statusText = (Math.Abs(val - 1.0) < 0.001) ? "Success" : "Fail";

                    // 3. 라벨 텍스트 덮어쓰기
                    // 형식: "UAV 1: Success"
                    element.LabelElement.Text = $"{uavPrefix}: {statusText}";
                }
            }
        }

        public void SyncCrosshairs(DateTime time)
        {
            // 활성 탭의 차트만 업데이트 (3개 동시 업데이트 시 성능 저하 방지)
            var activeChart = GetActiveChart();
            if (activeChart != null)
                ShowCrosshair(activeChart, time);
        }

        private ChartControl GetActiveChart()
        {
            // DocumentPanel의 IsActive 속성으로 현재 보이는 탭 판별
            var parent1 = ScoreChart1?.Parent as FrameworkElement;
            var parent2 = ScoreChart2?.Parent as FrameworkElement;
            var parent3 = ScoreChart3?.Parent as FrameworkElement;

            while (parent1 != null && parent1 is not DevExpress.Xpf.Docking.DocumentPanel) parent1 = parent1.Parent as FrameworkElement;
            while (parent2 != null && parent2 is not DevExpress.Xpf.Docking.DocumentPanel) parent2 = parent2.Parent as FrameworkElement;
            while (parent3 != null && parent3 is not DevExpress.Xpf.Docking.DocumentPanel) parent3 = parent3.Parent as FrameworkElement;

            if (parent1 is DevExpress.Xpf.Docking.DocumentPanel dp1 && dp1.IsActive) return ScoreChart1;
            if (parent2 is DevExpress.Xpf.Docking.DocumentPanel dp2 && dp2.IsActive) return ScoreChart2;
            if (parent3 is DevExpress.Xpf.Docking.DocumentPanel dp3 && dp3.IsActive) return ScoreChart3;

            return ScoreChart1; // fallback
        }

        private void ShowCrosshair(ChartControl chart, DateTime time)
        {
            // 차트가 로드되었고, 현재 화면에 보일 때만 크로스헤어 렌더링 시도
            if (chart != null && chart.IsLoaded && chart.IsVisible && chart.Diagram is XYDiagram2D diagram)
            {
                diagram.ShowCrosshair(time, null);
            }
        }

        private void HandleChartClick(object sender, MouseButtonEventArgs e)
        {
            var chart = sender as ChartControl;
            if (chart == null || !chart.IsLoaded) return;

            // 1. 클릭 위치 정보 가져오기
            var position = e.GetPosition(chart);
            var hitInfo = chart.CalcHitInfo(position);

            if (hitInfo != null && (hitInfo.SeriesPoint != null || hitInfo.InDiagram))
            {
                DateTime clickedTime;

                if (hitInfo.SeriesPoint != null)
                {
                    clickedTime = hitInfo.SeriesPoint.DateTimeArgument;
                }
                else
                {
                    var diagram = chart.Diagram as XYDiagram2D;
                    if (diagram == null) return;

                    var coords = diagram.PointToDiagram(position);
                    if (coords == null || coords.DateTimeArgument == DateTime.MinValue) return;

                    clickedTime = coords.DateTimeArgument;
                }

                // 2. 부모에게 이벤트 발생 (구독자가 있을 경우)
                ChartClicked?.Invoke(clickedTime);
            }
        }
        private void Chart_MouseLeftButtonDown1(object sender, MouseButtonEventArgs e) => HandleChartClick(sender, e);
        private void Chart_MouseLeftButtonDown2(object sender, MouseButtonEventArgs e) => HandleChartClick(sender, e);
        private void Chart_MouseLeftButtonDown3(object sender, MouseButtonEventArgs e) => HandleChartClick(sender, e);

    }
}
