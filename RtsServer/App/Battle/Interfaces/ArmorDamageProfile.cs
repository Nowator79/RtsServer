namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>
    /// Абсолютный урон оружия по типам брони (не множитель).
    /// </summary>
    public readonly struct ArmorDamageProfile
    {
        public float VsLight { get; }
        public float VsMedium { get; }
        public float VsHeavy { get; }

        public ArmorDamageProfile(float vsLight, float vsMedium, float vsHeavy)
        {
            VsLight = vsLight;
            VsMedium = vsMedium;
            VsHeavy = vsHeavy;
        }

        public static ArmorDamageProfile Zero { get; } = new(0f, 0f, 0f);

        public float For(ArmorType armor) => armor switch
        {
            ArmorType.Light => VsLight,
            ArmorType.Medium => VsMedium,
            ArmorType.Heavy => VsHeavy,
            _ => 0f
        };
    }
}
