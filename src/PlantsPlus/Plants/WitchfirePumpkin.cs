using HarmonyLib;
using Il2Cpp;
using CustomizeLib.BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsPlus.Plants
{
    /// <summary>
    /// Witchfire Pumpkin's prefab bridge and custom mechanics. PVZ Fusion's
    /// JalaPumpkin still owns the working energy counter, while this component
    /// supplies the interactions that are hard-coded to the vanilla type.
    /// </summary>
    public class WitchfirePumpkin : MonoBehaviour
    {
        public const int WitchfirePumpkinID = 6004;
        public const int Toughness = 4000;
        public const int BiteDamage = 300;

        // Pyro Pumpkin has no exposed PlantData entry in PVZ Fusion 3.8.
        // Witchfire therefore owns stable card values instead of attempting
        // to copy data that the game does not publish through PlantDataManager.
        public const float CardRecharge = 30f;
        public const int CardCost = 500;

        public const float ProtectedPlantHealPerSecond = 50f;
        public const float SacrificeCooldownSeconds = 45f;
        public const int BaseExplosionDamage = 1800;
        public const int FusionSacrificeEnergy = 3600;
        public const float RadiationInterval = 0.2f;
        public const int RadiationBaseDamage = 100;
        public const int RadiationDamagePerKill = 20;
        public const float RadiationRadiusPerKill = 0.2f;

        public static int GrenadesBuffID { get; private set; } = -1;
        public static int RadiationBuffID { get; private set; } = -1;

        // Keep the exact protected plant registered while Witchfire is unlit.
        // Re-scanning the tile during a projectile hit proved unreliable once
        // the native pumpkin state changed, so combat hooks use this registry.
        private static readonly Dictionary<int, WitchfirePumpkin>
            ProtectedPlantOwners = new();

        private static bool prefabBridgeLogged;
        private static bool biteEffectLogged;
        [ThreadStatic]
        private static bool applyingProtectedSplash;

        [ThreadStatic]
        private static JalaPumpkin? witchfireFireLineSource;

        private Plant? protectedPlant;
        private Plant? pendingSacrificePlant;
        private bool protectedDamageHitLogged;
        private float healTimer;
        private float sacrificeCooldown;
        private bool wasLit;
        private bool deathExplosionHandled;
        private int lastObservedEnergy = int.MinValue;
        private float radiationTimer;
        private int radiationKills;
        private bool modifierStateLogged;

        public WitchfirePumpkin(IntPtr ptr) : base(ptr) { }

        public JalaPumpkin? NativePlant =>
            gameObject.GetComponent<JalaPumpkin>();

        public static void SetOdysseyBuffIDs(
            int grenadesBuffID,
            int radiationBuffID
        )
        {
            GrenadesBuffID = grenadesBuffID;
            RadiationBuffID = radiationBuffID;
        }

        public void Awake()
        {
            JalaPumpkin? plant = NativePlant;

            if (plant != null && IsWitchfirePumpkin(plant))
                EnsureRuntimeReferences(plant);
        }

        public void Start()
        {
            JalaPumpkin? plant = NativePlant;

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Witchfire Pumpkin] Start failed: no JalaPumpkin component."
                );
                return;
            }

            Plugin.Logger.LogInfo(
                "[Witchfire Pumpkin] Ready" +
                " | Native behaviour = JalaPumpkin" +
                " | HP = " + plant.thePlantHealth +
                "/" + plant.thePlantMaxHealth +
                " | back = " + Describe(plant.back) +
                " | fire3 = " + Describe(plant.fire3) +
                " | Lit(Energy/FireCover) = " + IsLit(plant) +
                " | Energy = " + plant.attributeCount
            );

            wasLit = IsLit(plant);
            lastObservedEnergy = plant.attributeCount;
        }

        public void Update()
        {
            JalaPumpkin? self = NativePlant;

            if (self == null || !IsWitchfirePumpkin(self) || self.dying)
            {
                ClearProtectedPlantBuff();
                return;
            }

            if (sacrificeCooldown > 0f)
                sacrificeCooldown = Mathf.Max(0f, sacrificeCooldown - Time.deltaTime);

            bool lit = IsLit(self);

            if (lit != wasLit)
            {
                Plugin.Logger.LogInfo(
                    "[Witchfire Pumpkin] Lit state changed | " +
                    wasLit + " -> " + lit +
                    " | EnergyLit = " + (self.attributeCount > 0) +
                    " | FireCover = " + self.HasBuff(EffectType.FireCover) +
                    " | Energy = " + self.attributeCount
                );
            }

            if (self.attributeCount != lastObservedEnergy)
            {
                Plugin.Logger.LogInfo(
                    "[Witchfire Pumpkin] Native energy changed | " +
                    lastObservedEnergy + " -> " + self.attributeCount
                );
                lastObservedEnergy = self.attributeCount;
            }

            if (lit)
            {
                // Preserve the exact plant selected while Witchfire was unlit.
                // The native lit transition can remove that plant from the
                // 1x1 lookup before the automatic sacrifice runs.
                if (protectedPlant != null)
                    pendingSacrificePlant = protectedPlant;
                else if (pendingSacrificePlant == null)
                    pendingSacrificePlant = FindProtectedPlant(self);

                ClearProtectedPlantBuff();
                healTimer = 0f;

                // A lit Witchfire automatically consumes the next valid plant
                // as soon as its 45-second sacrifice cooldown is available.
                if (sacrificeCooldown <= 0f &&
                    TryAutomaticSacrifice(self, pendingSacrificePlant))
                {
                    pendingSacrificePlant = null;
                }
            }
            else
            {
                pendingSacrificePlant = null;
                Plant? target = FindProtectedPlant(self);
                SetProtectedPlant(target);
                HealProtectedPlantOverTime(target);
            }

            UpdateRadiation(self);

            wasLit = lit;
        }

        public void OnDestroy()
        {
            ClearProtectedPlantBuff();
        }

        private static bool IsWitchfirePumpkin(Plant? plant)
        {
            return
                plant != null &&
                (int)plant.thePlantType == WitchfirePumpkinID;
        }

        private static bool IsLit(JalaPumpkin? plant)
        {
            if (plant == null)
                return false;

            // Native Pyro Pumpkin stores the energy received from a fire line
            // in attributeCount. A positive counter is its usable lit state.
            if (plant.attributeCount > 0)
                return true;

            try
            {
                // Keep compatibility with effects/modifiers that ignite a
                // plant through PVZ Fusion's generic plant-fire status.
                return plant.HasBuff(EffectType.FireCover);
            }
            catch
            {
                return false;
            }
        }

        private static Plant? FindProtectedPlant(JalaPumpkin shell)
        {
            if (shell == null)
                return null;

            try
            {
                var plants = Lawnf.Get1x1Plants(
                    shell.thePlantColumn,
                    shell.thePlantRow
                );

                if (plants == null)
                    return null;

                // Prefer the exact relationship maintained by the game.
                for (int index = 0; index < plants.Count; index++)
                {
                    Plant plant = plants[index];

                    if (!IsValidProtectedPlant(plant, shell))
                        continue;

                    try
                    {
                        if (plant.Pumpkin == shell)
                            return plant;
                    }
                    catch
                    {
                        // Fall through to the exact-tile lookup below.
                    }
                }

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant plant = plants[index];

                    if (!IsValidProtectedPlant(plant, shell))
                        continue;

                    if (TypeMgr.IsPumpkin(plant.thePlantType))
                        continue;

                    if (TypeMgr.IsPot(plant.thePlantType))
                        continue;

                    if (TypeMgr.IsLily(plant.thePlantType))
                        continue;

                    return plant;
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Protected-plant lookup failed: " +
                    exception.Message
                );
            }

            return null;
        }

        private static bool IsValidProtectedPlant(
            Plant? plant,
            JalaPumpkin shell
        )
        {
            if (plant == null || shell == null)
                return false;

            if (plant.gameObject == shell.gameObject)
                return false;

            if (plant.thePlantColumn != shell.thePlantColumn ||
                plant.thePlantRow != shell.thePlantRow)
            {
                return false;
            }

            if (plant.dying || plant.thePlantHealth <= 0)
                return false;

            // Flying plants are deliberately never buffed or sacrificed.
            try
            {
                if (plant.plantTag.flyingPlant)
                    return false;
            }
            catch
            {
                // TypeMgr checks below still reject support plants.
            }

            return !IsWitchfirePumpkin(plant);
        }

        private void SetProtectedPlant(Plant? target)
        {
            if (SamePlant(protectedPlant, target))
            {
                if (target != null)
                    RegisterProtectedPlant(target);

                return;
            }

            ClearProtectedPlantBuff();
            protectedPlant = target;
            protectedDamageHitLogged = false;

            if (target == null)
                return;

            RegisterProtectedPlant(target);

            Plugin.Logger.LogInfo(
                "[Witchfire Pumpkin] Protected plant buff enabled" +
                " | Plant = " + target.thePlantType +
                " | Damage multiplier = exact x2 on hit" +
                " | Heal = 50 HP/s"
            );
        }

        private void ClearProtectedPlantBuff()
        {
            Plant? target = protectedPlant;
            protectedPlant = null;

            if (target != null)
                UnregisterProtectedPlant(target);

            protectedDamageHitLogged = false;
        }

        private static int DoubleDamage(int damage)
        {
            if (damage <= 0)
                return damage;

            long doubled = (long)damage * 2L;
            return doubled >= int.MaxValue ? int.MaxValue : (int)doubled;
        }

        private static int GetPlantKey(Plant? plant)
        {
            if (plant == null)
                return 0;

            try
            {
                return plant.gameObject.GetInstanceID();
            }
            catch
            {
                return 0;
            }
        }

        private void RegisterProtectedPlant(Plant target)
        {
            int key = GetPlantKey(target);

            if (key != 0)
                ProtectedPlantOwners[key] = this;
        }

        private void UnregisterProtectedPlant(Plant target)
        {
            int key = GetPlantKey(target);

            if (key == 0 ||
                !ProtectedPlantOwners.TryGetValue(
                    key,
                    out WitchfirePumpkin? owner
                ))
            {
                return;
            }

            if (ReferenceEquals(owner, this))
                ProtectedPlantOwners.Remove(key);
        }

        private static bool TryGetProtectedPlantOwner(
            Plant source,
            out WitchfirePumpkin? owner
        )
        {
            owner = null;
            int key = GetPlantKey(source);

            if (key == 0 ||
                !ProtectedPlantOwners.TryGetValue(
                    key,
                    out WitchfirePumpkin? registered
                ) ||
                registered == null)
            {
                return false;
            }

            JalaPumpkin? shell = registered.NativePlant;

            if (shell == null ||
                shell.dying ||
                IsLit(shell) ||
                !SamePlant(registered.protectedPlant, source))
            {
                ProtectedPlantOwners.Remove(key);
                return false;
            }

            owner = registered;
            return true;
        }

        private void HealProtectedPlantOverTime(Plant? target)
        {
            if (target == null)
            {
                healTimer = 0f;
                return;
            }

            healTimer += Time.deltaTime;

            while (healTimer >= 1f)
            {
                healTimer -= 1f;
                HealProtectedPlant(target, Mathf.RoundToInt(
                    ProtectedPlantHealPerSecond
                ));
            }
        }

        private static void HealProtectedPlant(Plant target, int amount)
        {
            if (target == null || target.dying || amount <= 0)
                return;

            int oldHealth = target.thePlantHealth;
            int wantedHealth = Mathf.Min(
                target.thePlantMaxHealth,
                oldHealth + amount
            );

            if (wantedHealth <= oldHealth)
                return;

            try
            {
                target.Recover(
                    wantedHealth - oldHealth,
                    DamageType.Normal,
                    true,
                    false
                );
            }
            catch
            {
                // Direct fallback below keeps the specified 50 HP/s reliable.
            }

            if (target.thePlantHealth < wantedHealth)
                target.thePlantHealth = wantedHealth;

            try
            {
                target.LimHealth();
                target.UpdateText();
            }
            catch
            {
                // HP is already correct; these calls only refresh limits/UI.
            }
        }

        private static bool SamePlant(Plant? first, Plant? second)
        {
            if (first == null || second == null)
                return first == null && second == null;

            try
            {
                return first.gameObject == second.gameObject;
            }
            catch
            {
                return false;
            }
        }

        private bool TryAutomaticSacrifice(
            JalaPumpkin self,
            Plant? preferredTarget = null
        )
        {
            SacrificeInfo? sacrifice = PrepareSacrifice(
                self,
                preferredTarget
            );

            if (!sacrifice.HasValue)
                return false;

            SacrificeInfo info = sacrifice.Value;
            int energyBefore = Mathf.Max(0, self.attributeCount);
            int explosionDamage = CalculateExplosionDamage(energyBefore);

            ShovelSacrificedPlant(info);
            TriggerDualExplosion(self, explosionDamage, "automatic lit sacrifice");
            AwardSacrifice(self, info);

            sacrificeCooldown = SacrificeCooldownSeconds;
            lastObservedEnergy = self.attributeCount;
            return true;
        }

        private bool TriggerClickOrDeathExplosion(
            JalaPumpkin self,
            string source
        )
        {
            if (self == null)
                return false;

            SacrificeInfo? sacrifice = sacrificeCooldown <= 0f
                ? PrepareSacrifice(
                    self,
                    protectedPlant ?? pendingSacrificePlant
                )
                : null;

            if (sacrifice.HasValue)
            {
                ShovelSacrificedPlant(sacrifice.Value);
                AwardSacrifice(self, sacrifice.Value);
                sacrificeCooldown = SacrificeCooldownSeconds;
                pendingSacrificePlant = null;
            }

            // Click/death damage uses the charges after the protected plant's
            // value has been awarded, then consumes all stored charges.
            int energyForDamage = Mathf.Max(0, self.attributeCount);
            int explosionDamage = CalculateExplosionDamage(energyForDamage);
            TriggerDualExplosion(self, explosionDamage, source);

            self.attributeCount = 0;
            lastObservedEnergy = 0;

            try
            {
                self.UpdateText();
            }
            catch
            {
                // The native counter is already depleted.
            }

            return true;
        }

        private static int CalculateExplosionDamage(int energyLevel)
        {
            long value = BaseExplosionDamage +
                (long)Mathf.Max(0, energyLevel) * 10L;

            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }

        private static SacrificeInfo? PrepareSacrifice(
            JalaPumpkin self,
            Plant? preferredTarget = null
        )
        {
            Plant? target = IsValidProtectedPlant(preferredTarget, self)
                ? preferredTarget
                : FindProtectedPlant(self);

            if (target == null)
                return null;

            bool returnsFusionCard = false;
            PlantType returnedCard = PlantType.Nothing;

            // V1's four promised return cards are exact and take priority over
            // MixData, whose startup state can vary between game modes.
            if (TryGetExplicitSacrificeFusion(
                target.thePlantType,
                out PlantType explicitDoomMix
            ))
            {
                returnsFusionCard = true;
                returnedCard = explicitDoomMix;
            }
            // Doom is deliberately checked before Jalapeno when both exist.
            else if (TryGetFusion(
                target.thePlantType,
                PlantType.DoomShroom,
                out PlantType doomMix
            ))
            {
                returnsFusionCard = true;
                returnedCard = doomMix;
            }
            else if (TryGetFusion(target.thePlantType, PlantType.Jalapeno, out PlantType jalaMix))
            {
                returnsFusionCard = true;
                returnedCard = jalaMix;
            }

            int energyGain = returnsFusionCard
                ? FusionSacrificeEnergy
                : BaseExplosionDamage + GetPlantCost(target) * 10;

            return new SacrificeInfo(
                target,
                Mathf.Max(0, energyGain),
                returnsFusionCard,
                returnedCard
            );
        }

        private static int GetPlantCost(Plant plant)
        {
            try
            {
                PlantDataManager.PlantData data =
                    PlantDataManager.GetPlantData(plant.thePlantType);

                return data != null ? Mathf.Max(0, data.cost) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static bool TryGetFusion(
            PlantType plant,
            PlantType ingredient,
            out PlantType result
        )
        {
            result = PlantType.Nothing;

            try
            {
                if (MixData.TryGetMix(plant, ingredient, out result, true) &&
                    result != PlantType.Nothing)
                {
                    return true;
                }

                if (MixData.TryGetMix(ingredient, plant, out result, true) &&
                    result != PlantType.Nothing)
                {
                    return true;
                }

                if (MixData.TryGetMix(plant, ingredient, out result, false) &&
                    result != PlantType.Nothing)
                {
                    return true;
                }

                if (MixData.TryGetMix(ingredient, plant, out result, false) &&
                    result != PlantType.Nothing)
                {
                    return true;
                }
            }
            catch
            {
                // A missing recipe simply falls back to the normal cost gain.
            }

            result = PlantType.Nothing;
            return false;
        }

        private static bool TryGetExplicitSacrificeFusion(
            PlantType plant,
            out PlantType result
        )
        {
            switch (plant)
            {
                case PlantType.SunFlower:
                    result = PlantType.DoomSunflower;
                    return true;
                case PlantType.WallNut:
                    result = PlantType.DoomNut;
                    return true;
                case PlantType.PotatoMine:
                    result = PlantType.PotatoDoom;
                    return true;
                case PlantType.Chomper:
                    result = PlantType.DoomChomper;
                    return true;
                default:
                    result = PlantType.Nothing;
                    return false;
            }
        }

        private static void ShovelSacrificedPlant(SacrificeInfo sacrifice)
        {
            try
            {
                sacrifice.Plant.Die(Plant.DieReason.ByShovel);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Sacrifice shovel failed: " +
                    exception.Message
                );
            }
        }

        private static void AwardSacrifice(
            JalaPumpkin self,
            SacrificeInfo sacrifice
        )
        {
            try
            {
                self.EatEnergy(sacrifice.EnergyGain);
            }
            catch (Exception exception)
            {
                // Keep the counter functional even if a special native state
                // rejects EatEnergy while the shell is dying.
                self.attributeCount += sacrifice.EnergyGain;
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Native EatEnergy fallback used: " +
                    exception.Message
                );
            }

            if (sacrifice.ReturnsFusionCard)
            {
                try
                {
                    // CreateCard returns a regular CardUI. Outside the card
                    // bank it has no dropped-card movement, pickup or expiry,
                    // so it remains on the lawn forever. SetDroppedCard uses
                    // the native collectible lifecycle instead.
                    DroppedCard? returnedCard = Lawnf.SetDroppedCard(
                        self.transform.position,
                        sacrifice.ReturnedCard
                    );

                    if (returnedCard == null)
                    {
                        Plugin.Logger.LogWarning(
                            "[Witchfire Pumpkin] Native dropped fusion card " +
                            "creation returned null."
                        );
                    }
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[Witchfire Pumpkin] Returned fusion card failed: " +
                        exception.Message
                    );
                }
            }

            Plugin.Logger.LogInfo(
                "[Witchfire Pumpkin] Plant sacrificed" +
                " | Plant = " + sacrifice.SacrificedType +
                " | Energy +" + sacrifice.EnergyGain +
                (sacrifice.ReturnsFusionCard
                    ? " | Returned card = " + sacrifice.ReturnedCard
                    : string.Empty)
            );
        }

        private static void TriggerDualExplosion(
            JalaPumpkin self,
            int damage,
            string source
        )
        {
            if (self == null || damage <= 0)
                return;

            bool nativeDoom = false;
            bool nativeFireLine = false;
            bool grenadesModifier = HasGrenadesModifier();
            BoardAction? boardAction = null;

            try
            {
                if (self.board != null)
                    boardAction = self.board.boardAction;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Native board action unavailable: " +
                    exception.Message
                );
            }

            if (boardAction != null)
            {
                if (grenadesModifier)
                {
                    nativeDoom = LaunchGrenadeDoomBombs(
                        self,
                        boardAction,
                        damage
                    );
                }
                else
                {
                    try
                    {
                        Vector3 position = self.transform.position;
                        boardAction.SetDoom(
                            self.thePlantColumn,
                            self.thePlantRow,
                            false,
                            false,
                            new Vector2(position.x, position.y),
                            damage,
                            0,
                            null,
                            true,
                            (PlantType)WitchfirePumpkinID
                        );
                        nativeDoom = true;
                    }
                    catch (Exception exception)
                    {
                        Plugin.Logger.LogWarning(
                            "[Witchfire Pumpkin] Native Doom failed; using " +
                            "damage fallback: " + exception.Message
                        );
                    }
                }

                JalaPumpkin? previousSource = witchfireFireLineSource;
                witchfireFireLineSource = self;

                try
                {
                    boardAction.CreateFireLine(
                        self.thePlantRow,
                        damage,
                        false,
                        false,
                        true,
                        null,
                        (PlantType)WitchfirePumpkinID
                    );
                    nativeFireLine = true;
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[Witchfire Pumpkin] Native Jalapeno fire line failed; " +
                        "using damage fallback: " + exception.Message
                    );
                }
                finally
                {
                    witchfireFireLineSource = previousSource;
                }
            }

            var zombies = (!nativeDoom || !nativeFireLine)
                ? SnapshotEnemyZombies()
                : new List<Zombie>();
            int doomHits = 0;
            int jalapenoHits = 0;

            if (!nativeDoom)
            {
                TryCreateExplosionParticle(self, ParticleType.Doom);

                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];

                    if (!CanExplosionHit(zombie))
                        continue;

                    if (InDoomArea(self, zombie))
                    {
                        DamageZombie(
                            zombie,
                            self,
                            damage,
                            DamageType.DoomExplode
                        );
                        doomHits++;
                    }
                }
            }

            if (!nativeFireLine)
            {
                TryCreateExplosionParticle(self, ParticleType.Fire);

                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];

                    if (!CanExplosionHit(zombie))
                        continue;

                    if (zombie.theZombieRow == self.thePlantRow)
                    {
                        DamageZombie(
                            zombie,
                            self,
                            damage,
                            DamageType.Explode
                        );
                        jalapenoHits++;
                    }
                }
            }

            Plugin.Logger.LogInfo(
                "[Witchfire Pumpkin] Doom + Jalapeno explosion" +
                " | Source = " + source +
                " | Damage each = " + damage +
                " | Grenades modifier = " + grenadesModifier +
                " | Native Doom = " + nativeDoom +
                " | Native fire line = " + nativeFireLine +
                (!nativeDoom ? " | Fallback Doom hits = " + doomHits : "") +
                (!nativeFireLine
                    ? " | Fallback row hits = " + jalapenoHits
                    : "")
            );
        }

        private static bool LaunchGrenadeDoomBombs(
            JalaPumpkin self,
            BoardAction boardAction,
            int damage
        )
        {
            if (self == null || boardAction == null || self.board == null)
                return false;

            int rowCount = Mathf.Max(1, self.board.rowNum);
            int columnCount = Mathf.Max(1, self.board.columnNum);
            bool allRowsHandled = true;
            int thrown = 0;
            int directFallbacks = 0;

            for (int row = 0; row < rowCount; row++)
            {
                Zombie? target = FindLeftmostZombieInRow(row);
                int targetColumn = target != null
                    ? Mathf.Clamp(
                        Lawnf.GetColumnFromX(target.transform.position.x),
                        0,
                        columnCount - 1
                    )
                    : columnCount - 1;
                Vector2 targetPosition = target != null
                    ? new Vector2(
                        target.transform.position.x,
                        target.transform.position.y
                    )
                    : new Vector2(
                        Lawnf.GetBoxXFromColumn(targetColumn),
                        Lawnf.GetBoxYFromRow(row, rowCount)
                    );

                if (TryThrowDoomBomb(
                    self,
                    row,
                    target,
                    targetPosition,
                    damage
                ))
                {
                    thrown++;
                    continue;
                }

                try
                {
                    boardAction.SetDoom(
                        targetColumn,
                        row,
                        false,
                        false,
                        targetPosition,
                        damage,
                        0,
                        null,
                        true,
                        (PlantType)WitchfirePumpkinID
                    );
                    ApplyGrenadeIrradiated(targetPosition, row);
                    directFallbacks++;
                }
                catch (Exception exception)
                {
                    allRowsHandled = false;
                    Plugin.Logger.LogWarning(
                        "[Witchfire Pumpkin] Grenade row " + row +
                        " failed safely: " + exception.Message
                    );
                }
            }

            Plugin.Logger.LogInfo(
                "[Witchfire Pumpkin] Grenades launched" +
                " | Rows = " + rowCount +
                " | Thrown = " + thrown +
                " | Direct fallbacks = " + directFallbacks
            );

            return allRowsHandled && thrown + directFallbacks == rowCount;
        }

        private static bool TryThrowDoomBomb(
            JalaPumpkin self,
            int row,
            Zombie? target,
            Vector2 targetPosition,
            int damage
        )
        {
            try
            {
                CreateBullet creator = CreateBullet.Instance;

                if (creator == null)
                    return false;

                Vector3 origin = self.transform.position;
                Bullet bullet = creator.SetBullet(
                    origin.x,
                    origin.y + 0.35f,
                    row,
                    BulletType.Bullet_doom_throw,
                    BulletMoveWay.Throw,
                    false
                );

                if (bullet == null)
                    return false;

                bullet.from = self;
                bullet.fromType = (PlantType)WitchfirePumpkinID;
                bullet.theBulletRow = row;
                bullet.Damage = damage;

                var position =
                    new Il2CppSystem.Nullable<Vector2>(targetPosition);
                var flightTime = new Il2CppSystem.Nullable<float>(0.8f);

                if (target != null)
                    bullet.ThrowTo(target, position, flightTime);
                else
                    bullet.ThrowToNull(position, flightTime);

                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Native Doom Bomb throw failed; " +
                    "using direct lane detonation: " + exception.Message
                );
                return false;
            }
        }

        private static Zombie? FindLeftmostZombieInRow(int row)
        {
            try
            {
                var zombies = Lawnf.GetZombiesByRow(row, false);

                if (zombies == null)
                    return null;

                Zombie? leftmost = null;
                float leftmostX = float.PositiveInfinity;

                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];

                    if (!CanExplosionHit(zombie))
                        continue;

                    float x = zombie.transform.position.x;

                    if (leftmost == null || x < leftmostX)
                    {
                        leftmost = zombie;
                        leftmostX = x;
                    }
                }

                return leftmost;
            }
            catch
            {
                return null;
            }
        }

        private static List<Zombie> SnapshotEnemyZombies()
        {
            var result = new List<Zombie>();

            try
            {
                var zombies = Lawnf.GetAllZombies(false);

                if (zombies == null)
                    return result;

                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];

                    if (zombie != null)
                        result.Add(zombie);
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Zombie snapshot failed: " +
                    exception.Message
                );
            }

            return result;
        }

        private static bool CanExplosionHit(Zombie? zombie)
        {
            if (zombie == null)
                return false;

            try
            {
                return zombie.theHealth > 0 && !zombie.isMindControlled;
            }
            catch
            {
                return false;
            }
        }

        private static bool InDoomArea(JalaPumpkin self, Zombie zombie)
        {
            if (Math.Abs(zombie.theZombieRow - self.thePlantRow) > 1)
                return false;

            try
            {
                int zombieColumn = Lawnf.GetColumnFromX(
                    zombie.transform.position.x
                );

                return Math.Abs(zombieColumn - self.thePlantColumn) <= 1;
            }
            catch
            {
                return Vector2.Distance(
                    zombie.transform.position,
                    self.transform.position
                ) <= 2.5f;
            }
        }

        private static void DamageZombie(
            Zombie zombie,
            JalaPumpkin source,
            int damage,
            DamageType damageType
        )
        {
            try
            {
                ((Entity)zombie).TakeDamage(
                    damage,
                    source.ToIDamageMaker(),
                    damageType,
                    (PlantType)WitchfirePumpkinID,
                    false
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Explosion damage failed: " +
                    exception.Message
                );
            }
        }

        private static void TryCreateExplosionParticle(
            JalaPumpkin self,
            ParticleType particleType
        )
        {
            try
            {
                CreateParticle.SetParticle(
                    (int)particleType,
                    self.transform.position,
                    self.thePlantRow,
                    true
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Fallback particle unavailable (" +
                    particleType + "): " +
                    exception.Message
                );
            }
        }

        private static bool HasGrenadesModifier()
        {
            if (GrenadesBuffID < 0)
                return false;

            try
            {
                return Lawnf.TravelAdvanced((AdvBuff)GrenadesBuffID);
            }
            catch
            {
                return false;
            }
        }

        private static bool HasRadiationModifier()
        {
            if (RadiationBuffID < 0)
                return false;

            try
            {
                return Lawnf.TravelAdvanced((AdvBuff)RadiationBuffID);
            }
            catch
            {
                return false;
            }
        }

        private void UpdateRadiation(JalaPumpkin self)
        {
            bool radiationActive = HasRadiationModifier();

            if (!radiationActive)
            {
                radiationTimer = 0f;
                return;
            }

            if (!modifierStateLogged)
            {
                modifierStateLogged = true;
                Plugin.Logger.LogInfo(
                    "[Witchfire Pumpkin] Odyssey modifier active" +
                    " | Grenades = " + HasGrenadesModifier() +
                    " | Radiation = true"
                );
            }

            radiationTimer += Time.deltaTime;
            int pulseGuard = 0;

            while (radiationTimer >= RadiationInterval && pulseGuard < 5)
            {
                radiationTimer -= RadiationInterval;
                DamageRadiationPulse(self);
                pulseGuard++;
            }

            // Do not replay an unbounded number of pulses after a long pause.
            if (pulseGuard == 5 && radiationTimer >= RadiationInterval)
                radiationTimer = 0f;
        }

        private void DamageRadiationPulse(JalaPumpkin self)
        {
            if (self == null || self.dying)
                return;

            long rawDamage = RadiationBaseDamage +
                (long)radiationKills * RadiationDamagePerKill;
            int damage = rawDamage >= int.MaxValue
                ? int.MaxValue
                : (int)rawDamage;
            float radiusTiles = 0.5f +
                radiationKills * RadiationRadiusPerKill;
            List<Zombie> zombies = SnapshotEnemyZombies();

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie zombie = zombies[index];

                if (!CanExplosionHit(zombie) ||
                    !InRadiationArea(self, zombie, radiusTiles))
                {
                    continue;
                }

                DamageZombie(
                    zombie,
                    self,
                    damage,
                    DamageType.NormalAll
                );
            }
        }

        private static bool InRadiationArea(
            JalaPumpkin self,
            Zombie zombie,
            float radiusTiles
        )
        {
            if (self == null || zombie == null || self.board == null)
                return false;

            try
            {
                int column = self.thePlantColumn;
                int neighborColumn = column + 1 < self.board.columnNum
                    ? column + 1
                    : Mathf.Max(0, column - 1);
                float tileWidth = Mathf.Abs(
                    Lawnf.GetBoxXFromColumn(neighborColumn) -
                    Lawnf.GetBoxXFromColumn(column)
                );

                if (tileWidth < 0.01f)
                    tileWidth = 1f;

                float horizontalTiles = Mathf.Abs(
                    zombie.transform.position.x -
                    self.transform.position.x
                ) / tileWidth;
                float verticalTiles = Mathf.Abs(
                    zombie.theZombieRow - self.thePlantRow
                );

                return Mathf.Max(horizontalTiles, verticalTiles) <=
                    radiusTiles;
            }
            catch
            {
                return false;
            }
        }

        private void RecordRadiationKill()
        {
            if (!HasRadiationModifier() || radiationKills == int.MaxValue)
                return;

            radiationKills++;

            long rawDamage = RadiationBaseDamage +
                (long)radiationKills * RadiationDamagePerKill;

            Plugin.Logger.LogInfo(
                "[Witchfire Pumpkin] Radiation upgraded by a kill" +
                " | Kills = " + radiationKills +
                " | Damage/0.2s = " +
                (rawDamage >= int.MaxValue ? int.MaxValue : rawDamage) +
                " | Radius = " +
                (0.5f + radiationKills * RadiationRadiusPerKill) +
                " tiles"
            );
        }

        private static void ApplyGrenadeIrradiated(
            Vector2 position,
            int row
        )
        {
            List<Zombie> zombies = SnapshotEnemyZombies();

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie zombie = zombies[index];

                if (!CanExplosionHit(zombie) ||
                    Math.Abs(zombie.theZombieRow - row) > 1)
                {
                    continue;
                }

                try
                {
                    int hitColumn = Lawnf.GetColumnFromX(position.x);
                    int zombieColumn = Lawnf.GetColumnFromX(
                        zombie.transform.position.x
                    );

                    if (Math.Abs(zombieColumn - hitColumn) > 1)
                        continue;

                    // PVZ Fusion 3.8 has no separate Irradiated EffectType;
                    // Poison is its native damage-over-time equivalent.
                    EffectManager.SetEffect(
                        zombie,
                        EffectType.Poison,
                        10f,
                        1f
                    );
                }
                catch
                {
                    // The Doom Bomb damage remains valid for special zombies
                    // that reject generic damage-over-time effects.
                }
            }
        }

        private static bool IsProtectedByUnlitWitchfire(Plant source)
        {
            if (source == null || source.dying)
                return false;

            if (TryGetProtectedPlantOwner(source, out _))
                return true;

            try
            {
                Plant pumpkin = source.Pumpkin;

                if (pumpkin != null && IsWitchfirePumpkin(pumpkin))
                {
                    JalaPumpkin shell =
                        pumpkin.gameObject.GetComponent<JalaPumpkin>();

                    if (shell != null)
                        return !IsLit(shell);
                }
            }
            catch
            {
                // Use the exact-tile fallback below.
            }

            try
            {
                var plants = Lawnf.Get1x1Plants(
                    source.thePlantColumn,
                    source.thePlantRow
                );

                if (plants == null)
                    return false;

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant candidate = plants[index];

                    if (!IsWitchfirePumpkin(candidate))
                        continue;

                    JalaPumpkin shell =
                        candidate.gameObject.GetComponent<JalaPumpkin>();

                    if (shell == null || IsLit(shell))
                        continue;

                    if (SamePlant(FindProtectedPlant(shell), source))
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static Plant? GetDamageSourcePlant(IDamageMaker damageFrom)
        {
            if (damageFrom == null)
                return null;

            try
            {
                Bullet bullet;

                if (damageFrom.IsBullet(out bullet) && bullet != null)
                    return bullet.from;

                Plant plant;

                if (damageFrom.IsPlant(out plant) && plant != null)
                    return plant;
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static WitchfirePumpkin? FindWitchfireKillOwner(
            Plant? source
        )
        {
            if (source == null)
                return null;

            if (TryGetProtectedPlantOwner(
                source,
                out WitchfirePumpkin? protectedOwner
            ))
            {
                return protectedOwner;
            }

            try
            {
                if (IsWitchfirePumpkin(source))
                {
                    return source.gameObject
                        .GetComponent<WitchfirePumpkin>();
                }

                Plant pumpkin = source.Pumpkin;

                if (pumpkin != null && IsWitchfirePumpkin(pumpkin))
                {
                    return pumpkin.gameObject
                        .GetComponent<WitchfirePumpkin>();
                }
            }
            catch
            {
                // Use the exact-tile lookup below.
            }

            try
            {
                var plants = Lawnf.Get1x1Plants(
                    source.thePlantColumn,
                    source.thePlantRow
                );

                if (plants == null)
                    return null;

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant candidate = plants[index];

                    if (!IsWitchfirePumpkin(candidate))
                        continue;

                    WitchfirePumpkin owner = candidate.gameObject
                        .GetComponent<WitchfirePumpkin>();

                    if (owner != null)
                        return owner;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static void ApplyProtectedPlantSplash(
            Zombie primary,
            int newDamage,
            IDamageMaker damageFrom,
            DamageType damageType,
            Plant source
        )
        {
            if (primary == null || newDamage <= 0 || applyingProtectedSplash)
                return;

            int splashDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(newDamage / 3f)
            );

            applyingProtectedSplash = true;

            try
            {
                int targetColumn = Lawnf.GetColumnFromX(
                    primary.transform.position.x
                );
                var zombies = Lawnf.GetZombiesByRow(
                    primary.theZombieRow,
                    false
                );

                if (zombies == null)
                    return;

                var snapshot = new List<Zombie>();
                for (int index = 0; index < zombies.Count; index++)
                {
                    if (zombies[index] != null)
                        snapshot.Add(zombies[index]);
                }

                for (int index = 0; index < snapshot.Count; index++)
                {
                    Zombie zombie = snapshot[index];

                    if (zombie.gameObject == primary.gameObject ||
                        !CanExplosionHit(zombie))
                    {
                        continue;
                    }

                    int column = Lawnf.GetColumnFromX(
                        zombie.transform.position.x
                    );

                    if (column != targetColumn)
                        continue;

                    ((Entity)zombie).TakeDamage(
                        splashDamage,
                        damageFrom,
                        damageType,
                        source.thePlantType,
                        false
                    );

                    EffectManager.SetEffect(
                        zombie,
                        EffectType.Ember,
                        3f,
                        1f
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Protected splash failed safely: " +
                    exception.Message
                );
            }
            finally
            {
                applyingProtectedSplash = false;
            }
        }

        private readonly struct GrenadeExplosionState
        {
            public GrenadeExplosionState(Vector2 position, int row)
            {
                Position = position;
                Row = row;
                Applied = true;
            }

            public Vector2 Position { get; }
            public int Row { get; }
            public bool Applied { get; }
        }

        private readonly struct ProtectedDamageState
        {
            public ProtectedDamageState(
                Plant source,
                int boostedDamage,
                bool applyProtectedEffects,
                WitchfirePumpkin? killOwner,
                int healthBefore
            )
            {
                Source = source;
                BoostedDamage = boostedDamage;
                Applied = applyProtectedEffects;
                KillOwner = killOwner;
                HealthBefore = healthBefore;
                TracksKill = killOwner != null && healthBefore > 0;
            }

            public Plant Source { get; }
            public int BoostedDamage { get; }
            public bool Applied { get; }
            public WitchfirePumpkin? KillOwner { get; }
            public int HealthBefore { get; }
            public bool TracksKill { get; }
        }

        private readonly struct FireLineEnergyState
        {
            public FireLineEnergyState(JalaPumpkin plant)
            {
                Plant = plant;
                EnergyBefore = plant.attributeCount;
            }

            public JalaPumpkin Plant { get; }
            public int EnergyBefore { get; }
        }

        private readonly struct SacrificeInfo
        {
            public SacrificeInfo(
                Plant plant,
                int energyGain,
                bool returnsFusionCard,
                PlantType returnedCard
            )
            {
                Plant = plant;
                SacrificedType = plant.thePlantType;
                EnergyGain = energyGain;
                ReturnsFusionCard = returnsFusionCard;
                ReturnedCard = returnedCard;
            }

            public Plant Plant { get; }
            public PlantType SacrificedType { get; }
            public int EnergyGain { get; }
            public bool ReturnsFusionCard { get; }
            public PlantType ReturnedCard { get; }
        }

        private static void EnsureRuntimeReferences(JalaPumpkin plant)
        {
            if (!IsWitchfirePumpkin(plant))
                return;

            GameObject? nativePrefab = TryGetPlantPrefab(PlantType.JalaPumpkin);
            BridgePrefabReferences(plant, nativePrefab);
        }

        private static void ConfigureRegisteredPrefab()
        {
            GameObject? customPrefab =
                TryGetPlantPrefab((PlantType)WitchfirePumpkinID);

            if (customPrefab == null)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Registered prefab was unavailable " +
                    "after GameAPP.LoadResources."
                );
                return;
            }

            JalaPumpkin? plant = customPrefab.GetComponent<JalaPumpkin>();

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Witchfire Pumpkin] Registered prefab has no " +
                    "JalaPumpkin component."
                );
                return;
            }

            GameObject? nativePrefab = TryGetPlantPrefab(PlantType.JalaPumpkin);
            BridgePrefabReferences(plant, nativePrefab);
        }

        private static void BridgePrefabReferences(
            JalaPumpkin plant,
            GameObject? nativePrefab
        )
        {
            if (plant == null)
                return;

            GameObject customRoot = plant.gameObject;
            JalaPumpkin? nativePlant = nativePrefab != null
                ? nativePrefab.GetComponent<JalaPumpkin>()
                : null;

            if (plant.back == null)
            {
                plant.back = MapNativeReference(
                    customRoot,
                    nativePrefab,
                    nativePlant != null ? nativePlant.back : null,
                    "pumpkin_back"
                );
            }

            if (plant.fire3 == null)
            {
                plant.fire3 = MapNativeReference(
                    customRoot,
                    nativePrefab,
                    nativePlant != null ? nativePlant.fire3 : null,
                    "1_3"
                );
            }

            if (!prefabBridgeLogged)
            {
                prefabBridgeLogged = true;

                Plugin.Logger.LogInfo(
                    "[Witchfire Pumpkin] Prefab bridge" +
                    " | back = " + Describe(plant.back) +
                    " | fire3 = " + Describe(plant.fire3) +
                    " | Native template = " +
                    (nativePrefab != null ? "available" : "unavailable")
                );
            }
        }

        private static GameObject? MapNativeReference(
            GameObject customRoot,
            GameObject? nativeRoot,
            GameObject? nativeReference,
            string fallbackName
        )
        {
            if (customRoot == null)
                return null;

            if (nativeReference != null && nativeRoot != null)
            {
                string? relativePath = GetRelativePath(
                    nativeReference.transform,
                    nativeRoot.transform
                );

                if (!string.IsNullOrEmpty(relativePath))
                {
                    Transform exact = customRoot.transform.Find(relativePath);

                    if (exact != null)
                        return exact.gameObject;
                }

                Transform? sameName = FindDescendant(
                    customRoot.transform,
                    nativeReference.name
                );

                if (sameName != null)
                    return sameName.gameObject;

                // A reference outside the vanilla plant hierarchy is an
                // effect prefab rather than a plant child; it is safe to use
                // the same shared resource from the custom prefab.
                if (!IsDescendantOf(
                    nativeReference.transform,
                    nativeRoot.transform
                ))
                {
                    return nativeReference;
                }
            }

            Transform? fallback = FindDescendant(
                customRoot.transform,
                fallbackName
            );

            return fallback != null ? fallback.gameObject : null;
        }

        private static GameObject? TryGetPlantPrefab(PlantType plantType)
        {
            try
            {
                if (
                    GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(plantType)
                )
                {
                    return null;
                }

                return GameAPP.resourcesManager.plantPrefabs[plantType];
            }
            catch
            {
                return null;
            }
        }

        private static Transform? FindDescendant(
            Transform root,
            string wantedName
        )
        {
            if (root == null || string.IsNullOrEmpty(wantedName))
                return null;

            if (root.name == wantedName)
                return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                Transform? found = FindDescendant(child, wantedName);

                if (found != null)
                    return found;
            }

            return null;
        }

        private static bool IsDescendantOf(
            Transform possibleChild,
            Transform root
        )
        {
            if (possibleChild == null || root == null)
                return false;

            Transform? current = possibleChild;

            while (current != null)
            {
                if (current == root)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static string? GetRelativePath(
            Transform possibleChild,
            Transform root
        )
        {
            if (!IsDescendantOf(possibleChild, root))
                return null;

            if (possibleChild == root)
                return string.Empty;

            var parts = new List<string>();
            Transform? current = possibleChild;

            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Describe(GameObject? value)
        {
            return value != null ? value.name : "<missing>";
        }

        [HarmonyPatch(typeof(GameAPP), nameof(GameAPP.LoadResources))]
        private static class GameAPP_LoadResources_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                try
                {
                    ConfigureRegisteredPrefab();
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogError(
                        "[Witchfire Pumpkin] Resource bridge failed safely: " +
                        exception
                    );
                }
            }
        }

        // PVZ Fusion's native fire-line code feeds energy only to the exact
        // Pyro Pumpkin PlantType. Witchfire uses the same JalaPumpkin class but
        // a custom ID, so mirror that one native interaction for custom plants.
        [HarmonyPatch(typeof(BoardAction), nameof(BoardAction.CreateFireLine))]
        private static class BoardAction_CreateFireLine_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(
                BoardAction __instance,
                int theFireRow,
                out List<FireLineEnergyState>? __state
            )
            {
                __state = null;

                if (__instance == null || __instance.board == null)
                    return;

                try
                {
                    var rowPlants = Lawnf.GetPlantsByRow(
                        __instance.board,
                        theFireRow
                    );

                    if (rowPlants == null)
                        return;

                    var snapshots = new List<FireLineEnergyState>();

                    for (int index = 0; index < rowPlants.Count; index++)
                    {
                        Plant candidate = rowPlants[index];

                        if (!IsWitchfirePumpkin(candidate) || candidate.dying)
                            continue;

                        JalaPumpkin customPyro =
                            candidate.gameObject.GetComponent<JalaPumpkin>();

                        if (customPyro == null ||
                            SamePlant(customPyro, witchfireFireLineSource))
                        {
                            continue;
                        }

                        snapshots.Add(new FireLineEnergyState(customPyro));
                    }

                    if (snapshots.Count > 0)
                        __state = snapshots;
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[Witchfire Pumpkin] Pyro fire-line scan failed " +
                        "safely: " + exception.Message
                    );
                }
            }

            [HarmonyPostfix]
            private static void Postfix(
                int damage,
                List<FireLineEnergyState>? __state
            )
            {
                if (__state == null || damage <= 0)
                    return;

                for (int index = 0; index < __state.Count; index++)
                {
                    FireLineEnergyState snapshot = __state[index];
                    JalaPumpkin plant = snapshot.Plant;

                    if (plant == null ||
                        plant.dying ||
                        !IsWitchfirePumpkin(plant) ||
                        plant.attributeCount != snapshot.EnergyBefore)
                    {
                        continue;
                    }

                    try
                    {
                        plant.EatEnergy(damage);
                    }
                    catch (Exception exception)
                    {
                        long wanted = (long)plant.attributeCount + damage;
                        plant.attributeCount = wanted >= int.MaxValue
                            ? int.MaxValue
                            : (int)wanted;

                        try
                        {
                            plant.UpdateText();
                        }
                        catch
                        {
                        }

                        Plugin.Logger.LogWarning(
                            "[Witchfire Pumpkin] Native Pyro energy fallback " +
                            "used: " + exception.Message
                        );
                    }

                    Plugin.Logger.LogInfo(
                        "[Witchfire Pumpkin] Lit by native fire line" +
                        " | Energy +" + damage +
                        " | Total = " + plant.attributeCount
                    );
                }
            }
        }

        [HarmonyPatch(typeof(JalaPumpkin), nameof(JalaPumpkin.Start))]
        private static class JalaPumpkin_Start_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(JalaPumpkin __instance)
            {
                if (!IsWitchfirePumpkin(__instance))
                    return;

                try
                {
                    EnsureRuntimeReferences(__instance);
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogError(
                        "[Witchfire Pumpkin] Runtime reference bridge failed: " +
                        exception
                    );
                }
            }
        }

        [HarmonyPatch(typeof(JalaPumpkin), nameof(JalaPumpkin.OnEat))]
        private static class JalaPumpkin_OnEat_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(
                JalaPumpkin __instance,
                Zombie zombie,
                out bool __state
            )
            {
                __state = false;

                if (!IsWitchfirePumpkin(__instance) || zombie == null)
                    return;

                try
                {
                    __state = zombie.HasBuff(EffectType.Jala);
                }
                catch
                {
                    __state = false;
                }
            }

            [HarmonyPostfix]
            private static void Postfix(
                JalaPumpkin __instance,
                Zombie zombie,
                bool __state
            )
            {
                if (!IsWitchfirePumpkin(__instance) || zombie == null)
                    return;

                float duration = 1f;
                float value = 1f;

                try
                {
                    // Remove only the Enflammed effect introduced by this
                    // exact bite. A pre-existing Enflammed effect is preserved.
                    if (!__state &&
                        zombie.TryGetEffect<JalaEffect>(
                            EffectType.Jala,
                            out JalaEffect jalaEffect
                        ) &&
                        jalaEffect != null)
                    {
                        duration = Mathf.Max(1f, jalaEffect.totalDuration);
                        value = jalaEffect.Value;
                        zombie.RemoveBuff(EffectType.Jala);
                    }

                    EffectManager.SetEffect(
                        zombie,
                        EffectType.Ember,
                        duration,
                        value
                    );

                    if (!biteEffectLogged)
                    {
                        biteEffectLogged = true;
                        Plugin.Logger.LogInfo(
                            "[Witchfire Pumpkin] Bite effect corrected" +
                            " | Enflammed -> Irritated (Ember)" +
                            " | Native 300 damage preserved"
                        );
                    }
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[Witchfire Pumpkin] Bite Irritated effect failed: " +
                        exception.Message
                    );
                }
            }
        }

        [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
        private static class Zombie_TakeDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(
                Zombie __instance,
                ref int theDamage,
                IDamageMaker damageFrom,
                out ProtectedDamageState __state
            )
            {
                __state = default;

                if (damageFrom == null ||
                    __instance == null ||
                    theDamage <= 0)
                {
                    return;
                }

                Plant? source = GetDamageSourcePlant(damageFrom);

                if (source == null)
                    return;

                WitchfirePumpkin? killOwner =
                    FindWitchfireKillOwner(source);
                TryGetProtectedPlantOwner(
                    source,
                    out WitchfirePumpkin? protectedOwner
                );
                bool applyProtectedEffects =
                    !applyingProtectedSplash &&
                    protectedOwner != null;

                if (!applyProtectedEffects && killOwner == null)
                    return;

                int incomingDamage = theDamage;

                // Double the actual incoming hit instead of mutating the
                // plant's attackDamage. Native projectile formulas can turn
                // a stored 40 into 30; 20 -> 40 here remains an exact x2.
                if (applyProtectedEffects)
                    theDamage = DoubleDamage(theDamage);

                if (applyProtectedEffects &&
                    protectedOwner != null &&
                    !protectedOwner.protectedDamageHitLogged)
                {
                    protectedOwner.protectedDamageHitLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Witchfire Pumpkin] Protected damage x2 active" +
                        " | Plant = " + source.thePlantType +
                        " | Hit " + incomingDamage +
                        " -> " + theDamage +
                        " | Plant attackDamage = " + source.attackDamage
                    );
                }

                __state = new ProtectedDamageState(
                    source,
                    theDamage,
                    applyProtectedEffects,
                    killOwner,
                    __instance.theHealth
                );
            }

            [HarmonyPostfix]
            private static void Postfix(
                Zombie __instance,
                IDamageMaker damageFrom,
                DamageType theDamageType,
                ProtectedDamageState __state
            )
            {
                if (__instance == null || damageFrom == null)
                {
                    return;
                }

                if (__state.TracksKill &&
                    __state.KillOwner != null &&
                    __state.HealthBefore > 0 &&
                    __instance.theHealth <= 0)
                {
                    __state.KillOwner.RecordRadiationKill();
                }

                if (!__state.Applied || applyingProtectedSplash)
                    return;

                try
                {
                    // Ember is the 3.8 internal status displayed as Irritated.
                    EffectManager.SetEffect(
                        __instance,
                        EffectType.Ember,
                        3f,
                        1f
                    );
                }
                catch
                {
                    // Splash damage remains useful if a special zombie rejects
                    // the visual/status effect.
                }

                ApplyProtectedPlantSplash(
                    __instance,
                    __state.BoostedDamage,
                    damageFrom,
                    theDamageType,
                    __state.Source
                );
            }
        }

        [HarmonyPatch(
            typeof(Lawnf),
            nameof(Lawnf.IsUltiPlant)
        )]
        private static class Lawnf_IsUltiPlant_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                PlantType thePlantType,
                ref bool __result
            )
            {
                if ((int)thePlantType == WitchfirePumpkinID)
                    __result = true;
            }
        }

        [HarmonyPatch(
            typeof(Lawnf),
            nameof(Lawnf.GetUltimatePlants)
        )]
        private static class Lawnf_GetUltimatePlants_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                ref Il2CppSystem.Collections.Generic.List<PlantType> __result
            )
            {
                PlantType witchfireType = (PlantType)WitchfirePumpkinID;

                if (__result != null && !__result.Contains(witchfireType))
                    __result.Add(witchfireType);
            }
        }

        [HarmonyPatch(
            typeof(TravelHelper),
            nameof(TravelHelper.GetAllUltimatePlantTypes)
        )]
        private static class TravelHelper_GetAllUltimatePlantTypes_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                bool isStrongUltimate,
                ref Il2CppSystem.Collections.Generic.List<PlantType> __result
            )
            {
                if (__result == null)
                    return;

                PlantType witchfireType = (PlantType)WitchfirePumpkinID;

                if (!isStrongUltimate)
                {
                    if (!__result.Contains(witchfireType))
                        __result.Add(witchfireType);

                    return;
                }

                while (__result.Contains(witchfireType))
                    __result.Remove(witchfireType);
            }
        }

        [HarmonyPatch(
            typeof(AlmanacPlantMenu.__c),
            "_LookUlti_b__20_0"
        )]
        private static class AlmanacPlantMenu_LookUlti_Filter_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(PlantType p, ref bool __result)
            {
                if ((int)p == WitchfirePumpkinID)
                    __result = true;
            }
        }

        [HarmonyPatch(
            typeof(AlmanacPlantMenu.__c__DisplayClass21_0),
            "_LookTravelUlti_b__0"
        )]
        private static class AlmanacPlantMenu_LookTravelUlti_Filter_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                AlmanacPlantMenu.__c__DisplayClass21_0 __instance,
                PlantType p,
                ref bool __result
            )
            {
                if ((int)p == WitchfirePumpkinID)
                    __result = !__instance.isStrongUltimate;
            }
        }

        [HarmonyPatch(
            typeof(Bullet_doom_throw),
            nameof(Bullet_doom_throw.Explode)
        )]
        private static class BulletDoomThrow_Explode_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(
                Bullet_doom_throw __instance,
                out GrenadeExplosionState __state
            )
            {
                __state = default;

                if (__instance == null || !HasGrenadesModifier())
                    return;

                Plant source = __instance.from;

                if (!IsWitchfirePumpkin(source))
                    return;

                Vector3 position = __instance.transform.position;
                __state = new GrenadeExplosionState(
                    new Vector2(position.x, position.y),
                    __instance.theBulletRow
                );
            }

            [HarmonyPostfix]
            private static void Postfix(GrenadeExplosionState __state)
            {
                if (__state.Applied)
                {
                    ApplyGrenadeIrradiated(
                        __state.Position,
                        __state.Row
                    );
                }
            }
        }

        [HarmonyPatch(typeof(JalaPumpkin), nameof(JalaPumpkin.OnClicked))]
        private static class JalaPumpkin_OnClicked_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(
                JalaPumpkin __instance,
                ref bool __result
            )
            {
                if (!IsWitchfirePumpkin(__instance) || !IsLit(__instance))
                    return true;

                WitchfirePumpkin behaviour =
                    __instance.gameObject.GetComponent<WitchfirePumpkin>();

                if (behaviour == null)
                    return true;

                try
                {
                    __result = behaviour.TriggerClickOrDeathExplosion(
                        __instance,
                        "lit click"
                    );

                    // Our implementation performs the custom 1800+energy
                    // double explosion and charge depletion exactly once.
                    return false;
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogError(
                        "[Witchfire Pumpkin] Lit click failed; native click " +
                        "will be used: " + exception
                    );
                    return true;
                }
            }
        }

        [HarmonyPatch(
            typeof(JalaPumpkin),
            nameof(JalaPumpkin.DieEventMustExecute)
        )]
        private static class JalaPumpkin_DieEventMustExecute_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(JalaPumpkin __instance)
            {
                if (!IsWitchfirePumpkin(__instance))
                    return;

                WitchfirePumpkin behaviour =
                    __instance.gameObject.GetComponent<WitchfirePumpkin>();

                if (behaviour == null || behaviour.deathExplosionHandled)
                    return;

                behaviour.deathExplosionHandled = true;

                try
                {
                    behaviour.ClearProtectedPlantBuff();
                    behaviour.TriggerClickOrDeathExplosion(
                        __instance,
                        "death"
                    );
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogError(
                        "[Witchfire Pumpkin] Death explosion failed safely: " +
                        exception
                    );
                }
            }
        }
    }
}
