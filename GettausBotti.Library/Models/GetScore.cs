namespace GettausBotti.Library.Models
{
    using GettausBotti.Interfaces.Models;

    public class GetScore : IGetScore
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = default!;
        public int Score { get; set; }

        public override string ToString()
        {
            return (UserName ?? "no username") + ": " + Score;
        }

        public string ToStringRow(int length)
        {
            var result = (UserName ?? "no username");
            result = result.PadRight(length);
            result = result.Substring(0, length - Score.ToString().Length - 1);
            result += " ";
            result += Score;

            return result;
        }
    }
}