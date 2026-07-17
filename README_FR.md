# Plants+

**Plants+** est un mod de contenu pour **Plants vs. Zombies Fusion 3.8**. La version **1.0.0** ajoute dix plantes personnalisées avec leurs propres mécaniques, fiches d'Almanach, recettes de fusion/conversion, prefabs et compatibilité Odyssey.

> Version interne MelonLoader : `1.0.0-release-ml.3`

## Prérequis

- Plants vs. Zombies Fusion **3.8**
- MelonLoader **0.7.3**
- Le port MelonLoader de **CustomizeLib 3.8** (`CustomizeLib.BepInEx.dll`)

## Plantes incluses

| ID | Plante | Type | Recette / création |
|---:|---|---|---|
| 6000 | Lotus Pumpkin | Basic Cross Fusion | Pumpkin + Snow Lotus |
| 6001 | Bambnut | Basic Cross Fusion | Bamblock + Wall-nut |
| 6002 | Magnet-o-pea | Basic Cross Fusion | Peashooter + Magnet-shroom |
| 6003 | Iceberg-shroom | Basic Double Fusion | Ice-shroom + Ice-shroom |
| 6004 | Witchfire Pumpkin | Weak Odyssey | Pyro Pumpkin + Doom Pumpkin |
| 6005 | Nutty Sharpshooter | Basic Cross Fusion | Spruce Sharpshooter + Wall-nut |
| 6006 | Inferno Torchflower | Advanced Alt | Infernowood + Sunflower |
| 6007 | Pumpkin Podbomber | Advanced Alt | Explode-o-shooter + Pumpkin |
| 6008 | Ceasarweed | Advanced Alt | Salad-pult + Spikeweed ; Melon-pult la reconvertit |
| 6009 | Solar Firnace | Cross Adventure / fusion spéciale | Firnace absorbe le Sunflower placé dessous |

Tous les IDs personnalisés utilisent la plage 6000 afin d'éviter la plage native déjà occupée dans PVZ Fusion 3.8.

## Installation

1. Installe MelonLoader 0.7.3 pour PVZ Fusion 3.8.
2. Place le port MelonLoader de `CustomizeLib.BepInEx.dll` dans le dossier `Mods` du jeu.
3. Supprime les anciennes copies de `PlantsPlus.dll`.
4. Place le `PlantsPlus.dll` de la release GitHub dans le dossier `Mods`.
5. Lance le jeu et vérifie la présence de `Plants+ 1.0.0-release-ml.3 loaded!` dans le log MelonLoader.

## Documentation

- [Mécaniques des plantes](docs/PLANTS_FR.md)
- [Compiler le projet](docs/BUILDING_FR.md)
- [Historique des versions](CHANGELOG.md)
- [README anglais](README.md)

## Compilation

Le projet cible `.NET 6` et référence les assemblies IL2CPP générées par MelonLoader. Les DLL du jeu et CustomizeLib ne sont **pas redistribuées dans ce dépôt**. Consulte [BUILDING_FR.md](docs/BUILDING_FR.md) pour les chemins et commandes nécessaires.

## Signaler un bug

Ouvre une issue avec le modèle de rapport de bug et joins le log MelonLoader complet. Indique la plante, la recette et les étapes exactes qui provoquent le problème.

## Crédits

- Créateur du mod et concepts des plantes : **Auro**
- Crédit du sprite d'Iceberg-shroom : **@(sin of lust) red reel**
- Créé pour la communauté de modding PvZ Fusion avec le port MelonLoader de CustomizeLib

## Avertissement

Plants+ est un mod de fan non officiel. Il n'est ni affilié ni approuvé par Electronic Arts, PopCap Games ou les développeurs de PvZ Fusion. Plants vs. Zombies et les noms associés appartiennent à leurs propriétaires respectifs.
