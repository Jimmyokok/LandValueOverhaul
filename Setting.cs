using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandValueOverhaul
{
    [FileLocation(nameof(LandValueOverhaul))]
    public class Setting : ModSetting
    {
        public Setting(IMod mod) : base(mod)
        {
        }

        public const string kLandvalueSection = "Land Value";

        [SettingsUIHidden]
        public bool _Hidden { get; set; }

        [SettingsUISection(kLandvalueSection)]
        [SettingsUISlider(min = 100, max = 10000, step = 100)]
        public int FadeDistance { get; set; } = 200;

        [SettingsUISection(kLandvalueSection)]
        [SettingsUISlider(min = 1, max = 100, step = 1)]
        public int FadeSpeed { get; set; } = 10;

        [SettingsUISection(kLandvalueSection)]
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ButtonDefault { set { SetDefaults(); } }

        [SettingsUISection(kLandvalueSection)]
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ButtonVanilla { set { SetVanilla(); } }

        public override void SetDefaults()
        {
            _Hidden = true;
            FadeDistance = 200;
            FadeSpeed = 10;
        }

        public void SetVanilla()
        {
            FadeDistance = 2000;
            FadeSpeed = 10;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Land Value Overhaul v1.5" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FadeDistance)), "Land value update distance factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FadeDistance)), "Change how the land value influence falls with distance." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FadeSpeed)), "Land value spread speed factor (X time slower than vanilla)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FadeSpeed)), "Change how quickly the land value spreads to nearby regions. Setting it to 10 means 10 times slower than vanilla" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ButtonDefault)), "Default Setting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ButtonDefault)), "Resets setting to Default values." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ButtonDefault)), "Confirm Default setting" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ButtonVanilla)), "Vanilla Setting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ButtonVanilla)), "Resets setting to Vanilla values." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ButtonVanilla)), "Confirm Vanilla setting" },
            };
        }

        public void Unload()
        {

        }
    }

    public class LocaleCN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleCN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "地价机制大修 v1.5" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FadeDistance)), "地价衰减距离系数" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FadeDistance)), "设置地价传播随距离衰减程度" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FadeSpeed)), "地价传播速度系数（比原版慢X倍）" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FadeSpeed)), "设置地价传播到相邻地区的速度，设置为10代表比原版慢10倍" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ButtonDefault)), "使用模组默认设置" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ButtonDefault)), "重置到模组默认设置" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ButtonDefault)), "是否重置到模组默认设置" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ButtonVanilla)), "使用游戏本体默认设置" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ButtonVanilla)), "重置到游戏本体默认设置" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ButtonVanilla)), "是否重置到游戏本体默认设置" },
            };
        }

        public void Unload()
        {

        }
    }
}
