using System.Linq;
using ColossalFramework.UI;
using UnityEngine;

namespace CargoInfoMod
{
    class UICargoChart: UIRadialChart
    {
        public UICargoChart()
        {
            spriteName = "PieChartBg";
            size = new Vector2(90, 90);
            for (int i = 0; i < CargoParcel.ResourceTypes.Length; i++)
            {
                var resourceColor = TransferManager.instance.m_properties.m_resourceColors[13 + i / 2];
                if (i % 2 != 0)
                {
                    // Refined resources are slightly darker colored
                    resourceColor.r *= 0.8f;
                    resourceColor.g *= 0.8f;
                    resourceColor.b *= 0.8f;
                }
                AddSlice();
                GetSlice(i).innerColor = GetSlice(i).outterColor = resourceColor;
            }
            SetValues(CargoParcel.ResourceTypes.Select(t => 0f).ToArray());
        }
    }
}
