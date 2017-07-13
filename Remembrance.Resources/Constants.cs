using System;
using JetBrains.Annotations;
using Scar.Common.Networking;

namespace Remembrance.Resources
{
    public static class Constants
    {
        [NotNull]
        public const string AutoDetectLanguage = "auto";

        [NotNull]
        public const string EnLanguage = "en-US";

        [NotNull]
        public const string RuLanguage = "ru-RU";

        [NotNull]
        public const string EnLanguageTwoLetters = "en";

        [NotNull]
        public const string RuLanguageTwoLetters = "ru";

        public static readonly string MachineKey = Environment.MachineName
                                                   + (NetworkUtility.IsNetworkAvailable()
                                                       ? $"_{NetworkUtility.GetLocalIpAddress()}"
                                                       : null);
    }
}