using ColossalFramework;
using ICities;

namespace CargoInfoMod
{
    public class ModInfo : IUserMod
    {
        public const string Namespace = "com.github.rumkex.cargomod";

        public string Name => "Cargo Info";

        public string Description => "Displays statistics panel for Cargo Stations service view and allows monitoring cargo dynamics";

        internal CargoData data;

        internal Options Options = new Options();

        public void OnSettingsUI(UIHelperBase helper)
        {
            helper.AddCheckbox("Use months instead of weeks", Options.UseMonthlyValues, state => Options.UseMonthlyValues.value = state);
        }
    }

    public class Options
    {
        public SavedBool UseMonthlyValues = new SavedBool("useMonthlyCargoValues", Settings.gameSettingsFile);
    }
}