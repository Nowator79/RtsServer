using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Цель для оружия: юнит или постройка.</summary>
    public interface IAttackTarget
    {
        int Id { get; }
        int OwnerId { get; }
        bool IsDestroyed { get; }
        /// <summary>Точка прицеливания (для зданий — центр footprint).</summary>
        Vector2Float AimPosition { get; }
        Health Health { get; }
        double GetBodyHeight();
        /// <summary>Воздух / земля / постройка — фильтр для турелей.</summary>
        AttackTargetDomain AttackDomain { get; }
        /// <summary>Класс брони — влияет на итоговый урон от оружия.</summary>
        ArmorType Armor { get; }
        Action? OnDestroyAction { get; set; }
    }
}
