using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle
{
    /// <summary>
    /// Туман войны игрока: круг зрения своих юнитов и достроенных зданий.
    /// Стрелять можно только по клетке, которая сейчас в этом зрении.
    /// Радиусы совпадают с клиентским VisionRanges.
    /// </summary>
    public sealed class FogOfWar
    {
        private readonly Dictionary<int, byte[]> _visible = new();
        private int _width;
        private int _length;

        public void Rebuild(Game game)
        {
            MapBattle.Map? map = game.Map;
            if (map == null || map.Width <= 0 || map.Length <= 0)
            {
                _width = 0;
                _length = 0;
                return;
            }

            _width = map.Width;
            _length = map.Length;
            int cells = _width * _length;

            var owners = new HashSet<int>();
            foreach (Player player in game.Players)
            {
                if (player.Id >= 0)
                    owners.Add(player.Id);
            }

            foreach (Unit unit in game.Units)
            {
                if (unit != null && !unit.IsDestroyed && unit.OwnerId >= 0)
                    owners.Add(unit.OwnerId);
            }

            foreach (Construction construction in game.Constructions)
            {
                if (construction != null && !construction.IsDestroyed && construction.OwnerId >= 0)
                    owners.Add(construction.OwnerId);
            }

            foreach (int ownerId in owners)
            {
                if (!_visible.TryGetValue(ownerId, out byte[]? grid) || grid.Length != cells)
                {
                    grid = new byte[cells];
                    _visible[ownerId] = grid;
                }
                else
                {
                    Array.Clear(grid);
                }
            }

            foreach (Unit unit in game.Units)
            {
                if (unit == null || unit.IsDestroyed || unit.OwnerId < 0)
                    continue;
                if (!_visible.TryGetValue(unit.OwnerId, out byte[]? grid))
                    continue;
                Reveal(grid, unit.Position.X, unit.Position.Y, VisionRadiusForUnit(unit.Code));
            }

            foreach (Construction construction in game.Constructions)
            {
                if (construction == null || construction.IsDestroyed || !construction.IsBuilt || construction.OwnerId < 0)
                    continue;
                if (!_visible.TryGetValue(construction.OwnerId, out byte[]? grid))
                    continue;
                Reveal(grid, construction.AimPosition.X, construction.AimPosition.Y, VisionRadiusForConstruction(construction.Code));
            }
        }

        public bool CanSee(int playerId, Vector2Float position)
        {
            if (playerId < 0 || _width <= 0 || _length <= 0)
                return false;
            if (!_visible.TryGetValue(playerId, out byte[]? grid))
                return false;

            int x = (int)Math.Floor(position.X);
            int y = (int)Math.Floor(position.Y);
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_length)
                return false;

            return grid[y * _width + x] != 0;
        }

        private void Reveal(byte[] grid, double originX, double originY, int radius)
        {
            int cx = (int)Math.Floor(originX);
            int cy = (int)Math.Floor(originY);
            int r = Math.Max(1, radius);
            int r2 = r * r;
            int minX = Math.Max(0, cx - r);
            int maxX = Math.Min(_width - 1, cx + r);
            int minY = Math.Max(0, cy - r);
            int maxY = Math.Min(_length - 1, cy + r);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - cy;
                int row = y * _width;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= r2)
                        grid[row + x] = 1;
                }
            }
        }

        public static int VisionRadiusForUnit(string code) => code switch
        {
            "Soldier" => 8,
            "Grenadier" => 10,
            "Baggy" => 10,
            "TankT1" => 12,
            "TankAa" => 14,
            "TankArtillery" => 12,
            "AirUnit" => 16,
            "Helicopter" => 14,
            "MobileConstructionCenter" => 10,
            "Ship" => 14,
            "AirTransporter" => 12,
            _ => 8
        };

        public static int VisionRadiusForConstruction(string code) => code switch
        {
            "Headquarters" => 14,
            "MobileHq" => 12,
            "AirDropBase" => 10,
            "PowerStation" => 7,
            "SupplyCenter" => 9,
            "Barracks" => 9,
            "MilitaryFactory" => 9,
            "AirFactory" => 11,
            "LandingPad" => 8,
            "Wharf" => 10,
            "Crane" => 8,
            "AntiAirTurret" => 12,
            "MachineGunTurret" => 11,
            "Artillery" => 10,
            _ => 8
        };
    }
}
