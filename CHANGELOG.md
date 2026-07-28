# Changelog

## [1.1.1] - 2026-07-28

Compatibility and quality hotfix for PvZ Fusion 3.8.1 and CustomizeLib.MelonLoader 3.8.1-ml.1.

### Fixed

- Updated Plants+ to the real `CustomizeLib.MelonLoader` assembly and namespace used by PvZ Fusion 3.8.1.
- Fixed Electronion's locked card in restricted challenge levels, including The Gods: Evolved.
- Kept Electronion on the second normal-card page without the Peashooter packet, missing preview or duplicate template card.
- Preserved the normal Adventure card and Carbon Copy behavior.
- Preserved the animated Plants+ main-menu logo on the updated game build.

## [1.1.0] - 2026-07-26

Second public Plants+ release for PvZ Fusion 3.8 and MelonLoader 0.7.3.

### Added

- Ten new custom plants with IDs 6010-6019:
  - Not-a-pea
  - Not-a-storm Commando
  - Frost Furflower
  - Doomtronion
  - Lichen-pea
  - Logic Blover
  - Solar Sharpshooter
  - Sea Ballista
  - Pineshooter
  - Icytronion
- Custom projectiles, visual effects, Almanac entries and recipes for the new plants.
- Logic Blover as a Harvest-exclusive Red Card with Gift Box luck and Gold Bean reactivation.
- Christmas Snow in the Super Level Editor.
- Themed map backgrounds and matching plant previews for every editor scene.
- Plants+ v1.1 logo and a dedicated in-game Plants+ changelog on the main menu.
- Night Roof card presentation for Electronion/Amp-nion.

### Changed

- Reworked Inferno Torchflower. Lit projectiles now charge up to 250 energy; clicking releases the stored Sun with a multiplier based on the charge and resets every related counter.
- Electronion/Amp-nion now behaves as a regular selectable plant where allowed, supports Carbon Copy, appears in the correct Almanac position and has a dedicated Sandbox slot.
- Christmas Snow now uses the Snowy Night music.
- Logic Blover now has its complete intended Harvest mechanics and card classification.
- Reworked and cleaned custom Almanac copy to match PVZ Fusion's native presentation.

### Fixed and polished

- Doomtronion and Icytronion preserve Irradiation, slow and freeze effects while connected to their electric family.
- Doomtronion's `biu` beam, impact position, attack timing and target chaining.
- Solar Firnace cold protection.
- Solar Sharpshooter piercing, Sun drops and projectile position.
- Sea Ballista range and projectile height.
- Pineshooter knockback/collision mechanics, projectile height, preview scale, pooled projectile cleanup and shadow.
- Electronion card backgrounds, restricted-level availability, Carbon Copy, Almanac and page placement.
- Multiple custom plant animations, previews, shadows, cards and effect synchronization issues.

## [1.0.0] - 2026-07-17

First public release of Plants+ for PvZ Fusion 3.8 and MelonLoader 0.7.3.

### Added

- Ten custom plants with IDs 6000-6009.
- Custom AssetBundles, projectiles, Almanac mechanics and flavor text.
- Weak-Odyssey registration and the Grenades/Radiation modifiers for Witchfire Pumpkin.
- Advanced Alt conversion systems for Inferno Torchflower, Pumpkin Podbomber and Ceasarweed.
- Solar Firnace's special flying-plant absorption recipe and timed Sun system.

### Fixed and polished

- Safe custom ID range that preserves the Sandbox zombie list.
- Lotus Pumpkin prefab and healing behavior.
- Iceberg-shroom slow behavior against freeze-immune enemies.
- Magnet-o-pea projectile visuals, reuse cleanup and base pea scale.
- Infernowood recycling and Inferno Torchflower reverse conversion.
- Pumpkin Podbomber shovel conversion and explosive volley counter.
- Ceasarweed's correct Salad-pult recipe, missed-salad scaling and Melon-pult conversion.
- Native-format Almanac layout, separated mechanics/lore fields and cleaned recipes.
- Solar Firnace classified as a flying plant.
