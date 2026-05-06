namespace ClientManager.Infrastructure.Presentation.Validators
{
    internal static class InnValidator
    {
        private static readonly int[] Weights10 = { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
        private static readonly int[] Weights11 = { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
        private static readonly int[] Weights12 = { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };

        public static bool IsValid(string? inn)
        {
            if (string.IsNullOrEmpty(inn) || !inn.All(char.IsDigit))
                return false;

            return inn.Length switch
            {
                10 => CheckDigit(inn, Weights10) == inn[9] - '0',
                12 => CheckDigit(inn, Weights11) == inn[10] - '0'
                   && CheckDigit(inn, Weights12) == inn[11] - '0',
                _ => false
            };
        }

        private static int CheckDigit(string inn, int[] weights)
        {
            var sum = 0;
            for (var i = 0; i < weights.Length; i++)
                sum += (inn[i] - '0') * weights[i];
            return sum % 11 % 10;
        }
    }
}
