using System;
using System.Collections.Generic;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>Общая отмена одного заказа из очереди/текущего производства.</summary>
    public static class UnitProductionQueueOps
    {
        public static bool TryCancelOne(
            ref string? currentUnitCode,
            ref float productionProgress,
            ref float productionDuration,
            Queue<string> queue,
            Action rebuildQueueCsv,
            Func<string, (bool ok, float seconds)> tryGetSeconds,
            string unitCode)
        {
            if (string.IsNullOrEmpty(unitCode) || queue == null || rebuildQueueCsv == null || tryGetSeconds == null)
                return false;

            // Сначала снимаем из ожидания (с конца — последний заказ этого типа).
            if (queue.Count > 0)
            {
                var list = new List<string>(queue);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!string.Equals(list[i], unitCode, StringComparison.Ordinal))
                        continue;

                    list.RemoveAt(i);
                    queue.Clear();
                    for (int j = 0; j < list.Count; j++)
                        queue.Enqueue(list[j]);
                    rebuildQueueCsv();
                    return true;
                }
            }

            // Иначе отменяем текущий производящийся.
            if (!string.Equals(currentUnitCode, unitCode, StringComparison.Ordinal))
                return false;

            currentUnitCode = null;
            productionProgress = 0f;

            if (queue.Count > 0)
            {
                string next = queue.Dequeue();
                rebuildQueueCsv();
                (bool ok, float seconds) = tryGetSeconds(next);
                if (ok)
                {
                    currentUnitCode = next;
                    productionDuration = Math.Max(0.1f, seconds);
                    productionProgress = 0f;
                }
            }
            else
            {
                rebuildQueueCsv();
            }

            return true;
        }
    }
}
