using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace MLAH_Controller
{
    public class ShellViewModel : CommonBase
    {
        public static ShellViewModel Instance { get; private set; }

  

        private Visibility _isMainViewVisible = Visibility.Visible;
        public Visibility IsMainViewVisible
        {
            get
            {
            return _isMainViewVisible;
            }

            set
            {
                _isMainViewVisible = value;
                OnPropertyChanged("IsMainViewVisible");
            }
        }

        private Visibility _isScenarioViewVisible = Visibility.Collapsed;
        public Visibility IsScenarioViewVisible
        {
            get
            {
                return _isScenarioViewVisible;
            }
            set
            {
                _isScenarioViewVisible = value;
                OnPropertyChanged("IsScenarioViewVisible");
            }
        }

        public ICommand GoToMainViewCommand { get; }
        public ICommand GoToScenarioViewCommand { get; }

        // View 인스턴스를 여기서 생성할 필요가 없어졌습니다. XAML이 생성합니다.
        // private readonly View_MainView _mainViewInstance;
        // private readonly View_ScenarioView _scenarioViewInstance;

        public ShellViewModel()
        {
            Instance = this;

            GoToMainViewCommand = new RelayCommand(_ => {
                //IsMainViewVisible = true;
                //IsScenarioViewVisible = false;
                IsMainViewVisible = Visibility.Visible;
                IsScenarioViewVisible = Visibility.Collapsed;
            });

            GoToScenarioViewCommand = new RelayCommand(_ => {
                //IsMainViewVisible = false;
                //IsScenarioViewVisible = true;
                IsMainViewVisible = Visibility.Collapsed;
                IsScenarioViewVisible = Visibility.Visible;
            });

            // 시작 화면 설정
            //IsMainViewVisible = true;
            //IsScenarioViewVisible = false;
        }

   
    }
}