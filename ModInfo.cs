using ICities;

namespace CargoInfoMod
{
    public class ModInfo : IUserMod
    {
        public const string Namespace = "com.github.rumkex.cargomod";

        public string Name => "Cargo Info";

        public string Description => "Displays statistics panel for Cargo Stations service view and allows monitoring cargo dynamics";

        internal CargoData data;

        public static readonly uint GameVersion = 163832080u;
        public static readonly uint GameVersionA = 1u;
        public static readonly uint GameVersionB = 7u;
        public static readonly uint GameVersionC = 1u;
        public static readonly uint GameVersionBuild = 1u;
    }
}