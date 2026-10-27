using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// 4 посадочных слота на постройке (LandingPad). Каждый AirUnit держит слот, пока жив.
    /// </summary>
    public sealed class LandingPads
    {
        public const int SlotCount = 4;

        // Центры четвертей 3×3 относительно визуального центра площадки.
        // (±0.75 = середина каждой четверти; half-extent footprint = 1.5).
        private static readonly (double Dx, double Dy)[] Offsets =
        {
            (-0.75, -0.75),
            (0.75, -0.75),
            (-0.75, 0.75),
            (0.75, 0.75),
        };

        private readonly Construction _host;
        private readonly AirUnit?[] _slots = new AirUnit?[SlotCount];

        public LandingPads(Construction host)
        {
            _host = host;
        }

        public int FreeCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < SlotCount; i++)
                {
                    if (IsSlotFree(i))
                        n++;
                }
                return n;
            }
        }

        public bool TryReserve(AirUnit unit, out int slotIndex, out Vector2Float worldPos)
        {
            slotIndex = -1;
            worldPos = default;
            if (unit == null)
                return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (ReferenceEquals(_slots[i], unit))
                {
                    slotIndex = i;
                    worldPos = GetWorldPosition(i);
                    return true;
                }
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsSlotFree(i))
                    continue;
                _slots[i] = unit;
                slotIndex = i;
                worldPos = GetWorldPosition(i);
                return true;
            }

            return false;
        }

        public void Release(AirUnit unit)
        {
            if (unit == null)
                return;
            for (int i = 0; i < SlotCount; i++)
            {
                if (ReferenceEquals(_slots[i], unit))
                    _slots[i] = null;
            }
        }

        public bool TryGetWorldPosition(AirUnit unit, out Vector2Float worldPos)
        {
            worldPos = default;
            for (int i = 0; i < SlotCount; i++)
            {
                if (!ReferenceEquals(_slots[i], unit))
                    continue;
                worldPos = GetWorldPosition(i);
                return true;
            }
            return false;
        }

        public Vector2Float GetWorldPosition(int slotIndex)
        {
            slotIndex = Math.Clamp(slotIndex, 0, SlotCount - 1);
            // AimPosition = угол + Size/2 в «клеточных» координатах.
            // Юниты рисуются как GetFloat (целые = визуальный центр клетки при MapGrid −0.5),
            // а здание — через TransformPoint → визуальный центр = AimPosition − 0.5.
            // Без этой поправки вся четвёрка уезжает от площадки на полклетки.
            Vector2Float center = new(
                _host.AimPosition.X - 0.5,
                _host.AimPosition.Y - 0.5);
            (double dx, double dy) = Offsets[slotIndex];
            return new Vector2Float(center.X + dx, center.Y + dy);
        }

        private bool IsSlotFree(int i)
        {
            AirUnit? u = _slots[i];
            if (u == null)
                return true;
            if (u.IsDestroyed)
            {
                _slots[i] = null;
                return true;
            }
            return false;
        }
    }
}
