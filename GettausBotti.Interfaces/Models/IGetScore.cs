namespace GettausBotti.Interfaces.Models
{
    public interface IGetScore
    {
        long UserId { get; }
        string UserName { get; }
        int Score { get; }

        string ToStringRow(int length);
    }
}