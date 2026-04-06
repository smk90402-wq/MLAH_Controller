using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MLAH_Controller; // ViewModel 네임스페이스

namespace MLAH_Controller
{
    public partial class UC_Unit_INITMissionInfo : UserControl
    {
        #region Singleton
        private static volatile UC_Unit_INITMissionInfo _SingletonInstance = null;
        private static readonly object syncRoot = new object();
        public static UC_Unit_INITMissionInfo SingletonInstance
        {
            get
            {
                if (_SingletonInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (_SingletonInstance == null)
                        {
                            _SingletonInstance = new UC_Unit_INITMissionInfo();
                        }
                    }
                }
                return _SingletonInstance;
            }
            set
            {
                _SingletonInstance = value;
            }
        }
        #endregion Singleton

        public UC_Unit_INITMissionInfo()
        {
            InitializeComponent();

            // 뷰 로드/언로드 시 전역 키 이벤트 연결
            this.Loaded += UC_Unit_INITMissionInfo_Loaded;
            this.Unloaded += UC_Unit_INITMissionInfo_Unloaded;
        }

        private void UC_Unit_INITMissionInfo_Loaded(object sender, RoutedEventArgs e)
        {
            // 중복 방지를 위해 제거 후 추가
            InputManager.Current.PreProcessInput -= OnGlobalKeyDown;
            InputManager.Current.PreProcessInput += OnGlobalKeyDown;
        }

        private void UC_Unit_INITMissionInfo_Unloaded(object sender, RoutedEventArgs e)
        {
            InputManager.Current.PreProcessInput -= OnGlobalKeyDown;
        }

        // 전역 키 입력 핸들러
        private void OnGlobalKeyDown(object sender, NotifyInputEventArgs e)
        {
            if (!this.IsVisible) return;
            // 1. 키 다운 이벤트인지 확인
            if (!(e.StagingItem.Input is KeyEventArgs args) || args.RoutedEvent != Keyboard.KeyDownEvent)
                return;

            // 2. 텍스트 박스 입력 중에는 무시 (오입력 방지)
            if (Keyboard.FocusedElement is TextBox) return;
            if (Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase) return; // DevExpress TextEdit 등

            // 3. ViewModel 가져오기
            var vm = ViewModel_UC_Unit_INITMissionInfo.SingletonInstance;
            if (vm == null) return;

            // 이 뷰(UserControl)가 현재 화면에 보이는 상태인지 체크가 필요할 수 있음
            // (예: 다른 탭에 있는데 단축키가 먹히면 곤란할 경우)
            // if (!this.IsVisible) return; 

            switch (args.Key)
            {
                // [Q] Point 모드 토글
                case Key.Q:
                    // Point 모드 상태일 때만 동작 (라디오 버튼이 Point일 때)
                    if (vm.IsPointSelected && vm.EditEnable)
                    {
                        vm.PointTypeChecked = !vm.PointTypeChecked;
                        args.Handled = true;
                    }
                    break;

                // [W] Linear 모드 토글
                case Key.W:
                    // Linear 모드 상태일 때만 동작
                    if (vm.IsLineSelected && vm.EditEnable)
                    {
                        vm.LinearTypeChecked = !vm.LinearTypeChecked;
                        args.Handled = true;
                    }
                    break;

                // [E] Polygon 모드 토글
                case Key.E:
                    // Polygon 모드 상태일 때만 동작 (Polygon 생성/수정 모드여야 함)
                    // -> PolygonEditEnable이 true일 때만 토글 가능하게 설정되어 있음 (ViewModel 로직 상)
                    if (vm.IsAreaSelected && vm.PolygonEditEnable)
                    {
                        vm.PolygonTypeChecked = !vm.PolygonTypeChecked;

                        // (옵션) 토글 끄면 임시 데이터 클리어 로직 필요 시 추가
                        if (!vm.PolygonTypeChecked)
                        {
                            // vm.ClearTempPolygonData(); // 필요하다면
                        }

                        args.Handled = true;
                    }
                    break;

                // [Enter] 저장 / 완료
                case Key.Enter:
                    if (vm.IsPointSelected && TryExecuteSave(vm.Button1Command))
                    {
                        args.Handled = true;
                    }
                    else if (vm.IsLineSelected && TryExecuteSave(vm.Button1Command))
                    {
                        args.Handled = true;
                    }
                    else if (vm.IsAreaSelected)
                    {
                        // 1. 하위 폴리곤을 '그리는 중(Creating/Editing)'일 때 Enter -> 하위 폴리곤 저장
                        if (vm.PolygonState != ViewModel_UC_Unit_INITMissionInfo.MissionEditState.None)
                        {
                            if (TryExecuteSave(vm.PolygonButton1Command))
                                args.Handled = true;
                        }
                        // 2. 그리는 중이 '아닐 때' Enter -> 메인 협업기저임무 저장
                        else
                        {
                            // ★ 사용자 요구사항: 만들어진 폴리곤이 1개 이상 있을 때만 메인 임무 저장 허용
                            if (vm.AreaWrapperList.Count > 0)
                            {
                                if (TryExecuteSave(vm.Button1Command))
                                    args.Handled = true;
                            }
                            else
                            {
                                // (옵션) 폴리곤이 하나도 없을 때 엔터를 치면 안내 로그를 띄울 수 있습니다.
                                // ViewModel_ScenarioView.SingletonInstance.AddLog("저장할 폴리곤 영역이 없습니다. 구역을 먼저 생성해주세요.", 2);
                            }
                        }
                    }
                    break;

                // [ESC] 취소
                case Key.Escape:
                    if (vm.IsPointSelected && TryExecuteCancel(vm.Button3Command))
                    {
                        args.Handled = true;
                    }
                    else if (vm.IsLineSelected && TryExecuteCancel(vm.Button3Command))
                    {
                        args.Handled = true;
                    }
                    else if (vm.IsAreaSelected)
                    {
                        // 1. 하위 폴리곤을 '그리는 중'일 때 ESC -> 하위 폴리곤 그리기 취소
                        if (vm.PolygonState != ViewModel_UC_Unit_INITMissionInfo.MissionEditState.None)
                        {
                            if (TryExecuteCancel(vm.PolygonButton3Command))
                                args.Handled = true;
                        }
                        // 2. 그리는 중이 '아닐 때' ESC -> 메인 협업기저임무 생성 자체를 취소 (전체 초기화)
                        else
                        {
                            // 뷰모델의 Button3CommandAction에는 이미 AreaWrapperList와 Map 객체를 
                            // 싹 비우는 로직이 완벽하게 구현되어 있습니다. 이것만 호출해주면 끝납니다.
                            if (TryExecuteCancel(vm.Button3Command))
                                args.Handled = true;
                        }
                    }
                    break;
            }
        }

        // 헬퍼 메서드: 저장 커맨드 실행 시도
        private bool TryExecuteSave(RelayCommand saveCommand)
        {
            if (saveCommand != null && saveCommand.CanExecute(null))
            {
                saveCommand.Execute(null);
                return true;
            }
            return false;
        }

        // 헬퍼 메서드: 취소 커맨드 실행 시도
        private bool TryExecuteCancel(RelayCommand cancelCommand)
        {
            if (cancelCommand != null && cancelCommand.CanExecute(null))
            {
                cancelCommand.Execute(null);
                return true;
            }
            return false;
        }
    }
}