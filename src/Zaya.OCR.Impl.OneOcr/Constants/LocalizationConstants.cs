namespace Zaya.OCR.Impl.OneOcr.Constants;

internal static class LocalizationConstants
{
    public static class Settings
    {
        public const string EngineName = "Ocr_EngineName";
        public const string EngineDesc = "Ocr_EngineDesc";
        public const string Source = "Ocr_Source";
        public const string Source_Desc = "Ocr_Source_Desc";
        public const string Source_Auto = "Ocr_Source_Auto";
        public const string Source_SnippingTool = "Ocr_Source_SnippingTool";
        public const string Source_Directory = "Ocr_Source_Directory";
        public const string Source_Url = "Ocr_Source_Url";
        public const string EngineDir = "Ocr_EngineDir";
        public const string EngineDir_Desc = "Ocr_EngineDir_Desc";
        public const string DownloadUrl = "Ocr_DownloadUrl";
        public const string DownloadUrl_Desc = "Ocr_DownloadUrl_Desc";
        public const string CacheDir = "Ocr_CacheDir";
        public const string CacheDir_Desc = "Ocr_CacheDir_Desc";
        public const string MinConfidence = "Ocr_MinConfidence";
        public const string MinConfidence_Desc = "Ocr_MinConfidence_Desc";
    }

    public static class Exceptions
    {
        public const string SnippingToolNotFound = "Ocr_Err_SnippingToolNotFound";
        public const string ModelNotFound = "Ocr_Err_ModelNotFound";
        public const string DllLoadFailed = "Ocr_Err_DllLoadFailed";
        public const string DllNotFound = "Ocr_Err_DllNotFound";


        public const string DirectoryPathRequired = "Ocr_Err_DirectoryPathRequired";
        public const string DownloadUrlRequired = "Ocr_Err_DownloadUrlRequired";
        public const string UnknownSource = "Ocr_Err_UnknownSource";
        public const string NativeFailed = "Ocr_Err_NativeFailed";
    }
}
