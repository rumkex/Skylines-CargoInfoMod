using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CargoInfoMod.Data;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace CargoInfoMod
{
    class CargoUIPanel: UIPanel
    {
        private const int Width = 384;
        private const int HandleHeight = 40;
        private const int LabelHeight = 20;
        private const int LabelWidth = 90;
        private const int StatPanelHeight = 30;
        private readonly Vector2 ExitButtonSize = new Vector2(32, 32);
        private readonly Vector2 ModeButtonSize = new Vector2(32, 10);
        private readonly Vector2 ChartSize = new Vector2(90, 90);
        private readonly RectOffset Padding = new RectOffset(2, 2, 2, 2);
        private readonly Color32 CargoUnitColor = new Color32(206, 248, 0, 255);

        private ModInfo mod;

        private bool displayCurrent;
        private ushort lastSelectedBuilding;

        public CargoUIPanel()
        {
            mod = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).userModInstance as ModInfo;
        }

        private List<UICargoChart> charts = new List<UICargoChart>();
        private List<UILabel> labels = new List<UILabel>();
        private UILabel windowLabel, localLabel, importLabel, exportLabel, rcvdLabel, sentLabel;
        private UIButton resetButton, modeButton;

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
            handle.size = new Vector2(Width, HandleHeight);

            windowLabel = handle.AddUIComponent<UILabel>();
            windowLabel.anchor = UIAnchorStyle.CenterVertical | UIAnchorStyle.CenterHorizontal;

            var closeButton = handle.AddUIComponent<UIButton>();
            closeButton.size = ExitButtonSize;
            closeButton.relativePosition = new Vector3(Width - ExitButtonSize.x, 0, 0);
            closeButton.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
            closeButton.normalBgSprite = "buttonclose";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.eventClicked += (sender, e) => Hide();

            var labelPanel = AddUIComponent<UIPanel>();
            labelPanel.size = new Vector2(Width, LabelHeight);
            labelPanel.autoLayout = true;
            labelPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            labelPanel.autoLayoutStart = LayoutStart.TopRight;
            labelPanel.autoLayoutPadding = Padding;

            localLabel = labelPanel.AddUIComponent<UILabel>();
            localLabel.autoSize = false;
            localLabel.size = new Vector2(ChartSize.x, LabelHeight);
            localLabel.textAlignment = UIHorizontalAlignment.Center;

            importLabel = labelPanel.AddUIComponent<UILabel>();
            importLabel.autoSize = false;
            importLabel.size = new Vector2(ChartSize.x, LabelHeight);
            importLabel.textAlignment = UIHorizontalAlignment.Center;

            exportLabel = labelPanel.AddUIComponent<UILabel>();
            exportLabel.autoSize = false;
            exportLabel.size = new Vector2(ChartSize.x, LabelHeight);
            exportLabel.textAlignment = UIHorizontalAlignment.Center;

            var rcvdPanel = AddUIComponent<UIPanel>();
            rcvdPanel.size = new Vector2(Width, ChartSize.y);
            rcvdPanel.autoLayout = true;
            rcvdPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rcvdPanel.autoLayoutStart = LayoutStart.TopRight;
            rcvdPanel.autoLayoutPadding = Padding;

            rcvdLabel = rcvdPanel.AddUIComponent<UILabel>();
            rcvdLabel.textAlignment = UIHorizontalAlignment.Right;
            rcvdLabel.verticalAlignment = UIVerticalAlignment.Middle;
            rcvdLabel.autoSize = false;
            rcvdLabel.size = new Vector2(LabelWidth, ChartSize.y);

            var rcvdStatPanel = AddUIComponent<UIPanel>();
            rcvdStatPanel.size = new Vector2(Width, StatPanelHeight);
            rcvdStatPanel.autoLayout = true;
            rcvdStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rcvdStatPanel.autoLayoutStart = LayoutStart.TopRight;
            rcvdStatPanel.autoLayoutPadding = Padding;

            var sentPanel = AddUIComponent<UIPanel>();
            sentPanel.size = new Vector2(Width, ChartSize.y);
            sentPanel.autoLayout = true;
            sentPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            sentPanel.autoLayoutStart = LayoutStart.TopRight;
            sentPanel.autoLayoutPadding = Padding;

            sentLabel = sentPanel.AddUIComponent<UILabel>();
            sentLabel.textAlignment = UIHorizontalAlignment.Right;
            sentLabel.verticalAlignment = UIVerticalAlignment.Middle;
            sentLabel.autoSize = false;
            sentLabel.size = new Vector2(LabelWidth, ChartSize.y);

            var sentStatPanel = AddUIComponent<UIPanel>();
            sentStatPanel.size = new Vector2(Width, StatPanelHeight);
            sentStatPanel.autoLayout = true;
            sentStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            sentStatPanel.autoLayoutStart = LayoutStart.TopRight;
            sentStatPanel.autoLayoutPadding = Padding;

            resetButton = sentStatPanel.AddUIComponent<UIButton>();
            resetButton.text = "Res";
            resetButton.normalBgSprite = "ButtonMenu";
            resetButton.pressedBgSprite = "ButtonMenuPressed";
            resetButton.hoveredBgSprite = "ButtonMenuHovered";
            resetButton.textScale = 0.6f;
            resetButton.autoSize = false;
            resetButton.size = ModeButtonSize;

            resetButton.eventClicked += (sender, e) =>
            {
                if (!mod.data.TryGetEntry(WorldInfoPanel.GetCurrentInstanceID().Building, out CargoStats2 stats)) return;
                Array.Clear(stats.CarsCounted, 0, stats.CarsCounted.Length);
            };

            modeButton = sentStatPanel.AddUIComponent<UIButton>();
            modeButton.text = "Prev";
            modeButton.normalBgSprite = "ButtonMenu";
            modeButton.pressedBgSprite = "ButtonMenuPressed";
            modeButton.hoveredBgSprite = "ButtonMenuHovered";
            modeButton.textScale = 0.6f;
            modeButton.autoSize = false;
            modeButton.size = ModeButtonSize;

            modeButton.eventClicked += (sender, e) =>
            {
                displayCurrent = !displayCurrent;
                modeButton.text = displayCurrent ? "Cur" : "Prev";
                modeButton.tooltip = displayCurrent
                    ? Localization.Get("SWITCH_MODES_TOOLTIP_CUR")
                    : Localization.Get("SWITCH_MODES_TOOLTIP_PREV");
                modeButton.RefreshTooltip();
            };

            for (int n = 0; n < cargoCategories.Count; n++)
            {
                var chart = (n > 2 ? sentPanel: rcvdPanel).AddUIComponent<UICargoChart>();
                chart.size = ChartSize;
                charts.Add(chart);

                var label = (n > 2 ? sentStatPanel : rcvdStatPanel).AddUIComponent<UILabel>();
                label.autoSize = false;
                label.size = new Vector2(ChartSize.x, StatPanelHeight);
                label.textScale = 0.8f;
                label.textColor = CargoUnitColor;
                label.textAlignment = UIHorizontalAlignment.Center;
                labels.Add(label);
            }

            FitChildren(new Vector2(Padding.top, Padding.left));

            // Load the locale and update it if game locale changes
            UpdateLocale();
            LocaleManager.eventLocaleChanged += UpdateLocale;

            base.Awake();
        }

        public void UpdateLocale()
        {
            windowLabel.text = Localization.Get("STATS_WINDOW_LABEL");
            localLabel.text = Localization.Get("LOCAL");
            importLabel.text = Localization.Get("IMPORT");
            exportLabel.text = Localization.Get("EXPORT");
            rcvdLabel.text = Localization.Get("RECEIVED");
            sentLabel.text = Localization.Get("SENT");
            modeButton.tooltip = displayCurrent ?
                Localization.Get("SWITCH_MODES_TOOLTIP_CUR") :
                Localization.Get("SWITCH_MODES_TOOLTIP_PREV");
            resetButton.tooltip = Localization.Get("RESET_COUNTERS_TOOLTIP");

            UpdateCounterValues();
        }

        public void UpdateCounterValues()
        {
            if (!mod.data.TryGetEntry(lastSelectedBuilding, out CargoStats2 stats)) return;

            for (var i = 0; i < cargoCategories.Count; i++)
            {
                var category = cargoCategories[i];

                var testFlag = displayCurrent ? CarFlags.None : CarFlags.Previous;

                var categoryTotal = stats.GetTotalWhere(t => category(t) && (t & CarFlags.Previous) == testFlag);

                labels[i].text = string.Format("{0:0}{1}", categoryTotal / 1000, Localization.Get("KILO_UNITS"));

                if (categoryTotal == 0)
                {
                    charts[i].SetValues(CargoParcel.ResourceTypes.Select(t => 0f).ToArray());
                    continue;
                }

                var partStats = CargoParcel.ResourceTypes.Select(type => stats.GetTotalWhere(
                                                                             t => category(t) && (t & CarFlags.Resource) == type && (t & CarFlags.Previous) == testFlag
                                                                         ) / (float)categoryTotal).ToArray();

                charts[i].SetValues(partStats.ToArray());
            }
        }

        public override void Start()
        {
            base.Start();

            canFocus = true;
            isInteractive = true;

            Hide();
        }

        public override void Update()
        {
            if (!isVisible) return;

            if (mod?.data == null) return;

            if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                lastSelectedBuilding = WorldInfoPanel.GetCurrentInstanceID().Building;

            UpdateCounterValues();
        }
    }
}

