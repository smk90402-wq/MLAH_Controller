using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MLAH_Controller
{
    public interface IDialogService
    {
        void ShowMonitoringPopup();
        void ShowObjectSetPopup();
        // 필요하다면 다른 팝업을 위한 메서드도 추가할 수 있습니다.
    }

    public class DialogService : IDialogService
    {
        //public void ShowMonitoringPopup()
        //{
        //    var viewModel = ViewModel_Mornitoring_PopUp.SingletonInstance;

        //    // 팝업을 띄우기 전, 이전 View에서 사용했을 수 있는 선택 항목을 초기화합니다.
        //    // 이는 새로운 View가 뜰 때 발생할 수 있는 불필요한 연쇄 업데이트를 방지합니다.
        //    viewModel.SelectedMornitoringData = null;
        //    viewModel.SelectedMornitoringDataJoin = null;

        //    viewModel.ActivatePopup();

        //    var popup = new View_Mornitoring_PopUp
        //    {
        //        DataContext = viewModel,
        //        Owner = Application.Current.MainWindow,
        //        Topmost = true
        //    };

        //    popup.Show();
        //}
        public void ShowMonitoringPopup()
        {
            //var monitoringViewModel = ViewModel_Mornitoring_PopUp.SingletonInstance;
            //monitoringViewModel.ActivatePopup();

            //var popup = new View_Mornitoring_PopUp
            //{
            //    DataContext = monitoringViewModel,
            //    Owner = Application.Current.MainWindow,
            //    Topmost = true
            //};

            // ★★★★★★★★★★★★★★★★★★★★★★★
            // ★★★ 여기가 이번 해결책의 핵심입니다 ★★★
            // ★★★★★★★★★★★★★★★★★★★★★★★
            // 팝업의 'Closed' 이벤트가 발생했을 때 실행될 코드를 등록합니다.
            //popup.Closed += (sender, e) =>
            //{
            //    팝업이 완전히 닫히면, ScenarioView의 커맨드를 다시 활성화시킵니다.
            //    ViewModel_ScenarioView.SingletonInstance.IsMonitoringCommandEnabled = true;
            //};

            //popup.Show();
            // 1. 메인 앱이 실행된 폴더의 경로를 가져옵니다.
            // (예: ...\MLAH_Controller\bin\x64\Debug\net8.0-windows...)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 2. 모니터링 앱 경로 탐색 (SingleFile: 같은 폴더 / Debug: 서브폴더)
            string monitoringAppPath = Path.Combine(baseDir, "MLAH_Mornitoring.exe");
            if (!File.Exists(monitoringAppPath))
                monitoringAppPath = Path.Combine(baseDir, "MLAH_Mornitoring", "MLAH_Mornitoring.exe");

            if (File.Exists(monitoringAppPath))
            {
                Process.Start(monitoringAppPath);
            }
            else
            {
                MessageBox.Show("모니터링 프로그램을 찾을 수 없습니다.\n경로: " + baseDir);
            }
        }

        public void ShowObjectSetPopup()
        {
            var popup = new View_Object_Set_PopUp
            {
                DataContext = ViewModel_Object_Set_PopUp.SingletonInstance,
                Owner = Application.Current.MainWindow,
                Topmost = true
            };

            popup.Show();
        }
    }

    public static class ServiceProvider
    {
        // 애플리케이션 전체에서 단 하나의 DialogService 인스턴스를 사용합니다.
        public static IDialogService DialogService { get; } = new DialogService();
    }
}

