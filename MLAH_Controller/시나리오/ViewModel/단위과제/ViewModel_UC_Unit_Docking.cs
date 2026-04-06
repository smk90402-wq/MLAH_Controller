
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
    public class ViewModel_UC_Docking : CommonBase
    {
        #region Singleton
        static ViewModel_UC_Docking _ViewModel_UC_Docking = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_UC_Docking SingletonInstance
        {
            get
            {
                if (_ViewModel_UC_Docking == null)
                {
                    _ViewModel_UC_Docking = new ViewModel_UC_Docking();
                }
                return _ViewModel_UC_Docking;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_UC_Docking()
        {

        }

        #endregion 생성자 & 콜백



    }
}
