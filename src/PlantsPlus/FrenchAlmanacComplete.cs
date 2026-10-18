using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlantsPlus.Core
{
    internal static class FrenchAlmanacContent
    {
        private sealed record Copy(string Name, string Summary, string Details,
            string Lore, string Recipe = "", string Conversion = "", string Modifiers = "");

        private static readonly Dictionary<string, Copy> Entries = new()
        {
            ["Solstice-shroom"] = new("Champi-solstice", "Champi-solstice projette les zombies et donne 25 soleils pour chacun d'eux.",
                "Repousse les zombies terrestres proches de sa ligne d'une case et demie, puis se repose 5 s.|Fait tomber un soleil de 25 par zombie réellement repoussé. Trois zombies rapportent donc 75 soleils.|Ne produit pas de soleil passivement. Ne peut pas être emporté par un Zombie à l'élastique.",
                "Champi-solstice a tout prévu pour le jour le plus long de l'année : petit-déjeuner au jardin, baignade, pique-nique, puis promenade avant le coucher du soleil. Chaque année, il en reste au petit-déjeuner. Il aime beaucoup le petit-déjeuner.", "Champi-bond + Tournesol"),
            ["Bamboom"] = new("Bamboom", "Bamboom percute les zombies comme Bam'bou, se soigne avec les explosions de cerise proches et explose à sa mort.",
                "Conserve les attaques de contact de Bam'bou et leur consommation de PV.|Une explosion de Cerise Explosive dans les 3 x 3 cases autour de lui rend 500 PV, sans dépasser ses PV maximum.|À sa mort, explose pour 1800 dégâts. Le retirer à la pelle ou le fusionner ne déclenche pas l'explosion.",
                "Bamboom s'occupe du feu d'artifice. Il a trié les couleurs, vérifié les mèches et préparé un petit discours. Personne n'a demandé de discours. Il le répète quand même, tout bas, pendant que les autres comptent à rebours.", "Bam'bou + Cerise Explosive"),
            ["Ceasarweed"] = new("Ortie de César", "Ortie de César lance davantage de salades lorsque des projectiles manquent leur cible.",
                "Lance 1 salade, plus le nombre de salades ratées à la salve précédente, jusqu'à 10. Les autres projectiles lobés ratés ajoutent aussi une salade.|Chaque salade touchée immobilise le zombie dans du beurre pendant 4 s.|Les zombies ordinaires et les Zombonis l'ignorent. Les Gargantuas peuvent l'attaquer.",
                "Ortie de César a apporté le déjeuner pour tout le monde. Personne n'a demandé à être resservi.", Conversion: "Herbe à clous <-> Lance-melon"),
            ["Solar Firnace"] = new("Four Solaire", "Four Solaire produit du soleil jusqu'à la fin de son compteur. Cliquez dessus pour prolonger sa durée.",
                "Un clic coûte 100 soleils et ajoute 45 s. Chaque prolongation coûte 50 soleils de plus.|Protège la plante sous lui du gel causé par la neige.|Reprend sa forme de Four Polaire quand le compteur atteint zéro.",
                "Son réveil sonne chaque matin. Il le paie pour revenir plus tard.", "Four Polaire + Tournesol"),
            ["Lotus Pumpkin"] = new("Citrouille Lotus", "Citrouille Lotus protège une plante et la soigne lorsqu'elle a assez de charges.",
                "Gagne des charges avec le temps et lorsqu'elle perd des PV.|À 5 charges, soigne la plante à l'intérieur de 600 PV maximum. Ne se soigne pas elle-même.",
                "Citrouille Lotus a toujours un pansement dans sa poche. Une poche ? Oui, sous la feuille. Non, pas celle-là.", "Citrouille + Lotus de Neige"),
            ["Bambnut"] = new("Bamb-noix", "Bamb-noix bloque les zombies et riposte lorsqu'ils le touchent.",
                "Plante défensive.|Contre-attaque les zombies qui le heurtent ou le mordent.",
                "Bamb-noix suit des cours de bambou depuis six mois. Il maîtrise déjà la leçon numéro un : ne pas plier.", "Bambou + Noix"),
            ["Iceberg-shroom"] = new("Champi-iceberg", "Champi-iceberg gèle les zombies deux fois plus longtemps que Champi-glace.",
                "Gèle les zombies pendant 8 s.|Les zombies immunisés au gel sont ralentis de 50 % pendant 8 s.",
                "Son chapeau ne rentre plus dans le réfrigérateur. Depuis, c'est le réfrigérateur qui vient chez lui.", "Champi-glace + Champi-glace"),
            ["Witchfire Pumpkin"] = new("Citrouille Sorcière", "Citrouille Sorcière renforce sa plante, puis la sacrifie pour déclencher deux explosions.",
                "Avant l'allumage : dégâts de la plante terrestre à l'intérieur x2, soins de 50 PV/s et dégâts de zone sur 1 x 1 égaux au tiers de ses dégâts.|Une fois allumée, sacrifie la plante et déclenche Champi-mort et Jalapeño : 1800 + 10 x énergie chacun.|Le sacrifice rapporte de l'énergie selon le coût de la plante. Les fusions de Champi-mort et Jalapeño rendent leur carte et rapportent 3600 énergie.|Gagne aussi de l'énergie grâce aux zombies enflammés ou irrités tués et aux explosions de Champi-mort proches.|À sa mort, déclenche les deux explosions sans sacrifier de plante.",
                "Sa recette de soupe est un secret de famille. On connaît tout de même un ingrédient : il faut une grande marmite.", "Citrouille Pyro + Citrouille Maudite",
                Modifiers: "Grenades : lance une bombe Champi-mort sur chaque ligne à la place de l'explosion locale.|Radiation : 100 dégâts toutes les 0,2 s autour d'elle. Les éliminations augmentent les dégâts et la portée."),
            ["Nutty Sharpshooter"] = new("Épicéa Blindé", "Épicéa Blindé tire des aiguilles qui transpercent et repoussent les zombies.",
                "Les aiguilles traversent une cible et repoussent les zombies touchés d'une demi-case.|N'ignore plus les armures de type 2.|Immunisé au gel et à la glaciation.",
                "Il a demandé une coque avec une fenêtre. Noix lui a proposé deux trous pour les yeux. Marché conclu.", "Épicéa Tireur d'Élite + Noix"),
            ["Inferno Torchflower"] = new("Tournesol Infernal", "Tournesol Infernal stocke le soleil une fois allumé. Cliquez dessus pour récupérer sa réserve.",
                "Avant l'allumage, produit du soleil normalement.|Chaque ligne de feu de Jalapeño reçue ajoute 50 énergie. Une fois allumé, stocke le soleil produit ou extrait.|Chaque tranche de 50 énergie ajoute 0,5 au multiplicateur. Un clic verse la réserve et remet l'énergie à zéro.",
                "« Je garde ça pour les jours de pluie. » Il fait beau depuis trois semaines et personne n'arrive à lui faire ouvrir sa tirelire.", Conversion: "Tournesol <-> Torche"),
            ["Pumpkin Podbomber"] = new("Citrouille Grenadière", "Citrouille Grenadière copie les tirs de la plante qu'elle protège.",
                "Copie le nombre de projectiles, les dégâts et les cibles d'un tireur de la famille Pois.|Tire à la moitié de sa cadence. Une salve sur quatre contient des pois explosifs.|Avec Pisto-cerise à l'intérieur, tous ses tirs sont explosifs, toutes les 3 s.|Retirer la coque à la pelle rend une carte de Pisto-grenade.",
                "Elle compte les salves avec ses doigts. Au quatrième tir, elle les met dans ses oreilles.", Conversion: "Citrouille <-> Pelle"),
            ["Cherry StarBomber"] = new("Carambombe", "Carambombe tire deux salves de cinq étoiles explosives.",
                "Lance les mêmes étoiles que les projectiles de Sulfateuse à Grenades traversant le bois étoilé infernal.",
                "Carambombe n'aime pas les horoscopes. Le sien annonce toujours une pluie d'étoiles, et elle doit tout nettoyer après.", Conversion: "Carambole <-> Pisto-pois",
                Modifiers: "Constellation guidée : les étoiles suivent les zombies.|Élan stellaire : après 3 cases, les étoiles grossissent et leurs dégâts doublent.|Renfort roulant : chaque Jugger-noix crée une Noix roulante sur sa ligne toutes les 60 s."),
            ["Not-a-pea"] = new("Pisto-scie", "Pisto-scie tire des pois dentés qui traversent les zombies et peuvent rester accrochés.",
                "Les pois traversent tous les zombies.|25 % de chances par cible d'y accrocher une scie.|La scie inflige 10 dégâts toutes les 1,5 s pendant 10 s.",
                "Le dentiste de Pisto-scie a pris sa retraite après le premier rendez-vous. Il avait pourtant acheté une nouvelle fraise.", "Chrysanfer + Pisto-pois"),
            ["Not-a-storm Commando"] = new("Commando Scieur", "Commando Scieur remplace les pois de Pois Commando par des pois dentés.",
                "Conserve la cadence, les angles de tir et le nombre de projectiles de Pois Commando.|Chaque projectile traverse les zombies et a 25 % de chances d'accrocher une scie à chaque cible.|Les scies infligent 10 dégâts toutes les 1,5 s pendant 10 s.",
                "Il a voulu se reconvertir dans la menuiserie. Premier jour : six canons braqués sur une étagère. Il est retourné au front.", "Chrysanfer + Pois Commando"),
            ["Frost Furflower"] = new("Tournesol Givré", "Tournesol Givré produit du soleil et en donne davantage lorsqu'il reçoit une boule de neige.",
                "Produit du soleil au rythme d'un Tournesol.|Une boule de neige reçue rapporte immédiatement 100 soleils.|Gèle alors les autres plantes sensibles au gel dans une zone de 3 x 3.",
                "Elle adore les batailles de boules de neige. Ses voisins préfèrent jouer aux cartes. De loin.", "Lichen Givré + Tournesol"),
            ["Doomtronion"] = new("Amp-mort", "Amp-mort frappe les zombies avec des éclairs irradiants qui peuvent provoquer une explosion.",
                "Chaque Amp-gnion ou Amp-mort inactif à portée ajoute 150 dégâts.|L'éclair se propage à 3 zombies maximum, pour la moitié des dégâts.|Irradie les cibles pendant 10 s.|Chaque cible a 25 % de chances de déclencher une explosion de Champi-mort à 1800 dégâts, sans cratère.",
                "Amp-mort dort avec une veilleuse. Elle n'est pas branchée.", "Amp-gnion + Champi-mort"),
            ["Lichen-pea"] = new("Pisto-lichen", "Pisto-lichen refroidit les zombies, mais risque de geler les plantes près de sa cible.",
                "Applique Froid à chaque cible touchée.|25 % de chances de geler 2 à 4 plantes sensibles au gel dans les 3 x 3 cases autour de la cible.",
                "« Gardez vos distances. » C'est écrit sur son pot, son paillasson et sa carte de visite.", "Lichen Givré + Pisto-pois"),
            ["Logic Blover"] = new("Troufle Logique", "Troufle Logique repousse les zombies et leur applique un effet aléatoire.",
                "Chaque zombie reçoit Braise, Froid, Beurre ou Poison, choisi séparément.|Tant qu'il est présent, utiliser Troufle, Troufle Chanceux ou Seigle imitateur ajoute respectivement 5 %, 15 % ou 16 % de chance aux cadeaux.|Chaque cadeau utilise la chance accumulée.|Un Grain d'or lui permet de souffler à nouveau contre 10000 pièces.",
                "Il a passé la nuit à calculer quel côté d'une pièce tomberait en premier. Au matin, il a perdu la pièce."),
            ["Solar Sharpshooter"] = new("Epicéa Tireur Solaire", "Epicéa Tireur Solaire produit du soleil pour chaque zombie touché.",
                "Les tirs traversent plusieurs zombies.|Chaque cible touchée rapporte 25 soleils.",
                "Epicéa Tireur Solaire tient les comptes de la pelouse. Les zombies sont dans la colonne des recettes.", "Épicéa Tireur d'Élite + Tournesol"),
            ["Sea Ballista"] = new("Baliste Marine", "Baliste Marine occupe deux cases d'eau et tire des carreaux qui explosent après l'impact.",
                "Plante aquatique de deux cases.|Les carreaux traversent deux cibles et explosent 0,3 s après avoir touché un zombie.|L'explosion repousse la cible.",
                "Elle a appris à nager avant de tirer. Pour récupérer les carreaux, il faut savoir faire les deux.", "Baliste Épicéa + Champi-mer"),
            ["Sea Sharpshooter"] = new("Epicéa Tireur des Mers", "Epicéa Tireur des Mers grandit en trois étapes. Ses tirs gagnent en puissance à chaque étape.",
                "Plante aquatique.|Grandit toutes les 30 s : dégâts de 20, puis 40, puis 60.",
                "Petit, il voulait être un grand arbre. Il n'avait pas précisé à quelle profondeur.", "Épicéa Tireur d'Élite + Champi-mer"),
            ["Three-Buckpeater"] = new("Triple Pisto-seau", "Triple Pisto-seau tire des pois de fer sur trois lignes.",
                "Attaque sa ligne et les deux lignes voisines.|Les pois repoussent les zombies et détruisent immédiatement les armures de type 2.",
                "Il a commandé un casque pour chaque tête. Le magasin lui a livré trois seaux. Il a gardé le reçu.", "Triple Pisto-pois + Seau"),
            ["Pineshooter"] = new("Pisto-sapin", "Pisto-sapin lance des arbres qui repoussent les zombies les uns contre les autres.",
                "Chaque arbre repousse le premier zombie touché.|Une collision entre deux zombies leur inflige 40 dégâts supplémentaires et les étourdit brièvement.|Immunisé au gel et à la glaciation.",
                "Il achète son sapin de Noël en gros. Le livreur refuse désormais de venir en décembre.", "Pisto-pois + Épicéa Tireur d'Élite"),
            ["Icytronion"] = new("Amp-glace", "Amp-glace lance des éclairs qui ralentissent les zombies et peuvent les geler.",
                "Chaque Amp-gnion ou Amp-glace inactif à portée ajoute 150 dégâts.|L'éclair se propage à 3 zombies maximum pour la moitié des dégâts.|Ralentit les cibles pendant 10 s, avec 25 % de chances de les geler.|Immunisé au gel et à la glaciation.",
                "Amp-glace garde les boissons fraîches aux pique-niques. Il faut juste penser à débrancher le grille-pain.", "Amp-gnion + Champi-glace"),
            ["Sakura Sharpshooter"] = new("Sakura Tireur d'Explosif", "Sakura Tireur d'Explosif tire des épines explosives et prolonge les explosions des Pisto-grenades proches.",
                "15 % de chances de provoquer une explosion de Cerise Explosive à l'impact.|Une cible touchée par un Pisto-grenade proche a 50 % de chances d'exploser à son tour pour les mêmes dégâts.|Une réaction réussie fait aussi exploser un zombie aléatoire dans les 3 x 3 cases voisines, pour la moitié des dégâts.",
                "La fête des fleurs a été déplacée à l'autre bout du jardin. Sakura Tireur d'Explosif a quand même reçu une invitation. Sans adresse.", "Cerise Explosive + Épicéa Tireur d'Élite"),
            ["Boreal Orchid"] = new("Orchidée Boréale", "Orchidée Boréale envoie une vague qui renforce les plantes nocturnes de sa ligne.",
                "La vague renforce les plantes pendant 10 s.|Dégâts, robustesse, cadence de tir, production et vitesse de déplacement des plantes nocturnes de la ligne +35 %.|Planterne reçoit aussi le bonus et produit des points boréaux au lieu des points Lumos.",
                "Orchidée Boréale fait pousser ses fleurs la nuit. Le jour, elle les recompte. Il en manque toujours une : elle la porte sur la tête."),
            ["Bounce-shroom"] = new("Champi-bond", "Champi-bond renvoie les zombies qui lui marchent dessus une case et demie en arrière.",
                "Peut projeter plusieurs zombies de sa ligne à la fois.|Se repose 5 s après un bond.|Ne peut pas être emporté par un Zombie à l'élastique.",
                "Champi-bond voulait un lit à ressorts. Il s'est contenté des ressorts."),
            ["Superaqua-shroom"] = new("Super Champi-sabre d'Eau", "Super Champi-sabre d'Eau frappe les zombies proches et accélère les tirs des Super Épicéas.",
                "Sa première entaille donne 1 charge de glace à chaque Super Épicéa du terrain et contre les signaux verts.|Chaque charge qu'il possède augmente leur cadence de 4 %, jusqu'à 60 %.|Gagne des charges sous l'effet du gel et du Four Infirmier. Maximum : 15.|Ses coups chargés infligent davantage de dégâts et appliquent du froid aux zombies.",
                "Il a fait aiguiser son chapeau. Le chapelier lui a demandé de ne plus saluer les clients à l'intérieur.", Conversion: "Épicéa Tireur d'Élite <-> Champi-sabre"),
            ["Pulse-shroom"] = new("Champi-pulse", "Champi-pulse projette les zombies et renforce les plantes nocturnes autour de lui.",
                "Chaque bond renforce les plantes nocturnes dans une zone de 3 x 3 pendant 10 s.|25 % de chances par bond de libérer une vague boréale sur toute la ligne.|Projette les zombies d'une case et demie. Repos : 5 s.",
                "Son groupe répète le soir. Champi-bond fait la batterie, Orchidée Boréale s'occupe des lumières. Les zombies n'ont pas de billet.", "Champi-bond + Orchidée Boréale"),
            ["Bomber Drone"] = new("Drone Bombardeur", "Drone Bombardeur survole la pelouse et tire des projectiles de Pisto-cerise.",
                "Plante volante.|Ses cerises n'explosent pas à l'impact.|Adopte la cadence du tireur droit sous lui si celui-ci tire plus vite qu'une fois toutes les 1,5 s.",
                "Il voulait livrer les courses. Le premier client a demandé pourquoi les cerises arrivaient par le toit.", "Pisto-cerise + Troufle"),
            ["Frostbite Drone"] = new("Drone Givré", "Drone Givré survole la pelouse et tire des pois glacés.",
                "Plante volante.|Ses pois ralentissent les zombies.|Adopte la cadence du tireur droit sous lui si celui-ci tire plus vite qu'une fois toutes les 1,5 s.",
                "La climatisation est en panne depuis mardi. Drone Givré a été élu employé du mois mercredi.", "Pois Glacé + Troufle"),
            ["Ice-Lord Cactus"] = new("Cactus Rois du Givre", "Cactus Rois du Givre adapte ses tirs aux zombies terrestres ou aériens.",
                "Tire trois projectiles sur les zombies au sol et un projectile sur ceux en l'air.|Dégâts aux zombies aériens x4.|Gèle les zombies pendant 8 s. Les cibles immunisées sont ralenties de 50 % pendant 8 s.",
                "Sa couronne fond tous les étés. Il appelle ça les vacances de la monarchie.", "Cactus de Glace + Champi-iceberg"),
            ["Scovilia Pepper"] = new("Piment Scovilia", "Piment Scovilia brûle les zombies sur trois lignes.",
                "Brûle sa ligne et les deux lignes voisines.",
                "« Une pincée suffira. » La casserole, la cuisinière et la cuisine ont disparu. Le cuisinier a commandé une pizza.", "Jalapeño + Jalapeño"),
            ["Atomray-shroom"] = new("Champi-rayon", "Champi-rayon ralentit sa cible et peut déclencher l'effet de Champi-omnicide.",
                "Ralentit sa cible pendant 3 s.|25 % de chances par attaque de déclencher un effet de Champi-omnicide sur la cible. Déclenche aussi cet effet à sa mort.|Chaque Planterne Atomique proche augmente sa cadence de 25 %, jusqu'à une attaque toutes les 0,5 s.",
                "Il a offert une lampe à son voisin. Le voisin lit désormais son journal à travers le mur.", Conversion: "Planterne <-> Champi-fumée"),
            ["Cherry Cabbage"] = new("Chou Cerise", "Chou Cerise frappe plus fort lorsque les zombies s'approchent de la maison.",
                "Dégâts de 40 à 160 selon la proximité de la maison.|Probabilité d'explosion de 15 % à 60 %, selon la même distance.|L'explosion inflige en zone autant de dégâts que le projectile.",
                "Son panneau dit : « Ne pas marcher sur la pelouse. » Les zombies le lisent généralement trop tard.", "Cerise Explosive + Lance-chou"),
            ["Lob-shroom"] = new("Champi-lob", "Champi-lob lance ses spores en cloche sur les zombies.",
                "Lance des spores de Champi-gnon.|Peut être empilé sur une même case, comme Champi-gnon.",
                "Champi-lob a acheté un panier pour aller au marché. Il s'en sert tous les jours. Il n'est toujours pas allé au marché."),
            ["Bumping-shroom"] = new("Champi-secousse", "Champi-secousse projette les zombies, puis lance trois spores.",
                "Empilez trois Champi-lobs sur une case, puis ajoutez Champi-bond.|Repousse les zombies terrestres proches d'environ 1,5 case.|Après le bond, vise les trois zombies les plus proches de sa ligne.|Se repose 5 s. Immunisé aux attaques des Zombies à l'élastique et Catapultes.",
                "Trois Champi-lobs se partagent le panier. Celui du milieu a insisté pour avoir une fenêtre.", "3 Champi-lobs + Champi-bond"),
            ["Spark-shroom"] = new("Champi-éclaire", "Champi-éclaire fait passer le courant entre les zombies qu'il projette et leurs voisins.",
                "Repousse les zombies terrestres proches d'environ 1,5 case.|Chaque zombie projeté électrocute le zombie le plus proche à portée d'Amp-gnion, sans s'électrocuter lui-même.|Se repose 5 s. Ne peut pas être emporté par un Zombie à l'élastique.",
                "Son père lui avait dit de ne jamais toucher une prise. Depuis, il laisse les zombies le faire.", "Amp-gnion + Champi-bond"),
            ["Sauerkraut-pult"] = new("Lance-choucroute", "Lance-choucroute change de tir selon la distance entre sa cible et la maison.",
                "De 1 à 5 cases de la maison : 300 dégâts sur une cible et Enflammé.|De 6 à 10 cases : 100 dégâts de zone et Poison.",
                "Il conserve ses munitions dans des bocaux étiquetés. Le seul sans étiquette est celui qu'il a oublié depuis l'hiver dernier.", "Lance-chou puant + Piment d'Espelette"),
            ["Saw-my-pot"] = new("Pot-scie", "Pot-scie augmente les dégâts des fusions de Chrysanfer plantées dessus.",
                "Dégâts des fusions de Chrysanfer sur ce pot +50 %.",
                "Pot-scie n'a jamais eu besoin de tondre autour de chez lui. La pelouse s'arrête avant.", "Chrysanfer + Pot de Fleur"),
            ["Vortex Pot"] = new("Pot Vortex", "Pot Vortex transforme la plante posée dessus en sa fusion avec Champi-glace.",
                "Renforce les fusions de Champi-glace plantées dessus.|Si aucune fusion avec Champi-glace n'existe, gèle les zombies du terrain.|Rend leur forme normale aux plantes grillées, comme Pot Glacé.",
                "On lui a confié une fleur en plastique. Elle aussi a fini par grelotter.", Conversion: "Pelle <-> Pot de Fleur"),
            ["Magnet-o-pea"] = new("Pisto-aimant", "Pisto-aimant change de munitions lorsqu'on lui donne un objet métallique.",
                "Absorbe les objets posés directement sur lui, sans les attirer à distance. Garde ses munitions jusqu'au prochain objet.|Seau, casque de football et disque temporel donnent des pois de fer, de casque et de portail.|Les boîtes explosives et fragments de Méga-noix donnent des projectiles explosifs.|Dégâts x2 contre les zombies qui portent encore du métal.",
                "« Tu as fini avec ton seau ? » Pisto-aimant demande toujours la permission. Il n'attend pas toujours la réponse.", "Champi-aimant + Pisto-pois")
        };

        internal static string TranslateApprovedNames(string text)
        {
            foreach (var pair in Entries.OrderByDescending(pair => pair.Key.Length))
                text = text.Replace(pair.Key, pair.Value.Name);
            return text;
        }

        internal static bool TryCreate(string englishName, string? usage,
            string[]? stats, out AlmanacEntry entry)
        {
            if (!Entries.TryGetValue(englishName, out Copy? copy))
            { entry = default; return false; }
            var info = new StringBuilder(copy.Summary).Append("\n\n");
            if (!string.IsNullOrEmpty(usage))
                info.Append("<color=#4A2900>Condition : ").Append(usage switch
                {
                    "Odyssey Mode" => "Mode Odyssée",
                    "Odyssey Mode | Epic Odyssey" => "Mode Odyssée | Odyssée épique",
                    "Harvest Mode" => "Mode Récolte",
                    "Advanced Alt" => "Alternative avancée",
                    _ => usage
                }).Append("</color>\n");
            if (stats != null)
                for (int i = 0; i + 1 < stats.Length; i += 2)
                    info.Append("<color=black>").Append(StatLabel(stats[i]))
                        .Append(" : </color><color=#8B0000>")
                        .Append(StatValue(stats[i + 1])).Append("</color>\n");
            info.Append("<color=black>Spécial :</color>");
            foreach (string detail in copy.Details.Split('|'))
                info.Append("\n<color=black>-</color><color=#8B0000> ")
                    .Append(detail).Append("</color>");
            if (copy.Modifiers.Length > 0)
            {
                info.Append("\n\n<color=black>Modificateurs d'Odyssée :</color>");
                foreach (string modifier in copy.Modifiers.Split('|'))
                    info.Append("\n<color=#8B0000>- ").Append(modifier).Append("</color>");
            }
            var cost = new StringBuilder();
            if (copy.Recipe.Length > 0)
                cost.Append("\nFormule de fusion : <color=#8B0000>")
                    .Append(copy.Recipe).Append("</color>");
            if (copy.Conversion.Length > 0)
                cost.Append("\nConversion : <color=#8B0000>")
                    .Append(copy.Conversion).Append("</color>");
            entry = new AlmanacEntry(copy.Name, info.ToString()
                .Replace("Super Épicéas", "Épicéas Tireurs d'Exception")
                .Replace("Super Épicéa", "Épicéa Tireur d'Exception"),
                "<color=black>" + PlantFlavorText.Get(englishName,
                    PlantsPlusLanguage.French, copy.Lore) + "</color>", cost.ToString());
            return true;
        }

        private static string StatLabel(string label) => label switch
        {
            "Toughness" => "Robustesse", "Damage" => "Dégâts", "Lifetime" => "Durée",
            "Sacrifice Cooldown" => "Intervalle entre sacrifices", "Sun Output" => "Production de soleil",
            "Maximum Energy" => "Énergie maximale", "Maximum Multiplier" => "Multiplicateur maximal",
            "Attack Rate" => "Cadence", "Projectile Damage" => "Dégâts des projectiles",
            "Attached Damage" => "Dégâts des scies", "Snowball Bonus" => "Bonus par boule de neige",
            "Sun per Hit" => "Soleils par cible", "Range" => "Portée", "Growth Interval" => "Croissance",
            "Attack Range" => "Portée d'attaque", "Ground Attack" => "Attaque au sol",
            "Air Attack" => "Attaque aérienne", "Demise Chance" => "Effet omnicide",
            "Spore Damage" => "Dégâts des spores", "Rest" => "Repos", "Recharge" => "Recharge",
            "Lightning Damage" => "Dégâts électriques", "Focus Damage" => "Dégâts concentrés",
            "Splash Damage" => "Dégâts de zone", "Damage Boost" => "Bonus de dégâts",
            "Boreal Wave" => "Vague boréale", "Melee Damage" => "Dégâts de mêlée",
            "Ice Charges" => "Charges de glace", _ => label
        };

        private static string StatValue(string value) => value
            .Replace("Up to 15", "15 maximum").Replace("Jalapeno damage", "Dégâts de Jalapeño")
            .Replace("50% of copied shooter", "50 % du tireur copié").Replace("per lane", "par ligne")
            .Replace("per attack", "par attaque").Replace("each", "par projectile")
            .Replace("Every ", "Toutes les ").Replace("Medium", "Moyenne")
            .Replace("tiles", "cases").Replace("lanes", "lignes").Replace("Sun", "soleils")
            .Replace(" for ", " pendant ").Replace("1.5", "1,5").Replace("3.5", "3,5")
            .Replace("2.5", "2,5").Replace("×", "x").Replace("→", "->");
    }
}
