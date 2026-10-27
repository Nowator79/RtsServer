using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units;
using RtsServer.App.Battle.Units.AttackingPoint;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Battle.Lab
{
    /// <summary>Собирает описания юнитов/зданий из боевых классов (источник правды по х-кам).</summary>
    public static class LabCatalogBuilder
    {
        private static readonly object Lock = new();
        private static NLabCatalog? _cached;

        public static NLabCatalog Get()
        {
            if (_cached != null)
                return _cached;

            lock (Lock)
            {
                _cached ??= Build();
                return _cached;
            }
        }

        private static NLabCatalog Build()
        {
            return new NLabCatalog
            {
                Units =
                [
                    BuildUnit("Soldier", "Солдат",
                        "Базовая пехота. Быстрая и дешёвая, слабо защищена.",
                        "Barracks", BarracksConstruction.TryGetUnitRecipe),
                    BuildUnit("Grenadier", "Гренадер",
                        "Пехота с гранатомётом. Эффективна против техники и укреплений.",
                        "Barracks", BarracksConstruction.TryGetUnitRecipe),
                    BuildUnit("Baggy", "Багги",
                        "Лёгкая разведка и поддержка. Высокая скорость, средний урон.",
                        "MilitaryFactory", MilitaryFactoryConstruction.TryGetUnitRecipe),
                    BuildUnit("TankT1", "Танк",
                        "Основной боевой танк. Тяжёлая броня и сильный снаряд.",
                        "MilitaryFactory", MilitaryFactoryConstruction.TryGetUnitRecipe),
                    BuildUnit("TankAa", "ПВО-машина",
                        "Мобильная зенитная установка. Специализация — воздух.",
                        "MilitaryFactory", MilitaryFactoryConstruction.TryGetUnitRecipe),
                    BuildUnit("TankArtillery", "САУ",
                        "Самоходная артиллерия. Дальний огонь по земле и зданиям.",
                        "MilitaryFactory", MilitaryFactoryConstruction.TryGetUnitRecipe),
                    BuildUnit("AirUnit", "Самолёт",
                        "Ударная авиация. Атакует пролётами, уязвима для ПВО.",
                        "AirFactory", AirFactoryConstruction.TryGetUnitRecipe),
                    BuildUnit("Helicopter", "Вертолёт",
                        "Воздушная поддержка. Летает над препятствиями по сетке.",
                        "AirFactory", AirFactoryConstruction.TryGetUnitRecipe),
                    BuildUnit("AirTransporter", "Транспортник",
                        "Воздушный транспорт. Разворачивается в базу с зоной стройки 7×7.",
                        "AirFactory", AirFactoryConstruction.TryGetUnitRecipe),
                    BuildUnit("Ship", "Корабль",
                        "Военный корабль. Ходит по воде, пушка танка и артиллерия.",
                        "Wharf", WharfConstruction.TryGetUnitRecipe),
                    BuildUnit("MobileConstructionCenter", "МСЦ",
                        "Мобильный стройцентр. Разворачивается в полевой штаб 1×1.",
                        "Headquarters", HeadquartersConstruction.TryGetUnitRecipe),
                ],
                Buildings =
                [
                    BuildBuilding("Headquarters", "Штаб",
                        "Главное здание. Даёт энергию, хранит и добывает ресурсы. Производит МСЦ."),
                    BuildBuilding("MobileHq", "Полевой штаб",
                        "Развёрнутый МСЦ. Даёт зону строительства. Можно свернуть в юнит."),
                    BuildBuilding("AirDropBase", "Воздушная база",
                        "Развёрнутый транспортник. Зона строительства 7×7. Можно свернуть."),
                    BuildBuilding("PowerStation", "Электростанция",
                        "Основной источник энергии для базы."),
                    BuildBuilding("SupplyCenter", "Центр снабжения",
                        "Склад и добыча руды. Нужна хотя бы одна клетка с рудой под зданием."),
                    BuildBuilding("Barracks", "Казармы",
                        "Производство пехоты: солдаты и гренадеры."),
                    BuildBuilding("MilitaryFactory", "Военный завод",
                        "Производство наземной техники."),
                    BuildBuilding("AirFactory", "Авиазавод",
                        "Производство авиации."),
                    BuildBuilding("LandingPad", "Посадочная площадка",
                        "4 места для самолётов. Нужна, чтобы производить самолёты."),
                    BuildBuilding("Wharf", "Верфь",
                        "Производство кораблей. Ставится у воды."),
                    BuildBuilding("Crane", "Кран",
                        "Стационарный кран. Расширяет зону строительства."),
                    BuildBuilding("AntiAirTurret", "Турель ПВО",
                        "Стационарная защита от самолётов."),
                    BuildBuilding("MachineGunTurret", "Пулемётная турель",
                        "Hitscan по пехоте и лёгкой технике."),
                    BuildBuilding("Artillery", "Артиллерия",
                        "Стационарный дальнобойный огонь по земле и зданиям."),
                ]
            };
        }

        private delegate bool RecipeFn(string code, out int cost, out float buildSeconds);

        private static NLabEntry BuildUnit(string code, string name, string description, string producedIn, RecipeFn recipe)
        {
            Unit unit = UnitFactory.GetByCode(code, new Vector2Float(0, 0), 0);
            foreach (BaseAttackingPoint ap in unit.AttackingPoints)
                ap.Init();

            recipe(code, out int cost, out float buildSeconds);

            return new NLabEntry
            {
                Code = code,
                Name = name,
                Description = description,
                Kind = "unit",
                Health = unit.Health.Max,
                Cost = cost,
                BuildTime = buildSeconds,
                Speed = (float)unit.MaxSpeed,
                Armor = ArmorRu(unit.Armor),
                ProducedIn = DisplayBuilding(producedIn),
                Weapons = unit.AttackingPoints.Select(MapWeapon).ToArray()
            };
        }

        private static NLabEntry BuildBuilding(string code, string name, string description)
        {
            Construction c = ConstructionFactory.GetByCode(code, new Vector2Int(0, 0), 0);
            int provide = c is IEnergyProvider p ? p.EnergyProvided : 0;
            int require = c is IEnergyConsumer e ? e.EnergyRequired : 0;

            return new NLabEntry
            {
                Code = code,
                Name = name,
                Description = description,
                Kind = "building",
                Health = c.Health.Max,
                Cost = ConstructionFactory.GetBuildCost(code),
                BuildTime = ConstructionFactory.GetBuildTime(code),
                Armor = ArmorRu(c.Armor),
                SizeX = c.Size.X,
                SizeY = c.Size.Y,
                EnergyProvide = provide,
                EnergyRequire = require,
                Weapons = ConstructionWeapons(code)
            };
        }

        private static NLabWeapon MapWeapon(BaseAttackingPoint ap)
        {
            return new NLabWeapon
            {
                Name = WeaponRu(ap.GetType().Name),
                Range = ap.Range,
                Cooldown = (float)GetCooldown(ap),
                VsLight = ap.ArmorDamage.VsLight,
                VsMedium = ap.ArmorDamage.VsMedium,
                VsHeavy = ap.ArmorDamage.VsHeavy,
                Targets = DomainsRu(ap.AllowedTargetDomains)
            };
        }

        private static string WeaponRu(string typeName) => typeName switch
        {
            "SoldierRifle" => "Винтовка",
            "GrenadierLauncher" => "Гранатомёт",
            "BaggyMachineGun" => "Пулемёт",
            "TankTower" => "Орудие танка",
            "TankAaCannon" => "Зенитное орудие",
            "TankArtilleryCannon" => "Арт. орудие",
            "AgmAirCannon" => "УРВВ / УРВЗ",
            "AamAirCannon" => "УРВВ",
            "AirCannon" => "Авиапушка",
            _ => typeName
        };

        private static double GetCooldown(BaseAttackingPoint ap)
        {
            // fireCooldown protected — читаем через reflection один раз на сборку каталога.
            var field = typeof(BaseAttackingPoint).GetField("fireCooldown",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return field?.GetValue(ap) is double d ? d : 0;
        }

        private static NLabWeapon[] ConstructionWeapons(string code) => code switch
        {
            "AntiAirTurret" =>
            [
                new NLabWeapon
                {
                    Name = "ПВО-ракета",
                    Range = 36f,
                    Cooldown = 1.15f,
                    VsLight = 300f,
                    VsMedium = 180f,
                    VsHeavy = 84f,
                    Targets = "воздух"
                }
            ],
            "Artillery" =>
            [
                new NLabWeapon
                {
                    Name = "Арт. залп (4)",
                    Range = 10f,
                    Cooldown = 6f,
                    VsLight = 120f,
                    VsMedium = 340f,
                    VsHeavy = 420f,
                    Targets = "земля, здания"
                }
            ],
            _ => Array.Empty<NLabWeapon>()
        };

        private static string ArmorRu(ArmorType armor) => armor switch
        {
            ArmorType.Light => "Лёгкая",
            ArmorType.Medium => "Средняя",
            ArmorType.Heavy => "Тяжёлая",
            _ => armor.ToString()
        };

        private static string DomainsRu(AttackTargetDomain domains)
        {
            var parts = new List<string>();
            if ((domains & AttackTargetDomain.Ground) != 0) parts.Add("земля");
            if ((domains & AttackTargetDomain.Air) != 0) parts.Add("воздух");
            if ((domains & AttackTargetDomain.Construction) != 0) parts.Add("здания");
            return parts.Count == 0 ? "—" : string.Join(", ", parts);
        }

        private static string DisplayBuilding(string code) => code switch
        {
            "Barracks" => "Казармы",
            "MilitaryFactory" => "Военный завод",
            "AirFactory" => "Авиазавод",
            "LandingPad" => "Посадочная площадка",
            "Wharf" => "Верфь",
            "Headquarters" => "Штаб",
            _ => code
        };
    }
}
