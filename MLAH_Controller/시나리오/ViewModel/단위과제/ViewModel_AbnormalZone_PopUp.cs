
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System;
using MLAH_Controller;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Controls;
//using GMap.NET;
using System.Security.Policy;



namespace MLAH_Controller
{
    public class ViewModel_AbnormalZone_PopUp : CommonBase
    {
        #region Singleton
        static ViewModel_AbnormalZone_PopUp _ViewModel_AbnormalZone_PopUp = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_AbnormalZone_PopUp SingletonInstance
        {
            get
            {
                if (_ViewModel_AbnormalZone_PopUp == null)
                {
                    _ViewModel_AbnormalZone_PopUp = new ViewModel_AbnormalZone_PopUp();
                }
                return _ViewModel_AbnormalZone_PopUp;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_AbnormalZone_PopUp()
        {
            //ConfirmCommand = new RelayCommand(ConfirmCommandAction);
            CancelCommand = new RelayCommand(CancelCommandAction);

            AddCommand = new RelayCommand(AddCommandAction);
            EditCommand = new RelayCommand(EditCommandAction);
            DeleteCommand = new RelayCommand(DeleteCommandAction);

            CommonEvent.OnAbnormalZoneRectSelectStart += Callback_OnAbnormalZoneRectSelectStart;
            CommonEvent.OnAbnormalZoneRectSelectEnd += Callback_OnAbnormalZoneRectSelectEnd;
        }

        #endregion 생성자 & 콜백

        private Visibility _ObjectNumberVisibility = Visibility.Visible;
        public Visibility ObjectNumberVisibility
        {

            get { return _ObjectNumberVisibility; }
            set
            {
                _ObjectNumberVisibility = value;
                OnPropertyChanged("ObjectNumberVisibility");
            }
        }

        private bool _ObjectTypeComboBoxEnable = true;
        public bool ObjectTypeComboBoxEnable
        {

            get { return _ObjectTypeComboBoxEnable; }
            set
            {
                _ObjectTypeComboBoxEnable = value;
                OnPropertyChanged("ObjectTypeComboBoxEnable");
            }
        }

        private bool _AbnormalZoneSelectChecked = false;
        public bool AbnormalZoneSelectChecked
        {

            get { return _AbnormalZoneSelectChecked; }
            set
            {
                _AbnormalZoneSelectChecked = value;
                OnPropertyChanged("AbnormalZoneSelectChecked");
            }
        }

        private int _AbnormalZoneTypeIndex = 0;
        public int AbnormalZoneTypeIndex
        {

            get { return _AbnormalZoneTypeIndex; }
            set
            {
                _AbnormalZoneTypeIndex = value;
                OnPropertyChanged("AbnormalZoneTypeIndex");
            }
        }

        private double _AbnormalZoneRectStartLat = 0;
        public double AbnormalZoneRectStartLat
        {

            get
            {
                return _AbnormalZoneRectStartLat;
            }
            set
            {
                _AbnormalZoneRectStartLat = value;
                OnPropertyChanged("AbnormalZoneRectStartLat");
            }
        }

        private double _AbnormalZoneRectStartLon;
        public double AbnormalZoneRectStartLon
        {

            get 
            {
                return _AbnormalZoneRectStartLon;
            }
            set
            {
                _AbnormalZoneRectStartLon = value;
                OnPropertyChanged("AbnormalZoneRectStartLon");
            }
        }

        private double _AbnormalZoneRectStartAlt;
        public double AbnormalZoneRectStartAlt
        {

            get
            {
                return _AbnormalZoneRectStartAlt;
            }
            set
            {
                _AbnormalZoneRectStartAlt = value;
                OnPropertyChanged("AbnormalZoneRectStartAlt");
            }
        }

        private double _AbnormalZoneRectEndLat;
        public double AbnormalZoneRectEndLat
        {

            get
            {
                return _AbnormalZoneRectEndLat;
            }
            set
            {
                _AbnormalZoneRectEndLat = value;
                OnPropertyChanged("AbnormalZoneRectEndLat");
            }
        }

        private double _AbnormalZoneRectEndLon;
        public double AbnormalZoneRectEndLon
        {

            get
            {
                return _AbnormalZoneRectEndLon;
            }
            set
            {
                _AbnormalZoneRectEndLon = value;
                OnPropertyChanged("AbnormalZoneRectEndLon");
            }
        }

        private double _AbnormalZoneRectEndAlt;
        public double AbnormalZoneRectEndAlt
        {

            get
            {
                return _AbnormalZoneRectEndAlt;
            }
            set
            {
                _AbnormalZoneRectEndAlt = value;
                OnPropertyChanged("AbnormalZoneRectEndAlt");
            }
        }

        private double _AbnormalZoneRectArea;
        public double AbnormalZoneRectArea
        {

            get
            {
                return _AbnormalZoneRectArea;
            }
            set
            {
                _AbnormalZoneRectArea = value;
                OnPropertyChanged("AbnormalZoneRectArea");
            }
        }

        private bool _EditEnable = false;
        public bool EditEnable
        {

            get
            {
                return _EditEnable;
            }
            set
            {
                _EditEnable = value;
                OnPropertyChanged("EditEnable");
            }
        }

        private bool _AddButtonEnable = true;
        public bool AddButtonEnable
        {

            get
            {
                return _AddButtonEnable;
            }
            set
            {
                _AddButtonEnable = value;
                OnPropertyChanged("AddButtonEnable");
            }
        }

        private string _AddButtonText = "추가";
        public string AddButtonText
        {

            get
            {
                return _AddButtonText;
            }
            set
            {
                _AddButtonText = value;
                OnPropertyChanged("AddButtonText");
            }
        }

        private string _EditButtonText = "수정";
        public string EditButtonText
        {

            get
            {
                return _EditButtonText;
            }
            set
            {
                _EditButtonText = value;
                OnPropertyChanged("EditButtonText");
            }
        }

        private string _DeleteButtonText = "삭제";
        public string DeleteButtonText
        {

            get
            {
                return _DeleteButtonText;
            }
            set
            {
                _DeleteButtonText = value;
                OnPropertyChanged("DeleteButtonText");
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
                OnPropertyChanged("DeleteButtonEnable");
            }
        }

        private bool _AbnormalZonesDataGridEnable = true;
        public bool AbnormalZonesDataGridEnable
        {

            get
            {
                return _AbnormalZonesDataGridEnable;
            }
            set
            {
                _AbnormalZonesDataGridEnable = value;
                OnPropertyChanged("AbnormalZonesDataGridEnable");
            }
        }

        private Scenario_AbnormalZone _TempEditAbnormalZone = new Scenario_AbnormalZone();
        public Scenario_AbnormalZone TempEditAbnormalZone
        {

            get
            {
                return _TempEditAbnormalZone;
            }
            set
            {
                _TempEditAbnormalZone = value;
                OnPropertyChanged("TempEditAbnormalZone");
            }
        }


        private int _AbnormalZoneDataGridSelectedIndex;
        public int AbnormalZoneDataGridSelectedIndex
        {

            get
            {
                return _AbnormalZoneDataGridSelectedIndex;
            }
            set
            {
                _AbnormalZoneDataGridSelectedIndex = value;
                OnPropertyChanged("AbnormalZoneDataGridSelectedIndex");
            }
        }




        public RelayCommand AddCommand { get; set; }

        public void AddCommandAction(object param)
        {
            //추가 클릭
            if(EditEnable == false)
            {
                EditEnable = true;
                EditButtonEnable = false;
                DeleteButtonEnable = true;
                AddButtonText = "저장";
                DeleteButtonText = "취소";
            }
            //저장 클릭
            else
            {
                EditEnable = false;
                EditButtonEnable = false;
                DeleteButtonEnable = false;
                AddButtonText = "추가";
                DeleteButtonText = "삭제";

                var TempAbnormalZone = new Scenario_AbnormalZone();
                TempAbnormalZone.AbnormalZoneType = AbnormalZoneTypeIndex;
                TempAbnormalZone.AbnormalZoneStartLat = AbnormalZoneRectStartLat;
                TempAbnormalZone.AbnormalZoneStartLon = AbnormalZoneRectStartLon;
                TempAbnormalZone.AbnormalZoneEndLat = AbnormalZoneRectEndLat;
                TempAbnormalZone.AbnormalZoneEndLon = AbnormalZoneRectEndLon;

                //넓이 계산
                //List<PointLatLng> points = new List<PointLatLng>
                //                {
                //                    new PointLatLng(AbnormalZoneRectStartLat, AbnormalZoneRectStartLon),
                //                    new PointLatLng(AbnormalZoneRectStartLat, AbnormalZoneRectEndLon),
                //                    new PointLatLng(AbnormalZoneRectEndLat, AbnormalZoneRectEndLon),
                //                    new PointLatLng(AbnormalZoneRectEndLat, AbnormalZoneRectStartLon)
                //                };
                //AbnormalZoneRectArea = CommonUtil.CalculateRectangleArea(points);

                TempAbnormalZone.AbnormalZoneRectArea = AbnormalZoneRectArea;

                DataGridAbnormalZones.Add(TempAbnormalZone);
                //ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.AbnormalZones.Add(TempAbnormalZone);

                //임시 사각형 지우기
                CommonEvent.OnConfirmAbnormalZone?.Invoke();

                //마지막에 추가한 비정상 구역 선택
                var TempIndex = DataGridAbnormalZones.Count();
                AbnormalZoneDataGridSelectedIndex = TempIndex - 1;
            }
        }

        public RelayCommand EditCommand { get; set; }

        public void EditCommandAction(object param)
        {
            //수정 클릭
            if(EditEnable == false)
            {
                EditEnable = true;
                EditButtonText = "저장";
                DeleteButtonText = "취소";
                AddButtonEnable = false;
                AbnormalZonesDataGridEnable = false;

                TempEditAbnormalZone.AbnormalZoneType = SelectedDataGridAbnormalZone.AbnormalZoneType;
                TempEditAbnormalZone.AbnormalZoneStartLat = SelectedDataGridAbnormalZone.AbnormalZoneStartLat;
                TempEditAbnormalZone.AbnormalZoneStartLon = SelectedDataGridAbnormalZone.AbnormalZoneStartLon;
                TempEditAbnormalZone.AbnormalZoneEndLat = SelectedDataGridAbnormalZone.AbnormalZoneEndLat;
                TempEditAbnormalZone.AbnormalZoneEndLon = SelectedDataGridAbnormalZone.AbnormalZoneEndLon;
                TempEditAbnormalZone.AbnormalZoneRectArea = SelectedDataGridAbnormalZone.AbnormalZoneRectArea;

            }
            //저장 클릭
            else
            {
                EditEnable = false;
                EditButtonText = "수정";
                DeleteButtonText = "삭제";
                AddButtonEnable = true;
                AbnormalZonesDataGridEnable = true;

                var TempAbnormalZone = new Scenario_AbnormalZone();
                TempAbnormalZone.AbnormalZoneType = AbnormalZoneTypeIndex;
                TempAbnormalZone.AbnormalZoneStartLat = AbnormalZoneRectStartLat;
                TempAbnormalZone.AbnormalZoneStartLon = AbnormalZoneRectStartLon;
                TempAbnormalZone.AbnormalZoneEndLat = AbnormalZoneRectEndLat;
                TempAbnormalZone.AbnormalZoneEndLon = AbnormalZoneRectEndLon;

                //넓이 계산
                //List<PointLatLng> points = new List<PointLatLng>
                //                {
                //                    new PointLatLng(AbnormalZoneRectStartLat, AbnormalZoneRectStartLon),
                //                    new PointLatLng(AbnormalZoneRectStartLat, AbnormalZoneRectEndLon),
                //                    new PointLatLng(AbnormalZoneRectEndLat, AbnormalZoneRectEndLon),
                //                    new PointLatLng(AbnormalZoneRectEndLat, AbnormalZoneRectStartLon)
                //                };
                //AbnormalZoneRectArea = CommonUtil.CalculateRectangleArea(points);

                TempAbnormalZone.AbnormalZoneRectArea = AbnormalZoneRectArea;

                var TempSelectedIndex = AbnormalZoneDataGridSelectedIndex;

                DataGridAbnormalZones[TempSelectedIndex] = TempAbnormalZone;
                //ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.AbnormalZones[TempSelectedIndex] = TempAbnormalZone;

                //임시 사각형 지우기
                CommonEvent.OnConfirmAbnormalZone?.Invoke();

                AbnormalZoneDataGridSelectedIndex = TempSelectedIndex;
            }
        }

        public RelayCommand DeleteCommand { get; set; }

        public void DeleteCommandAction(object param)
        {
            //삭제 클릭
            if(EditEnable == false)
            {
                DataGridAbnormalZones.Remove(SelectedDataGridAbnormalZone);
                //ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.AbnormalZones.Remove(SelectedDataGridAbnormalZone);
            }
            //취소 클릭
            else
            {
                EditEnable = false;
                EditButtonText = "수정";
                DeleteButtonText = "삭제";
                AddButtonText = "추가";
                AddButtonEnable = true;
                AbnormalZonesDataGridEnable = true;
                DeleteButtonEnable = false;

                AbnormalZoneTypeIndex = TempEditAbnormalZone.AbnormalZoneType;
                AbnormalZoneRectStartLat = TempEditAbnormalZone.AbnormalZoneStartLat;
                AbnormalZoneRectStartLon = TempEditAbnormalZone.AbnormalZoneStartLon;
                AbnormalZoneRectEndLat = TempEditAbnormalZone.AbnormalZoneEndLat;
                AbnormalZoneRectEndLon = TempEditAbnormalZone.AbnormalZoneEndLon;
                AbnormalZoneRectArea = TempEditAbnormalZone.AbnormalZoneRectArea;

                //임시 사각형 지우기
                CommonEvent.OnConfirmAbnormalZone?.Invoke();
            }
            
        }



        private ObservableCollection<Scenario_AbnormalZone> _DataGridAbnormalZones = new ObservableCollection<Scenario_AbnormalZone>();
        public ObservableCollection<Scenario_AbnormalZone> DataGridAbnormalZones
        {
            get
            {
                return _DataGridAbnormalZones;
            }
            set
            {
                _DataGridAbnormalZones = value;
                OnPropertyChanged("DataGridAbnormalZones");
            }
        }

        private Scenario_AbnormalZone _SelectedDataGridAbnormalZone = new Scenario_AbnormalZone();
        public Scenario_AbnormalZone SelectedDataGridAbnormalZone
        {
            get
            {
                return _SelectedDataGridAbnormalZone;
            }
            set
            {
                _SelectedDataGridAbnormalZone = value;
                if(value != null)
                {
                    AbnormalZoneTypeIndex = _SelectedDataGridAbnormalZone.AbnormalZoneType;
                    AbnormalZoneRectStartLat = _SelectedDataGridAbnormalZone.AbnormalZoneStartLat;
                    AbnormalZoneRectStartLon = _SelectedDataGridAbnormalZone.AbnormalZoneStartLon;
                    AbnormalZoneRectEndLat = _SelectedDataGridAbnormalZone.AbnormalZoneEndLat;
                    AbnormalZoneRectEndLon = _SelectedDataGridAbnormalZone.AbnormalZoneEndLon;
                    AbnormalZoneRectArea = _SelectedDataGridAbnormalZone.AbnormalZoneRectArea;
                    EditButtonEnable = true;
                    DeleteButtonEnable = true;
                }
                else
                {
                    AbnormalZoneTypeIndex = 0;
                    AbnormalZoneRectStartLat = 0;
                    AbnormalZoneRectStartLon = 0;
                    AbnormalZoneRectEndLat = 0;
                    AbnormalZoneRectEndLon = 0;
                    AbnormalZoneRectArea= 0;
                    EditButtonEnable = false;
                    DeleteButtonEnable = false;
                }
                
                OnPropertyChanged("SelectedDataGridAbnormalZone");
            }
        }



        public RelayCommand CancelCommand { get; set; }

        public void CancelCommandAction(object param)
        {
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.5))
            };
            fadeOutAnimation.Completed += (s, a) =>
            {
                View_AbnormalZone_PopUp.SingletonInstance.Hide();
            };

            View_AbnormalZone_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }


        private void Callback_OnAbnormalZoneRectSelectStart(double lat, double lon)
        {
            AbnormalZoneRectStartLat = lat;
            AbnormalZoneRectStartLon = lon;
            //AbnormalZoneRectStartAlt = CommonUtil.GetElevationFromCoords(lat, lon);
            AbnormalZoneRectStartAlt = 0;
        }

        private void Callback_OnAbnormalZoneRectSelectEnd(double lat, double lon)
        {
            AbnormalZoneSelectChecked = false;
            AbnormalZoneRectEndLat = lat;
            AbnormalZoneRectEndLon = lon;
            //AbnormalZoneRectEndAlt = CommonUtil.GetElevationFromCoords(lat, lon);
            AbnormalZoneRectEndAlt = 0;

            //넓이 계산
            //List<PointLatLng> points = new List<PointLatLng>
            //                    {
            //                        new PointLatLng(AbnormalZoneRectStartLat, AbnormalZoneRectStartLon),
            //                        new PointLatLng(AbnormalZoneRectStartLat, AbnormalZoneRectEndLon),
            //                        new PointLatLng(AbnormalZoneRectEndLat, AbnormalZoneRectEndLon),
            //                        new PointLatLng(AbnormalZoneRectEndLat, AbnormalZoneRectStartLon)
            //                    };
            //AbnormalZoneRectArea = CommonUtil.CalculateRectangleArea(points);
        }



    }


}
