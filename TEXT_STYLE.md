# Textes de PvZ Fusion+

## Références examinées

- Fichiers locaux du mod de traduction 4.0.5.2 : English/French, Almanac/LawnStringsTranslate.json et DetailStringsTranslate.json.
- LawnStrings.json du projet extrait PVZFUSIONINNER4.0 pour les tournures et noms chinois. Ce projet est une référence de texte, pas une garantie sur les mécaniques de la version actuelle.
- Fiches comparées : Pisto-pois, Tournesol, Amp-gnion, Champi-sabre, Champi-sabre d'Eau, Four Infirmier et plusieurs fusions Odyssée.
- Les fichiers français de la traduction comportent encore des passages anglais et quelques coquilles. Ils servent de référence pour les noms et la structure, pas de modèle à copier sans relecture.

## Ligne éditoriale

- Une phrase d'ouverture explique ce que fait la plante. Les chiffres, conditions et exceptions viennent ensuite.
- Garder les effets, anecdotes et recettes dans leurs champs respectifs.
- Une anecdote peut raconter une petite mésaventure, une habitude ou un dialogue. Pas de formule obligatoire ni de blague ajoutée au milieu des règles.
- Éviter les suites « il affirme », « personne n'a osé vérifier », les métaphores abstraites et le vocabulaire de documentation technique.
- Ne pas parler de prefab, hook, fonction native, registre ou code dans les descriptions des plantes.
- Ne pas inventer de mécanisme pour améliorer une histoire. Une réécriture ne modifie pas les statistiques du jeu.
- Français : textes entiers, noms d'ingrédients cohérents avec la traduction installée, virgule décimale et unités lisibles. Conserver les noms validés par Cecil.
- Chinois : descriptions compactes, 韧性 pour la robustesse et 特点 pour les effets. Numéros simples quand les symboles décoratifs posent problème avec la police.
- Anglais : formulation directe et vocabulaire du jeu, sans explications de programmation.
- Les nouveaux noms français non validés restent des propositions éditoriales, pas des noms approuvés par Cecil.

## Ajustements validés le 7 octobre

- Epicéa Tireur Solaire ; Sakura Tireur d'Explosif ; Epicéa Tireur des Mers ; Ortie de César.
- Cactus Rois du Givre ; Drone Bombardeur ; Drone Givré ; Citrouille Grenadière.
- Majuscules validées : les mots reliés par un trait d'union restent en minuscules après le trait (Champi-iceberg, Lance-choucroute). Les mots distincts prennent leur majuscule (Orchidée Boréale, Champi-éclaire), sauf les petits mots de liaison comme de, des et d'. Bumping-shroom se nomme Champi-secousse.
- Les flavor texts doivent être plus courts et varier en ton : drôle, mystérieux, inquiétant, tendre, énergique, etc. Le texte de Cryolina Reaper fourni dans la traduction illustre une image forte suivie d'une réplique, pas un modèle à recopier pour toutes les plantes.
- PlantFlavorText rassemble les 40 textes dans les trois langues ; le contrôle de longueur est un garde-fou, pas une mesure de qualité.

## Correction après relecture

La limite de 160 caractères produisait surtout des slogans et des chutes isolées. Elle est abandonnée. Les textes doivent avoir le temps d'installer une scène, une voix ou une atmosphère, sans imposer le même nombre de phrases à toutes les plantes. Le plafond technique de 700 caractères évite seulement les erreurs de contenu démesurées ; il ne définit pas la longueur souhaitée.

## Nouvelle relecture de la traduction

Fiches relues en français et en anglais : Noix, Cerise Explosive, Champi-hypno, Nénuphar, Ortie, Torche, Champi-aimant, Four Polaire, Four Infirmier, Planterne Boréale, Ombrelle Boréale, Champi-sabre, Champi-sabre d'Eau et Épicéa Tireur d'Exception. Les fiches chinoises du Four Polaire, du Four Infirmier, des deux Champi-sabres et de l'Épicéa Tireur d'Exception ont aussi été comparées.

Le narrateur peut décrire une habitude, laisser parler la plante ou révéler un détail inattendu. Un hobby ou un défaut peut exister sans illustrer son attaque. Ne pas faire de chaque texte une catastrophe en quatre phrases, une leçon ou une chute menaçante. Les liens avec des plantes du jeu peuvent servir l'histoire : coiffeur de Champi-sabre d'Eau, caractère du coach Épicéa Tireur d'Exception. Ce sont des références de personnalité, pas de nouvelles règles de combat. Les histoires originales sont adaptées dans chaque langue, pas recopiées depuis la traduction.

## Contrôle des textes

tools/TextAudit charge uniquement les classes de texte, sans Unity ni lancement du jeu. Il vérifie les 40 fiches dans les trois langues, les champs vides, certains restes anglais et les balises de couleur.

Ce contrôle ne juge pas l'humour et ne prouve ni le rendu dans la police du jeu ni le comportement des plantes. La relecture et la validation visuelle restent nécessaires.
