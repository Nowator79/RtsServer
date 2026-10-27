using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// Развёрнутый мобильный стройцентр (1×1). Сворачивается обратно в MobileConstructionCenter.
    /// </summary>
    public class MobileHqConstruction : Construction, IEnergyProvider, IEnergyConsumer, IUndeployableConstruction
    {
        public const int sizeX = 1;
        public const int sizeY = 1;
        public const int maxHealth = 3500;
        public const string Code = "MobileHq";
        /// <summary>0 — нельзя купить через buildConstruction, только deploy.</summary>
        public const int BuildCost = 0;
        /// <summary>Распаковка MCC → MobileHq и обратная сборка (сек).</summary>
        public const float BuildTime = 7f;

        public string PackedUnitCode => "MobileConstructionCenter";

        public int EnergyProvided => IsBuilt ? 15 : 0;
        public int EnergyRequired => IsBuilt ? 5 : 0;

        public override ArmorType Armor => ArmorType.Heavy;

        public MobileHqConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public override double GetBodyHeight() => GetGroundElevation() + 1.2;

        public bool TryUndeploy()
        {
            if (Game == null || IsDestroyed)
                return false;

            // Уже сворачиваемся — повторный клик игнор.
            if (IsPacking)
                return true;

            return BeginPacking(BuildTime);
        }

        protected override void OnPackingComplete()
        {
            if (Game == null || IsDestroyed)
                return;

            // Как заводы/LandingPads: GetFloat = (X,Y), визуальный центр при MapGrid −0.5.
            // (+0.5,+0.5) ставит технику на полклетки мимо постройки.
            Vector2Int cell = Position.ToInt();
            Vector2Float spawnPos = cell.GetFloat();

            try
            {
                Unit unit = UnitFactory.GetByCode(PackedUnitCode, spawnPos, OwnerId);
                unit.SetGame(Game);
                Game.UnitsForAdd.Add(unit);
                Destroy(suppressDeathFx: true);
            }
            catch
            {
                // Если спавн юнита упал — здание уже с progress 0; оставляем разрушенным без юнита.
                Destroy(suppressDeathFx: true);
            }
        }
    }
}
