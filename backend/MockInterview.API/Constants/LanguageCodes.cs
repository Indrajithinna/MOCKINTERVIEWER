namespace MockInterview.API.Constants
{
    public static class LanguageCodes
    {
        public const string English = "en-IN";
        public const string Hindi = "hi-IN";
        public const string Kannada = "kn-IN";
        public const string Telugu = "te-IN";
        public const string Malayalam = "ml-IN";

        public static bool IsSupported(string languageCode)
        {
            return languageCode switch
            {
                English => true,
                Hindi => true,
                Kannada => true,
                Telugu => true,
                Malayalam => true,
                _ => false
            };
        }
    }
}
