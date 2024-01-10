namespace GettausBotti.Library.Models
{
    using GettausBotti.Interfaces.Models;

    public class FameRow : IFameRow
    {
        public int Year { get; set; }
        public int Score { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = default!;

        public string ToStringRow(int length)
        {
            var nameAndScore = $"{UserName} ({Score})";

            var result = Year.ToString();
            result = result.PadRight(length);
            result = result.Substring(0, length - nameAndScore.ToString().Length - 1);
            result += " ";
            result += nameAndScore;

            return result;
        }
    }
}