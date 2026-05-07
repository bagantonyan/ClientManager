namespace ClientManager.UnitTests.TestData
{
    internal static class ValidInns
    {
        // Legal_Entity (10 digits, single check digit)
        public const string LegalEntity1 = "7707083893";
        public const string LegalEntity2 = "7710030411";
        public const string LegalEntity3 = "7728168971";
        public const string LegalEntity4 = "7701001005";
        public const string LegalEntity5 = "7812004001";

        // Individual (12 digits, two check digits) — used for Individual_Entrepreneur and Founder
        public const string Individual1 = "770708389324";
        public const string Individual2 = "771003041120";
        public const string Individual3 = "502500500062";
        public const string Individual4 = "781200400110";
        public const string Individual5 = "772816897110";
    }
}
