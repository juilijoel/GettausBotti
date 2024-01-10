namespace GettausBotti.Interfaces.Models
{
    public interface IGetObject
    {
        int Hour { get; }
        int Minute { get; }
        List<string> Messages { get; }

        /// <summary>
        /// Check if get is valid
        /// </summary>
        /// <param name="paTimeStamp"></param>
        /// <returns></returns>
        bool CheckGet(DateTime paTimeStamp);

        /// <summary>
        /// Check penalty zone
        /// </summary>
        /// <param name="paTimeStamp"></param>
        /// <returns></returns>
        bool CheckPenalty(DateTime paTimeStamp);

        string RandomMessage();
    }
}