namespace Markyu.LastKernel
{
    /// <summary>
    /// Centralizes player-facing identity while the serialized runtime namespace remains stable.
    /// Rename the namespace/folder in a dedicated Unity migration so scene and prefab references can be verified together.
    /// </summary>
    public static class GameIdentity
    {
        public const string DisplayName = "Last Kernel";
        public const string ProductNameNoSpaces = "LastKernel";
        public const string LegacyDisplayName = "FortStack";
        public const string CompanyName = "Markyu";

        public const string CurrentRootNamespace = "Markyu.LastKernel";
        public const string LegacyRuntimeNamespace = "Markyu.FortStack";

        public const string LocaleCodePlayerPrefsKey = ProductNameNoSpaces + ".LocaleCode";
        public const string LanguagePlayerPrefsKey = ProductNameNoSpaces + ".Language";
        public const string LegacyLanguagePlayerPrefsKey = LegacyDisplayName + ".Language";
    }
}
