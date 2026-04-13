using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Core;
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
        // 탭별 시나리오 데이터가 최신인지 추적
        private readonly HashSet<int> _pendingTabUpdates = new HashSet<int>();

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

            // 홈 탭(0)은 항상 즉시 업데이트
            if (control.ScoreHomeView != null) control.ScoreHomeView.SelectedScenarioSummary = newValue;

            // 현재 선택된 탭도 즉시 업데이트
            int currentIdx = control.MainTabControl.SelectedIndex;
            control.ApplyScenarioToTab(currentIdx, newValue);

            // 나머지 탭은 전환 시점까지 대기
            control._pendingTabUpdates.Clear();
            for (int i = 1; i <= 6; i++)
            {
                if (i != currentIdx)
                    control._pendingTabUpdates.Add(i);
            }
        }

        private void MainTabControl_SelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
        {
            int idx = MainTabControl.SelectedIndex;
            if (_pendingTabUpdates.Remove(idx))
            {
                ApplyScenarioToTab(idx, SelectedScenario);
            }
        }

        private void ApplyScenarioToTab(int tabIndex, ScoreScenarioSummary value)
        {
            switch (tabIndex)
            {
                case 0: // 홈 - 이미 OnSelectedScenarioChanged에서 처리
                    break;
                case 1:
                    if (CoverageView != null) CoverageView.SelectedScenarioSummary = value;
                    break;
                case 2:
                    if (CommunicationView != null) CommunicationView.SelectedScenarioSummary = value;
                    break;
                case 3:
                    if (SafetyView != null) SafetyView.SelectedScenarioSummary = value;
                    break;
                case 4:
                    if (DistributionView != null) DistributionView.SelectedScenarioSummary = value;
                    break;
                case 5:
                    if (TargetView != null) TargetView.SelectedScenarioSummary = value;
                    break;
                case 6:
                    if (SRView != null) SRView.SelectedScenarioSummary = value;
                    break;
            }
        }
    }
}

