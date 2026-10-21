# Домены атаки и броня

`AttackDomain` — кем является цель (фильтр «можно ли стрелять»).  
`AllowedTargetDomains` — что может бить турель.  
`Armor` — класс брони цели.  
`ArmorDamage` — абсолютный урон турели по Light / Medium / Heavy (не множитель).

## Домены (кто кого видит как цель)

| Юнит / здание | Турель | AllowedTargetDomains | Может атаковать |
|------|--------|----------------------|-----------------|
| Baggy | BaggyMachineGun | Ground \| Air \| Construction | наземные, воздух, здания |
| TankT1 | TankTower | Ground \| Construction | наземные юниты, здания |
| TankAa | TankAaCannon | Air | только авиация (aam_rocket) |
| TankArtillery | TankArtilleryCannon | Ground \| Construction | залп 4×: чуть вверх → резкий разворот + дым, КД 6 с |
| AirUnit | AgmAirCannon | Ground \| Construction | земля/здания (agm_rocket) |
| AirUnit | AamAirCannon | Air | только авиация (aam_rocket) |
| Soldier | SoldierRifle (автомат) | Ground \| Construction | наземные юниты, здания |
| Grenadier | GrenadierLauncher | Ground \| Construction | наземные юниты, здания (граната) |
| AntiAirTurret | (встроенная ПВО) | Air | только авиация (aam_rocket) |
| Artillery | (залп 4 ракеты) | Ground \| Construction | дальние AGM по земле/зданиям |

Самолёт: общий магазин **4 ракеты** (AGM+AAM), затем перезарядка **5 с**.  
В одном пролёте не переключается с воздушной цели на наземную и наоборот.

### Справочник доменов цели

| Цель | AttackDomain |
|------|----------------|
| Наземный юнит (Baggy, TankT1, Soldier, Grenadier, TankAa, …) | `Ground` |
| Воздушный юнит (AirUnit) | `Air` |
| Постройка | `Construction` |

## Броня (насколько больно попадёт)

| Цель | Armor |
|------|-------|
| Soldier, Grenadier | Light |
| AirUnit | Light |
| Baggy | Medium |
| AntiAirTurret | Medium |
| Artillery | Medium |
| TankT1, TankAa, TankArtillery | Heavy |
| Остальные здания (HQ, заводы, …) | Heavy |

### Урон турелей по броне (абсолютные числа)

| Оружие | Light | Medium | Heavy | Смысл |
|--------|------:|-------:|------:|-------|
| SoldierRifle | 52 | 20 | 5 | пехота vs пехота, почти нет vs танк |
| GrenadierLauncher | 180 | 880 | 680 | низкая дуга; сильно vs Medium/Heavy |
| BaggyMachineGun | 138 | 66 | 17 | пулемёт режет лёгких, слаб vs танк |
| TankTower | 154 | 490 | 735 | ОФС слаб vs пехота, силён vs броня/здания |
| TankAaCannon | 288 | 180 | 84 | AAM vs самолёты |
| AgmAirCannon | 98 | 238 | 308 | AGM vs техника/здания |
| AamAirCannon | 322 | 196 | 98 | воздух-воздух |
| AntiAirTurret | 300 | 180 | 84 | стационарная ПВО |
| Artillery (здание) | 120 | 340 | 420 | залп 4×, дальность 10, loft→резкий разворот, КД 6 с |
| TankArtilleryCannon | 120 | 340 | 420 | залп 4× artillery_rocket, дальность 48, КД 6 с |

Итоговый урон: `ArmorDamage.For(target.Armor) × falloff`.

## Производство

### Barracks (барак, 120 / 7 с, энергия 25)

| Юнит | Стоимость | Время |
|------|-----------|-------|
| Soldier | 40 | 4 с |
| Grenadier | 70 | 6 с |

### MilitaryFactory (техника)

| Юнит | Стоимость | Время |
|------|-----------|-------|
| Baggy | 80 | 5 с |
| TankT1 | 200 | 12 с |
| TankAa | 180 | 10 с |
| TankArtillery | 220 | 12 с |

### Оборона / поддержка

| Здание | Стоимость | Время | Энергия | Заметки |
|--------|-----------|-------|---------|---------|
| AntiAirTurret | 160 | 7 с | 35 | ПВО vs воздух |
| Artillery | 220 | 9 с | 40 | 4 ракеты, перезарядка 6 с, дальность 10 |
