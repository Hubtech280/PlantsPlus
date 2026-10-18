# Changelog

## [1.3.0] - 2026-10-18

- Public release for PvZ Fusion 4.0.5.2 and CustomizeLib.MelonLoader 4.0.5.2-ml.5, based on the author's tested alpha.46.
- 42 custom plant Almanac entries; FR/EN/ZH text, localized logos, Boreal Plants category and persistent Almanac favorites.
- Night Roof Adventure content and three Survival variants. Bounce-shroom and its fusions support bare tiles on classic Roof and Night Roof.
- Bamboom and Solstice-shroom, plus the Bumping and roof-fusion corrections confirmed by the author.
- Known issue: zombie damage/armor-loss ArgumentOutOfRangeException reports remain unresolved. No visible effect was reported in that test; no claim of harmlessness or a fix is made.
- Future large Adventure reworks are not shipped. Detailed development notes follow below.

## [1.3.0-alpha.46] - 2026-10-18

- Alpha.45 log contains coexisting Bounce (6035) and Sunflower (1), but no queued/created Roof Fusion message. Observe completed roof tiles from Bounce LateUpdate instead of relying exclusively on the optional CheckMix path.
- Capture both placed ingredients, remove both with native ByMix, and create the exact result after input processing. Merge with any existing one-parent/incoming-card job to avoid duplicate jobs or leftover incoming plants. Pots are never selected; full three-Lob stacks keep their separate Bumping route.
- Observation is limited to live Bounce plants on classic Roof or Night Roof; not previews, other result plants or unrelated boards. Bare-roof policy is unchanged.
- Add queued-job trace with expected result and ingredient count. Offline mapping/target/JIT probes pass; actual fusion ID, cleanup and visuals remain to verify in game.

## [1.3.0-alpha.45] - 2026-10-18

- CORRECTION: the author explicitly allows Bounce-shroom and all four fusions on bare roof tiles. Earlier pot requirements were a misunderstanding, not intended design. Exempt the family and matching fusion inputs from mandatory-pot rules; cover both classic Roof and Night Roof.
- Validate native Caltrop obstacle/ice/space rules, excluding terrain support rules that conflict with this explicit roof exception. Preserve pit/grave checks; permit replacement occupancy only for a matching recipe (or complete three-Lob stack).
- Defer the three two-parent creations until after CheckMix/input processing. Consume only the captured parent with the confirmed native ByMix bridge, then create the exact result via native free SetPlant. Log actual result ID; leave pots untouched when present.
- Offline tests validate both recipe orders, exact roof scene scope and metadata. Actual placement/visuals still require author confirmation; release remains held.

## [1.3.0-alpha.44] - 2026-10-18

- Alpha.43 parent-list reordering did not prevent the native pot fusion, and Bounce placement regressed. Remove that hook rather than stacking another reordering workaround.
- Explicitly handle the three two-ingredient Bounce recipes (Sunflower, Electronion, Boreal Orchid), in either order, on custom Night Roof boards only. Select the actual ingredient, validate ordinary potted-plant terrain rules excluding only the replaced parent's occupied layer, create the exact result via native free SetPlant and consume only that parent via native ByMix. The pot is not consumed.
- Restore ground-support adaptation only inside an active CheckBox call with an actual pot present; never apply it to bare cells. Existing strict pot guard remains. Bumping's separate four-ingredient runner and other boards are unchanged.
- Offline recipe-map and metadata checks pass; both Solstice orders and standalone Bounce-on-pot/no-pot tests remain required in game before release.

## [1.3.0-alpha.43] - 2026-10-18

- Alpha.42 still failed and user reported no-pot placement. Remove the speculative OnHardLand override entirely. Enforce grid pot count late in CheckBox, with no pot-classification exemption for Bounce-family mushrooms.
- Captured logs show Sunflower has two competing parents on the same tile: pot 27 -> 1184 and Bounce 6035 -> Solstice 6041; the next trace contains 1184. Prioritize the non-support mushroom recipe during CheckBox/CheckMix by returning a reordered copy of Get1x1Plants, never mutating the board collection. Leave all parents present for other native checks.
- Scope parent priority to custom Night Roof placement/mix calls and results in the five-member Bounce family; restore nested card context in finalizers. Other boards and pot recipes without a matching mushroom are unchanged.
- Build/metadata checks only. Retest on a fresh pot, including no-pot rejection and both Solstice ingredient orders, before release.

## [1.3.0-alpha.42] - 2026-10-18

- Alpha.41 still rejects Solstice fusion in both ingredient orders without reaching the previous rejection trace. Override CreatePlant.OnHardLand only for the five Bounce-family IDs on custom Night Roof boards; preserve combat classification and all other support/placement restrictions, including required pots.
- Add bounded CheckBox rejection traces for the actual ingredient cards, logging pot count, existing plant IDs and native recipe lookup results. This observes Sunflower-on-Bounce as well as Bounce-on-Sunflower, not only direct result cards.
- Offline build/target checks only. The early-support rejection hypothesis and both Solstice ingredient orders require in-game confirmation before release.

## [1.3.0-alpha.41] - 2026-10-18

- User confirmed Bounce-shroom now plants on Night Roof. Extend the exact scoped RoofRule support fix to Bumping-shroom, Spark-shroom, Pulse-shroom and Solstice-shroom, which share the Caltrop-based placement classification.
- Keep flower pots mandatory and preserve all other placement rules, native Caltrops and non-Night-Roof boards. Rejection diagnostics now include plant IDs for the five-family allowlist.
- Offline allowlist regression check covers IDs 0–7000 and rejects all IDs outside the five intended plants. In-game placement of the four fusions remains to confirm.

## [1.3.0-alpha.40] - 2026-10-18

- User confirmed alpha.39 fixes the Bumping fusion crash. Keep the native ByMix bridge unchanged.
- Bounce-shroom (6035) uses a Caltrop controller but must be supported by pots on custom Night Roof boards. Override only its native RoofRule.Check on those boards using the grid's pot count; do not force the final placement result or alter native Caltrop rules elsewhere.
- Add bounded rejection diagnostics for Bounce on Night Roof: replay validation rules read-only after a rejected placement to log rejecting rule names, layer and pot count (at most six distinct records per board).
- Build and patch-target audit are offline checks. Night Roof planting still requires in-game confirmation; release remains on hold.

## [1.3.0-alpha.39] - 2026-10-18

- Alpha.38 still crashes in BumpingFusionWork.RemoveIngredients. Replace both Bumping ingredient-removal call sites with an explicit IL2CPP runtime invocation of the native virtual Plant.Die(ByMix), bypassing the generated managed Die wrapper. This retains native death/list cleanup, not raw object destruction.
- Snapshot candidate references before removal so native list mutation cannot change the iteration source. Validate method metadata before removal; log capture, per-ingredient native-call boundaries and result creation for further diagnosis.
- Offline audit checks the exact installed method field/signature and JIT-compiles the new bodies without invoking native game code. Root cause and crash resolution still require an in-game retest; no release claim is made.

## [1.3.0-alpha.38] - 2026-10-18

- Alpha.37 did not resolve the reported fatal CLR error. Move all deferred fusion state and processing into BumpingFusionWork, a non-injected static service. The injected runner now only forwards lifecycle calls; removal/result helpers accept primitive coordinates rather than PendingFusion objects and are not inlined into the bridge.
- Require three remaining Lobs with Bounce, or two with a native Bumping result that already consumed one. Keep Bumping preview components out of board cleanup/combat.
- Strengthen the structural audit and add an isolated JIT probe with a harmless managed MonoMod fixture. Both old and new helper bodies compile in that offline context; this does not reproduce the game's fatal error or establish a runtime fix.
- Retest individual ingredient placement, incomplete recipes and both complete recipe orders in game before release.

## [1.3.0-alpha.37] - 2026-10-18

- Bumping-shroom: mark deferred fusion helpers HideFromIl2Cpp, including both methods taking the managed PendingFusion type. Only Unity lifecycle methods should be exposed to native callbacks; this targets the fatal CLR CompileMethodHook stack in RemoveIngredients.
- Restrict the shared pending queue to its owning live board. Recipe, ingredient removal and deferred result creation behavior are unchanged.
- Metadata regression check verifies the managed/native boundary. Build/audit do not reproduce a native JIT crash; the four-ingredient fusion still needs an in-game retest.

## [1.3.0-alpha.36] - 2026-10-18

- Added Almanac favorites for vanilla and mod plants. The supplied original heart PNGs are embedded unchanged: grey means not favorited, red means favorited. Click again to remove a plant. Native UIButton and collider input; no changes to native plant selection or skin controls.
- Added a localized Favorites category (FR/EN/ZH) using the native category template and ShowPlants filter. Removing a favorite refreshes this category immediately; native categories/search clear the favorites-only state.
- Favorites persist locally in Mods/PlantsPlusData/favorites.txt, keyed by plant ID independently of language or skin. Complete-file replacement keeps previous choices intact on write failures; no game save files are changed.
- Offline tests cover persistence, removal, empty lists, malformed lines and write failures, plus exact embedded-image hashes. In-game placement, click and filtering checks remain pending.

## [1.3.0-alpha.35] - 2026-10-18

- Night Roof: logs identify PlaceRule.IceRoadRule.Check reading missing per-row ice-road slots during CheckBox. Restore missing/destroyed entries using CustomizeLib's native IceRoad prefab and initialization values, retaining existing roads. Repair at runtime startup and before placement checks; native placement restrictions and pot requirements remain active.
- Night Roof music and Moon sound pause outside active gameplay (including zero time scale), then resume with UnPause, preserving playback position. Theme loops natively. Do not unpause vanilla audio on component destruction while the game is paused.
- Build and metadata checks are offline only; planting and pause/resume still require in-game confirmation.

## [1.3.0-alpha.34] - 2026-10-17

- Added Solstice-shroom (6041): Bounce-shroom + Sunflower in either order, using the supplied original bundle. French name: Champi-solstice; Chinese: 至日菇.
- Reuses the existing Bounce controller, rest cycle and immunities, with 300 HP, 15 s cooldown and initial cost 125. Each successfully launched zombie produces one normal 25-Sun pickup; no passive generation or preview combat.
- Zero-distance launches at the lawn boundary do not count as successful repels. Recipe/mix-tree registration and 42-entry localized Almanac arrays are included.
- Read-only bundle inspection confirms CaltropPrefab/CaltropPreview; text/metadata/build checks pass. In-game fusion, animation and Sun collection remain for player validation.

## [1.3.0-alpha.33] - 2026-10-17

- Restore the three custom survival captions after native/localization initialization, including their active/visible state, without reflowing the grid.
- Place the Night Roof terrain and plant portrait behind the native stone frame, aligned with the frame's transform. The frame's transparent picture opening now provides the final border silhouette instead of a rectangle being rendered above the border.
- Keep the requested Boreal Orchid/Pulse-shroom icons, stable hover and existing layout. Build/patch audit only; no game launched.

## [1.3.0-alpha.32] - 2026-10-17

- Prevented native mouse-enter/exit handlers from restoring pre-reflow card positions on the managed survival page. The frame's native highlight/normal sprites are retained without positional hover writes.
- Added the requested plant portraits: Boreal Orchid on Night Roof Normal/Hard, Pulse-shroom on Endless.
- The native root Image was the template plant portrait, not the terrain: replaced it with a clipped terrain/plant viewport inside the frame's picture area, preserving the border, trophy and label. Hard/Endless labels use two lines.
- Compiled and audited without starting the game; hover stability and visual appearance await player confirmation.

## [1.3.0-alpha.31] - 2026-10-17

- Fixed the survival grid overlap: assign all 21 entries explicit row/column slots using anchored coordinates and a fixed scale, after native button initialization. No rounding of partially initialized positions or cumulative scaling.
- Replace only each Night Roof card's dedicated terrain portrait, preserving the original frame, hover sprites, title and trophy hierarchy. Do not copy the native Roof trophy's active state onto a new incomplete level.
- Compared the exported SurvivalMenu prefab hierarchy and dimensions; compilation/metadata audits only, visual verification remains pending.

## [1.3.0-alpha.30] - 2026-10-17

- Repaired normal Night Roof packet availability and Adventure's separate unlock gate; restored basic classification for all four starters. Challenge and Evolution War restrictions are preserved.
- Added Normal, Hard and Endless Night Roof survival entries after Roof in the native survival grid, fitting seven columns without removing the former final column.
- Reserved survival IDs 101-103, extended in-memory trophy slots without resetting existing values, and reused native Roof initialization/wave selection with scoped ID restoration. Night Roof map, night/roof tags and Moon runtime are applied separately. Survival transitions, reloads and trophy/save persistence need player validation.
- Fixed the alpha.28 metadata registrar to include Harmony's convention-named Prefix/Postfix/Finalizer methods in patch classes. Expanded the offline audit accordingly; no game launch performed.

## [1.3.0-alpha.29] - 2026-10-17

- Reduced both menu logos from 280 to 180 UI units wide, preserving their original images, aspect ratios, location and language selection.
- Added Bamboom (6040), a normal two-order Bamblock + Cherry Bomb fusion using the supplied bamboom bundle and native Bamboo behaviour: 1500 HP, 500 HP restored per nearby Cherry Bomb explosion in 3x3, and a native 1800-damage cherry blast on death. Removal by shovel/fusion does not explode.
- Added complete French, English and Chinese Almanac entries and aligned the localized entry/type arrays. The added values are initial balance defaults pending player testing.
- Read-only bundle inspection confirmed BambooPrefab/BambooPreview. TextAudit covers 41 entries; PatchAudit covers 221 methods in 183 groups. No game launch or gameplay test was performed.

## [1.3.0-alpha.28] - 2026-10-09

- Replaced this mod's automatic Harmony attribute-instantiation scan with metadata-based explicit patch registration, addressing the startup path shown in the reported fatal CLR stack. CustomizeLib's DLL is unchanged.
- Preserved IL2CPP type registration and CorePlugin lifecycle, patch targets, prefixes/postfixes/finalizers and declared priorities. Added per-group startup diagnostics.
- Offline PatchAudit resolves all 219 patch methods in 181 groups without applying any patches or starting the game. Startup crash resolution remains pending player confirmation.
- Kept both localized logo artworks unchanged.

## [1.3.0-alpha.27] - 2026-10-09

- Added Cecil's original transparent logos to the main menu: principal artwork in French/English, Chinese artwork in Chinese and without the translator.
- Embedded both original PNGs unchanged, preserved their aspect ratios and disabled pointer interception. Logo selection refreshes when the language changes.
- Replaced the text-only mod label while preserving its existing menu placement and entrance animation. Visual placement remains to be checked in game.

## [1.3.0-alpha.26] - 2026-10-07

- Renamed Spark-shroom to Champi-éclaire in French entries, flavor text and in-game changelog. Other languages and gameplay are unchanged.

## [1.3.0-alpha.25] - 2026-10-07

- Re-read native and translated Almanac anecdotes; revised 31 flavor texts in French, English and Chinese with plant-specific habits, interests and voices. Retained nine stories after review.
- Applied Cecil's French naming rule: lowercase after hyphens, capitals on separate main words. Updated Orchidée Boréale, Lance-choucroute, Champi-iceberg and Champi-bond Électrique across entries, recipes and mod notes.
- Renamed Bumping-shroom to Champi-secousse in French. Internal identifiers, fusion logic and other languages are unchanged.
- Expanded text-only name assertions and checked hyphen capitalization. No gameplay testing performed.

## [1.3.0-alpha.24] - 2026-10-07

- Expanded all 40 flavor texts in French, English and Chinese into short scenes with setup, character voice and varied moods instead of isolated punchlines.
- Removed the 160-character editorial constraint; text checks now use only a generous safety ceiling.
- Preserved approved French plant names, descriptions, recipes and gameplay behaviour. Rendering and tone remain subject to player review.

## [1.3.0-alpha.23] - 2026-10-07

- Applied Cecil's eight revised French plant names exactly, including capitalization; updated descriptions and changelog references.
- Harmonized capitalization of other French compound names while preserving approved names and hyphenated family names.
- Replaced the 40 flavor texts in French, English and Chinese with shorter texts and varied tones. No plant mechanics or statistics changed.
- Added text-only assertions for the eight names and flavor-text length, alongside the existing language and formatting checks.

## [1.3.0-alpha.22] - 2026-10-07

- Reworked the 40 active plant entries with shorter opening descriptions and original character anecdotes in English, French and Chinese, using the installed translation's Almanac structure as a reference.
- Completed all French entry bodies, stats, usage conditions, modifiers and recipes. Preserved Cecil's approved plant names; other new French names are editorial proposals.
- Integrated Ceasarweed and Solar Firnace into the language-aware Almanac refresh path instead of leaving their standalone Chinese descriptions.
- Updated Bumping-shroom's target description and removed programming terminology from Superaqua-shroom's description. No gameplay behaviour was changed.
- Rewrote the Boreal mechanics page with concrete effects instead of vague references to future abilities.
- Revised the multilingual changelog and localized custom level/editor scene labels.
- Checked 40 entries across three languages in an isolated text-only harness; build checks do not verify font rendering or gameplay.

## [1.3.0-alpha.21] - 2026-10-07

- Fixed the custom changelog text lookup: the native notice's TextMeshPro component belongs to a child of Content, not Content itself.
- Apply the custom text after the original open callback and translator initialization; retain a bounded retry for deferred opening and only refresh the notice owned by the custom button.
- Recalculate text/content height and reset scrolling to the top when opening the custom notes. The original game's changelog button is unchanged.
- Reorganized the Chinese, English and French custom changelog into numbered sections: Almanac/interface, plants and recipes, plant selection, languages, fixes, and future plans. Vanilla changelog screenshots are a formatting reference, not mod release content.
- Player confirmed the Boreal Plants category works in alpha.20. This changelog hotfix is build-checked only and awaits player UI verification.

## [1.3.0-alpha.20] - 2026-10-07

### Added in this build

- Added the Almanac family category **Boreal Plants / Plantes boréales / 极光植物**, before the custom-plants category. It includes Boreal Orchid and registered fusion descendants such as Pulse-shroom, not every night plant affected by a Boreal boost.
- The category reads the managed fusion registry when opened, so future registered Boreal fusions join the family automatically.
- Updated the in-game changelog and its menu button for Chinese, English and French. Without the optional translation mod, Chinese remains the default.

### Changes accumulated since alpha.4

- Updated compatibility references for PvZ Fusion **4.0.5.2** and the current CustomizeLib port.
- Added Bumping-shroom, Spark-shroom, Superaqua-shroom and Pulse-shroom, with their recipes and custom behaviours.
- Integrated optional translator language detection in the same DLL; French translation is still partial.
- Applied the approved French names: Champi-lob, Champi-bond, Citrouille Lotus, Bamb-noix, Pisto-aimant and Orchidée boréale.
- Reworked Adventure/Sandbox card ordering and paging, custom Almanac data restoration and skin handling during the 4.0 migration.
- Superaqua-shroom charge support now targets Firnurse (1234). Its opening strike uses the prefab's native Shoot2 animation event instead of the manual impact helper involved in CLR crashes; board-identity guards exclude Almanac previews.

### Validation and future plans

- Build checks are not gameplay confirmation. The newest Superaqua-shroom changes and this category/menu update require player verification; no gameplay tests are performed by the coding agent.
- **Not shipped:** the Adventure rework. The intended design is a large mouse-pannable level-selection menu, more levels per world, optional Advanced bonus levels, and shortcuts to the latest unlocked level or an incomplete level. All plants belonging to a world unlock within that world, without one unlock per level.
- The existing Super Level Editor event-level browsing menu is a reference for that future navigation, not an Adventure implementation delivered in this build.

## [1.3.0-alpha.4] - 2026-09-21

- Officially renamed the mod **PvZ Fusion+**; the Chinese 4.0 build outputs `PvZFusionPlus.dll`.
- Localized custom plant Almanac entries, Boreal System mechanics and Night Roof labels for the Chinese game. The separate English/French multi-language edition is deferred.
- Moved the native Snowfield row to page 2 beside Memory Snow, and moved Night Roof plants after Roof on page 1 in Adventure and Sandbox selection.
- Added ingredient-side detection for the three Lob-shroom plus Bounce-shroom Bumping-shroom recipe. This compiles but awaits player gameplay verification.

## [1.3.0-alpha.3] - 2026-09-13

Early v1.3 development build for PvZ Fusion 3.9.

### Added

- Saw-my-pot (6033): Saw-me-not + Flower Pot; Saw-me-not fusion descendants planted on it deal 50% more damage.
- Vortex Pot (6034): Strong Odyssey created from Flower Pot + Maelonstorm; shoveling Vortex Pot returns a collectible Maelonstorm seed packet. It also supports direct Ice-shroom-form conversion, screen-freeze fallback, and native Ice Pot behavior.
- Adventure roster rework: Endoflame takes Plant Giftbox's slot and Gravebuster takes Zombie Giftbox's slot. Endoflame and Gravebuster are now normal basic plants that can participate in the regular fusion system; both Giftboxes become White Cards in their former slots.
- Applied the same Endoflame/Giftbox and Gravebuster/Zombie Giftbox slot swap to the Almanac and Sandbox plant grids.
- Preserved the Sandbox's zero-cost and instant-recharge values after those card swaps.
- Stopped Bomber Drone from firing without a living enemy and restored its native PeaBlover animation controller.
- Persisted Saw-my-pot's Almanac discovery in the active player profile so it remains unlocked after restarting the game.
- Prevented the Sandbox-only card swap from running a second time on the Adventure selection screen.
- Fixed the native previous-loadout restore path so saved Plant Giftbox and Zombie Giftbox cards are selected instead of being ignored.

### Development notes

- Vortex Pot keeps its creation recipe hidden from the Almanac as intended; the Almanac presents the Shovel <-> Flower Pot hint instead.
- Fixed repeated loose Ice-shroom spawning when an ice conversion was attempted.
- Removed the unsafe 3.9 `MixData.TryGetDisMix` tuple read from Saw-my-pot's damage hook.
- Added both v1.3 plants to the Almanac refresh path and restored native UI-controlled description colours.

## [1.2.1] - 2026-09-13

Compatibility hotfix for **PvZ Fusion 3.9**.

### Compatibility

- Updated Plants+ to target **PvZ Fusion 3.9**.
- Updated the required CustomizeLib build to **CustomizeLib.MelonLoader 3.9.0-ml.1**.
- Adapted custom projectile movement, hit counters and piercing behavior to the changes introduced in 3.9.
- Adapted zombie health tracking to the larger health values used by PvZ Fusion 3.9.
- Updated Odyssey modifier text presentation for the 3.9 interface.

### Fixed and polished

- Fixed **Ice-Lord Cactus** anti-air shots by preserving the game's native Cactus air-shot behavior.
- Restored affected piercing, saw and lobbed-projectile mechanics after the 3.9 update.
- Kept existing v1.2 plant mechanics compatible with the new projectile and health systems.

### 3.9 integrations

- Added the **Reaping Collision** Odyssey interaction for Grim Arbiter and Cryolina Reaper.
- Added the **Excavator Obsidian Spike** skin integration for the native Obsidian Spike-nut.
- Added **Slime-shroom** as a native skin for Bubbler-shroom (`CornFume`).
- Updated the Vortex Pot AssetBundle.
- Added the complete Night Roof Adventure level range: six main levels and six challenge levels.

### Notes

- No new custom plant is added in v1.2.1; the Plants+ roster remains at **32 custom plants**.
- **Boreal Orchid (ID 6024)** is still delayed.
- Plants+ v1.2.1 targets PvZ Fusion 3.9. Plants+ v1.2.0 remains the release for PvZ Fusion 3.8.1.

---

## [1.2.0] - 2026-09-06

Third public Plants+ release for PvZ Fusion 3.8.1.

### Added

- 12 new custom plants, bringing Plants+ to 32 plants total:
  - Sea Sharpshooter - an aquatic Sharpshooter that grows through three stronger stages.
  - Cherry StarBomber - fires two rapid volleys of explosive Cherry Stars.
  - Three-Buckpeater - attacks three lanes with armor-breaking iron peas.
  - Sakura Sharpshooter - fires explosive thorns and reacts with nearby Explode-o-shooter blasts.
  - Bomber Drone - a hovering Cherryshooter-based support shooter.
  - Frostbite Drone - a hovering Snow Pea-based support shooter.
  - Ice-Lord Cactus - switches between ground and anti-air attack patterns.
  - Scovilia Pepper - unleashes Jalapeno fire across three lanes.
  - Atomray-shroom - fires a slowing ray and can trigger Demise-shroom effects.
  - Sauerkraut-pult - changes ammunition depending on how close zombies are to the house.
  - Cherry Cabbage - becomes stronger and more explosive as zombies advance.
  - Lob-shroom - lobs spores and can share a tile like Puff-shroom.
- New Almanac entries, recipes, projectiles, effects and previews for the v1.2 plants.
- Night Roof support in Super Level Editor+.
- New Plants+ v1.2 main-menu logo.
- Updated in-game Plants+ changelog.

### Changed and polished

- Three-Buckpeater now fires correctly and has its intended shadow.
- Sakura Sharpshooter's projectile position was corrected.
- General gameplay and visual polish across Plants+.

### Delayed

- Boreal Orchid is not included in v1.2.0 and will arrive in a later update.

---

## [1.1.0] - 2026-07-26

Second public Plants+ release for PvZ Fusion 3.8 and MelonLoader 0.7.3.

### Added

- 10 new custom plants with IDs 6010-6019:
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

---

## [1.0.0] - 2026-07-17

First public release of Plants+ for PvZ Fusion 3.8 and MelonLoader 0.7.3.

### Added

- 10 custom plants with IDs 6000-6009.
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
