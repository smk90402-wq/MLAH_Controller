
using System;
using System.Windows;
using System.Windows.Media;

namespace MLAH_Controller
{
    public partial class ViewModel_ScenarioView : CommonBase
    {

        #region 시나리오 모델 프로퍼티

        //View에서 사용할 시나리오 모델
        private Model_UnitScenario _model_UnitScenario = new Model_UnitScenario();
        public Model_UnitScenario model_UnitScenario
        {
            get
            {
                return _model_UnitScenario;
            }
            set
            {
                _model_UnitScenario = value;
                OnPropertyChanged("model_UnitScenario");
            }
        }

        private Model_Unit_Develop _model_Unit_Develop = new Model_Unit_Develop();
        public Model_Unit_Develop model_Unit_Develop
        {
            get
            {
                return _model_Unit_Develop;
            }
            set
            {
                _model_Unit_Develop = value;
                OnPropertyChanged("model_Unit_Develop");
            }
        }

        #endregion 시나리오 모델 프로퍼티

        #region 시나리오 뷰 상단 프로퍼티

        private Visibility _SceneBorderVisibility = Visibility.Visible;
        public Visibility SceneBorderVisibility
        {
            get
            {
                return _SceneBorderVisibility;
            }
            set
            {
                _SceneBorderVisibility = value;
                OnPropertyChanged("SceneBorderVisibility");
            }
        }

        private string _SceneFileName = "";
        public string SceneFileName
        {
            get
            {
                return _SceneFileName;
            }
            set
            {
                _SceneFileName = value;
                //WindowSceneFileName = " 시나리오 파일명 : " + _SceneFileName;
                OnPropertyChanged("SceneFileName");
            }
        }

        private string _SceneFileDescription = "";
        public string SceneFileDescription
        {
            get
            {
                return _SceneFileDescription;
            }
            set
            {
                _SceneFileDescription = value;
                //WindowSceneFileName = " 시나리오 파일명 : " + _SceneFileName;
                OnPropertyChanged("SceneFileDescription");
            }
        }


        private string _SceneStatus = "-";
        public string SceneStatus
        {
            get
            {
                return _SceneStatus;
            }
            set
            {
                _SceneStatus = value;
                OnPropertyChanged("SceneStatus");
            }
        }

        private TimeSpan _SceneTime = TimeSpan.FromSeconds(0);
        public TimeSpan SceneTime
        {
            get
            {
                return _SceneTime;
            }
            set
            {
                _SceneTime = value;
                OnPropertyChanged("SceneTime");
            }
        }

        #endregion 시나리오 뷰 상단 프로퍼티

        #region 모의 상태 프로퍼티

        private bool _IsSimPlaying = false;
        /// <summary>
        /// 모의 여부 Flag - 모의 시작 누르면 false
        /// </summary>
        public bool IsSimPlaying
        {
            get
            {
                return _IsSimPlaying;
            }
            set
            {
                _IsSimPlaying = value;
                OnPropertyChanged("IsSimPlaying");
            }
        }

        private bool _IsSimPlayingRev = true;
        /// <summary>
        /// 모의 여부 Flag - 모의 시작 누르면 false
        /// </summary>
        public bool IsSimPlayingRev
        {
            get
            {
                return _IsSimPlayingRev;
            }
            set
            {
                _IsSimPlayingRev = value;
                OnPropertyChanged("IsSimPlayingRev");
            }
        }

        private bool _SimPlayButtonEnable = true;
        /// <summary>
        /// 모의 여부 Flag - 모의 시작 누르면 false
        /// </summary>
        public bool SimPlayButtonEnable
        {
            get
            {
                return _SimPlayButtonEnable;
            }
            set
            {
                _SimPlayButtonEnable = value;
                OnPropertyChanged("SimPlayButtonEnable");
            }
        }

        #endregion 모의 상태 프로퍼티

        #region 객체 생성/수정/삭제 프로퍼티

        //객체 목록 선택 Index
        private int _ListSelectedIndex;
        public int ListSelectedIndex
        {
            get
            {
                return _ListSelectedIndex;
            }
            set
            {
                _ListSelectedIndex = value;
                OnPropertyChanged("ListSelectedIndex");
            }
        }

        private bool _EditButtonEnable = false;
        public bool EditButtonEnable
        {
            get
            {
                return _EditButtonEnable;
            }
            set
            {
                _EditButtonEnable = value;
                if (IsSimPlaying == true)
                {
                    _EditButtonEnable = false;
                }
                else
                {
                    _EditButtonEnable = value;
                }
                OnPropertyChanged("EditButtonEnable");
            }
        }



        private bool _DeleteButtonEnable = false;
        public bool DeleteButtonEnable
        {
            get
            {
                return _DeleteButtonEnable;
            }
            set
            {
                _DeleteButtonEnable = value;
                if (IsSimPlaying == true)
                {
                    _DeleteButtonEnable = false;
                }
                else
                {
                    _DeleteButtonEnable = value;
                }
                OnPropertyChanged("DeleteButtonEnable");
            }
        }

        private UnitObjectInfo _SelectedScenarioObject = new UnitObjectInfo();

        public UnitObjectInfo SelectedScenarioObject
        {
            get
            {
                return _SelectedScenarioObject;
            }
            set
            {
                _SelectedScenarioObject = value;
                //View_Unit_Map.SingletonInstance.UpdateFocusSquare();
                if (value != null)
                {
                    EditButtonEnable = true;
                    DeleteButtonEnable = true;
                    if (value.Type == 3)
                    {
                        HelicopterVisible = Visibility.Visible;
                        DroneVisible = Visibility.Collapsed;
                        TargetVisible = Visibility.Collapsed;
                    }
                    else if (value.Type == 1)
                    {
                        HelicopterVisible = Visibility.Collapsed;
                        DroneVisible = Visibility.Visible;
                        TargetVisible = Visibility.Collapsed;
                    }
                    else
                    {
                        HelicopterVisible = Visibility.Collapsed;
                        DroneVisible = Visibility.Collapsed;
                        TargetVisible = Visibility.Visible;
                    }
                }
                OnPropertyChanged("SelectedScenarioObject");

            }
        }


        private int _PackageTypeComboIndex = 0;
        /// <summary>
        /// 협업기저임무 패키지 유형
        /// </summary>
        public int PackageTypeComboIndex
        {
            get
            {
                return _PackageTypeComboIndex;
            }
            set
            {
                _PackageTypeComboIndex = value;
                OnPropertyChanged("PackageTypeComboIndex");
            }
        }

        private int _SensorTypeComboIndex = 0;
        /// <summary>
        /// 메인 센서 유형
        /// </summary>
        public int SensorTypeComboIndex
        {
            get
            {
                return _SensorTypeComboIndex;
            }
            set
            {
                _SensorTypeComboIndex = value;
                OnPropertyChanged("SensorTypeComboIndex");
            }
        }


        private string _InputMissionPackageIDControl = "-";
        /// <summary>
        /// 협업기저임무 패키지 ID
        /// </summary>
        public string InputMissionPackageIDControl
        {
            get
            {
                return _InputMissionPackageIDControl;
            }
            set
            {
                _InputMissionPackageIDControl = value;
                OnPropertyChanged("InputMissionPackageIDControl");
            }
        }

        private string _ReferencePackageIDControl = "-";
        /// <summary>
        /// 비행참조정보 패키지 ID
        /// </summary>
        public string ReferencePackageIDControl
        {
            get
            {
                return _ReferencePackageIDControl;
            }
            set
            {
                _ReferencePackageIDControl = value;
                OnPropertyChanged("ReferencePackageIDControl");
            }
        }

        private Visibility _HelicopterVisible = Visibility.Visible;
        public Visibility HelicopterVisible
        {

            get
            {
                return _HelicopterVisible;
            }
            set
            {
                _HelicopterVisible = value;
                OnPropertyChanged("HelicopterVisible");
            }
        }

        private Visibility _DroneVisible = Visibility.Collapsed;
        public Visibility DroneVisible
        {

            get
            {
                return _DroneVisible;
            }
            set
            {
                _DroneVisible = value;
                OnPropertyChanged("DroneVisible");
            }
        }

        private Visibility _TargetVisible = Visibility.Collapsed;
        public Visibility TargetVisible
        {

            get
            {
                return _TargetVisible;
            }
            set
            {
                _TargetVisible = value;
                OnPropertyChanged("TargetVisible");
            }
        }

        #endregion 객체 생성/수정/삭제 프로퍼티

        #region 모니터링 프로퍼티

        private bool _isMonitoringCommandEnabled = true;
        public bool IsMonitoringCommandEnabled
        {
            get => _isMonitoringCommandEnabled;
            set
            {
                _isMonitoringCommandEnabled = value;
                OnPropertyChanged(nameof(IsMonitoringCommandEnabled));
            }
        }

        #endregion 모니터링 프로퍼티

        #region 복잡협업 복잡도 프로퍼티

        private int _ScenarioComplexity = 0;
        public int ScenarioComplexity
        {
            get { return _ScenarioComplexity; }
            set
            {
                _ScenarioComplexity = value;

                // BrushConverter를 재사용하거나 캐싱하면 더 좋지만, 편의상 여기서 바로 변환합니다.
                var converter = new BrushConverter();

                if (value >= 100)
                {
                    // [완료/안전 색상]
                    // 기존: MLAH_COLOR_Value_Brush (녹색 계열)
                    // 변경: #00FF99 (SF 스타일에 맞는 형광 민트색)
                    // 또는 기존 리소스를 쓰고 싶다면 Application.Current.Resources[...] 유지
                    ComplexityColor = (Brush)converter.ConvertFrom("#00FF99");
                }
                else
                {
                    // [진행중/위험 색상]
                    // 기존: Colors.Red
                    // 변경: #FF5555 (배경에 묻히지 않는 부드러운 네온 레드)
                    ComplexityColor = (Brush)converter.ConvertFrom("#FF5555");
                }

                OnPropertyChanged("ScenarioComplexity");
            }
        }

        private System.Windows.Media.Brush _ComplexityColor =
         (SolidColorBrush)new BrushConverter().ConvertFrom("#FF5555");

        public System.Windows.Media.Brush ComplexityColor
        {
            get { return _ComplexityColor; }
            set
            {
                _ComplexityColor = value;
                OnPropertyChanged("ComplexityColor");
            }
        }

        #endregion 복잡협업 복잡도 프로퍼티

    }
}
