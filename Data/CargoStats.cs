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
        Petrol =   0x010,
        Coal =     0x020,
        Lumber =   0x040,
        Food =     0x080,
        Goods =    0x100,
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
