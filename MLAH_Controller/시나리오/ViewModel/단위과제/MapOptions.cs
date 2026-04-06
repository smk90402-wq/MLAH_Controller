using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using DevExpress.Map;
using DevExpress.Map.Kml.Model;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.CodeView.Margins;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using DevExpress.XtraPrinting.Native;
using MLAH_Controller;
using Windows.Storage.Provider;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using static DevExpress.Utils.Drawing.Helpers.NativeMethods;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static MLAH_Controller.CommonEvent;

namespace MLAH_Controller
{
    internal class MapOptions
    {
        // [효과 1] 전기가 흐르는 듯한 레이저 브러시 생성
        public static Brush GetFlowingStrokeBrush()
        {
            // 그라데이션 브러시 생성 (Cyan 계열)
            var brush = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 0), // 가로 방향 흐름
                MappingMode = BrushMappingMode.RelativeToBoundingBox,
                SpreadMethod = GradientSpreadMethod.Reflect
            };

            // 색상 배치: 투명 -> 밝은 Cyan -> 투명 (빛이 지나가는 느낌)
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(60, 0, 255, 255), 0.0));
            brush.GradientStops.Add(new GradientStop(Colors.Cyan, 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(60, 0, 255, 255), 1.0));

            // 이동 애니메이션 (TranslateTransform)
            var transform = new TranslateTransform();
            brush.RelativeTransform = transform;

            var animation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(1.5), // 속도 조절
                RepeatBehavior = RepeatBehavior.Forever
            };

            transform.BeginAnimation(TranslateTransform.XProperty, animation);
            return brush;
        }

        // [효과 2] 두 지점 사이의 방위각(Heading) 계산 (0~360도)
        public static double CalculateHeading(GeoPoint start, GeoPoint end)
        {
            double lat1 = start.Latitude * (Math.PI / 180.0);
            double lon1 = start.Longitude * (Math.PI / 180.0);
            double lat2 = end.Latitude * (Math.PI / 180.0);
            double lon2 = end.Longitude * (Math.PI / 180.0);

            double dLon = lon2 - lon1;
            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double brng = Math.Atan2(y, x);
            double deg = brng * (180.0 / Math.PI);

            // 지도는 보통 0도가 북쪽(12시) 기준이므로 보정 필요할 수 있음. 
            // 여기서는 일반적인 방위각 계산 후 정규화
            return (deg + 360) % 360;
        }


    }

    public class CustomMapPoint : GeoPoint
    {
        public int MissionID { get; set; }
        //public int PolygonIndex { get; set; }

        //private bool _IsShow = true;
        //public bool IsShow
        //{
        //    get
        //    {
        //        return _IsShow;
        //    }
        //    set
        //    {
        //        _IsShow = value;
        //        OnPropertyChanged("IsShow");
        //    }
        //}

        private string _TagString = "";
        public string TagString
        {
            get
            {
                return _TagString;
            }
            set
            {
                _TagString = value;
                OnPropertyChanged("TagString");
            }
        }

        private double _Heading = 0;
        public double Heading
        {
            get
            {
                return _Heading;
            }
            set
            {
                _Heading = value;
                OnPropertyChanged("Heading");
            }
        }



        #region 인터페이스 고정 구현부
        public event PropertyChangedEventHandler PropertyChangedEvent;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEvent?.Invoke(this, new PropertyChangedEventArgs(name));

        }
        #endregion 인터페이스 고정 구현부    
    }

    public class UnitMapObjectInfo : CommonBase
    {
        public UnitMapObjectInfo()
        {
            // 기본 좌표(예: 0,0)로 초기화하여 null을 방지합니다.
            Location = new GeoPoint(0, 0);
        }
        public uint ID { get; set; }

        public uint Status { get; set; }

        private GeoPoint _location;
        public GeoPoint Location
        {
            get => _location;
            set
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }
        public int Type { get; set; }

        public string TypeString { get; set; }
        public string PlatformString { get; set; }

        private double _heading;
        public double Heading
        {
            get => _heading;
            set
            {
                _heading = value;
                OnPropertyChanged(nameof(Heading));
            }
        }
        public string Name { get; set; }
        public string Description { get; set; }
        private ImageSource _imagesource;
        public ImageSource imagesource
        {
            get => _imagesource;
            set
            {
                _imagesource = value;
                OnPropertyChanged("imagesource");
            }
        }
    }

    public class LinearMissionResultSet
    {
        public List<GeoPoint> CenterPoints { get; set; }

        // List<GeoPoint> CorridorVertices -> List<List<GeoPoint>> SegmentRectangles
        // 이제 '사각형 목록'을 전달한다. 각 사각형은 GeoPoint 리스트(꼭짓점 4개)이다.
        public List<List<GeoPoint>> SegmentRectangles { get; set; }
        public int WidthMeters { get; set; }
    }
    public class CustomMapPolygon : MapPolygon
    {
        public int MissionID { get; set; }
        public int PolygonIndex { get; set; }

        //public bool IsShow { get; set; }

        //public int IsShow { get; set; }

        //private PolygonCoordCollection _PolygonCoordItems = new PolygonCoordCollection();
        //public PolygonCoordCollection PolygonCoordItems
        //{
        //    get
        //    {
        //        return _PolygonCoordItems;
        //    }
        //    set
        //    {
        //        _PolygonCoordItems = value;
        //        OnPropertyChanged("PolygonCoordItems");
        //    }
        //}

        #region 인터페이스 고정 구현부
        public event PropertyChangedEventHandler PropertyChangedEvent;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEvent?.Invoke(this, new PropertyChangedEventArgs(name));

        }
        #endregion 인터페이스 고정 구현부    
    }

    public class CustomMapLine : MapPolyline
    {
        //public CustomMapLine()
        //{
        //    Points = new Points();
        //}
        //나중에 enum으로
        //public int MissionType { get; set; }
        public int MissionId { get; set; }
        //public int ShapeId { get; set; }
        public int Width { get; set; }

        public MapCustomElement RelatedLabel { get; set; }
    }

    //Polygon 그리기
    // 폴리곤 그리기 상태
    public enum PolygonState
    {
        None,       // 폴리곤 안 그리는 중
        Drawing     // 폴리곤 그리고 있는 중
    }
}
