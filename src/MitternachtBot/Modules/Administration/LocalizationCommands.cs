using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class LocalizationCommands : MitternachtSubmodule
        {
            private static ImmutableDictionary<string, string> supportedLocales { get; } = new Dictionary<string, string>()
            {
                {"ar", "Ø§Ù„Ø¹Ø±Ø¨ÙŠØ©" },
                {"zh-TW", "ç¹é«”ä¸­æ–‡, å°ç£" },
                {"zh-CN", "ç®€ä½“ä¸­æ–‡, ä¸­åŽäººæ°‘å…±å’Œå›½"},
                {"nl-NL", "Nederlands, Nederland"},
                {"en-US", "English, United States"},
                {"fr-FR", "FranÃ§ais, France"},
                {"cs-CZ", "ÄŒeÅ¡tina, ÄŒeskÃ¡ republika" },
                {"da-DK", "Dansk, Danmark" },
                {"de-DE", "Deutsch, Deutschland"},
                {"he-IL", "×¢×‘×¨×™×ª, ×™×©×¨××œ"},
                {"id-ID", "Bahasa Indonesia, Indonesia" },
                {"it-IT", "Italiano, Italia" },
                {"ja-JP", "æ—¥æœ¬èªž, æ—¥æœ¬"},
                {"ko-KR", "í•œêµ­ì–´, ëŒ€í•œë¯¼êµ­" },
                {"nb-NO", "Norsk, Norge"},
                {"pl-PL", "Polski, Polska" },
                {"pt-BR", "PortuguÃªs Brasileiro, Brasil"},
                {"ro-RO", "RomÃ¢nÄƒ, RomÃ¢nia" },
                {"ru-RU", "Ð ÑƒÑÑÐºÐ¸Ð¹, Ð Ð¾ÑÑÐ¸Ñ"},
                {"sr-Cyrl-RS", "Ð¡Ñ€Ð¿ÑÐºÐ¸, Ð¡Ñ€Ð±Ð¸Ñ˜Ð°"},
                {"es-ES", "EspaÃ±ol, EspaÃ±a"},
                {"sv-SE", "Svenska, Sverige"},
                {"tr-TR", "TÃ¼rkÃ§e, TÃ¼rkiye"}
            }.ToImmutableDictionary();

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task LanguageSet()
            {
                var cul = Localization.GetCultureInfo(Context.Guild);
                await ReplyConfirmLocalized("lang_set_show", Format.Bold(cul.ToString()), Format.Bold(cul.NativeName))
                    .ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task LanguageSet(string name)
            {
                try
                {
                    CultureInfo ci;
                    if (name.Trim().ToLowerInvariant() == "default")
                    {
                        Localization.RemoveGuildCulture(Context.Guild);
                        ci = Localization.DefaultCultureInfo;
                    }
                    else
                    {
                        ci = new CultureInfo(name);
                        Localization.SetGuildCulture(Context.Guild, ci);
                    }

                    await ReplyConfirmLocalized("lang_set", Format.Bold(ci.ToString()), Format.Bold(ci.NativeName)).ConfigureAwait(false);
                }
                catch(Exception)
                {
                    await ReplyErrorLocalized("lang_set_fail").ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task LanguageSetDefault()
            {
                var cul = Localization.DefaultCultureInfo;
                await ReplyConfirmLocalized("lang_set_bot_show", cul, cul.NativeName).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task LanguageSetDefault(string name)
            {
                try
                {
                    CultureInfo ci;
                    if (name.Trim().ToLowerInvariant() == "default")
                    {
                        Localization.ResetDefaultCulture();
                        ci = Localization.DefaultCultureInfo;
                    }
                    else
                    {
                        ci = new CultureInfo(name);
                        Localization.SetDefaultCulture(ci);
                    }
                    await ReplyConfirmLocalized("lang_set_bot", Format.Bold(ci.ToString()), Format.Bold(ci.NativeName)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalized("lang_set_fail").ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task LanguagesList()
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("lang_list"))
                    .WithDescription(string.Join("\n",
                        supportedLocales.Select(x => $"{Format.Code(x.Key), -10} => {x.Value}"))));
            }
        }
    }
}
/* list of language codes for reference. 
 * taken from https://github.com/dotnet/coreclr/blob/ee5862c6a257e60e263537d975ab6c513179d47f/src/mscorlib/src/System/Globalization/CultureData.cs#L192
            { "029", "en-029" },
            { "AE",  "ar-AE" },
            { "AF",  "prs-AF" },
            { "AL",  "sq-AL" },
            { "AM",  "hy-AM" },
            { "AR",  "es-AR" },
            { "AT",  "de-AT" },
            { "AU",  "en-AU" },
            { "AZ",  "az-Cyrl-AZ" },
            { "BA",  "bs-Latn-BA" },
            { "BD",  "bn-BD" },
            { "BE",  "nl-BE" },
            { "BG",  "bg-BG" },
            { "BH",  "ar-BH" },
            { "BN",  "ms-BN" },
            { "BO",  "es-BO" },
            { "BR",  "pt-BR" },
            { "BY",  "be-BY" },
            { "BZ",  "en-BZ" },
            { "CA",  "en-CA" },
            { "CH",  "it-CH" },
            { "CL",  "es-CL" },
            { "CN",  "zh-CN" },
            { "CO",  "es-CO" },
            { "CR",  "es-CR" },
            { "CS",  "sr-Cyrl-CS" },
            { "CZ",  "cs-CZ" },
            { "DE",  "de-DE" },
            { "DK",  "da-DK" },
            { "DO",  "es-DO" },
            { "DZ",  "ar-DZ" },
            { "EC",  "es-EC" },
            { "EE",  "et-EE" },
            { "EG",  "ar-EG" },
            { "ES",  "es-ES" },
            { "ET",  "am-ET" },
            { "FI",  "fi-FI" },
            { "FO",  "fo-FO" },
            { "FR",  "fr-FR" },
            { "GB",  "en-GB" },
            { "GE",  "ka-GE" },
            { "GL",  "kl-GL" },
            { "GR",  "el-GR" },
            { "GT",  "es-GT" },
            { "HK",  "zh-HK" },
            { "HN",  "es-HN" },
            { "HR",  "hr-HR" },
            { "HU",  "hu-HU" },
            { "ID",  "id-ID" },
            { "IE",  "en-IE" },
            { "IL",  "he-IL" },
            { "IN",  "hi-IN" },
            { "IQ",  "ar-IQ" },
            { "IR",  "fa-IR" },
            { "IS",  "is-IS" },
            { "IT",  "it-IT" },
            { "IV",  "" },
            { "JM",  "en-JM" },
            { "JO",  "ar-JO" },
            { "JP",  "ja-JP" },
            { "KE",  "sw-KE" },
            { "KG",  "ky-KG" },
            { "KH",  "km-KH" },
            { "KR",  "ko-KR" },
            { "KW",  "ar-KW" },
            { "KZ",  "kk-KZ" },
            { "LA",  "lo-LA" },
            { "LB",  "ar-LB" },
            { "LI",  "de-LI" },
            { "LK",  "si-LK" },
            { "LT",  "lt-LT" },
            { "LU",  "lb-LU" },
            { "LV",  "lv-LV" },
            { "LY",  "ar-LY" },
            { "MA",  "ar-MA" },
            { "MC",  "fr-MC" },
            { "ME",  "sr-Latn-ME" },
            { "MK",  "mk-MK" },
            { "MN",  "mn-MN" },
            { "MO",  "zh-MO" },
            { "MT",  "mt-MT" },
            { "MV",  "dv-MV" },
            { "MX",  "es-MX" },
            { "MY",  "ms-MY" },
            { "NG",  "ig-NG" },
            { "NI",  "es-NI" },
            { "NL",  "nl-NL" },
            { "NO",  "nn-NO" },
            { "NP",  "ne-NP" },
            { "NZ",  "en-NZ" },
            { "OM",  "ar-OM" },
            { "PA",  "es-PA" },
            { "PE",  "es-PE" },
            { "PH",  "en-PH" },
            { "PK",  "ur-PK" },
            { "PL",  "pl-PL" },
            { "PR",  "es-PR" },
            { "PT",  "pt-PT" },
            { "PY",  "es-PY" },
            { "QA",  "ar-QA" },
            { "RO",  "ro-RO" },
            { "RS",  "sr-Latn-RS" },
            { "RU",  "ru-RU" },
            { "RW",  "rw-RW" },
            { "SA",  "ar-SA" },
            { "SE",  "sv-SE" },
            { "SG",  "zh-SG" },
            { "SI",  "sl-SI" },
            { "SK",  "sk-SK" },
            { "SN",  "wo-SN" },
            { "SV",  "es-SV" },
            { "SY",  "ar-SY" },
            { "TH",  "th-TH" },
            { "TJ",  "tg-Cyrl-TJ" },
            { "TM",  "tk-TM" },
            { "TN",  "ar-TN" },
            { "TR",  "tr-TR" },
            { "TT",  "en-TT" },
            { "TW",  "zh-TW" },
            { "UA",  "uk-UA" },
            { "US",  "en-US" },
            { "UY",  "es-UY" },
            { "UZ",  "uz-Cyrl-UZ" },
            { "VE",  "es-VE" },
            { "VN",  "vi-VN" },
            { "YE",  "ar-YE" },
            { "ZA",  "af-ZA" },
            { "ZW",  "en-ZW" }
 */
