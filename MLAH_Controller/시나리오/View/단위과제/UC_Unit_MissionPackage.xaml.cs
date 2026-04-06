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
using static MLAH_Controller.ViewModel_UC_Unit_MissionPackage;

namespace MLAH_Controller
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UC_Unit_MissionPackage : UserControl
    {

        #region Singleton
        private static volatile UC_Unit_MissionPackage _SingletonInstance = null;
        private static readonly object syncRoot = new object();
        public static UC_Unit_MissionPackage SingletonInstance
        {
            get
            {
                if (_SingletonInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (_SingletonInstance == null)
                        {
                            _SingletonInstance = new UC_Unit_MissionPackage();
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

        public UC_Unit_MissionPackage()
        {
            InitializeComponent();
            //this.PreviewKeyDown += OnPreviewKeyDown;

            // 뷰가 로드될 때 전역 이벤트 구독, 언로드될 때 해제
            this.Loaded += UC_Unit_MissionPackage_Loaded;
            this.Unloaded += UC_Unit_MissionPackage_Unloaded;
        }

        private void UC_Unit_MissionPackage_Loaded(object sender, RoutedEventArgs e)
        {
            // 중복 구독 방지를 위해 제거 후 추가
            InputManager.Current.PreProcessInput -= OnGlobalKeyDown;
            InputManager.Current.PreProcessInput += OnGlobalKeyDown;
        }

        private void UC_Unit_MissionPackage_Unloaded(object sender, RoutedEventArgs e)
        {
            InputManager.Current.PreProcessInput -= OnGlobalKeyDown;
        }

        // 앱 전체에서 키 입력을 가로채는 핸들러
        private void OnGlobalKeyDown(object sender, NotifyInputEventArgs e)
        {
            if (!this.IsVisible) return;
            // 1. 키 다운 이벤트인지 확인
            if (!(e.StagingItem.Input is KeyEventArgs args) || args.RoutedEvent != Keyboard.KeyDownEvent)
                return;

            // 2. 텍스트 박스 입력 중일 때는 단축키 무시 (전역 포커스 체크)
            if (Keyboard.FocusedElement is TextBox) return;
            if (Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase) return; // DevExpress TextEdit 등 대비

            // 3. ViewModel 가져오기 (전역 이벤트이므로 SingletonInstance 사용 권장)
            var vm = ViewModel_UC_Unit_MissionPackage.SingletonInstance;
            if (vm == null) return;

            // 4. 키 입력 처리 (Z, X, C, V, B, Enter)
            switch (args.Key)
            {
                case Key.Z:
                    ProcessShortcut(vm.TakeOverState, () => vm.TakeOverChecked = !vm.TakeOverChecked, args);
                    break;
                case Key.X:
                    ProcessShortcut(vm.HandOverState, () => vm.HandOverChecked = !vm.HandOverChecked, args);
                    break;
                case Key.C:
                    ProcessShortcut(vm.RTBState, () => vm.RTBChecked = !vm.RTBChecked, args);
                    break;

                //V키: 켜져있으면 -> 취소(Clear), 꺼져있으면 -> 켜기
                case Key.V:
                    ProcessShortcut(vm.FlightAreaState, () =>
                    {
                        if (vm.FlightAreaChecked)
                            vm.ClearFlightAreaTempData(); // 취소: 데이터 삭제
                        else
                            vm.FlightAreaChecked = true;  // 시작
                    }, args);
                    break;

                case Key.B:
                    ProcessShortcut(vm.ProhibitedAreaState, () =>
                    {
                        if (vm.ProhibitedAreaChecked)
                            vm.ClearProhibitedAreaTempData();
                        else
                            vm.ProhibitedAreaChecked = true;
                    }, args);
                    break;

                // --- [Enter: 저장 / 모드 해제] ---
                case Key.Enter:
                    // 1. [Point 타입] 좌표 찍기 모드(Z, X, C)가 활성화된 상태라면?
                    //    -> 엔터는 "저장"이 아니라 "찍기 모드 취소(토글 해제)"로 동작해야 함.
                    if (vm.TakeOverState == MissionEditState.Creating && vm.TakeOverChecked)
                    {
                        vm.TakeOverChecked = false; // 토글만 끔
                        args.Handled = true;        // 저장 로직 실행 안 함
                        return;
                    }
                    if (vm.HandOverState == MissionEditState.Creating && vm.HandOverChecked)
                    {
                        vm.HandOverChecked = false;
                        args.Handled = true;
                        return;
                    }
                    if (vm.RTBState == MissionEditState.Creating && vm.RTBChecked)
                    {
                        vm.RTBChecked = false;
                        args.Handled = true;
                        return;
                    }

                    // 2. [Polygon 타입] 영역 그리기 중이라면?
                    //    -> 엔터는 "그리기 강제 완료(Finish)" 후 저장으로 이어짐
                    //    (View_Unit_Map에 우리가 만든 메서드 호출)
                    MLAH_Controller.View_Unit_Map.SingletonInstance.ForceFinishDrawing();

                    // 3. [공통] 저장 실행
                    //    (위의 if문에 걸리지 않았다면, 이미 좌표가 찍혔거나 수동 입력된 상태이므로 저장 수행)
                    if (TryExecuteSave(vm.TakeOverState, vm.TakeOverButton1Command)) args.Handled = true;
                    else if (TryExecuteSave(vm.HandOverState, vm.HandOverButton1Command)) args.Handled = true;
                    else if (TryExecuteSave(vm.RTBState, vm.RTBButton1Command)) args.Handled = true;
                    else if (TryExecuteSave(vm.FlightAreaState, vm.FlightAreaButton1Command)) args.Handled = true;
                    else if (TryExecuteSave(vm.ProhibitedAreaState, vm.ProhibitedAreaButton1Command)) args.Handled = true;
                    break;

                // --- [취소] ESC 키  ---
                case Key.Escape:
                    // Button3Command가 '생성 중'일 때는 '취소' 역할을 합니다.
                    if (TryExecuteCancel(vm.TakeOverState, vm.TakeOverButton3Command)) args.Handled = true;
                    else if (TryExecuteCancel(vm.HandOverState, vm.HandOverButton3Command)) args.Handled = true;
                    else if (TryExecuteCancel(vm.RTBState, vm.RTBButton3Command)) args.Handled = true;
                    else if (TryExecuteCancel(vm.FlightAreaState, vm.FlightAreaButton3Command)) args.Handled = true;
                    //else if (TryExecuteCancel(vm.ProhibitedAreaState, vm.ProhibitedAreaButton3Command)) args.Handled = true;
                    break;
            }
        }

        // [간소화 1] 상태가 Creating/Editing 일 때만 동작(Action) 수행
        private void ProcessShortcut(MissionEditState state, Action action, KeyEventArgs e)
        {
            // None이 아니면 (= 생성 혹은 수정 중이면)
            if (state != MissionEditState.None)
            {
                action?.Invoke();
                e.Handled = true;
            }
        }

        // [간소화 2] 상태가 Creating/Editing 일 때만 저장 커맨드 실행
        private bool TryExecuteSave(MissionEditState state, RelayCommand saveCommand)
        {
            if (state != MissionEditState.None && saveCommand.CanExecute(null))
            {
                saveCommand.Execute(null);
                return true; // 실행 성공
            }
            return false;
        }

        private bool TryExecuteCancel(MissionEditState state, RelayCommand cancelCommand)
        {
            // 생성(Creating) 또는 수정(Editing) 상태일 때만 취소 동작 수행
            if (state != MissionEditState.None && cancelCommand.CanExecute(null))
            {
                cancelCommand.Execute(null);
                return true;
            }
            return false;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = this.DataContext as ViewModel_UC_Unit_MissionPackage;
            if (vm == null) return;

            // 텍스트 박스 입력 중일 때는 단축키 무시
            if (Keyboard.FocusedElement is TextBox) return;

            switch (e.Key)
            {
                // Q, W, E, R, T: 체크박스 토글
                case Key.Z:
                    ProcessShortcut(vm.TakeOverState, () => vm.TakeOverChecked = !vm.TakeOverChecked, e);
                    break;
                case Key.X:
                    ProcessShortcut(vm.HandOverState, () => vm.HandOverChecked = !vm.HandOverChecked, e);
                    break;
                case Key.C:
                    ProcessShortcut(vm.RTBState, () => vm.RTBChecked = !vm.RTBChecked, e);
                    break;
                case Key.V:
                    ProcessShortcut(vm.FlightAreaState, () => vm.FlightAreaChecked = !vm.FlightAreaChecked, e);
                    break;
                case Key.B:
                    ProcessShortcut(vm.ProhibitedAreaState, () => vm.ProhibitedAreaChecked = !vm.ProhibitedAreaChecked, e);
                    break;

                // Enter: 저장 (현재 Creating 상태인 것 찾아서 실행)
                case Key.Enter:
                    if (TryExecuteSave(vm.TakeOverState, vm.TakeOverButton1Command)) e.Handled = true;
                    else if (TryExecuteSave(vm.HandOverState, vm.HandOverButton1Command)) e.Handled = true;
                    else if (TryExecuteSave(vm.RTBState, vm.RTBButton1Command)) e.Handled = true;
                    else if (TryExecuteSave(vm.FlightAreaState, vm.FlightAreaButton1Command)) e.Handled = true;
                    else if (TryExecuteSave(vm.ProhibitedAreaState, vm.ProhibitedAreaButton1Command)) e.Handled = true;
                    break;
            }
        }

    }
}
