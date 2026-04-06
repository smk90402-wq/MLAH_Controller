using System.Windows.Threading;
using MLAHInterop;
using REALTIMEVISUAL.Native.FederateInterface;
using System.IO;


namespace MLAH_Controller
{
    public partial class ViewModel_Parameter : CommonBase
    {
        #region LAH 1 Property

        private int _LAH1Hit = 0;
        public int LAH1Hit
        {
            get
            {
                return _LAH1Hit;
            }
            set
            {
                _LAH1Hit = value;
                OnPropertyChanged("LAH1Hit");
            }
        }

        private int _LAH1Link1 = 0;
        public int LAH1Link1
        {
            get
            {
                return _LAH1Link1;
            }
            set
            {
                _LAH1Link1 = value;
                OnPropertyChanged("LAH1Link1");
            }
        }

        private int _LAH1Link2 = 0;
        public int LAH1Link2
        {
            get
            {
                return _LAH1Link2;
            }
            set
            {
                _LAH1Link2 = value;
                OnPropertyChanged("LAH1Link2");
            }
        }

        private int _LAH1Link3 = 0;
        public int LAH1Link3
        {
            get
            {
                return _LAH1Link3;
            }
            set
            {
                _LAH1Link3 = value;
                OnPropertyChanged("LAH1Link3");
            }
        }

        private int _LAH1FuelWarning = 0;
        public int LAH1FuelWarning
        {
            get
            {
                return _LAH1FuelWarning;
            }
            set
            {
                _LAH1FuelWarning = value;
                OnPropertyChanged("LALAH1FuelWarningH1Link3");
            }
        }

        private int _LAH1FuelDanger = 0;
        public int LAH1FuelDanger
        {
            get
            {
                return _LAH1FuelDanger;
            }
            set
            {
                _LAH1FuelDanger = value;
                OnPropertyChanged("LAH1FuelDanger");
            }
        }

        private int _LAH1FuelZero = 0;
        public int LAH1FuelZero
        {
            get
            {
                return _LAH1FuelZero;
            }
            set
            {
                _LAH1FuelZero = value;
                OnPropertyChanged("LAH1FuelZero");
            }
        }

        private int _LAH1Crash = 0;
        public int LAH1Crash
        {
            get
            {
                return _LAH1Crash;
            }
            set
            {
                _LAH1Crash = value;
                OnPropertyChanged("LAH1Crash");
            }
        }

        private int _LAH1Sensor = 0;
        public int LAH1Sensor
        {
            get
            {
                return _LAH1Sensor;
            }
            set
            {
                _LAH1Sensor = value;
                OnPropertyChanged("LAH1Sensor");
            }
        }

        #endregion LAH 1 Property

        #region LAH 2 Property

        private int _LAH2Hit = 0;
        public int LAH2Hit
        {
            get
            {
                return _LAH2Hit;
            }
            set
            {
                _LAH2Hit = value;
                OnPropertyChanged("LAH2Hit");
            }
        }

        private int _LAH2Link1 = 0;
        public int LAH2Link1
        {
            get
            {
                return _LAH2Link1;
            }
            set
            {
                _LAH2Link1 = value;
                OnPropertyChanged("LAH2Link1");
            }
        }

        private int _LAH2Link2 = 0;
        public int LAH2Link2
        {
            get
            {
                return _LAH2Link2;
            }
            set
            {
                _LAH2Link2 = value;
                OnPropertyChanged("LAH2Link2");
            }
        }

        private int _LAH2Link3 = 0;
        public int LAH2Link3
        {
            get
            {
                return _LAH2Link3;
            }
            set
            {
                _LAH2Link3 = value;
                OnPropertyChanged("LAH2Link3");
            }
        }

        private int _LAH2FuelWarning = 0;
        public int LAH2FuelWarning
        {
            get
            {
                return _LAH2FuelWarning;
            }
            set
            {
                _LAH2FuelWarning = value;
                OnPropertyChanged("LALAH2FuelWarningH1Link3");
            }
        }

        private int _LAH2FuelDanger = 0;
        public int LAH2FuelDanger
        {
            get
            {
                return _LAH2FuelDanger;
            }
            set
            {
                _LAH2FuelDanger = value;
                OnPropertyChanged("LAH2FuelDanger");
            }
        }

        private int _LAH2FuelZero = 0;
        public int LAH2FuelZero
        {
            get
            {
                return _LAH2FuelZero;
            }
            set
            {
                _LAH2FuelZero = value;
                OnPropertyChanged("LAH2FuelZero");
            }
        }

        private int _LAH2Crash = 0;
        public int LAH2Crash
        {
            get
            {
                return _LAH2Crash;
            }
            set
            {
                _LAH2Crash = value;
                OnPropertyChanged("LAH2Crash");
            }
        }

        private int _LAH2Sensor = 0;
        public int LAH2Sensor
        {
            get
            {
                return _LAH2Sensor;
            }
            set
            {
                _LAH2Sensor = value;
                OnPropertyChanged("LAH2Sensor");
            }
        }

        #endregion LAH 2 Property

        #region LAH 3 Property

        private int _LAH3Hit = 0;
        public int LAH3Hit
        {
            get
            {
                return _LAH3Hit;
            }
            set
            {
                _LAH3Hit = value;
                OnPropertyChanged("LAH3Hit");
            }
        }

        private int _LAH3Link1 = 0;
        public int LAH3Link1
        {
            get
            {
                return _LAH3Link1;
            }
            set
            {
                _LAH3Link1 = value;
                OnPropertyChanged("LAH3Link1");
            }
        }

        private int _LAH3Link2 = 0;
        public int LAH3Link2
        {
            get
            {
                return _LAH3Link2;
            }
            set
            {
                _LAH3Link2 = value;
                OnPropertyChanged("LAH3Link2");
            }
        }

        private int _LAH3Link3 = 0;
        public int LAH3Link3
        {
            get
            {
                return _LAH3Link3;
            }
            set
            {
                _LAH3Link3 = value;
                OnPropertyChanged("LAH3Link3");
            }
        }

        private int _LAH3FuelWarning = 0;
        public int LAH3FuelWarning
        {
            get
            {
                return _LAH3FuelWarning;
            }
            set
            {
                _LAH3FuelWarning = value;
                OnPropertyChanged("LALAH3FuelWarningH1Link3");
            }
        }

        private int _LAH3FuelDanger = 0;
        public int LAH3FuelDanger
        {
            get
            {
                return _LAH3FuelDanger;
            }
            set
            {
                _LAH3FuelDanger = value;
                OnPropertyChanged("LAH3FuelDanger");
            }
        }

        private int _LAH3FuelZero = 0;
        public int LAH3FuelZero
        {
            get
            {
                return _LAH3FuelZero;
            }
            set
            {
                _LAH3FuelZero = value;
                OnPropertyChanged("LAH3FuelZero");
            }
        }

        private int _LAH3Crash = 0;
        public int LAH3Crash
        {
            get
            {
                return _LAH3Crash;
            }
            set
            {
                _LAH3Crash = value;
                OnPropertyChanged("LAH3Crash");
            }
        }

        private int _LAH3Sensor = 0;
        public int LAH3Sensor
        {
            get
            {
                return _LAH3Sensor;
            }
            set
            {
                _LAH3Sensor = value;
                OnPropertyChanged("LAH3Sensor");
            }
        }

        #endregion LAH 3 Property

        #region UAV 1 Property

        private int _UAV1Health = 0;
        public int UAV1Health
        {
            get
            {
                return _UAV1Health;
            }
            set
            {
                _UAV1Health = value;
                OnPropertyChanged("UAV1Health");
            }
        }

        private int _UAV1Sensor = 0;
        public int UAV1Sensor
        {
            get
            {
                return _UAV1Sensor;
            }
            set
            {
                _UAV1Sensor = value;
                OnPropertyChanged("UAV1Sensor");
            }
        }

        private int _UAV1Fuel = 0;
        public int UAV1Fuel
        {
            get
            {
                return _UAV1Fuel;
            }
            set
            {
                _UAV1Fuel = value;
                OnPropertyChanged("UAV1Fuel");
            }
        }

        #endregion UAV 1 Property

        #region UAV 2 Property

        private int _UAV2Health = 0;
        public int UAV2Health
        {
            get
            {
                return _UAV2Health;
            }
            set
            {
                _UAV2Health = value;
                OnPropertyChanged("UAV2Health");
            }
        }

        private int _UAV2Sensor = 0;
        public int UAV2Sensor
        {
            get
            {
                return _UAV2Sensor;
            }
            set
            {
                _UAV2Sensor = value;
                OnPropertyChanged("UAV2Sensor");
            }
        }

        private int _UAV2Fuel = 0;
        public int UAV2Fuel
        {
            get
            {
                return _UAV2Fuel;
            }
            set
            {
                _UAV2Fuel = value;
                OnPropertyChanged("UAV2Fuel");
            }
        }

        #endregion UAV 2 Property

        #region UAV 3 Property

        private int _UAV3Health = 0;
        public int UAV3Health
        {
            get
            {
                return _UAV3Health;
            }
            set
            {
                _UAV3Health = value;
                OnPropertyChanged("UAV3Health");
            }
        }

        private int _UAV3Sensor = 0;
        public int UAV3Sensor
        {
            get
            {
                return _UAV3Sensor;
            }
            set
            {
                _UAV3Sensor = value;
                OnPropertyChanged("UAV3Sensor");
            }
        }

        private int _UAV3Fuel = 0;
        public int UAV3Fuel
        {
            get
            {
                return _UAV3Fuel;
            }
            set
            {
                _UAV3Fuel = value;
                OnPropertyChanged("UAV3Fuel");
            }
        }

        #endregion UAV 3 Property

        private bool _Drone1Enable = false;
        public bool Drone1Enable
        {
            get
            {
                return _Drone1Enable;
            }
            set
            {
                _Drone1Enable = value;
                OnPropertyChanged("Drone1Enable");
            }
        }

        private double _Drone1Opacity = 1;
        public double Drone1Opacity
        {
            get
            {
                return _Drone1Opacity;
            }
            set
            {
                _Drone1Opacity = value;
                OnPropertyChanged("Drone1Opacity");
            }
        }

        private int _Drone1Status = 0;
        public int Drone1Status
        {
            get
            {
                return _Drone1Status;
            }
            set
            {
                _Drone1Status = value;
                if(value == 2)
                {
                    Drone1ReasonEnable = true;
                }
                else
                {
                    Drone1ReasonEnable = false;
                    Drone1Reason = 0;
                }
                OnPropertyChanged("Drone1Status");
            }
        }

        private int _Drone1Reason = 0;
        public int Drone1Reason
        {
            get
            {
                return _Drone1Reason;
            }
            set
            {
                _Drone1Reason = value;
                OnPropertyChanged("Drone1Reason");
            }
        }

        private bool _Drone2Enable = false;
        public bool Drone2Enable
        {
            get
            {
                return _Drone2Enable;
            }
            set
            {
                _Drone2Enable = value;
                OnPropertyChanged("Drone2Enable");
            }
        }

        private double _Drone2Opacity = 1;
        public double Drone2Opacity
        {
            get
            {
                return _Drone2Opacity;
            }
            set
            {
                _Drone2Opacity = value;
                OnPropertyChanged("Drone2Opacity");
            }
        }

        private int _Drone2Status = 0;
        public int Drone2Status
        {
            get
            {
                return _Drone2Status;
            }
            set
            {
                _Drone2Status = value;
                if (value == 2)
                {
                    Drone2ReasonEnable = true;
                }
                else
                {
                    Drone2ReasonEnable = false;
                    Drone2Reason = 0;
                }
                OnPropertyChanged("Drone2Status");
            }
        }

        private int _Drone2Reason = 0;
        public int Drone2Reason
        {
            get
            {
                return _Drone2Reason;
            }
            set
            {
                _Drone2Reason = value;
                OnPropertyChanged("Drone3Reason");
            }
        }

        private bool _Drone3Enable = false;
        public bool Drone3Enable
        {
            get
            {
                return _Drone3Enable;
            }
            set
            {
                _Drone3Enable = value;
                OnPropertyChanged("Drone3Enable");
            }
        }

        private double _Drone3Opacity = 1;
        public double Drone3Opacity
        {
            get
            {
                return _Drone3Opacity;
            }
            set
            {
                _Drone3Opacity = value;
                OnPropertyChanged("Drone3Opacity");
            }
        }

        private int _Drone3Status = 0;
        public int Drone3Status
        {
            get
            {
                return _Drone3Status;
            }
            set
            {
                _Drone3Status = value;
                if (value == 2)
                {
                    Drone3ReasonEnable = true;
                }
                else
                {
                    Drone3ReasonEnable = false;
                    Drone3Reason = 0;
                }
                OnPropertyChanged("Drone3Status");
            }
        }

        private int _Drone3Reason = 0;
        public int Drone3Reason
        {
            get
            {
                return _Drone3Reason;
            }
            set
            {
                _Drone3Reason = value;
                OnPropertyChanged("Drone3Reason");
            }
        }

        private bool _Heli1Enable = false;
        public bool Heli1Enable
        {
            get
            {
                return _Heli1Enable;
            }
            set
            {
                _Heli1Enable = value;
                OnPropertyChanged("Heli1Enable");
            }
        }

        private double _Heli1Opacity = 1;
        public double Heli1Opacity
        {
            get
            {
                return _Heli1Opacity;
            }
            set
            {
                _Heli1Opacity = value;
                OnPropertyChanged("Heli1Opacity");
            }
        }

        private int _Heli1Status = 0;
        public int Heli1Status
        {
            get
            {
                return _Heli1Status;
            }
            set
            {
                _Heli1Status = value;
                if (value == 2)
                {
                    Heli1ReasonEnable = true;
                }
                else
                {
                    Heli1ReasonEnable = false;
                    Heli1Reason = 0;
                }
                OnPropertyChanged("Heli1Status");
            }
        }

        private int _Heli1Reason = 0;
        public int Heli1Reason
        {
            get
            {
                return _Heli1Reason;
            }
            set
            {
                _Heli1Reason = value;
                OnPropertyChanged("Heli1Reason");
            }
        }

        private bool _Heli2Enable = false;
        public bool Heli2Enable
        {
            get
            {
                return _Heli2Enable;
            }
            set
            {
                _Heli2Enable = value;
                OnPropertyChanged("Heli2Enable");
            }
        }

        private double _Heli2Opacity = 1;
        public double Heli2Opacity
        {
            get
            {
                return _Heli2Opacity;
            }
            set
            {
                _Heli2Opacity = value;
                OnPropertyChanged("Heli2Opacity");
            }
        }

        private int _Heli2Status = 0;
        public int Heli2Status
        {
            get
            {
                return _Heli2Status;
            }
            set
            {
                _Heli2Status = value;
                if (value == 2)
                {
                    Heli2ReasonEnable = true;
                }
                else
                {
                    Heli2ReasonEnable = false;
                    Heli2Reason = 0;
                }
                OnPropertyChanged("Heli2Status");
            }
        }

        private int _Heli2Reason = 0;
        public int Heli2Reason
        {
            get
            {
                return _Heli2Reason;
            }
            set
            {
                _Heli2Reason = value;
                OnPropertyChanged("Heli2Reason");
            }
        }

        private bool _Heli3Enable = false;
        public bool Heli3Enable
        {
            get
            {
                return _Heli3Enable;
            }
            set
            {
                _Heli3Enable = value;
                OnPropertyChanged("Heli3Enable");
            }
        }

        private double _Heli3Opacity = 1;
        public double Heli3Opacity
        {
            get
            {
                return _Heli3Opacity;
            }
            set
            {
                _Heli3Opacity = value;
                OnPropertyChanged("Heli3Opacity");
            }
        }

        private int _Heli3Status = 0;
        public int Heli3Status
        {
            get
            {
                return _Heli3Status;
            }
            set
            {
                _Heli3Status = value;
                if (value == 2)
                {
                    Heli3ReasonEnable = true;
                }
                else
                {
                    Heli3ReasonEnable = false;
                    Heli3Reason = 0;
                }
                OnPropertyChanged("Heli3Status");
            }
        }

        private int _Heli3Reason = 0;
        public int Heli3Reason
        {
            get
            {
                return _Heli3Reason;
            }
            set
            {
                _Heli3Reason = value;
                OnPropertyChanged("Heli3Reason");
            }
        }

        private bool _Heli1ReasonEnable = false;
        public bool Heli1ReasonEnable
        {
            get
            {
                return _Heli1ReasonEnable;
            }
            set
            {
                _Heli1ReasonEnable = value;
                OnPropertyChanged("Heli1ReasonEnable");
            }
        }

        private bool _Heli2ReasonEnable = false;
        public bool Heli2ReasonEnable
        {
            get
            {
                return _Heli2ReasonEnable;
            }
            set
            {
                _Heli2ReasonEnable = value;
                OnPropertyChanged("Heli2ReasonEnable");
            }
        }

        private bool _Heli3ReasonEnable = false;
        public bool Heli3ReasonEnable
        {
            get
            {
                return _Heli3ReasonEnable;
            }
            set
            {
                _Heli3ReasonEnable = value;
                OnPropertyChanged("Heli3ReasonEnable");
            }
        }

        private bool _Drone2ReasonEnable = false;
        public bool Drone2ReasonEnable
        {
            get
            {
                return _Drone2ReasonEnable;
            }
            set
            {
                _Drone2ReasonEnable = value;
                OnPropertyChanged("Drone2ReasonEnable");
            }
        }

        private bool _Drone3ReasonEnable = false;
        public bool Drone3ReasonEnable
        {
            get
            {
                return _Drone3ReasonEnable;
            }
            set
            {
                _Drone3ReasonEnable = value;
                OnPropertyChanged("Drone3ReasonEnable");
            }
        }

        private bool _Drone1ReasonEnable = false;
        public bool Drone1ReasonEnable
        {
            get
            {
                return _Drone1ReasonEnable;
            }
            set
            {
                _Drone1ReasonEnable = value;
                OnPropertyChanged("Drone1ReasonEnable");
            }
        }
    }
}
