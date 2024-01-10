namespace GettausBotti.Data.Entities
{
    public class GetAttempt
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = default!;
        public long ChatId { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool IsGet { get; set; }
    }
}