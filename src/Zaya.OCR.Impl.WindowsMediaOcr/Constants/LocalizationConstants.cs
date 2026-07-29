namespace Zaya.OCR.Impl.WindowsMediaOcr.Constants;

internal static class LocalizationConstants
{
    public static class Settings
    {
        public const string EngineName = "Ocr_EngineName";
        public const string EngineDesc = "Ocr_EngineDesc";
        public const string Language = "Ocr_Language";
        public const string Language_Desc = "Ocr_Language_Desc";
        public const string Language_Auto = "Ocr_Language_Auto";
    }

    public static class Exceptions
    {
        public const string LanguageNotSupported = "Ocr_Err_LanguageNotSupported";
        public const string EngineCreateFailed = "Ocr_Err_EngineCreateFailed";
        public const string UnsupportedPixelFormat = "Ocr_Err_UnsupportedPixelFormat";
        public const string RecognizeFailed = "Ocr_Err_RecognizeFailed";
        public const string NotAvailable = "Ocr_Err_NotAvailable";
    }
}
