using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;

namespace CargoInfoMod
{
    public static class UIHelper
    {
        public static IEnumerable<UIPanel> GetUIPanelInstances() => UIView.library.m_DynamicPanels.Select(p => p.instance).OfType<UIPanel>();
        public static string[] GetUIPanelNames() => GetUIPanelInstances().Select(p => p.name).ToArray();
        public static UIPanel GetPanel(string name)
        {
            return GetUIPanelInstances().FirstOrDefault(p => p.name == name);
        }
    }
}
