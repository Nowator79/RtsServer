namespace RtsServer.App.Battle.Interfaces
{
    public interface IResourceStorage
    {
        float Resources { get; protected set; }
        int LimitResources { get; }
        /// <summary>Списать ресурсы. Возвращает true, если ресурсов было достаточно и списание выполнено.</summary>
        bool TrySpend(float amount);
        /// <summary>Вернуть ресурсы (например, при отмене постройки). Не превышает LimitResources.</summary>
        void AddResources(float amount);
    }
}
