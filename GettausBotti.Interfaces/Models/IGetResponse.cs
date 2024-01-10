namespace GettausBotti.Interfaces.Models
{
    public interface IGetResponse
    {
        bool IsGet { get; }
        string? ResponseMessage { get; }
    }
}