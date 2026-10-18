namespace PlantsPlus.Core
{
    internal static class ChangelogContent
    {
        internal static string ButtonLabel => LocalizationBridge.Current switch
        {
            PlantsPlusLanguage.French => "PvZ Fusion+ Nouveautés",
            PlantsPlusLanguage.English => "PvZ Fusion+ Updates",
            _ => "PvZ Fusion+ 更新"
        };
        private const string Header =
            "<align=center><size=135%><b>PvZ Fusion+ · 1.3.0</b></size></align>\n" +
            "<align=center>PvZ Fusion 4.0.5.2</align>\n\n";

        internal static string Text => Header + (LocalizationBridge.Current switch
        {
            PlantsPlusLanguage.French =>
                "<b>1. Almanach et traduction</b>\n" +
                "- Un cœur sur chaque fiche permet de garder tes plantes en favoris : gris si elle n'y est pas, rouge si elle y est. Retrouve-les dans la catégorie Favoris, même après avoir quitté le jeu.\n" +
                "- Noms français ajustés et majuscules corrigées.\n" +
                "- Flavor texts développés : de petites scènes, du caractère et des tons différents selon les plantes.\n" +
                "- Les 42 fiches du mod ont été reprises : descriptions, effets, anecdotes et recettes.\n" +
                "- Les fiches françaises sont maintenant traduites en entier, y compris Ortie de César et Four Solaire.\n" +
                "- Les noms d'ingrédients suivent la traduction du jeu. Champi-lob, Champi-bond, Citrouille Lotus, Bamb-noix et Pisto-aimant gardent leurs noms.\n" +
                "- La catégorie Plantes boréales regroupe Orchidée Boréale et ses fusions.\n" +
                "- Le mod suit la langue choisie dans le mod de traduction. Sans lui, les textes sont en chinois.\n\n" +
                "<b>2. Nouvelles fusions depuis la mise à jour 4.0</b>\n" +
                "- Champi-solstice : Champi-bond + Tournesol. Donne un soleil de 25 par zombie repoussé.\n" +
                "- Bamboom : Bam'bou + Cerise Explosive. Conserve les attaques de Bam'bou, récupère 500 PV avec les explosions de Cerise Explosive proches et explose à sa mort pour 1800 dégâts.\n" +
                "- Champi-secousse : trois Champi-lobs sur une case + Champi-bond. Projette les zombies, puis lance trois spores.\n" +
                "- Champi-éclaire : Amp-gnion + Champi-bond. Les zombies projetés électrocutent un autre zombie proche.\n" +
                "- Super Champi-sabre d'Eau : alternative avancée de mêlée qui partage ses charges de glace avec les Épicéas Tireurs d'Exception. Conversion : Épicéa Tireur d'Élite <-> Champi-sabre.\n" +
                "- Champi-pulse : Champi-bond + Orchidée Boréale. Renforce les plantes nocturnes proches après un bond, avec une chance de libérer une vague boréale.\n\n" +
                "<b>3. Sélection des plantes</b>\n" +
                "- Plantes du Toit nocturne : sélection rétablie en Aventure.\n" +
                "- Survie : ajout du Toit nocturne en normal, difficile et infini, après Toit.\n" +
                "- Toit nocturne placé après le toit.\n" +
                "- Snowfield et Memory Snow placés sur la deuxième page.\n\n" +
                "<b>4. Correctifs</b>\n" +
                "- Champi-bond et ses fusions peuvent se poser sans pot sur le Toit et le Toit nocturne.\n" +
                "- Champi-secousse : helpers de fusion exclus de l'interop pour éviter le crash CLR au retrait des ingrédients. À confirmer en jeu.\n" +
                "- Toit nocturne : réparation des pistes de glace manquantes qui bloquaient la plantation. La musique suit maintenant la pause du jeu.\n" +
                "- Données des fiches d'almanach restaurées.\n" +
                "- Le bouton PvZ Fusion+ ouvre les notes du mod, pas celles du jeu.\n" +
                "- Super Champi-sabre d'Eau : charges du Four Infirmier, première entaille et blocage des attaques dans l'almanach. Ces derniers correctifs restent à vérifier en jeu.\n\n" +
                "<b>Pour les prochaines mises à jour</b>\n" +
                "L'Aventure aura un grand menu de niveaux à parcourir à la souris, avec plus de niveaux par monde et des niveaux bonus pour les plantes avancées. Des boutons permettront de retrouver le dernier niveau débloqué ou un niveau non terminé. Toutes les plantes du monde seront débloquées dans celui-ci, mais pas forcément une par niveau. Ce rework n'est pas encore ajouté.",
            PlantsPlusLanguage.English =>
                "<b>1. Almanac and translation</b>\n" +
                "- Click the heart on a plant's page to add or remove it from Favorites. Grey means not saved; red means saved. Your choices are kept between sessions.\n" +
                "- Updated French names and capitalization.\n" +
                "- Expanded flavor texts into small scenes, with different voices and moods for each plant.\n" +
                "- Reworked all 42 mod entries: descriptions, effects, stories and recipes.\n" +
                "- Completed the French entries, including Ceasarweed and Solar Firnace.\n" +
                "- Ingredient names now follow the game's translation.\n" +
                "- Boreal Plants groups Boreal Orchid and its fusions.\n" +
                "- Text follows the translator's selected language. Without the translator, Chinese is used.\n\n" +
                "<b>2. New fusions since the 4.0 update</b>\n" +
                "- Solstice-shroom: Bounce-shroom + Sunflower. Drops 25 Sun per zombie bounced.\n" +
                "- Bamboom: Bamblock + Cherry Bomb. Keeps Bamblock's contact attacks, heals 500 HP from nearby Cherry Bomb blasts and explodes for 1800 damage when defeated.\n" +
                "- Bumping-shroom: three Lob-shrooms on one tile + Bounce-shroom. Bounces zombies, then throws three spores.\n" +
                "- Spark-shroom: Amp-nion + Bounce-shroom. Bounced zombies shock another nearby zombie.\n" +
                "- Superaqua-shroom: a melee Advanced alternative that shares ice charges with Super Sharpshooters. Conversion: Spruce Sharpshooter <-> Sword-shroom.\n" +
                "- Pulse-shroom: Bounce-shroom + Boreal Orchid. Bouncing boosts nearby night plants and may release a Boreal Wave.\n\n" +
                "<b>3. Plant selection</b>\n" +
                "- Restored Night Roof starter selection in Adventure.\n" +
                "- Survival: Night Roof Normal, Hard and Endless added after Roof.\n" +
                "- Night Roof follows Roof.\n" +
                "- Snowfield and Memory Snow are on the second page.\n\n" +
                "<b>4. Fixes</b>\n" +
                "- Bounce-shroom and its fusions can be planted without pots on Roof and Night Roof.\n" +
                "- Bumping-shroom: keep fusion helpers out of native interop to address the CLR crash when removing ingredients. In-game confirmation pending.\n" +
                "- Night Roof: restored missing ice-road entries that blocked planting. Custom music now pauses with the game.\n" +
                "- Restored custom Almanac data.\n" +
                "- The PvZ Fusion+ button opens the mod's notes instead of the game's notes.\n" +
                "- Superaqua-shroom: Firnurse charges, opening slash and no attacks in Almanac previews. These latest fixes still need in-game confirmation.\n\n" +
                "<b>For future updates</b>\n" +
                "Adventure will get a large level menu you can pan with the mouse, more levels per world and optional Advanced bonus levels. Shortcuts will take you to your latest unlocked level or an unfinished level. Each world's plants will unlock within that world, but not necessarily one per level. This rework hasn't been added yet.",
            _ =>
                "<b>1. 图鉴与翻译</b>\n" +
                "- 点击植物图鉴上的爱心即可收藏或取消收藏。灰色表示未收藏，红色表示已收藏。收藏分类包含已保存的植物，退出游戏后仍会保留。\n" +
                "- 调整法文植物名及大小写。\n" +
                "- 扩写植物趣闻，通过小故事表现各自的性格，保留幽默、神秘等不同风格。\n" +
                "- 重写模组的40份植物图鉴：简介、特点、趣闻和配方。\n" +
                "- 补全法文图鉴，包括凯撒草和太阳熔炉。\n" +
                "- 配方中的植物名称与游戏翻译保持一致。\n" +
                "- 极光植物分类包含极光兰及其融合植物。\n" +
                "- 安装翻译模组后，文本跟随所选语言；未安装时使用中文。\n\n" +
                "<b>2. 4.0更新以来新增的融合</b>\n" +
                "- 至日菇：弹弹菇 + 向日葵。每弹飞一个僵尸掉落25阳光。\n" +
                "- 爆竹：竹子 + 樱桃炸弹。保留竹子的碰撞攻击，附近樱桃炸弹爆炸恢复500耐久，死亡时爆炸造成1800伤害。\n" +
                "- 弹射孢菇：同格三株抛孢菇 + 弹弹菇。弹飞僵尸后投出三颗孢子。\n" +
                "- 电弹菇：闪电洋葱 + 弹弹菇。被弹飞的僵尸会电击附近另一只僵尸。\n" +
                "- 超级水刃菇：近战进阶替代植物，与超级云杉共享寒冰充能。转换：云杉弓手 <-> 刃刃菇。\n" +
                "- 脉冲菇：弹弹菇 + 极光兰。弹跳强化附近夜间植物，并有概率释放极地之波。\n\n" +
                "<b>3. 选卡界面</b>\n" +
                "- 修复冒险模式中夜间屋顶基础植物的选卡状态。\n" +
                "- 生存模式新增夜间屋顶普通、困难和无尽关卡，位于屋顶之后。\n" +
                "- 夜间屋顶排在屋顶之后。\n" +
                "- 雪原和记忆雪原移动到第二页。\n\n" +
                "<b>4. 修复</b>\n" +
                "- 弹跳菇及其融合可直接种植在白天及夜间屋顶上，无需花盆。\n" +
                "- 震荡菇：将融合辅助方法排除在原生互操作之外，处理移除材料时的CLR崩溃，仍需游戏内确认。\n" +
                "- 夜间屋顶：补全缺失的冰道数据，避免种植时报错。模组音乐现在会随游戏暂停，并从原位置继续播放。\n" +
                "- 恢复自定义植物的图鉴数据。\n" +
                "- PvZ Fusion+ 更新按钮显示模组的说明，不再显示游戏的说明。\n" +
                "- 超级水刃菇：超级小松炉充能、出场斩击，以及禁止图鉴预览攻击。这些最新修复仍需游戏内确认。\n\n" +
                "<b>后续更新计划</b>\n" +
                "冒险模式将改用可用鼠标自由浏览的大型选关菜单。每个世界会有更多关卡，进阶植物放在可选奖励关卡中，并提供前往最新解锁关卡或未完成关卡的快捷按钮。一个世界的植物都会在该世界解锁，但并非每关解锁一株。这个改版尚未加入。"
        });
    }
}
