namespace GettausBotti.Interfaces.Models
{
    public interface IFameRow
    {
        int Year { get; }
        int Score { get; }
        long UserId { get; }
        string UserName { get; }

        string ToStringRow(int length);
    }
}