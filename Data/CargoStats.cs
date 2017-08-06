using System;
using System.Linq;

namespace CargoInfoMod.Data
{
    [Flags]
    public enum CarFlags
    {
        None     = 0x000,
        Previous = 0x001,
        Sent =     0x002,
        Imported = 0x004,
        Exported = 0x008,

        Resource = 0x0F0,

        Oil =      0x000,
        Petrol =   0x010,
        Ore =      0x020,
        Coal =     0x030,
        Logs =     0x040,
        Lumber =   0x050,
        Grain =    0x060,
        Food =     0x070,
        Goods =    0x080
    }

    [Serializable]
    public class CargoStats2
    {
        public int[] CarsCounted;

        public CargoStats2()
        {
            CarsCounted = new int[(int)Enum.GetValues(typeof(CarFlags)).Cast<CarFlags>().Aggregate((v, agg) => agg | v) + 1];
        }

        // Syntactic sugar fluff
        public int GetTotalWhere(Func<CarFlags, bool> pred)
        {
            return CarsCounted.Where((t, idx) => pred((CarFlags)idx)).Sum();
        }

        public int CarsSentLastTime => GetTotalWhere(f => (f & CarFlags.Sent) != 0 && (f & CarFlags.Previous) != 0);
        public int CarsReceivedLastTime => GetTotalWhere(f => (f & CarFlags.Sent) == 0 && (f & CarFlags.Previous) != 0);
        public int CarsSent => GetTotalWhere(f => (f & CarFlags.Sent) != 0 && (f & CarFlags.Previous) == 0);
        public int CarsReceived => GetTotalWhere(f => (f & CarFlags.Sent) == 0 && (f & CarFlags.Previous) == 0);

        // In case the number of flags changes between versions
        public CargoStats2 Upgrade()
        {
            var upgradedStats = new CargoStats2();
            CarsCounted.CopyTo(upgradedStats.CarsCounted, 0);
            return upgradedStats;
        }
    }

    // Used only in v1.1 and below
    [Serializable]
    public class CargoStats
    {
        public int carsReceivedLastTime = 0;
        public int carsSentLastTime = 0;
    }
}
