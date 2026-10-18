# PvZ Fusion+ 1.3.0

[English](README.md)

De nouvelles plantes, des fusions et du contenu Toit nocturne pour **Plants vs. Zombies Fusion 4.0.5.2**.

![PvZ Fusion+](src/PlantsPlus/Resources/Branding/logo.png)

## Installation

Il te faut PvZ Fusion **4.0.5.2**, MelonLoader **0.7.3** et [CustomizeLib.MelonLoader **4.0.5.2-ml.5**](https://github.com/Hubtech280/CustomizeLib.Melonloader/releases/tag/v4.0.5.2-ml.5).

1. Ferme le jeu. Installe MelonLoader et lance une première fois le jeu si ce n'est pas déjà fait.
2. Installe la version de CustomizeLib indiquée ci-dessus, sans garder d'ancienne copie à côté.
3. Retire les anciens PlantsPlus.dll / PvZFusionPlus.dll du dossier Mods.
4. Télécharge le ZIP du mod dans [Releases](https://github.com/Hubtech280/PlantsPlus/releases). Extrais son dossier Mods à côté de l'exécutable, ou copie Mods/PvZFusionPlus.dll dans le dossier Mods du jeu.
5. Relance le jeu. Le log doit afficher **PvZ Fusion+ 1.3.0 loaded!**

Le ZIP du code source n'est pas le mod à installer. Le jeu, MelonLoader et CustomizeLib ne sont pas inclus.

## Dans cette version

- 42 fiches de plantes personnalisées, dont Bamboom et Champi-solstice.
- Du contenu Aventure au Toit nocturne et ses Survies normale, difficile et infinie.
- Une catégorie Plantes boréales et des favoris dans l'almanach : cœur gris si la plante n'est pas en favoris, rouge si elle y est.
- Des textes en français, anglais et chinois. Avec le mod de traduction facultatif, Fusion+ suit la langue choisie ; sans lui, le chinois est utilisé.
- Les logos selon la langue et une fenêtre de nouveautés propre au mod.

Champi-bond et ses fusions peuvent se poser **sans pot** sur le Toit classique et le Toit nocturne. Les autres plantes gardent leurs conditions de support. Champi-secousse demande **trois Champi-lobs sur une case + Champi-bond**. Champi-solstice se crée avec **Champi-bond + Tournesol** et donne **25 soleils par zombie effectivement repoussé**.

Les favoris sont enregistrés dans Mods/PlantsPlusData/favorites.txt ; la musique extraite est mise en cache dans Mods/PlantsPlusAudio. Ces dossiers personnels ne sont pas fournis. Garde ton fichier de favoris lors des mises à jour.

Les grands reworks de l'Aventure et de ses menus évoqués pour la suite **ne sont pas inclus**.

## Problème connu

Des erreurs ArgumentOutOfRangeException ont été observées dans les chemins de dégâts/perte d'armure des zombies, notamment Zombie.AnimLoseActive et Zombie.FirstArmorFall. Aucun problème visible n'a été signalé pendant ce test, mais leur cause et leur impact ne sont pas établis. Cette version ne prétend pas les corriger.

## Compiler

Il faut Windows, un SDK .NET capable de cibler .NET 6 et une installation du jeu avec ses MelonLoader/Il2CppAssemblies générées. Ces assemblies ne sont pas redistribuées.

Lance BUILD_RELEASE.bat avec le dossier du jeu en argument, ou tools/build_release.ps1 -GameDirectory "C:\Chemin\Vers\Game Files". Le script compile le mod, contrôle les textes, patches et favoris hors jeu, puis crée le ZIP et ses sommes de contrôle dans artifacts. Il ne lance pas le jeu et n'installe pas la DLL. Ces contrôles ne remplacent pas un test en jeu.

## Bugs et crédits

[Signale un bug](https://github.com/Hubtech280/PlantsPlus/issues) avec les versions du jeu, de la bibliothèque et du mod, les étapes de reproduction et le fichier MelonLoader/Latest.log. Vérifie les chemins personnels présents dans le log avant de le publier.

Création et concepts : **Auro**. Dessins des plantes/fusions : **Red Reel, Retrosphere, Aris et Roshade**. CustomizeLib original : **Infinite75** ; port MelonLoader : **Hubtech280**.

Voir les [notes de release](GITHUB_RELEASE_NOTES.md), le [changelog](CHANGELOG.md) et les [mentions](NOTICE.md). Mod de fans non officiel, sans affiliation à EA, PopCap ni aux développeurs de PvZ Fusion.
