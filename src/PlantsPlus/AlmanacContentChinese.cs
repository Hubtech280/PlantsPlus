using System;
using System.Collections.Generic;
using System.Text;

namespace PlantsPlus.Core
{
    // This build targets the original Chinese game. Keep the English source
    // in AlmanacContentRework for the later multi-language edition, but never
    // mix English descriptions into the Chinese in-game Almanac.
    internal static class ChineseAlmanacContent
    {
        private sealed class Copy
        {
            internal readonly string Name;
            internal readonly string Summary;
            internal readonly string Details;
            internal readonly string Lore;
            internal readonly string Recipe;
            internal readonly string Conversion;

            internal Copy(string name, string summary, string details,
                string lore, string recipe = "", string conversion = "")
            {
                Name = name;
                Summary = summary;
                Details = details;
                Lore = lore;
                Recipe = recipe;
                Conversion = conversion;
            }
        }

        private static readonly Dictionary<string, Copy> Entries = new()
        {
            ["Solstice-shroom"] = new("至日菇", "弹飞僵尸，每弹飞一个便掉落25阳光。",
                "将本行附近的地面僵尸弹回1.5格，随后休息5秒|每成功弹飞一个僵尸掉落一枚25阳光，三个僵尸共75阳光|不会自动生产阳光，也不会被蹦极僵尸抓走",
                "至日菇为一年里最长的白天列了长长的计划：在花园吃早饭，游泳，野餐，日落前再散个步。可每年都停在早饭这一项。他实在太喜欢吃早饭了。", "弹弹菇 + 向日葵"),
            ["Bamboom"] = new("爆竹", "像竹子一样撞击僵尸，受到附近樱桃炸弹爆炸影响时恢复耐久，死亡时爆炸。",
                "保留竹子的碰撞攻击及耐久消耗|周围3乘3范围内的樱桃炸弹爆炸恢复500耐久，不超过耐久上限|死亡时爆炸造成1800伤害；铲除或融合不会触发爆炸",
                "爆竹负责这次烟花。他分好了颜色，检查了引线，还准备了一段讲话。没人要求讲话。他还是小声练着，其他人已经开始倒数了。", "竹子 + 樱桃炸弹"),
            ["Ceasarweed"] = new("凯撒草", "投掷物落空后，凯撒草下次会投出更多沙拉。",
                "每轮投出1加上上一轮落空数量的沙拉，最多10份；其他投掷物落空也会增加1份|命中的沙拉使僵尸被黄油定身4秒|普通僵尸和雪橇车僵尸会忽略它，巨人僵尸仍可攻击",
                "凯撒草给大家带了午饭。没有人要求再来一份。", conversion: "地刺 <-> 西瓜投手"),
            ["Solar Firnace"] = new("太阳熔炉", "太阳熔炉在计时结束前生产阳光，点击可以延长使用时间。",
                "首次点击消耗100阳光，增加45秒；此后每次多花50阳光|保护下方植物免受雪的冻结|计时结束后变回原来的熔炉",
                "太阳熔炉每天早上都被闹钟叫醒。他会付钱请闹钟晚点再来。", "熔炉 + 向日葵"),
            ["Lotus Pumpkin"] = new("莲花南瓜", "将承受的伤害化作对内部植物的治疗。",
                "像南瓜一样保护同格植物|随时间及自身受到实际伤害积累能量|积满5层后消耗能量，为被保护植物恢复最多600点耐久",
                "她把每一道裂痕都留给自己，把每一份清凉都留给壳里的朋友。", "南瓜头 + 雪莲"),
            ["Bambnut"] = new("竹坚果", "能挡住僵尸，并在遭到接触或啃食时反击。",
                "坚果类防御植物|僵尸碰撞或啃食它时会遭到反击",
                "竹坚果的秘诀是纹丝不动，等麻烦自己撞上门。", "竹子 + 坚果"),
            ["Iceberg-shroom"] = new("冰山菇", "让冰冻效果持续得比寒冰菇更久。",
                "普通僵尸被冻结8秒|无法被冻结的僵尸仍会被减速至50%，持续8秒",
                "他坚称头顶那座冰山只是装饰。", "寒冰菇 + 寒冰菇"),
            ["Witchfire Pumpkin"] = new("巫火南瓜", "先强化壳内植物，点燃后再以献祭引发毁灭性的火焰。",
                "未点燃时：内部地面植物伤害翻倍，每秒回复50点耐久，并在1×1范围造成原伤害三分之一的溅射伤害|点燃且准备就绪时：消耗内部植物，同时触发毁灭菇与火爆辣椒效果；各造成1800＋储存能量×10的伤害|献祭按植物阳光消耗获得能量；相关融合植物会返还卡牌并提供3600能量|灼烧或激怒状态僵尸死亡、附近的毁灭菇爆炸均可提供能量|死亡时会释放双重爆炸，但不会再次献祭|奥德赛强化「手雷」：将毁灭爆炸替换为各行投掷的毁灭炸弹|奥德赛强化「辐射」：持续造成范围伤害，击杀会提高伤害和半径",
                "她愿意谈薰衣草、月光，唯独不愿让人看看壳里。", "烈焰南瓜 + 毁灭南瓜"),
            ["Nutty Sharpshooter"] = new("坚果神射手", "披着坚果护甲发射能够击退敌群的重型松针。",
                "松针可穿透一次，并将命中的僵尸击退半格|子弹不再无视手持防具|免疫冰冻与冰川化",
                "他声称每一次碰撞都经过精密计算。大概吧。", "云杉神射手 + 坚果"),
            ["Inferno Torchflower"] = new("炼狱火炬花", "点燃后储存阳光，再以不断增长的倍率一次释放。",
                "点燃前像普通火炬花一样生产阳光|有效的火爆辣椒火线提供50点能量；点燃后产出的阳光会被储存|点击释放储备；每50点能量使收益倍率增加0.5，随后能量清零",
                "她说这是阳光储蓄，账本却更像一座熔炉。", conversion: "向日葵 ↔ 火炬树桩"),
            ["Pumpkin Podbomber"] = new("南瓜荚轰炸手", "保护壳内射手、复制齐射，并定期将子弹换成爆炸豌豆。",
                "复制兼容豌豆射手的子弹数量、伤害、种类和索敌方式|每第4次复制齐射改为爆炸豌豆|内部为樱桃射手时，每发子弹都会爆炸，攻击间隔改为3秒|用铲子移除荚壳时返还爆炸射手卡牌",
                "一、二、三、轰！他的战术课永远停在最后一步。", conversion: "南瓜头 ↔ 铲子"),
            ["Cherry StarBomber"] = new("樱桃星爆手", "连续发射两轮樱桃星，迅速覆盖战场。",
                "每次攻击连续发射两轮，每轮5颗樱桃星|使用樱桃加特林轰炸手子弹穿过炼狱星木后形成的樱桃星|奥德赛强化「引导星座」：樱桃星追踪僵尸|奥德赛强化「星辰动量」：飞行3格后体积变大且伤害翻倍|奥德赛强化「滚动帮手」：每个巨型坚果每60秒在本行生成真正的滚动坚果",
                "十颗星星不是太多；完整的星座当然还要第二轮。", conversion: "杨桃 ↔ 豌豆射手"),
            ["Not-a-pea"] = new("锯齿豌豆", "发射能穿透敌群并附着在僵尸身上的锯齿豌豆。",
                "锯齿豌豆穿透沿途僵尸|每次命中有25%概率附着在目标身上|附着的锯齿持续10秒，每1.5秒造成10点伤害",
                "关于他究竟是豌豆还是锯子的采访，被他一刀截断了。", "锯齿草 + 豌豆射手"),
            ["Not-a-storm Commando"] = new("锯齿风暴突击手", "将豌豆风暴突击手的整轮射击换成持续伤害的锯齿豌豆。",
                "保留原本的攻击间隔、射击角度和子弹数量|每发锯齿豌豆都会穿透，并在每次命中时有25%概率附着目标|附着伤害每1.5秒触发一次，持续10秒",
                "他称这叫精密行动；六根枪管却总会一起冒烟。", "锯齿草 + 豌豆风暴突击手"),
            ["Frost Furflower"] = new("霜绒花", "生产阳光，并把迎面飞来的雪球变成额外收益。",
                "按普通向日葵周期生产阳光|被雪球击中时立即掉落100阳光|这次命中还会冻结周围3×3范围内其他可冻结植物",
                "她认为打雪仗是可再生能源计划。邻居们并不赞同。", "霜地衣 + 向日葵"),
            ["Doomtronion"] = new("毁灭电洋葱", "借助附近的电洋葱释放辐射连锁闪电与毁灭爆炸。",
                "范围内每个闲置的电洋葱或毁灭电洋葱使本次攻击增加150伤害|闪电最多连锁3个附近僵尸，后续目标受到一半伤害|命中者被辐射10秒|每个命中目标都有25%概率引发一次无坑、1800伤害的毁灭菇爆炸",
                "计数器从早餐起就没停止尖叫。", "电洋葱 + 毁灭菇"),
            ["Lichen-pea"] = new("地衣豌豆", "使僵尸受寒，并可能冻结目标附近的己方植物。",
                "豌豆命中的僵尸会获得寒冷状态|每次命中有25%概率随机冻结目标周围3×3范围内的2至4株可冻结植物",
                "白色外套是伪装，也是提醒队友站远一点。", "霜地衣 + 豌豆射手"),
            ["Logic Blover"] = new("逻辑三叶草", "留在草坪上吹退僵尸，并给每个目标施加随机状态。",
                "每个僵尸分别获得余烬、寒冷、黄油或中毒之一|在场时，使用三叶草、幸运三叶草或拟态黑麦分别增加5%、15%或16%的礼盒幸运值|每个礼盒都会按累积概率判定|使用金豆并花费10000金币可再次吹风",
                "他算尽所有可能，最后还是选择随机。"),
            ["Solar Sharpshooter"] = new("太阳神射手", "每次穿透子弹真正击中僵尸时都会产生阳光。",
                "子弹能穿透多个僵尸|每命中一个僵尸，立即获得25阳光",
                "只要太阳还在，他就不会放过任何一个机会。", "云杉神射手 + 向日葵"),
            ["Sea Ballista"] = new("海上弩炮", "占据相邻的两格水域，发射延时爆炸的弩箭。",
                "水生植物，占据相邻两格|弩箭最多穿透两次，命中僵尸0.3秒后爆炸|爆炸会击退被命中的僵尸",
                "他说潮水会把弩箭带回来。没人问过爆炸之后怎么办。", "云杉弩炮 + 海蘑菇"),
            ["Sea Sharpshooter"] = new("海洋神射手", "经历三个成长阶段，逐步获得更强的子弹。",
                "水生植物，共有三个成长阶段|初始体型为65%，随后长至82%，最后完全成熟|三个阶段的伤害依次为20、40和60",
                "水下的成长经历让他更坚韧，也让弹药更锋利。", "云杉神射手 + 海蘑菇"),
            ["Three-Buckpeater"] = new("三线铁桶射手", "向三行发射能破坏护甲的铁豌豆。",
                "同时攻击所在行和上下相邻行|铁豌豆击退僵尸，并立即摧毁二级护甲",
                "哪一行最需要铁桶？当然是三行都要。", "三线射手 + 铁桶"),
            ["Pineshooter"] = new("松树射手", "投出整棵松树，将命中的僵尸也变成飞行物。",
                "每棵松树击退第一个命中的僵尸|被击退的僵尸撞到其他僵尸时，双方额外受到40伤害并短暂眩晕|免疫冰冻与冰川化",
                "没人愿意到尸群里把那些树捡回来。", "豌豆射手 + 云杉神射手"),
            ["Icytronion"] = new("寒冰电洋葱", "借助附近的电洋葱释放冰冷的连锁闪电。",
                "范围内每个闲置的电洋葱或寒冰电洋葱增加150伤害|闪电最多连锁3个附近僵尸，后续目标受到一半伤害|命中目标减速10秒，并有25%概率被冻结|免疫冰冻与冰川化",
                "他坚持闪电不可能是冷的；僵尸们已经冻得无法反驳。", "电洋葱 + 寒冰菇"),
            ["Sakura Sharpshooter"] = new("樱花神射手", "发射会爆炸的花刺，并增强附近爆炸射手的连锁反应。",
                "花刺命中时有15%概率产生樱桃炸弹爆炸|附近爆炸射手的爆炸命中僵尸时，该僵尸有50%概率按原伤害再次爆炸|连锁反应成功时，周围3×3范围再随机选一个僵尸受到半伤爆炸",
                "她说花瓣只是装饰。僵尸们不信。", "樱桃炸弹 + 云杉神射手"),
            ["Boreal Orchid"] = new("极光兰", "定期放出一阵覆盖整行的极地之波。",
                "极地之波的动画沿整行掠过，增益持续10秒|本行夜间植物的伤害、耐久、攻击速度、生产速度和移动速度提高35%|路灯花获得增益后改为产生极地点数，而非光点",
                "她不要求黑夜变亮，只教黑暗如何开花。"),
            ["Bounce-shroom"] = new("弹弹菇", "把踩到自己的僵尸沿短弧线弹回约一格半，然后休息。",
                "可同时弹开同一行踩到它的多个僵尸|每次弹跳后休息5秒|不会被蹦极僵尸抓走",
                "嘣！"),
            ["Bomber Drone"] = new("樱桃轰炸无人机", "悬浮在草坪上，发射樱桃射手子弹。",
                "悬浮植物，避开地面障碍|子弹命中时不会爆炸|正下方直射植物的攻击间隔短于1.5秒时，会同步其攻击速度",
                "他说樱桃绝对安全，紧急降落按钮却是鲜红色。", "樱桃射手 + 三叶草"),
            ["Frostbite Drone"] = new("冰霜无人机", "悬浮在草坪上，发射寒冰豌豆。",
                "悬浮植物，避开地面障碍|寒冰豌豆可使僵尸减速|正下方直射植物的攻击间隔短于1.5秒时，会同步其攻击速度",
                "站得高一点，自然更容易保持冷静。", "寒冰射手 + 三叶草"),
            ["Ice-Lord Cactus"] = new("冰王仙人掌", "切换射击方式，同时压制地面与空中僵尸。",
                "对地面目标发射三枚冰仙人掌子弹，对空中目标发射一枚高射子弹|对空中僵尸造成四倍伤害|普通僵尸被冻结8秒；免疫冰冻的僵尸减速至50%，持续8秒",
                "头上的冰冠绝对没有粘住。至少他是这么说的。", "冰仙人掌 + 冰山菇"),
            ["Scovilia Pepper"] = new("斯科维尔辣椒", "同时用火爆辣椒的烈焰灼烧三行。",
                "灼烧所在行以及上下相邻两行的所有僵尸",
                "温度计提出辞职后，他就不再测量辣度。", "火爆辣椒 + 火爆辣椒"),
            ["Atomray-shroom"] = new("原子射线菇", "发射减速射线，并引发终结菇效果。",
                "射线令目标减速3秒|每次攻击有25%概率在目标处触发终结菇效果|死亡时也会触发终结菇效果|附近每株原子核心路灯花提高25%攻击速度，最快每0.5秒攻击一次",
                "蓝色警示光只是礼貌地提醒大家离远一点。", conversion: "路灯花 ↔ 大喷菇"),
            ["Cherry Cabbage"] = new("樱桃卷心菜", "投出不稳定的卷心菜；僵尸越靠近房子，威力越大。",
                "伤害随僵尸接近房子从40增长至160|命中爆炸概率随位置从15%增长至60%|爆炸额外造成与子弹伤害相同的范围伤害",
                "客人越接近门口，他的招待就越不客气。", "樱桃炸弹 + 卷心菜投手"),
            ["Lob-shroom"] = new("抛孢菇", "以抛物线投掷孢子，并可与其他小喷菇类植物同格。",
                "向僵尸投掷小喷菇孢子|可像小喷菇一样叠放在同一格",
                "他说篮子能让每颗孢子飞得更远。孢子们拒绝置评。"),
            ["Bumping-shroom"] = new("弹射孢菇", "弹飞附近僵尸，随后向它们投出三颗孢子。",
                "同一格先叠放3株抛孢菇，再放上弹弹菇才能合成|将附近地面僵尸沿弧线弹回约1.5格|弹跳结束后，向本行最近的三个僵尸各投一颗孢子|每次弹跳后休息5秒|免疫蹦极僵尸和投石车僵尸的攻击",
                "三层弹簧绝对稳固，只是没人想试第二次。", "抛孢菇×3 + 弹弹菇"),
            ["Spark-shroom"] = new("电弹菇", "弹飞踩到自己的僵尸，并让被弹飞者向附近目标放电。",
                "将附近地面僵尸沿弧线弹回约1.5格|每个被弹飞的僵尸会电击电洋葱范围内最近的其他僵尸|放电不会击中发出放电的僵尸自己|不会被蹦极僵尸抓走",
                "他称这为导电动量，僵尸们有不同意见。", "电洋葱 + 弹弹菇"),
            ["Superaqua-shroom"] = new("超级水刃菇", "近战挥刃，并与场上的超级云杉神射手共享寒冰能量。",
                "出场斩击使场上每株超级云杉神射手获得1层寒冰充能，并克制绿色信号|自身每拥有1层寒冰充能，超级云杉神射手射速提高4%，最多提高60%|受到冰冻或冷杉护士的补给时获得寒冰充能，上限15层；充能斩击提高伤害并使僵尸受寒",
                "刀锋很利，头脑更冷。", "", "云杉神射手 <-> 刀刃菇"),
            ["Pulse-shroom"] = new("脉冲菇", "弹飞僵尸时强化周围的夜间植物，并可能释放极地之波。",
                "每次成功弹跳强化周围3×3范围内的夜间植物10秒|每次弹跳有25%概率释放覆盖整行的极地之波|将僵尸弹回约1.5格，随后休息5秒",
                "每次弹跳，都有回响。", "弹弹菇 + 极光兰"),
            ["Sauerkraut-pult"] = new("酸菜投手", "根据目标与房子的距离切换弹药。",
                "目标距离房子1至5格时使用集中模式：高额单体伤害并施加灼烧|目标距离房子6至10格时使用溅射模式：范围伤害并施加中毒|两种模式使用不同的子弹",
                "近距离那一发，他称作精确发酵。", "垃圾投手 + 辣椒爆弹"),
            ["Saw-my-pot"] = new("锯齿花盆", "强化种在上面的所有锯齿草融合植物。",
                "上方锯齿草融合植物造成的伤害提高50%",
                "一家人都要保持锋利。非常锋利。", "锯齿草 + 花盆"),
            ["Vortex Pot"] = new("涡旋花盆", "强化寒冰菇融合植物，并改造种在上面的植物。",
                "强化上方的寒冰菇融合植物|若存在对应寒冰菇融合配方，就将上方植物转化为该融合植物|若没有配方，则冻结全场僵尸|像冰花盆一样将烤熟的植物恢复正常",
                "他能让每株植物冷静下来；没有冰属性的植物往往会照字面理解。", conversion: "铲子 ↔ 花盆"),
            ["Magnet-o-pea"] = new("磁力豌豆", "复制放在自身上的金属物品，并将其变成特殊弹药。",
                "只吸收直接放在身上的指定物品，不会远距离吸取|铁桶、橄榄球头盔、时间圆盘和爆炸道具会切换相应子弹|选中的弹药会一直保留，直到吸收下一件物品|对仍携带金属装备的僵尸造成双倍伤害",
                "远距离吸取叫偷东西，亲手递来的材料才是正当弹药。", "磁力菇 + 豌豆射手")
        };

        internal static bool TryCreate(string englishName, string? usage,
            string[]? stats, out AlmanacEntry entry)
        {
            if (!Entries.TryGetValue(englishName, out Copy? copy))
            {
                entry = default;
                return false;
            }

            StringBuilder info = new StringBuilder(copy.Summary).Append("\n\n");
            if (!string.IsNullOrEmpty(usage))
                info.Append("<color=#4A2900>使用条件：")
                    .Append(TranslateUsage(usage)).Append("</color>\n");

            if (stats != null)
            {
                for (int index = 0; index + 1 < stats.Length; index += 2)
                {
                    info.Append("<color=black>")
                        .Append(TranslateStatLabel(stats[index]))
                        .Append("：</color><color=#8B0000>")
                        .Append(TranslateStatValue(stats[index + 1]))
                        .Append("</color>\n");
                }
            }

            info.Append("<color=black>特点：</color>\n");
            string[] details = copy.Details.Split('|');
            for (int index = 0; index < details.Length; index++)
            {
                info.Append("<color=black>").Append(index + 1)
                    .Append(".</color><color=#8B0000> ")
                    .Append(SanitizeForGameFont(details[index]))
                    .Append("</color>");
                if (index + 1 < details.Length)
                    info.Append('\n');
            }

            StringBuilder recipe = new StringBuilder();
            if (!string.IsNullOrEmpty(copy.Recipe))
                recipe.Append("\n合成配方：<color=#8B0000>")
                    .Append(copy.Recipe).Append("</color>");
            if (!string.IsNullOrEmpty(copy.Conversion))
                recipe.Append("\n转换配方：<color=#8B0000>")
                    .Append(copy.Conversion).Append("</color>");

            entry = new AlmanacEntry(
                SanitizeForGameFont(copy.Name),
                SanitizeForGameFont(info.ToString()),
                "<color=black>" +
                    SanitizeForGameFont(PlantFlavorText.Get(englishName,
                        PlantsPlusLanguage.Chinese, copy.Lore)) + "</color>",
                SanitizeForGameFont(recipe.ToString()));
            return true;
        }

        private static string TranslateUsage(string usage) => usage switch
        {
            "Odyssey Mode" => "奥德赛模式",
            "Odyssey Mode | Epic Odyssey" => "奥德赛模式·史诗奥德赛",
            "Harvest Mode" => "收获模式",
            "Advanced Alt" => "高级替代融合",
            _ => usage
        };

        private static string TranslateStatLabel(string label) => label switch
        {
            "Toughness" => "韧性", "Lifetime" => "寿命",
            "Damage" => "伤害",
            "Sacrifice Cooldown" => "献祭冷却",
            "Sun Output" => "阳光产量",
            "Maximum Energy" => "能量上限",
            "Maximum Multiplier" => "最高倍率",
            "Attack Rate" => "攻击速度",
            "Projectile Damage" => "子弹伤害",
            "Attached Damage" => "附着伤害",
            "Snowball Bonus" => "雪球奖励",
            "Sun per Hit" => "每次命中产阳光",
            "Range" => "范围",
            "Growth Interval" => "成长间隔",
            "Attack Range" => "攻击范围",
            "Ground Attack" => "对地攻击",
            "Air Attack" => "对空攻击",
            "Demise Chance" => "终结效果概率",
            "Spore Damage" => "孢子伤害",
            "Rest" => "休息时间",
            "Recharge" => "冷却",
            "Lightning Damage" => "电击伤害",
            "Focus Damage" => "集中伤害",
            "Splash Damage" => "溅射伤害",
            "Damage Boost" => "伤害加成",
            "Boreal Wave" => "极地之波",
            "Melee Damage" => "近战伤害",
            "Ice Charges" => "寒冰充能",
            _ => label
        };

        private static string TranslateStatValue(string value)
        {
            if (value == "Up to 15")
                return "最多15层";
            return SanitizeForGameFont(value
                .Replace("Jalapeno damage", "火爆辣椒伤害")
                .Replace("50% of copied shooter", "复制射手的50%")
                .Replace("per lane", "每行")
                .Replace("each", "每发")
                .Replace("tiles", "格")
                .Replace("lanes", "行")
                .Replace("per attack", "每次攻击")
                .Replace("seconds", "秒")
                .Replace(" for ", "，持续")
                .Replace(" x3", " x3")
                .Replace("Sun", "阳光")
                .Replace("Medium", "中等")
                .Replace("Every ", "每 ")
                .Replace("s", "秒"));
        }

        private static string SanitizeForGameFont(string value) => value
            .Replace("冷杉护士", "超级小松炉")
            .Replace("超级云杉神射手", "超级云杉")
            .Replace("云杉神射手", "云杉弓手")
            .Replace("刀刃菇", "刃刃菇")
            .Replace("•", "-")
            .Replace("×", "x")
            .Replace("→", "->")
            .Replace("↔", "<->")
            .Replace("—", "-")
            .Replace("–", "-");
    }
}
