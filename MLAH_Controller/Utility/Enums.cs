using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLAH_Controller
{
    public enum ViewName
    {
        Main = 1,
        ScenarioView = 2,
        ScenarioObjectPopUp,
        AbnormalZone,
        ConfigPopup,
        BattlefieldEnv,
        ObjectSet,
        SetIM,
        Complexity,
        Mornitoring,
        UDPMornitoring,
        SINILSim,
        MUMTMission,
        Ref,
        CompScenarioMainView,
        Comp_ScenarioObjectSet_PopUp,
        Comp_LAH_Attack_Set_PopUp,

    }

    public enum VisibilityMode
    {
        Add = 1,
        Edit = 2,
    }
}
