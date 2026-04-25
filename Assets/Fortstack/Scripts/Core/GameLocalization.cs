using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Markyu.FortStack
{
    public enum GameLanguage
    {
        SimplifiedChinese = 0,
        English = 1
    }

    internal readonly struct LocalizedTextEntry
    {
        private readonly string chinese;
        private readonly string english;

        public LocalizedTextEntry(string chinese, string english)
        {
            this.chinese = chinese;
            this.english = english;
        }

        public string GetText(GameLanguage language)
        {
            return language == GameLanguage.English ? english : chinese;
        }
    }

    public static class GameLocalization
    {
        private static bool isInitialized;

        private static readonly Dictionary<string, LocalizedTextEntry> TextEntries = new()
        {
            ["language.label"] = new("语言：{0}", "Language: {0}"),
            ["language.chinese"] = new("简体中文", "Simplified Chinese"),
            ["language.english"] = new("英语", "English"),

            ["title.newGame"] = new("新游戏", "New Game"),
            ["title.loadGame"] = new("载入游戏", "Load Game"),
            ["title.options"] = new("运行设置", "Options"),
            ["title.quitGame"] = new("退出游戏", "Quit Game"),
            ["title.quitConfirmTitle"] = new("退出游戏", "Quit Game"),
            ["title.quitConfirmBody"] = new("确认要退出游戏吗？", "Are you sure you want to quit?"),
            ["title.versionDraft"] = new("版本：未发布", "Version: Unreleased"),
            ["title.loadHeader"] = new("读取存档", "Load Saves"),
            ["title.gameplayHeader"] = new("生存偏好设置", "Game Setup"),
            ["title.clearSaves"] = new("[清除全部存档]", "[Delete All Saves]"),

            ["pause.header"] = new("已暂停", "Paused"),
            ["pause.resume"] = new("继续游戏", "Resume"),
            ["pause.backToTitle"] = new("保存并返回标题", "Save & Title"),

            ["menu.quests"] = new("任务", "Quests"),
            ["menu.blueprints"] = new("蓝图", "Blueprints"),
            ["menu.infoPlaceholder"] = new(
                "<size=34>[情报面板]<size=30>\n信息会显示在这里。\n面板会根据文本长度自动调整尺寸。",
                "<size=34>[Intel Panel]<size=30>\nInformation appears here.\nThe panel resizes to fit the text."),

            ["ui.action"] = new("执行动作", "Run Action"),
            ["ui.footer"] = new("Last Kernel 堡垒街区试作 / 作者 Markyu", "Last Kernel Fortress District Prototype / Markyu"),

            ["common.confirmButton"] = new("[确认]", "[Confirm]"),
            ["common.cancelButton"] = new("[取消]", "[Cancel]"),
            ["common.closeButton"] = new("[关闭]", "[Close]"),
            ["common.resetButton"] = new("[重置]", "[Reset]"),
            ["common.loadButton"] = new("[读取]", "[Load]"),
            ["common.deleteButton"] = new("[删除]", "[Delete]"),

            ["options.header"] = new("运行设置", "Options"),
            ["options.sfx"] = new("音效 {0}%", "SFX {0}%"),
            ["options.bgm"] = new("背景音乐 {0}%", "BGM {0}%"),
            ["options.resetTitle"] = new("重置系统设置？", "Reset Settings?"),
            ["options.resetBody"] = new(
                "这会把全部图形与音频参数恢复为默认值，且无法撤销。",
                "This resets all graphics and audio settings to their defaults and cannot be undone."),

            ["gameplay.dayDuration"] = new("白昼时长：{0} 秒", "Day Length: {0}s"),
            ["gameplay.friendlyOn"] = new(
                "友好模式：开启\n<size=23>（敌对目标不会出现）",
                "Friendly Mode: On\n<size=23>(Hostile targets will not appear)"),
            ["gameplay.friendlyOff"] = new(
                "友好模式：关闭\n<size=23>（敌对目标可能出现）",
                "Friendly Mode: Off\n<size=23>(Hostile targets may appear)"),

            ["save.clearTitle"] = new("清除全部存档？", "Delete All Saves?"),
            ["save.clearBody"] = new(
                "确认要删除全部存档吗？\n此操作无法撤销。",
                "Are you sure you want to delete all save files?\nThis action cannot be undone."),
            ["save.deleteTitle"] = new("删除存档？", "Delete Save?"),
            ["save.deleteBody"] = new(
                "确认要删除 {0} 号存档吗？\n此操作无法撤销。",
                "Are you sure you want to delete save slot {0}?\nThis action cannot be undone."),
            ["save.slotLabel"] = new("[存档 {0:D3}] {1}{2}", "[Slot {0:D3}] {1}{2}"),
            ["save.progressSuffix"] = new(" ({0}%)", " ({0}%)"),
            ["save.lastSaved"] = new("\n上次保存：{0}", "\nLast Saved: {0}"),

            ["day.placeholder"] = new("第 N 天", "Day N"),
            ["day.current"] = new("第 {0} 天", "Day {0}"),

            ["daycycle.dayEndedTitle"] = new("第 {0} 天结束", "Day {0} Complete"),
            ["daycycle.dayEndedBody"] = new("街区需要补充口粮。", "The district needs rations."),
            ["daycycle.dayEndedAction"] = new("分发口粮", "Distribute Rations"),
            ["daycycle.feedingTitle"] = new("正在分发口粮", "Distributing Rations"),
            ["daycycle.feedingBody"] = new("正在为街区居民派发补给。", "Delivering supplies to the district residents."),
            ["daycycle.cleanupTitle"] = new("清理超载库存", "Reduce Overload"),
            ["daycycle.cleanupBody"] = new(
                "你还需要处理 {0} 张超额卡牌，系统才会推进到下一循环。",
                "You still need to remove {0} excess cards before the next cycle can begin."),
            ["daycycle.dayStartedTitle"] = new("第 {0} 天开始", "Day {0} Begins"),
            ["daycycle.dayStartedBody"] = new("街区节点已重新联机。", "District links are back online."),
            ["daycycle.dayStartedAction"] = new("启动新一天", "Start New Day"),
            ["daycycle.gameOverTitle"] = new("街区失守", "District Lost"),
            ["daycycle.gameOverBody"] = new("你已经没有任何可用人员了。", "No surviving Recruits remain."),
            ["daycycle.gameOverAction"] = new("返回标题", "Return to Title"),

            ["graphics.resolution"] = new("分辨率 {0}x{1}", "Resolution {0}x{1}"),
            ["graphics.fullscreen.windowed"] = new("显示模式 窗口化", "Display Windowed"),
            ["graphics.fullscreen.fullscreen"] = new("显示模式 全屏", "Display Fullscreen"),
            ["graphics.vsync.off"] = new("垂直同步 关闭", "VSync Off"),
            ["graphics.vsync.on"] = new("垂直同步 开启", "VSync On"),
            ["graphics.vsync.half"] = new("垂直同步 半速", "VSync Half Rate"),
            ["graphics.vsync.unknown"] = new("垂直同步 未知", "VSync Unknown"),
            ["graphics.fps.unlimited"] = new("帧率上限 不限", "FPS Cap Unlimited"),
            ["graphics.fps.capped"] = new("帧率上限 {0}", "FPS Cap {0}"),
            ["graphics.shadow.quality"] = new("阴影质量 {0}", "Shadows {0}"),
            ["graphics.shadow.off"] = new("关闭", "Off"),
            ["graphics.shadow.low"] = new("低", "Low"),
            ["graphics.shadow.medium"] = new("中", "Medium"),
            ["graphics.shadow.high"] = new("高", "High"),
            ["graphics.shadow.ultra"] = new("超高", "Ultra"),
            ["graphics.shadow.unknown"] = new("未知", "Unknown"),

            ["trade.price"] = new("价格：{0}", "Price: {0}"),
            ["trade.collectionComplete"] = new("<color=#FFD700>已完全解锁</color>", "<color=#FFD700>Fully Unlocked</color>"),
            ["trade.collectionProgress"] = new("已收录：\n{0}/{1}", "Collected:\n{0}/{1}"),
            ["trade.vendorHeader"] = new("卡包贩卖终端", "Pack Vendor"),
            ["trade.vendorBody"] = new("在这里可以购买 {0}。", "Buy {0} here."),
            ["trade.buyerHeader"] = new("卡牌回收终端", "Card Recycler"),
            ["trade.buyerBody"] = new("把可出售的卡牌拖到这里，换取信用点。", "Drag sellable cards here to exchange them for Credits."),
            ["trade.packUnlocked"] = new("新卡包已解锁", "New Pack Unlocked"),
            ["trade.sellStatic"] = new("出售", "Sell"),
            ["trade.expansionTitle"] = new("扩建", "Expand"),
            ["trade.expansionHeader"] = new("街区扩建终端", "District Expansion"),
            ["trade.expansionBody"] = new("拖入信用点，为街区棋盘购买一行新的可用区域。还需：{0}。", "Drag Credits here to buy a new district board row. Remaining: {0}."),
            ["trade.expansionProgress"] = new("扩建行：\n{0}/{1}", "Rows:\n{0}/{1}"),
            ["trade.expansionComplete"] = new("<color=#FFD700>空间已满</color>", "<color=#FFD700>Fully Expanded</color>"),
            ["trade.expansionCompleteBody"] = new("街区棋盘已经扩建到当前上限。", "The district board has reached its current expansion limit."),

            ["quest.progress"] = new("进度：{0} / {1}", "Progress: {0} / {1}"),

            ["recipe.blueprint"] = new("蓝图：{0}", "Blueprint: {0}"),
            ["recipe.unknown"] = new("蓝图：未知", "Blueprint: Unknown"),
            ["recipe.category.misc"] = new("杂项", "Misc"),
            ["recipe.category.gathering"] = new("回收", "Salvage"),
            ["recipe.category.construction"] = new("建造", "Construction"),
            ["recipe.category.cooking"] = new("烹饪", "Cooking"),
            ["recipe.category.forging"] = new("制造", "Fabrication"),
            ["recipe.category.refining"] = new("加工", "Refining"),
            ["recipe.category.husbandry"] = new("管控", "Containment"),

            ["card.timeLeft"] = new("剩余时间：{0:F1} 秒", "Time Left: {0:F1}s"),
            ["card.stackHeader"] = new("卡牌堆", "Stack of Cards"),
            ["card.health"] = new("生命值 ({0}/{1})", "Health ({0}/{1})"),

            ["placeholder.packName"] = new("卡包名称", "Pack Name"),
            ["placeholder.cardName"] = new("卡牌名称", "Card Name"),

            ["modal.title"] = new("操作确认", "Confirm Action"),
            ["modal.body"] = new("确认执行这项操作吗？\n执行后将立即生效。", "Confirm this action?\nIt will take effect immediately.")
        };

        private static readonly Dictionary<string, string> InspectorTextKeys = new()
        {
            ["执行动作"] = "ui.action",
            ["已暂停"] = "pause.header",
            ["第 N 天"] = "day.placeholder",
            ["继续游戏"] = "pause.resume",
            ["任务"] = "menu.quests",
            ["蓝图"] = "menu.blueprints",
            ["保存并返回标题"] = "pause.backToTitle",
            ["FortStack 赛博殖民试作 / 作者 Markyu"] = "ui.footer",
            ["Last Kernel 赛博殖民试作 / 作者 Markyu"] = "ui.footer",
            ["Last Kernel 堡垒街区试作 / 作者 Markyu"] = "ui.footer",
            ["运行设置"] = "options.header",
            ["[Delete]"] = "common.deleteButton",
            ["[Load]"] = "common.loadButton",
            ["[取消]"] = "common.cancelButton",
            ["操作确认"] = "modal.title",
            ["确认执行这项操作吗？\n执行后将立即生效。"] = "modal.body",
            ["[确认]"] = "common.confirmButton",
            ["版本：未发布"] = "title.versionDraft",
            ["读取存档"] = "title.loadHeader",
            ["生存偏好设置"] = "title.gameplayHeader",
            ["[清除全部存档]"] = "title.clearSaves",
            ["退出游戏"] = "title.quitGame",
            ["载入游戏"] = "title.loadGame",
            ["新游戏"] = "title.newGame",
            ["Pack Name"] = "placeholder.packName",
            ["Card Name"] = "placeholder.cardName",
            ["Sell"] = "trade.sellStatic",
            ["<size=34>[情报面板]<size=30>\n信息会显示在这里。\n面板会根据文本长度自动调整尺寸。"] = "menu.infoPlaceholder"
        };

        public static event Action<GameLanguage> LanguageChanged;

        public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.SimplifiedChinese;

        public static CultureInfo CurrentCulture
        {
            get
            {
                return CurrentLanguage == GameLanguage.English
                    ? CultureInfo.GetCultureInfo("en-US")
                    : CultureInfo.GetCultureInfo("zh-CN");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            int savedValue = LoadLanguagePreference();
            if (!Enum.IsDefined(typeof(GameLanguage), savedValue))
            {
                savedValue = (int)GameLanguage.SimplifiedChinese;
            }

            CurrentLanguage = (GameLanguage)savedValue;
        }

        public static void SetLanguage(GameLanguage language, bool force = false)
        {
            Initialize();

            if (!force && CurrentLanguage == language)
                return;

            CurrentLanguage = language;
            SaveLanguagePreference(language);
            LanguageChanged?.Invoke(language);
        }

        public static void CycleLanguage()
        {
            SetLanguage(CurrentLanguage == GameLanguage.SimplifiedChinese
                ? GameLanguage.English
                : GameLanguage.SimplifiedChinese);
        }

        public static string Get(string key)
        {
            Initialize();

            if (TextEntries.TryGetValue(key, out LocalizedTextEntry entry))
            {
                return entry.GetText(CurrentLanguage);
            }

            Debug.LogWarning($"GameLocalization: Missing key '{key}'.");
            return key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(CurrentCulture, Get(key), args);
        }

        public static string GetCurrentLanguageDisplayName()
        {
            return CurrentLanguage == GameLanguage.English
                ? Get("language.english")
                : Get("language.chinese");
        }

        public static string GetLanguageButtonLabel()
        {
            return Format("language.label", GetCurrentLanguageDisplayName());
        }

        public static string GetRecipeCategoryLabel(RecipeCategory category)
        {
            return category switch
            {
                RecipeCategory.Misc => Get("recipe.category.misc"),
                RecipeCategory.Gathering => Get("recipe.category.gathering"),
                RecipeCategory.Construction => Get("recipe.category.construction"),
                RecipeCategory.Cooking => Get("recipe.category.cooking"),
                RecipeCategory.Forging => Get("recipe.category.forging"),
                RecipeCategory.Refining => Get("recipe.category.refining"),
                RecipeCategory.Husbandry => Get("recipe.category.husbandry"),
                _ => category.ToString()
            };
        }

        public static bool TryTranslateInspectorText(string source, out string translated)
        {
            Initialize();

            translated = null;

            if (string.IsNullOrEmpty(source))
                return false;

            if (InspectorTextKeys.TryGetValue(source, out string key))
            {
                translated = Get(key);
                return true;
            }

            return false;
        }

        private static void SaveLanguagePreference(GameLanguage language)
        {
            PlayerPrefs.SetInt(GameIdentity.LanguagePlayerPrefsKey, (int)language);
            PlayerPrefs.Save();
        }

        private static int LoadLanguagePreference()
        {
            if (PlayerPrefs.HasKey(GameIdentity.LanguagePlayerPrefsKey))
            {
                return PlayerPrefs.GetInt(GameIdentity.LanguagePlayerPrefsKey, (int)GameLanguage.SimplifiedChinese);
            }

            if (PlayerPrefs.HasKey(GameIdentity.LegacyLanguagePlayerPrefsKey))
            {
                int legacyValue = PlayerPrefs.GetInt(GameIdentity.LegacyLanguagePlayerPrefsKey, (int)GameLanguage.SimplifiedChinese);
                PlayerPrefs.SetInt(GameIdentity.LanguagePlayerPrefsKey, legacyValue);
                PlayerPrefs.Save();
                return legacyValue;
            }

            return (int)GameLanguage.SimplifiedChinese;
        }
    }
}
