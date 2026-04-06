using DevExpress.Mvvm.UI;
using System;
using System.Collections.Generic;
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
    public partial class UC_Unit_Docking : UserControl
    {
        public UC_Unit_Docking()
        {
            InitializeComponent();
        }

        // 의존성 속성 정의
        public static readonly DependencyProperty SelectedScenarioProperty =
            DependencyProperty.Register("SelectedScenario", typeof(ScoreScenarioSummary), typeof(UC_Unit_Docking),
                new PropertyMetadata(null, OnSelectedScenarioChanged));

        public ScoreScenarioSummary SelectedScenario
        {
            get { return (ScoreScenarioSummary)GetValue(SelectedScenarioProperty); }
            set { SetValue(SelectedScenarioProperty, value); }
        }

        private static void OnSelectedScenarioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as UC_Unit_Docking;
            var newValue = e.NewValue as ScoreScenarioSummary;
            
            // 내부 뷰들에게 값 전파
            if (control.ScoreHomeView != null) control.ScoreHomeView.SelectedScenarioSummary = newValue;
            if (control.CoverageView != null) control.CoverageView.SelectedScenarioSummary = newValue;
            if (control.CommunicationView != null) control.CommunicationView.SelectedScenarioSummary = newValue;
            if (control.SafetyView != null) control.SafetyView.SelectedScenarioSummary = newValue;
            if (control.DistributionView != null) control.DistributionView.SelectedScenarioSummary = newValue;
            if (control.TargetView != null) control.TargetView.SelectedScenarioSummary = newValue;
            if (control.SRView != null) control.SRView.SelectedScenarioSummary = newValue;

            // 2. [추가] 부모 View_ScoreMain을 찾아서 ScenarioName 업데이트
            //if (newValue != null)
            //{
            //    // 시각적 트리를 타고 올라가서 View_ScoreMain을 찾음
            //    var mainView = LayoutTreeHelper.GetVisualParents(control).OfType<View_ScoreMain>().FirstOrDefault();

            //    if (mainView != null)
            //    {
            //        // Main 뷰의 속성에 이름 할당 (화면 갱신용)
            //        mainView.SelectedScenarioName = newValue.ScenarioName;

            //        // 만약 시간 정보도 필요하면 여기서 같이 업데이트
            //        // mainView.SomeTimeProperty = ...; 
            //    }
            //}
        }
    }
}

