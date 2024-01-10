namespace GettausBotti.Library.Models
{
    using GettausBotti.Interfaces.Models;

    public class GetResponse : IGetResponse
    {
        public bool IsGet { get; set; }
        public string? ResponseMessage { get; set; }
    }
}