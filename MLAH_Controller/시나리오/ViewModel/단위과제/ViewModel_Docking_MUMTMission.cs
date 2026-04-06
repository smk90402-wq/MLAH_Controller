
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
    public class ViewModel_Docking_MUMTMission : CommonBase
    {
        #region Singleton
        static ViewModel_Docking_MUMTMission _ViewModel_Docking_MUMTMission = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_Docking_MUMTMission SingletonInstance
        {
            get
            {
                if (_ViewModel_Docking_MUMTMission == null)
                {
                    _ViewModel_Docking_MUMTMission = new ViewModel_Docking_MUMTMission();
                }
                return _ViewModel_Docking_MUMTMission;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_Docking_MUMTMission()
        {

        }

        #endregion 생성자 & 콜백



    }
}
