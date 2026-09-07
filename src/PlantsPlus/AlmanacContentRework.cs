using System.Text;

namespace PlantsPlus.Core
{
    /// <summary>
    /// Plants+ Almanac copy written in the same compact order as native
    /// PvZ Fusion entries: purpose, availability, statistics, mechanics,
    /// modifiers, then character lore and the recipe.
    /// </summary>
    internal static class AlmanacContent
    {
        private const string BlackOpen = "<color=black>";
        private const string BrownOpen = "<color=#955300>";
        private const string RedOpen = "<color=#8B0000>";
        private const string UsageOpen = "<color=#4A2900>";
        private const string Close = "</color>";

        private static string Black(string text) => BlackOpen + text + Close;
        private static string Brown(string text) => BrownOpen + text + Close;
        private static string Red(string text) => RedOpen + text + Close;

        private static AlmanacEntry Plant(
            string name,
            string summary,
            string lore,
            string recipe,
            string? usage = null,
            string[]? stats = null,
            string[]? special = null,
            string[]? modifiers = null,
            string? conversion = null
        )
        {
            StringBuilder info = new StringBuilder(Brown(summary));
            info.Append("\n\n");

            if (!string.IsNullOrWhiteSpace(usage))
            {
                info.Append(UsageOpen).Append("Usage Condition: ")
                    .Append(usage).Append(Close).Append('\n');
            }

            if (stats != null)
            {
                for (int index = 0; index + 1 < stats.Length; index += 2)
                {
                    info.Append(Black(stats[index] + ": "))
                        .Append(Red(stats[index + 1])).Append('\n');
                }
            }

            if (special != null && special.Length > 0)
            {
                info.Append(Black("Special: ")).Append('\n');
                for (int index = 0; index < special.Length; index++)
                {
                    info.Append(Black("•")).Append(Red(" " + special[index]));
                    if (index + 1 < special.Length)
                        info.Append('\n');
                }
            }

            if (modifiers != null && modifiers.Length > 0)
            {
                info.Append('\n').Append(Black("Odyssey Modifiers:"));
                for (int index = 0; index < modifiers.Length; index++)
                {
                    info.Append('\n').Append(Red(
                        (index + 1) + ". " + modifiers[index]
                    ));
                }
            }

            string introduce = Black(lore);
            StringBuilder cost = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(recipe))
            {
                cost.Append('\n').Append(Brown("Fusion Recipe: "))
                    .Append(Red(recipe));
            }
            if (!string.IsNullOrWhiteSpace(conversion))
            {
                if (cost.Length > 0)
                    cost.Append('\n');
                else
                    cost.Append('\n');
                cost.Append(Brown("Conversion Recipe: "))
                    .Append(Red(conversion));
            }

            return new AlmanacEntry(
                name,
                info.ToString().TrimEnd(),
                introduce,
                cost.ToString()
            );
        }

        public static readonly AlmanacEntry LotusPumpkin = Plant(
            "Lotus Pumpkin",
            "Lotus Pumpkin converts the punishment she receives into healing for the plant inside.",
            "Lotus Pumpkin keeps a calm smile through every bite. She says serenity is easy when every bruise can be turned into somebody else's recovery.",
            "Pumpkin + Snow Lotus",
            stats: new[] { "Toughness", "4000" },
            special: new[]
            {
                "Protects one plant on the same tile.",
                "Builds charge over time and whenever she actually loses Toughness.",
                "At 5 charges, heals the protected plant for up to 600 Toughness instead of healing herself."
            }
        );

        public static readonly AlmanacEntry Bambnut = Plant(
            "Bambnut",
            "Bambnut is a defensive plant that punishes zombies for getting too close.",
            "Bambnut studied patience under Wall-nut and retaliation under Bamboo. His final lesson was simple: stand still and let the problem hit you first.",
            "Bamboo + Wall-nut",
            stats: new[] { "Toughness", "4000" },
            special: new[]
            {
                "Blocks zombies like a Nut-type defensive plant.",
                "Counterattacks zombies that collide with or bite him."
            }
        );

        public static readonly AlmanacEntry IcebergShroom = Plant(
            "Iceberg-shroom",
            "Iceberg-shroom freezes the lawn for twice as long as an Ice-shroom.",
            "\"This is not excessive,\" Iceberg-shroom insists from beneath several tons of ice. \"The lawn simply needed a longer winter.\"",
            "Ice-shroom + Ice-shroom",
            stats: new[] { "Damage", "40" },
            special: new[]
            {
                "Freezes normal zombies for 8 seconds.",
                "Ice-immune zombies cannot be frozen, but are slowed to 50% movement speed for 8 seconds."
            }
        );

        public static readonly AlmanacEntry WitchfirePumpkin = Plant(
            "Witchfire Pumpkin",
            "Witchfire Pumpkin empowers the plant she protects, then sacrifices it to fuel catastrophic fire.",
            "Witchfire Pumpkin refuses to discuss what feeds the flame. She would much rather talk about lavender, moonlight, and why nobody should inspect the inside of her shell.",
            "Pyro Pumpkin + Doom Pumpkin",
            "Odyssey Mode",
            new[] { "Toughness", "4000", "Sacrifice Cooldown", "45s" },
            new[]
            {
                "While unlit, the protected grounded plant deals ×2 damage, heals 50 Toughness per second and deals one-third of its final damage in a 1×1 area.",
                "When lit and ready, consumes the protected plant and creates both Doom-shroom and Jalapeno effects. Each deals 1800 + (stored energy ×10) damage.",
                "A sacrifice grants energy based on the plant's Sun cost. Doom-shroom and Jalapeno fusions are returned as cards and grant 3600 energy.",
                "Kills on Enflamed or Irritated zombies and nearby Doom explosions provide additional energy.",
                "When Witchfire Pumpkin dies, she releases both explosions without sacrificing another plant."
            },
            new[]
            {
                "Grenades — replaces the local Doom effect with one Doom Bomb thrown into every lane.",
                "Radiation — deals 100 damage every 0.2 seconds nearby; kills increase both damage and radius."
            }
        );

        public static readonly AlmanacEntry NuttySharpshooter = Plant(
            "Nutty Sharpshooter",
            "Nutty Sharpshooter combines a Wall-nut shell with heavy, crowd-controlling needles.",
            "Nutty Sharpshooter says the armor improves his aim. The dents, ricochets and zombies shoved into one another are all part of the calculation. Probably.",
            "Spruce Sharpshooter + Wall-nut",
            stats: new[] { "Toughness", "4000", "Damage", "30/1.5s" },
            special: new[]
            {
                "Needles pierce once and knock every struck zombie back by 0.5 tiles.",
                "Projectiles no longer ignore handheld armor.",
                "Immune to Freeze and Glaciation."
            }
        );

        public static readonly AlmanacEntry InfernoTorchflower = Plant(
            "Inferno Torchflower",
            "Inferno Torchflower stores Sun while ignited and releases it with a growing multiplier.",
            "Inferno Torchflower calls the reserve her sunny-day fund. Her accountant calls it an unsafe furnace full of concentrated sunlight.",
            "",
            stats: new[] { "Sun Output", "25/25s", "Maximum Energy", "250", "Maximum Multiplier", "×2.5" },
            special: new[]
            {
                "Before ignition, behaves like a normal Torchflower and drops Sun automatically.",
                "Each valid Jalapeno fire line grants 50 energy. While lit, produced and extracted Sun is stored.",
                "Click to release the reserve. Every 50 energy adds ×0.5 to the payout, then the reserve and energy reset."
            },
            conversion: "Sunflower <-> Torchwood"
        );

        public static readonly AlmanacEntry PumpkinPodbomber = Plant(
            "Pumpkin Podbomber",
            "Pumpkin Podbomber protects a shooter, copies its volleys and regularly replaces them with explosives.",
            "Pumpkin Podbomber learned tactical arithmetic: one, two, three, BOOM. Cherryshooter keeps answering every number with BOOM, which has not helped the lesson.",
            "",
            stats: new[] { "Toughness", "4000", "Attack Rate", "50% of copied shooter" },
            special: new[]
            {
                "Copies the projectile count, damage, type and targeting of a compatible Pea-family plant inside.",
                "Every fourth copied volley becomes Explode-o-peas.",
                "With Cherryshooter inside, every copied projectile is explosive and the attack interval becomes 3 seconds.",
                "Using the Shovel removes the pod and returns an Explode-o-shooter card."
            },
            conversion: "Pumpkin <-> Shovel"
        );

        public static readonly AlmanacEntry CherryStarBomber = Plant(
            "Cherry StarBomber",
            "Cherry StarBomber fills the lawn with two rapid volleys of explosive Cherry Stars.",
            "Cherry StarBomber insists that ten stars are not excessive. A complete tactical constellation simply requires proper coverage—and a second volley for certainty.",
            "",
            "Odyssey Mode | Epic Odyssey",
            new[] { "Damage", "300×10/1.5s" },
            new[]
            {
                "Each attack fires two consecutive volleys of five Cherry Stars.",
                "Uses the Cherry Stars created when Gatling Cherrybomber projectiles pass through Inferno Starwood."
            },
            new[]
            {
                "Guided Constellation — Cherry Stars home in on zombies.",
                "Stellar Momentum — after travelling 3 tiles, Cherry Stars grow and deal ×2 damage.",
                "Rolling Helpers — every Jugger-nut creates a real Rolling Wall-nut in its lane every 60 seconds."
            },
            "Starfruit <-> Peashooter"
        );

        public static readonly AlmanacEntry NotAPea = Plant(
            "Not-a-pea",
            "Not-a-pea fires saw-peas that pierce crowds and can remain lodged in zombies.",
            "Not-a-pea is tired of being asked whether he is a pea pretending to be a saw or a saw pretending to be a pea. He cut the interview short.",
            "Saw-me-not + Peashooter",
            stats: new[] { "Damage", "20/1.5s" },
            special: new[]
            {
                "Saw-peas pierce every zombie in their path.",
                "Each hit has a 25% chance to attach the saw to that zombie.",
                "An attached saw deals 10 damage every 1.5 seconds for 10 seconds."
            }
        );

        public static readonly AlmanacEntry NotAStormCommando = Plant(
            "Not-a-storm Commando",
            "Not-a-storm Commando replaces Pea-storm Commando's entire volley with persistent saw-peas.",
            "Every mission is described as surgical precision. Nobody argues after the briefing ends with six smoking barrels and a pile of sawdust.",
            "Saw-me-not + Pea-storm Commando",
            "Odyssey Mode | Epic Odyssey",
            new[] { "Projectile Damage", "20 each", "Attached Damage", "10/1.5s for 10s" },
            new[]
            {
                "Keeps Pea-storm Commando's attack interval, firing angles and projectile count.",
                "Every saw-pea pierces and has a 25% chance per hit to attach to its victim."
            }
        );

        public static readonly AlmanacEntry FrostFurflower = Plant(
            "Frost Furflower",
            "Frost Furflower produces Sun and turns direct snowball hits into a profitable cold snap.",
            "Frost Furflower considers snowball fights a renewable-energy program. Her frozen neighbors have requested a less interactive funding model.",
            "Hoarfrost Lichen + Sunflower",
            stats: new[] { "Sun Output", "25/25s", "Snowball Bonus", "100 Sun" },
            special: new[]
            {
                "Produces Sun with the normal Sunflower cycle.",
                "When struck by a snowball, immediately drops 100 Sun.",
                "The hit freezes every other freezable plant in the surrounding 3×3 area."
            }
        );

        public static readonly AlmanacEntry Doomtronion = Plant(
            "Doomtronion",
            "Doomtronion channels nearby Amp-nions into irradiated chain lightning and unstable Doom reactions.",
            "Doomtronion asks everyone to stop measuring the glow. The Geiger counter cannot respond because it has been screaming since breakfast.",
            "Amp-nion + Doom-shroom",
            stats: new[] { "Damage", "100/1.5s", "Attack Range", "5×5" },
            special: new[]
            {
                "Each idle Amp-nion or Doomtronion in range adds 150 damage to the attack.",
                "Lightning arcs to up to 3 nearby zombies for half damage.",
                "Struck zombies become Irradiated for 10 seconds.",
                "Each struck zombie has a 25% chance to trigger a craterless 1800-damage Doom-shroom explosion."
            }
        );

        public static readonly AlmanacEntry LichenPea = Plant(
            "Lichen-pea",
            "Lichen-pea chills zombies and may freeze friendly plants around each victim.",
            "Lichen-pea calls the white coat winter camouflage. His teammates call it advance warning to stand somewhere else.",
            "Hoarfrost Lichen + Peashooter",
            stats: new[] { "Damage", "20/1.5s" },
            special: new[]
            {
                "Peas inflict Cold on every zombie they hit.",
                "Each hit has a 25% chance to freeze 2–4 random freezable plants in the surrounding 3×3 area."
            }
        );

        public static readonly AlmanacEntry LogicBlover = Plant(
            "Logic Blover",
            "Logic Blover remains on the lawn, blows zombies backward and assigns a random status to each one.",
            "Logic Blover calculates every possible outcome before choosing one at random. Somehow, he considers this proof that luck is perfectly logical.",
            "",
            "Harvest Mode",
            new[] { "Toughness", "300" },
            new[]
            {
                "Each zombie independently receives Ember, Cold, Butter or Poison.",
                "While present, using Blover, Lucky Blover or Mimic Rye adds 5%, 15% or 16% Gift Box luck respectively.",
                "Every Gift Box rolls the accumulated luck chance.",
                "Using a Gold Bean spends 10,000 money to make Logic Blover blow again."
            }
        );

        public static readonly AlmanacEntry SolarSharpshooter = Plant(
            "Solar Sharpshooter",
            "Solar Sharpshooter converts every successful piercing hit into Sun.",
            "Solar Sharpshooter never misses an opportunity to make hay while the Sun shines. Zombies object to being classified as opportunities.",
            "Spruce Sharpshooter + Sunflower",
            stats: new[] { "Damage", "30/1.5s", "Sun per Hit", "25" },
            special: new[]
            {
                "Projectiles pierce multiple zombies.",
                "Immediately grants 25 Sun for every zombie actually struck."
            }
        );

        public static readonly AlmanacEntry SeaBallista = Plant(
            "Sea Ballista",
            "Sea Ballista occupies two aquatic tiles and fires delayed explosive bolts at short range.",
            "Sea Ballista claims the tide retrieves every bolt for reuse. Nobody has asked how that works after the bolt explodes.",
            "Spruce Ballista + Sea-shroom",
            stats: new[] { "Damage", "80/3s", "Range", "3.5 tiles" },
            special: new[]
            {
                "Aquatic plant occupying two adjacent tiles.",
                "Bolts pierce twice and explode 0.3 seconds after striking a zombie.",
                "The delayed explosion knocks the struck zombie backward."
            }
        );

        public static readonly AlmanacEntry SeaSharpshooter = Plant(
            "Sea Sharpshooter",
            "Sea Sharpshooter matures through three stages, gaining stronger projectiles as it grows.",
            "Sea Sharpshooter says growing underwater builds character. It also builds much sharper ammunition.",
            "Spruce Sharpshooter + Sea-shroom",
            stats: new[] { "Damage", "20 → 40 → 60/1.5s", "Growth Interval", "30s" },
            special: new[]
            {
                "Aquatic plant with three growth stages.",
                "Starts at 65% size, grows to 82%, then reaches full size."
            }
        );

        public static readonly AlmanacEntry ThreeBuckpeater = Plant(
            "Three-Buckpeater",
            "Three-Buckpeater fires armor-breaking iron peas across three lanes.",
            "Three-Buckpeater could not decide which lane needed the bucket most, so he selected all three.",
            "Threepeater + Bucket",
            stats: new[] { "Damage", "80/1.5s per lane" },
            special: new[]
            {
                "Attacks its own lane and both adjacent lanes.",
                "Iron peas knock zombies backward and instantly destroy Type 2 armor."
            }
        );

        public static readonly AlmanacEntry Pineshooter = Plant(
            "Pineshooter",
            "Pineshooter launches whole pine trees that turn zombies into projectiles of their own.",
            "Pineshooter was told that throwing the entire tree was wasteful. Nobody volunteered to retrieve one from the horde.",
            "Peashooter + Spruce Sharpshooter",
            stats: new[] { "Damage", "40/1.5s" },
            special: new[]
            {
                "Each pine knocks the first zombie hit backward.",
                "If that zombie collides with another zombie, both take 40 extra damage and are briefly stunned.",
                "Immune to Freeze and Glaciation."
            }
        );

        public static readonly AlmanacEntry Icytronion = Plant(
            "Icytronion",
            "Icytronion channels nearby Amp-nions into freezing chain lightning.",
            "Icytronion insists lightning cannot be cold. The frozen zombies are currently unable to disagree.",
            "Amp-nion + Ice-shroom",
            stats: new[] { "Damage", "100/1.5s", "Attack Range", "5×5" },
            special: new[]
            {
                "Each idle Amp-nion or Icytronion in range adds 150 damage.",
                "Lightning arcs to up to 3 nearby zombies for half damage.",
                "Each victim is slowed for 10 seconds and has a 25% chance to be frozen.",
                "Immune to Freeze and Glaciation."
            }
        );

        public static readonly AlmanacEntry SakuraSharpshooter = Plant(
            "Sakura Sharpshooter",
            "Sakura Sharpshooter fires explosive blossom-thorns and amplifies nearby Explode-o-shooter chain reactions.",
            "Sakura Sharpshooter calls the petals decorative. Zombies stopped believing her after one blossom triggered three explosions.",
            "Cherry Bomb + Spruce Sharpshooter",
            stats: new[] { "Damage", "30/1.5s" },
            special: new[]
            {
                "Each thorn has a 15% chance to create a Cherry Bomb explosion on impact.",
                "A zombie damaged by a nearby Explode-o-shooter blast has a 50% chance to explode for the blast's full damage.",
                "A successful chain reaction also makes one random zombie in the surrounding 3×3 explode for half damage."
            }
        );

        public static readonly AlmanacEntry BorealOrchid = Plant(
            "Boreal Orchid",
            "Boreal Orchid periodically covers her lane with a stationary Boreal Wave.",
            "Boreal Orchid does not ask the night to become brighter. She teaches darkness how to bloom.",
            "",
            stats: new[] { "Toughness", "300", "Boreal Wave", "Every 35s" },
            special: new[]
            {
                "The wave remains across the full lane for the duration of the Boreal boost.",
                "Night plants in the lane gain 35% damage, Toughness, attack speed, production speed and movement speed.",
                "Plantern receives the boost and generates Boreal Points instead of Lumos Points."
            }
        );

        public static readonly AlmanacEntry BomberDrone = Plant(
            "Bomber Drone",
            "Bomber Drone hovers above the lawn and fires Cherryshooter projectiles.",
            "Bomber Drone says the cherries are perfectly safe. The emergency landing button remains suspiciously red.",
            "Cherryshooter + Blover",
            stats: new[] { "Damage", "40/1.5s" },
            special: new[]
            {
                "Hovering plant; safely avoids ground-level obstacles.",
                "Uses Cherryshooter projectiles. They do not explode on impact.",
                "If the straight-shooting plant directly below attacks faster than once every 1.5 seconds, Bomber Drone matches its attack interval."
            }
        );

        public static readonly AlmanacEntry FrostbiteDrone = Plant(
            "Frostbite Drone",
            "Frostbite Drone hovers above the lawn and fires Snow Pea projectiles.",
            "Frostbite Drone prefers to keep a cool head. Being several feet above everyone else certainly helps.",
            "Snow Pea + Blover",
            stats: new[] { "Damage", "20/1.5s" },
            special: new[]
            {
                "Hovering plant; safely avoids ground-level obstacles.",
                "Fires Snow Pea projectiles that chill zombies.",
                "If the straight-shooting plant directly below attacks faster than once every 1.5 seconds, Frostbite Drone matches its attack interval."
            }
        );

        public static readonly AlmanacEntry IceLordCactus = Plant(
            "Ice-Lord Cactus",
            "Ice-Lord Cactus changes firing patterns to dominate both ground and airborne zombies.",
            "Ice-Lord Cactus insists the frozen crown is not attached to his head. Nobody has been brave enough to verify the claim.",
            "Ice Cactus + Iceberg-shroom",
            "Odyssey Mode",
            new[] { "Ground Attack", "20×3/2s", "Air Attack", "80/1s" },
            new[]
            {
                "Fires three Ice Cactus projectiles against grounded targets and one raised projectile against airborne targets.",
                "Deals ×4 damage to airborne zombies.",
                "Freezes normal zombies for 8 seconds.",
                "Ice-immune zombies cannot be frozen, but are slowed to 50% movement speed for 8 seconds."
            }
        );

        public static readonly AlmanacEntry ScoviliaPepper = Plant(
            "Scovilia Pepper",
            "Scovilia Pepper unleashes Jalapeno fire across three lanes at once.",
            "Scovilia Pepper stopped measuring heat in Scoville units after the thermometer resigned.",
            "Jalapeno + Jalapeno",
            stats: new[] { "Damage", "Jalapeno damage", "Range", "3 lanes" },
            special: new[]
            {
                "Burns every zombie in its own lane and both adjacent lanes."
            }
        );

        public static readonly AlmanacEntry AtomrayShroom = Plant(
            "Atomray-shroom",
            "Atomray-shroom fires a slowing energy ray and destabilizes the lawn with Demise-shroom effects.",
            "Atomray-shroom says the blue warning glow is simply a courteous reminder to stand somewhere else.",
            "",
            "Odyssey Mode",
            new[] { "Damage", "40/1.5s", "Demise Chance", "25% per attack" },
            new[]
            {
                "The ray slows its target for 3 seconds.",
                "Each attack has a 25% chance to trigger a Demise-shroom effect at the target.",
                "Triggers a Demise-shroom effect when it dies.",
                "Each nearby Atomheart Plantern increases attack speed by 25%, down to a minimum interval of 0.5 seconds."
            },
            conversion: "Plantern <-> Fume-shroom"
        );

        public static readonly AlmanacEntry CherryCabbage = Plant(
            "Cherry Cabbage",
            "Cherry Cabbage lobs volatile cabbages that become more dangerous as zombies approach the house.",
            "Cherry Cabbage has a strict doorstep policy. The closer an uninvited guest gets, the less polite his welcome becomes.",
            "Cherry Bomb + Cabbage-pult",
            stats: new[] { "Damage", "40-160/3s" },
            special: new[] {
                "Damage increases from 40 at the far edge of the lawn to 160 next to the house.",
                "Each cabbage has a 15%-60% chance to explode on impact, increasing as its impact gets closer to the house.",
                "Explosions deal additional area damage equal to the projectile's damage."
            }
        );

        public static readonly AlmanacEntry LobShroom = Plant(
            "Lob-shroom",
            "Lob-shroom launches spores in an arc and can share a tile with other Puff-shrooms.",
            "Lob-shroom claims the basket makes every spore more aerodynamic. The spores refuse to comment.",
            "",
            stats: new[] { "Damage", "20/1.5s", "Toughness", "300" },
            special: new[]
            {
                "Lobs Puff-shroom spores at zombies.",
                "Can be stacked on the same tile like Puff-shroom."
            }
        );

        public static readonly AlmanacEntry SauerkrautPult = Plant(
            "Sauerkraut-pult",
            "Sauerkraut-pult changes ammunition according to how far the target has advanced across the lawn.",
            "Sauerkraut-pult calls the close-range shot a focused fermentation. Pepper Popper calls it lunch with consequences.",
            "Garbage-pult + Pepper Popper",
            "Odyssey Mode",
            new[] { "Focus Damage", "300/3s", "Splash Damage", "100/3s" },
            new[]
            {
                "Focus Mode targets zombies 1-5 tiles from the house, dealing high single-target damage and inflicting Enflamed.",
                "Splash Mode targets zombies 6-10 tiles from the house, dealing moderate area damage and inflicting Poison.",
                "Uses a distinct projectile for each mode."
            }
        );

#if ENABLE_MAGNETOPEA
        public static readonly AlmanacEntry MagnetOPea = Plant(
            "Magnet-o-pea",
            "Magnet-o-pea copies supported metal items and converts them into specialized ammunition.",
            "\"Clown! Bucket! Football! Hand it over and I'll copy it—in pea form!\" Magnet-o-pea considers remote attraction stealing, but supplied ammunition perfectly legitimate.",
            "Magnet-shroom + Peashooter",
            stats: new[] { "Damage", "20/1.5s" },
            special: new[]
            {
                "Absorbs supported items placed directly on it and keeps the selected projectile until another item is supplied.",
                "Bucket, Football Helmet and Chrono Disc select iron, helmet and portal peas.",
                "Jack-in-the-box and Giga Mecha Fragments select explosive projectiles.",
                "Deals ×2 damage to zombies that still carry metal equipment."
            }
        );
#endif
    }
}
