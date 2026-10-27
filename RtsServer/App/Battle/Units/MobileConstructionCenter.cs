using System;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Constructions;

namespace RtsServer.App.Battle.Units
{
    /// <summary>
    /// Мобильный строительный центр: разворачивается в MobileHq (1×1).
    /// </summary>
    public class MobileConstructionCenter : Unit, IDeployableUnit
    {
        public string DeployedConstructionCode => MobileHqConstruction.Code;

        public override ArmorType Armor => ArmorType.Heavy;

        public MobileConstructionCenter(Vector2Float position, int playerOwner)
            : base("MobileConstructionCenter", 2200, 2200, position, playerOwner)
        {
            MaxSpeed = 0.85;
            RotationSpeed = 70;
            AccelerationForce = 12;
            AttackingPoints = [];
        }

        public override double GetBodyHeight() => GetGroundElevation() + 0.55;
        public override double GetMuzzleHeight() => GetGroundElevation() + 0.55;

        public bool CanDeployAt(Vector2Int cell)
        {
            if (Game == null || IsDestroyed)
                return false;

            // Разворот только «на месте» — клетка под юнитом.
            Vector2Int unitCell = new((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y));
            if (cell.X != unitCell.X || cell.Y != unitCell.Y)
                return false;

            return Game.CanPlaceConstruction(
                DeployedConstructionCode, cell, OwnerId, out _, forDeploy: true);
        }

        public bool TryDeploy(Vector2Int cell)
        {
            if (Game == null || IsDestroyed)
                return false;
            if (!CanDeployAt(cell))
                return false;

            try
            {
                Construction building = ConstructionFactory.GetByCode(
                    DeployedConstructionCode, cell, OwnerId);
                building.SetGame(Game);
                float unpackSeconds = ConstructionFactory.GetBuildTime(DeployedConstructionCode);
                if (unpackSeconds > 0f)
                    building.BeginConstruction(unpackSeconds);
                Game.ConstructionsForAdd.Add(building);
                Destroy(suppressDeathFx: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
