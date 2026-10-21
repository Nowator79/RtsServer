namespace RtsServer.App.NetWorkDto
{
    /// <summary>Каталог лаборатории: юниты и здания с характеристиками.</summary>
    public class NLabCatalog
    {
        public NLabEntry[] Units { get; set; } = Array.Empty<NLabEntry>();
        public NLabEntry[] Buildings { get; set; } = Array.Empty<NLabEntry>();
    }

    public class NLabEntry
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Kind { get; set; } = "";
        public int Health { get; set; }
        public int Cost { get; set; }
        public float BuildTime { get; set; }
        public float Speed { get; set; }
        public string Armor { get; set; } = "";
        public string ProducedIn { get; set; } = "";
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int EnergyProvide { get; set; }
        public int EnergyRequire { get; set; }
        public NLabWeapon[] Weapons { get; set; } = Array.Empty<NLabWeapon>();
    }

    public class NLabWeapon
    {
        public string Name { get; set; } = "";
        public float Range { get; set; }
        public float Cooldown { get; set; }
        public float VsLight { get; set; }
        public float VsMedium { get; set; }
        public float VsHeavy { get; set; }
        public string Targets { get; set; } = "";
    }
}
