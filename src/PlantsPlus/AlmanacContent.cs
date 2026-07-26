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
            return Red("\u2022 " + text);
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
                "Gains charges over time and gains one additional charge " +
                "whenever it actually loses health."
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
                "Counterattacks zombies that collide with or bite it."
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
                "Deals 50 base damage to biting zombies, plus 5 damage for " +
                "every 30 stored charges, and applies Irritated."
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
                "energy; Doom-shroom takes priority. Zero-cost plants count " +
                "as costing 25 Sun."
            ) + "\n" +
            Bullet(
                "Its charge display shows READY when a lit sacrifice is " +
                "available. Clicking then consumes the plant and all stored " +
                "charge, triggers both explosions, and leaves Witchfire " +
                "unlit for 45 seconds. Death triggers the double explosion " +
                "without processing another sacrifice."
            ) + "\n" +
            Bullet(
                "An Enflamed zombie dying in its lane grants 100 charge; an " +
                "Irritated zombie grants 250 instead. Nearby Doom-shroom " +
                "explosions also grant charge."
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
                    "Inferno Torchflower behaves like a regular Torchflower " +
                    "until fire awakens her furnace. While lit, she stores " +
                    "her Sun for a multiplied manual harvest."
                ) + "\n\n" +
                Stat("Sun Output", "25 / 25s") + "\n" +
                Stat("Maximum Energy", "250") + "\n" +
                Stat("Maximum Multiplier", "x2.5") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Before being ignited, behaves like a regular " +
                    "Torchflower and drops her Sun automatically."
                ) + "\n" +
                Bullet(
                    "Each valid Jalapeno fire line grants 50 energy, up to " +
                    "250. Once lit, all Sun she produces or extracts from " +
                    "nearby projectiles is stored instead of dropped."
                ) + "\n" +
                Bullet(
                    "Click her to release the stored Sun. Every 50 energy " +
                    "adds x0.5 to the payout, up to x2.5; clicking resets " +
                    "both the reserve and energy to 0."
                ),
                LoreWithConversionRecipe(
                    "Inferno Torchflower calls every stored ray a deposit " +
                    "in her \"sunny-day fund.\" The hotter her furnace gets, " +
                    "the more generous the withdrawal becomes. Her " +
                    "accountant still recommends waiting for 250.",
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
                    "every fourth copied volley is replaced by " +
                    "Explode-o-peas."
                ) + "\n" +
                Bullet(
                    "With Cherry Shooter inside, every copied projectile is " +
                    "an Explode-o-pea and the plant attacks once every 3 " +
                    "seconds. Other inner plants keep the normal copied " +
                    "cadence."
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

        public static readonly AlmanacEntry NotAPea =
            new AlmanacEntry(
                "Not-a-pea",
                Brown(
                    "Not-a-pea fires spinning saw-peas that keep cutting " +
                    "through a crowd and may decide to stay for a while."
                ) + "\n\n" +
                Stat("Damage", "20 / 1.5s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Its saw projectile pierces zombies instead of " +
                    "disappearing after the first hit."
                ) + "\n" +
                Bullet(
                    "Every zombie hit has a 25% chance to catch the saw."
                ) + "\n" +
                Bullet(
                    "An attached saw deals 10 damage every 1.5 seconds for " +
                    "10 seconds, then disappears."
                ),
                LoreWithRecipe(
                    "Not-a-pea is tired of being asked whether he is a pea " +
                    "pretending to be a saw or a saw pretending to be a " +
                    "pea. He filed the question under \"things to cut " +
                    "short\" and shredded the entire folder.",
                    "Saw-me-not > Peashooter"
                )
            );

        public static readonly AlmanacEntry NotAStormCommando =
            new AlmanacEntry(
                "Not-a-storm Commando",
                Brown(
                    "Not-a-storm Commando combines Pea-storm Commando's " +
                    "entire firing pattern with a storm of persistent " +
                    "saw-peas."
                ) + "\n\n" +
                Brown("Usage Conditions: ") + Red("Odyssey Mode") + "\n" +
                Stat("Projectile Damage", "20 each") + "\n" +
                Stat("Attached Damage", "10 / 1.5s for 10s") + "\n" +
                Stat("Attack Pattern", "Pea-storm Commando's volley") +
                "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Uses Pea-storm Commando's attack cadence, " +
                    "angles, and projectile count."
                ) + "\n" +
                Bullet(
                    "Each saw-pea deals 20 base damage and pierces every " +
                    "zombie in its path."
                ) + "\n" +
                Bullet(
                    "Every zombie hit has a 25% chance to catch the saw."
                ) + "\n" +
                Bullet(
                    "An attached saw deals half of its projectile's damage " +
                    "every 1.5 seconds for 10 seconds. At base damage, that " +
                    "is 10 per tick and 60 total damage."
                ) + "\n" +
                Brown("Odyssey Modifiers:") + "\n" +
                Red(
                    "None. Not-a-storm Commando is a strong Odyssey fusion; " +
                    "its complete saw-pea volley is always active."
                ),
                LoreWithRecipe(
                    "Not-a-storm Commando calls every operation \"surgical " +
                    "precision.\" The lawn accepts the description, mostly " +
                    "because every briefing ends with six smoking barrels, " +
                    "a pile of sawdust, and nobody brave enough to ask a " +
                    "follow-up question.",
                    "Saw-me-not > Pea-storm Commando"
                )
            );

        public static readonly AlmanacEntry FrostFurflower =
            new AlmanacEntry(
                "Frost Furflower",
                Brown(
                    "Frost Furflower produces Sun normally, then turns a " +
                    "direct snowball hit into a very profitable cold snap."
                ) + "\n\n" +
                Stat("Sun Output", "25 / 25s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Produces Sun with the normal Sunflower cycle."
                ) + "\n" +
                Bullet(
                    "When struck by a snowball, immediately drops 100 Sun."
                ) + "\n" +
                Bullet(
                    "The same hit freezes every other freezable plant in " +
                    "the surrounding 3x3 area. Frost Furflower is not " +
                    "frozen by its own cold snap."
                ),
                LoreWithRecipe(
                    "Frost Furflower considers snowball fights a renewable " +
                    "energy program. Her neighbors consider them an " +
                    "unannounced cryogenic experiment. Both sides agree the " +
                    "Sun is excellent; negotiations concerning the frozen " +
                    "petals are still ongoing.",
                    "Hoarfrost Lichen > Sunflower"
                )
            );


        public static readonly AlmanacEntry Doomtronion =
            new AlmanacEntry(
                "Doomtronion",
                Brown(
                    "Doomtronion focuses the power of nearby Amp-nions into " +
                    "one devastating electrical shot that arcs through the " +
                    "horde."
                ) + "\n\n" +
                Stat("Damage", "100 / 1.5s") + "\n" +
                Stat("Attack Range", "5 x 5") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Attacks one zombie inside its 5 x 5 range."
                ) + "\n" +
                Bullet(
                    "Idle Amp-nions and Doomtronions within range connect " +
                    "to the attacker, each adding 150 damage to the shot."
                ) + "\n" +
                Bullet(
                    "The shot arcs from its first target to a maximum of 3 " +
                    "nearby zombies, dealing half damage to each."
                ) + "\n" +
                Bullet(
                    "Every struck zombie becomes Irradiated for 10 seconds."
                ) + "\n" +
                Bullet(
                    "Every struck zombie has a 25% chance to trigger a " +
                    "Doom-shroom explosion at its position."
                ) + "\n" +
                Bullet(
                    "The triggered explosion deals 1800 damage and does " +
                    "not leave a crater."
                ),
                LoreWithRecipe(
                    "Doomtronion says the strange glow is perfectly normal " +
                    "and asks everyone to stop measuring it. The Geiger " +
                    "counter has declined to comment because it has been " +
                    "screaming continuously since breakfast.",
                    "Amp-nion > Doom-shroom"
                )
            );

        public static readonly AlmanacEntry LichenPea =
            new AlmanacEntry(
                "Lichen-pea",
                Brown(
                    "Lichen-pea fires freezing peas while turning the area " +
                    "around each struck zombie into a hazard for nearby " +
                    "plants."
                ) + "\n\n" +
                Stat("Damage", "20 / 1.5s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Its peas inflict Cold, slowing every zombie they hit."
                ) + "\n" +
                Bullet(
                    "Every zombie hit has a 25% chance to freeze between " +
                    "2 and 4 random freezable plants in the surrounding " +
                    "3x3 area."
                ) + "\n" +
                Bullet(
                    "If fewer than the chosen number of valid plants are " +
                    "nearby, every valid target in range is frozen."
                ),
                LoreWithRecipe(
                    "Lichen-pea insists the white coat is practical winter " +
                    "camouflage, not a fashion statement. He also insists " +
                    "that freezing his own teammates builds character. His " +
                    "teammates are currently drafting a strongly worded " +
                    "response.",
                    "Hoarfrost Lichen > Peashooter"
                )
            );

        public static readonly AlmanacEntry LogicBlover =
            new AlmanacEntry(
                "Logic Blover",
                Brown(
                    "Logic Blover considers every outcome before choosing " +
                    "one at random. Somehow, this makes perfect sense to him."
                ) + "\n\n" +
                Brown("Usage Condition: ") + Red("Harvest Mode") + "\n" +
                Stat("Toughness", "300") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Blows zombies backwards while remaining on the lawn."
                ) + "\n" +
                Bullet(
                    "Each zombie independently receives Ember, Cold, " +
                    "Butter or Poison, matching the four coloured petals."
                ) + "\n" +
                Bullet(
                    "While Logic Blover remains on the lawn, using Blover, " +
                    "Lucky Blover or Mimic Rye increases its Gift Box luck " +
                    "by 5%, 15% or 16% respectively."
                ) + "\n" +
                Bullet(
                    "Each Gift Box rolls this accumulated chance to receive " +
                    "the lucky result."
                ) + "\n" +
                Bullet(
                    "Using a Gold Bean on Logic Blover spends 10,000 money " +
                    "to make it blow again."
                ),
                Brown(
                    "A Harvest-exclusive Red Card with no fusion recipe. " +
                    "Logic Blover keeps careful track of every favourable " +
                    "outcome, then insists it was all perfectly logical."
                )
            );

        public static readonly AlmanacEntry SolarSharpshooter =
            new AlmanacEntry(
                "Solar Sharpshooter",
                Brown(
                    "Solar Sharpshooter converts every successful piercing " +
                    "hit into usable sunlight."
                ) + "\n\n" +
                Stat("Damage", "30 / 1.5s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet("Fires a projectile that pierces multiple zombies.") + "\n" +
                Bullet(
                    "Each zombie actually hit by the projectile immediately " +
                    "grants 25 sun."
                ) + "\n" +
                Bullet(
                    "A single shot grants sun once per pierced zombie."
                ),
                LoreWithRecipe(
                    "Solar Sharpshooter never misses an opportunity to make " +
                    "hay while the sun shines. The zombies object to being " +
                    "classified as an opportunity.",
                    "Spruce Sharpshooter > Sunflower"
                )
            );

        public static readonly AlmanacEntry SeaBallista =
            new AlmanacEntry(
                "Sea Ballista",
                Brown(
                    "Sea Ballista uses the water to hold its ground while " +
                    "its explosive bolts keep nearby zombies away."
                ) + "\n\n" +
                Stat("Damage", "80 / 3s") + "\n" +
                Stat("Range", "3.5 tiles") + "\n" +
                Brown("Special:") + "\n" +
                Bullet("Aquatic plant that occupies two adjacent tiles.") + "\n" +
                Bullet(
                    "Bolts pierce up to 2 times and explode 0.3s after " +
                    "hitting a zombie."
                ) + "\n" +
                Bullet(
                    "Each delayed explosion knocks its struck zombie back."
                ),
                LoreWithRecipe(
                    "Sea Ballista claims the tide pulls every bolt back for " +
                    "reuse. Nobody has had the courage to ask why the bolts " +
                    "still explode.",
                    "Spruce Ballista > Sea-shroom"
                )
            );

        public static readonly AlmanacEntry Pineshooter =
            new AlmanacEntry(
                "Pineshooter",
                Brown(
                    "Pineshooter launches entire pine trees to drive " +
                    "zombies back into their own crowd."
                ) + "\n\n" +
                Stat("Damage", "40 / 1.5s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Each pine knocks the first zombie it hits backward."
                ) + "\n" +
                Bullet(
                    "If the knocked zombie crashes into another zombie, " +
                    "both take 40 additional damage and are briefly stunned."
                ) + "\n" +
                Bullet("Immune to freeze and glaciation."),
                LoreWithRecipe(
                    "Pineshooter was told that throwing the whole tree was " +
                    "wasteful. He points out that nobody has volunteered to " +
                    "retrieve one from the zombies.",
                    "Peashooter > Spruce Sharpshooter"
                )
            );

        public static readonly AlmanacEntry Icytronion =
            new AlmanacEntry(
                "Icytronion",
                Brown(
                    "Icytronion conducts freezing electricity through " +
                    "nearby Amp-nions and clustered zombies."
                ) + "\n\n" +
                Stat("Damage", "100 / 1.5s") + "\n" +
                Stat("Attack Range", "5 x 5") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Idle Amp-nions and Icytronions within range channel " +
                    "power to the attacker, each adding 150 damage."
                ) + "\n" +
                Bullet(
                    "Each shot can arc to up to 3 nearby zombies for half " +
                    "damage."
                ) + "\n" +
                Bullet(
                    "Every struck zombie is slowed for 10 seconds and has " +
                    "a 25% chance to be frozen."
                ) + "\n" +
                Bullet("Immune to freeze and glaciation."),
                LoreWithRecipe(
                    "Icytronion insists that lightning never feels cold. " +
                    "The frozen zombies are currently unable to disagree.",
                    "Amp-nion > Ice-shroom"
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
                "Chrono Disc selects a portal pea."
            ) + "\n" +
            Bullet(
                "Jack-in-the-box selects a Zomppelin bomb projectile, while " +
                "Giga Mecha Fragments select a Kirov Flagship bomb. Both " +
                "explode in an area on impact; the Flagship shot deals 100 " +
                "contact damage and 60 splash damage before the metal bonus."
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
