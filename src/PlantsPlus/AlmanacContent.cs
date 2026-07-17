namespace PlantsPlus.Core
{
    internal readonly struct AlmanacEntry
    {
        public string Name { get; }
        public string Info { get; }
        public string Introduce { get; }

        public AlmanacEntry(string name, string info, string introduce)
        {
            Name = name;
            Info = info;
            Introduce = introduce;
        }
    }

    /// <summary>
    /// English Almanac copy following PVZ Fusion's native layout:
    /// mechanical information first, character lore in the introduce field.
    /// </summary>
    internal static class AlmanacContent
    {
        private const string BrownOpen = "<color=#3D1400>";
        private const string RedOpen = "<color=#8B0000>";
        private const string Close = "</color>";

        private static string Brown(string text)
        {
            return BrownOpen + text + Close;
        }

        private static string Red(string text)
        {
            return RedOpen + text + Close;
        }

        private static string Stat(string label, string value)
        {
            return Brown(label + ": ") + Red(value);
        }

        private static string Bullet(string text)
        {
            return Red("• " + text);
        }

        private static string LoreWithRecipe(string lore, string recipe)
        {
            return Brown(lore) + "\n\n" +
                Brown("Fusion Recipe: ") + Red(recipe);
        }

        private static string LoreWithConversionRecipe(
            string lore,
            string recipe
        )
        {
            return Brown(lore) + "\n\n" +
                Brown("Conversion Recipe: ") + Red(recipe);
        }

        public static readonly AlmanacEntry LotusPumpkin = new AlmanacEntry(
            "Lotus Pumpkin",
            Brown(
                "Lotus Pumpkin turns every hit it survives into energy " +
                "for the plant resting safely inside."
            ) + "\n\n" +
            Stat("Toughness", "4000") + "\n" +
            Brown("Special:") + "\n" +
            Bullet("Protects another plant on its tile like a normal Pumpkin.") + "\n" +
            Bullet(
                "Keeps Snow Lotus's passive charge cycle and gains one " +
                "additional charge whenever it actually loses health."
            ) + "\n" +
            Bullet(
                "At 5 charges, consumes the cycle to heal the protected " +
                "plant for up to 600 HP instead of healing itself."
            ),
            LoreWithRecipe(
                "Lotus Pumpkin insists that self-care means caring for the " +
                "plant inside first. She keeps every petal perfectly " +
                "arranged, every crack in her shell carefully ignored, and " +
                "a reserve of cool lotus energy ready for anyone who needs " +
                "it more than she does.",
                "Pumpkin > Snow Lotus"
            )
        );

        public static readonly AlmanacEntry Bambnut = new AlmanacEntry(
            "Bambnut",
            Brown(
                "Bambnut combines Bamboo's stubborn retaliation with " +
                "Wall-nut's sturdy shell, making every bite a bad idea."
            ) + "\n\n" +
            Stat("Toughness", "4000") + "\n" +
            Brown("Special:") + "\n" +
            Bullet("Blocks zombies as a defensive Nut-type plant.") + "\n" +
            Bullet(
                "Retains Bamboo's native counterattack and collision " +
                "behavior against zombies."
            ),
            LoreWithRecipe(
                "Bambnut has spent years studying the ancient art of " +
                "standing his ground. His technique consists of staying " +
                "perfectly still, looking extremely serious, and waiting " +
                "for the problem to run into him. Zombies keep proving that " +
                "the technique works.",
                "Bamboo > Wall-nut"
            )
        );

        public static readonly AlmanacEntry IcebergShroom = new AlmanacEntry(
            "Iceberg-shroom",
            Brown(
                "Iceberg-shroom brings twice the winter of an Ice-shroom " +
                "and still insists the lawn could use more ice."
            ) + "\n\n" +
            Stat("Damage", "40") + "\n" +
            Brown("Special:") + "\n" +
            Bullet(
                "Freezes normal zombies for 8 seconds, twice as long as " +
                "Ice-shroom."
            ) + "\n" +
            Bullet(
                "Zombies normally immune to ice cannot be frozen, but are " +
                "slowed to 50% movement speed for 8 seconds."
            ),
            LoreWithRecipe(
                "\"Me? Cold? Nah, I just love winter,\" says " +
                "Iceberg-shroom. The chattering teeth, frozen puddle, and " +
                "mountain of ice above his head are, according to him, " +
                "purely decorative.",
                "Ice-shroom > Ice-shroom"
            )
        );

        public static readonly AlmanacEntry WitchfirePumpkin = new AlmanacEntry(
            "Witchfire Pumpkin",
            Brown(
                "Witchfire Pumpkin protects plants while unlit, then " +
                "sacrifices them for overwhelming damage and energy once " +
                "ignited."
            ) + "\n\n" +
            Brown("Usage Conditions: ") + Red("Odyssey Mode") + "\n" +
            Stat("Toughness", "4000") + "\n" +
            Brown("Special:") + "\n" +
            Bullet(
                "Deals 300 damage to biting zombies and applies Irritated."
            ) + "\n" +
            Bullet(
                "While unlit, its protected non-flying plant deals 2x " +
                "damage, heals 50 HP/s, deals 1/3 of its final damage as " +
                "splash damage with no falloff in a 1x1 area, and applies " +
                "Irritated."
            ) + "\n" +
            Bullet(
                "While lit and off its 45-second sacrifice cooldown, it " +
                "consumes the protected non-flying plant and releases a " +
                "Doom explosion and a Jalapeno explosion. Each deals " +
                "1800 + (stored energy x 10) damage."
            ) + "\n" +
            Bullet(
                "A normal sacrifice grants 1800 + (the plant's sun cost x " +
                "10) energy. If that plant has a Doom-shroom or Jalapeno " +
                "fusion, the fusion is returned as a card and grants 3600 " +
                "energy; Doom-shroom takes priority."
            ) + "\n" +
            Bullet(
                "Clicking a lit Witchfire Pumpkin consumes all stored " +
                "energy after any sacrifice gain and triggers both " +
                "explosions. Death triggers the same double explosion " +
                "whether lit or not."
            ) + "\n" +
            Brown("Odyssey Modifiers:") + "\n" +
            Red(
                "1. Grenades: replaces the local Doom explosion with a Doom " +
                "Bomb thrown at the leftmost zombie in every lane, or the " +
                "rightmost column if a lane is empty. The bombs keep the " +
                "original damage and apply Irradiated."
            ) + "\n" +
            Red(
                "2. Radiation: deals 100 damage every 0.2 seconds in a 1x1 " +
                "area. Every zombie killed by Witchfire Pumpkin or its " +
                "protected plant adds 20 damage and 0.2 tiles of radius."
            ),
            LoreWithRecipe(
                "\"We won't discuss what - or who - it eats. Let's talk " +
                "about its hobbies instead! They include... definitely not " +
                "eating people or plants, but definitely eating zombies. " +
                "Witchfire Pumpkin also enjoys the scent of lavender and " +
                "jasmine.\"",
                "Pyro Pumpkin > Doom Pumpkin"
            )
        );

        public static readonly AlmanacEntry NuttySharpshooter =
            new AlmanacEntry(
                "Nutty Sharpshooter",
                Brown(
                    "Nutty Sharpshooter reinforces Spruce Sharpshooter " +
                    "with Wall-nut armor, trading armor-piercing tricks for " +
                    "heavier needles that shove through the crowd."
                ) + "\n\n" +
                Stat("Toughness", "4000") + "\n" +
                Stat("Damage", "30 / 1.5s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet("Immune to freeze and glaciation.") + "\n" +
                Bullet(
                    "Projectiles pierce once and knock every zombie they " +
                    "hit back by 0.5 tiles."
                ) + "\n" +
                Bullet(
                    "Projectiles no longer ignore handheld armor and deal " +
                    "their damage through the normal armor layers."
                ),
                LoreWithRecipe(
                    "Nutty Sharpshooter claims every shot is perfectly " +
                    "calculated. The zombies knocked into one another were " +
                    "also calculated. Probably. He says the Wall-nut shell " +
                    "around his roots improves stability; everyone else " +
                    "suspects he simply likes having somewhere to store " +
                    "spare needles.",
                    "Spruce Sharpshooter > Wall-nut"
                )
            );

        public static readonly AlmanacEntry InfernoTorchflower =
            new AlmanacEntry(
                "Inferno Torchflower",
                Brown(
                    "Inferno Torchflower stores the Sun extracted from " +
                    "nearby projectiles, then releases the entire reserve " +
                    "after gathering enough fire energy."
                ) + "\n\n" +
                Brown("Usage Conditions: ") + Red("Advanced Alt") + "\n" +
                Stat("Sun Output", "25 / 25s") + "\n" +
                Stat("Maximum Energy", "250") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Keeps Torchflower's native production cycle and " +
                    "projectile detection."
                ) + "\n" +
                Bullet(
                    "Whenever she produces Sun, converts eligible plant " +
                    "and hypnotized-zombie projectiles within 1.5 tiles " +
                    "into 5 stored Sun per projectile instead of dropping " +
                    "that Sun immediately."
                ) + "\n" +
                Bullet(
                    "Gains 25 energy whenever a fire line ignites her, up " +
                    "to 250 energy."
                ) + "\n" +
                Bullet(
                    "At 250 energy, click her to consume all energy and " +
                    "release every stored Sun onto the lawn."
                ),
                LoreWithConversionRecipe(
                    "Inferno Torchflower calls every converted projectile " +
                    "a contribution to her \"sunny-day fund.\" She refuses " +
                    "to spend a single Sun until the flames are exactly " +
                    "right, then empties the entire fund onto the lawn at " +
                    "once. Her accountant has requested protective eyewear.",
                    "Sunflower <-> Torchwood"
                )
            );

        public static readonly AlmanacEntry PumpkinPodbomber =
            new AlmanacEntry(
                "Pumpkin Podbomber",
                Brown(
                    "Pumpkin Podbomber copies the plant protected inside, " +
                    "then periodically replaces the copied ammunition with " +
                    "Explode-o-peas."
                ) + "\n\n" +
                Brown("Usage Conditions: ") + Red("Advanced Alt") + "\n" +
                Stat("Toughness", "4000") + "\n" +
                Stat("Damage", "Copied / 50% attack rate") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Protects a plant like Pumpkin Pod and copies an " +
                    "internal Pea-family plant's projectile count, damage, " +
                    "type, and targeting."
                ) + "\n" +
                Bullet(
                    "For every compatible plant except Cherry Shooter, " +
                    "every fourth copied volley is replaced by native " +
                    "Explode-o-peas."
                ) + "\n" +
                Bullet(
                    "With Cherry Shooter inside, every copied projectile is " +
                    "an Explode-o-pea and the plant keeps Pumpkin Pod's " +
                    "half-speed firing cadence."
                ) + "\n" +
                Bullet(
                    "Using the Shovel on Pumpkin Podbomber removes its pod " +
                    "and returns an Explode-o-shooter seed packet."
                ),
                LoreWithConversionRecipe(
                    "Pumpkin Podbomber learned to count for one reason: " +
                    "one, two, three... BOOM. He calls it advanced tactical " +
                    "arithmetic. Cherry Shooter keeps ruining the lesson by " +
                    "answering every number with BOOM, but Pumpkin " +
                    "Podbomber admits the enthusiasm is hard to dislike.",
                    "Pumpkin <-> Shovel"
                )
            );

#if ENABLE_MAGNETOPEA
        public static readonly AlmanacEntry MagnetOPea = new AlmanacEntry(
            "Magnet-o-pea",
            Brown(
                "Magnet-o-pea copies the last supported metal item it " +
                "absorbs and keeps firing its matching pea until a new item " +
                "is supplied."
            ) + "\n\n" +
            Stat("Damage", "20 / 1.5s") + "\n" +
            Brown("Special:") + "\n" +
            Bullet(
                "Absorbs supported items placed directly on it; it does not " +
                "attract equipment from a distance."
            ) + "\n" +
            Bullet(
                "Bucket and Football Helmet select iron and helmet peas. A " +
                "Chrono Disc or Portal Heart selects a portal pea."
            ) + "\n" +
            Bullet(
                "Jack-in-the-box selects a Zomppelin bomb projectile, while " +
                "Giga Mecha Fragments select a Kirov Flagship bomb. Both " +
                "explode in an area on impact."
            ) + "\n" +
            Bullet(
                "The copied projectile remains selected until another " +
                "supported item is absorbed."
            ) + "\n" +
            Bullet(
                "Deals double damage to zombies that still carry metal " +
                "equipment."
            ),
            LoreWithRecipe(
                "\"Clown! Bucket! Football! Whenever you need one, tell me! " +
                "I'll copy it for you - but in pea form!\" Magnet-o-pea " +
                "never pulls metal from a distance; he considers that " +
                "stealing. Hand him something useful, however, and he will " +
                "happily turn it into ammunition.",
                "Magnet-shroom > Peashooter"
            )
        );
#endif
    }
}
