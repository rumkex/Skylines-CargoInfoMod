using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CargoInfoMod.Data;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace CargoInfoMod
{
    class CargoUIPanel: UIPanel
    {
        private ModInfo mod;

        private bool displayCurrent;

        private readonly CarFlags[] resources =
        {
            CarFlags.Oil,
            CarFlags.Petrol,
            CarFlags.Ore,
            CarFlags.Coal,
            CarFlags.Logs,
            CarFlags.Lumber,
            CarFlags.Grain,
            CarFlags.Food,
            CarFlags.Goods
        };

        public CargoUIPanel()
        {
            mod = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).userModInstance as ModInfo;
        }

        private List<UIRadialChart> charts = new List<UIRadialChart>();
        private List<UILabel> labels = new List<UILabel>();

        private readonly List<Func<CarFlags, bool>> cargoCategories = new List<Func<CarFlags, bool>>
        {
            t => (t & CarFlags.Sent) == 0 && (t & CarFlags.Imported) == 0 && (t & CarFlags.Exported) == 0,
            t => (t & CarFlags.Sent) == 0 && (t & CarFlags.Imported) != 0 && (t & CarFlags.Exported) == 0,
            t => (t & CarFlags.Sent) == 0 && (t & CarFlags.Imported) == 0 && (t & CarFlags.Exported) != 0,
            t => (t & CarFlags.Sent) != 0 && (t & CarFlags.Imported) == 0 && (t & CarFlags.Exported) == 0,
            t => (t & CarFlags.Sent) != 0 && (t & CarFlags.Imported) != 0 && (t & CarFlags.Exported) == 0,
            t => (t & CarFlags.Sent) != 0 && (t & CarFlags.Imported) == 0 && (t & CarFlags.Exported) != 0
        };

        public override void Awake()
        {
            backgroundSprite = "MenuPanel2";
            opacity = 0.9f;

            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;

            var handle = AddUIComponent<UIDragHandle>();
            handle.size = new Vector2(384, 40);

            var windowLabel = handle.AddUIComponent<UILabel>();
            windowLabel.text = "Cargo Statistics";
            windowLabel.anchor = UIAnchorStyle.CenterVertical | UIAnchorStyle.CenterHorizontal;

            var closeButton = handle.AddUIComponent<UIButton>();
            closeButton.size = new Vector2(32, 32);
            closeButton.relativePosition = new Vector3(350, 2, 0);
            closeButton.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
            closeButton.normalBgSprite = "buttonclose";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.eventClicked += (sender, e) => Hide();

            var labelPanel = AddUIComponent<UIPanel>();
            labelPanel.size = new Vector2(384, 20);
            labelPanel.autoLayout = true;
            labelPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            labelPanel.autoLayoutStart = LayoutStart.TopRight;
            labelPanel.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var localLabel = labelPanel.AddUIComponent<UILabel>();
            localLabel.autoSize = false;
            localLabel.size = new Vector2(90, 20);
            localLabel.text = "Local";
            localLabel.textAlignment = UIHorizontalAlignment.Center;

            var importLabel = labelPanel.AddUIComponent<UILabel>();
            importLabel.autoSize = false;
            importLabel.size = new Vector2(90, 20);
            importLabel.text = "Import";
            importLabel.textAlignment = UIHorizontalAlignment.Center;

            var exportLabel = labelPanel.AddUIComponent<UILabel>();
            exportLabel.autoSize = false;
            exportLabel.size = new Vector2(90, 20);
            exportLabel.text = "Export";
            exportLabel.textAlignment = UIHorizontalAlignment.Center;

            var rcvdPanel = AddUIComponent<UIPanel>();
            rcvdPanel.size = new Vector2(384, 100);
            rcvdPanel.autoLayout = true;
            rcvdPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rcvdPanel.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var rcvdLabel = rcvdPanel.AddUIComponent<UILabel>();
            rcvdLabel.text = "Received";
            rcvdLabel.textAlignment = UIHorizontalAlignment.Center;
            rcvdLabel.verticalAlignment = UIVerticalAlignment.Middle;
            rcvdLabel.autoSize = false;
            rcvdLabel.size = new Vector2(75, 100);

            var rcvdStatPanel = AddUIComponent<UIPanel>();
            rcvdStatPanel.size = new Vector2(384, 20);
            rcvdStatPanel.autoLayout = true;
            rcvdStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rcvdStatPanel.autoLayoutStart = LayoutStart.TopRight;
            rcvdStatPanel.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var sentPanel = AddUIComponent<UIPanel>();
            sentPanel.size = new Vector2(384, 100);
            sentPanel.autoLayout = true;
            sentPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            sentPanel.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var sentLabel = sentPanel.AddUIComponent<UILabel>();
            sentLabel.text = "Sent";
            sentLabel.textAlignment = UIHorizontalAlignment.Center;
            sentLabel.verticalAlignment = UIVerticalAlignment.Middle;
            sentLabel.autoSize = false;
            sentLabel.size = new Vector2(75, 100);

            var sentStatPanel = AddUIComponent<UIPanel>();
            sentStatPanel.size = new Vector2(384, 30);
            sentStatPanel.autoLayout = true;
            sentStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            sentStatPanel.autoLayoutStart = LayoutStart.TopRight;
            sentStatPanel.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var resetButton = sentStatPanel.AddUIComponent<UIButton>();
            resetButton.text = "Res";
            resetButton.normalBgSprite = "ButtonMenu";
            resetButton.pressedBgSprite = "ButtonMenuPressed";
            resetButton.hoveredBgSprite = "ButtonMenuHovered";
            resetButton.textScale = 0.6f;
            resetButton.autoSize = false;
            resetButton.size = new Vector2(32, 10);
            resetButton.tooltip = "Reset all counters for this building - use with care!";

            resetButton.eventClicked += (sender, e) =>
            {
                if (!mod.data.TryGetEntry(WorldInfoPanel.GetCurrentInstanceID().Building, out CargoStats2 stats)) return;
                Array.Clear(stats.CarsCounted, 0, stats.CarsCounted.Length);
            };

            var modeButton = sentStatPanel.AddUIComponent<UIButton>();
            modeButton.text = "Prev";
            modeButton.normalBgSprite = "ButtonMenu";
            modeButton.pressedBgSprite = "ButtonMenuPressed";
            modeButton.hoveredBgSprite = "ButtonMenuHovered";
            modeButton.textScale = 0.6f;
            modeButton.autoSize = false;
            modeButton.size = new Vector2(32, 10);
            modeButton.tooltip = "Switch between display modes (now displaying values for last month)";

            modeButton.eventClicked += (sender, e) =>
            {
                displayCurrent = !displayCurrent;
                modeButton.text = displayCurrent ? "Cur" : "Prev";
                modeButton.tooltip = displayCurrent
                    ? "Switch between display modes (now displaying values for current month)"
                    : "Switch between display modes (now displaying values for previous month)";
                modeButton.RefreshTooltip();
            };

            for (int n = 0; n < cargoCategories.Count; n++)
            {
                var category = cargoCategories[n];
                var chart = (n > 2 ? sentPanel: rcvdPanel).AddUIComponent<UIRadialChart>();
                chart.size = new Vector2(90, 90);
                for (int i = 0; i < resources.Length; i++)
                {
                    var resourceColor = TransferManager.instance.m_properties.m_resourceColors[13 + i / 2];
                    if (i % 2 != 0)
                    {
                        // Refined resources are slightly darker colored
                        resourceColor.r *= 0.8f;
                        resourceColor.g *= 0.8f;
                        resourceColor.b *= 0.8f;
                    }
                    chart.AddSlice();
                    chart.GetSlice(i).innerColor = chart.GetSlice(i).outterColor = resourceColor;
                }
                chart.SetValues(resources.Select(t => 0f).ToArray());
                chart.spriteName = "PieChartBg";
                charts.Add(chart);

                var label = (n > 2 ? sentStatPanel : rcvdStatPanel).AddUIComponent<UILabel>();
                label.autoSize = false;
                label.size = new Vector2(90, 20);
                label.text = "0k units";
                label.textScale = 0.8f;
                label.textColor = new Color32(206, 248, 0, 255);
                label.textAlignment = UIHorizontalAlignment.Center;
                labels.Add(label);
            }

            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            canFocus = true;
            isInteractive = true;

            FitChildren();

            Hide();
        }

        public override void Update()
        {
            if (!isVisible) return;

            if (mod?.data == null) return;

            if (!mod.data.TryGetEntry(WorldInfoPanel.GetCurrentInstanceID().Building, out CargoStats2 stats)) return;

            for (var i = 0; i < cargoCategories.Count; i++)
            {
                var category = cargoCategories[i];

                var testFlag = displayCurrent ? CarFlags.None : CarFlags.Previous;

                var categoryTotal = stats.GetTotalWhere(t => category(t) && (t & CarFlags.Previous) == testFlag);

                labels[i].text = string.Format("{0:0}k units", categoryTotal / 1000);

                if (categoryTotal == 0)
                {
                    charts[i].SetValues(resources.Select(t => 0f).ToArray());
                    continue;
                }

                var partStats = resources.Select(type => stats.GetTotalWhere(
                    t => category(t) && (t & CarFlags.Resource) == type && (t & CarFlags.Previous) == testFlag
                    ) / (float)categoryTotal).ToArray();

                charts[i].SetValues(partStats.ToArray());
            }
        }
    }
}

