using DevExpress.Mvvm.DataAnnotations;
using MLAH_Controller;
using MLAHInterop;
using System.Collections.ObjectModel;
using System.ServiceModel.Channels;
using Windows.Services.Maps;

namespace MLAH_Controller
{
    // [UI 전용] RTB 데이터를 감싸는 포장지 클래스
    public class RTB_UI_Item : CommonBase
    {
        // 1. 생성자: 진짜 데이터와 관리용 ID를 받음
        public RTB_UI_Item(RTBCoordinateInfo realData, int uiId)
        {
            this.Model = realData;
            this.UI_ID = uiId;
        }

        // 2. 지도 연동을 위한 고유 ID (저장 안 됨, 전송 안 됨)
        public int UI_ID { get; }

        // 3. 실제 전송될 순수 데이터 (알맹이)
        public RTBCoordinateInfo Model { get; }

        // 4. 화면 바인딩용 속성 (값이 바뀌면 알맹이도 바꿈)
        public float Latitude
        {
            get => Model.Latitude;
            set
            {
                Model.Latitude = value;
                OnPropertyChanged("Latitude");
            }
        }

        public float Longitude
        {
            get => Model.Longitude;
            set
            {
                Model.Longitude = value;
                OnPropertyChanged("Longitude");
            }
        }

        public int Altitude
        {
            get => Model.Altitude;
            set
            {
                Model.Altitude = value;
                OnPropertyChanged("Altitude");
            }
        }
    }

    public class FlightAreaWrapper : CommonBase
    {
        public FlightAreaWrapper(FlightAreaInfo model, int uiId)
        {
            this.Model = model;
            this.UI_ID = uiId;
        }

        // 1. 지도 연동용 불변 ID
        public int UI_ID { get; }

        // 2. 실제 데이터 모델
        public FlightAreaInfo Model { get; }

        // 3. GridControl Master 행 바인딩용 속성 (고도)
        public int LowerLimit
        {
            get => Model.AltitudeLimits.LowerLimit;
            set
            {
                Model.AltitudeLimits.LowerLimit = value;
                OnPropertyChanged("LowerLimit");
            }
        }

        public int UpperLimit
        {
            get => Model.AltitudeLimits.UpperLimit;
            set
            {
                Model.AltitudeLimits.UpperLimit = value;
                OnPropertyChanged("UpperLimit");
            }
        }

        // 4. GridControl Detail(상세) 행 바인딩용 속성 (좌표 리스트)
        public ObservableCollection<AreaLatLonInfo> Points => Model.AreaLatLonList;
    }

    public class ProhibitedAreaWrapper : CommonBase
    {
        // 생성자
        public ProhibitedAreaWrapper(ProhibitedArea model, int uiId)
        {
            this.Model = model;
            this.UI_ID = uiId;
        }

        // 1. 지도 연동용 불변 ID
        public int UI_ID { get; }

        // 2. 실제 데이터 모델 (ProhibitedAreaInfo)
        public ProhibitedArea Model { get; }

        // 3. GridControl 바인딩용 속성 (최저 고도)
        public int LowerLimit
        {
            get => Model.AltitudeLimits.LowerLimit;
            set
            {
                Model.AltitudeLimits.LowerLimit = value;
                OnPropertyChanged("LowerLimit");
            }
        }

        // 4. GridControl 바인딩용 속성 (최고 고도)
        public int UpperLimit
        {
            get => Model.AltitudeLimits.UpperLimit;
            set
            {
                Model.AltitudeLimits.UpperLimit = value;
                OnPropertyChanged("UpperLimit");
            }
        }

        // 5. 좌표 리스트 (UI에서 점 추가/삭제 시 자동 반영)
        public ObservableCollection<AreaLatLonInfo> Points => Model.AreaLatLonList;
    }
}
