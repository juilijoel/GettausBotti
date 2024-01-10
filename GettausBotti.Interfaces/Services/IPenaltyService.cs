namespace GettausBotti.Interfaces.Services
{
    public interface IPenaltyService
    {
        /// <summary>
        /// Adds a penalty to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="chatId"></param>
        /// <param name="timeStamp"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        TimeSpan AddPenalty(long userId, long chatId, DateTime timeStamp, TimeSpan duration);

        /// <summary>
        /// Checks if the user has a penalty.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="chatId"></param>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        TimeSpan HasPenalty(long userId, long chatId, DateTime timeStamp);
    }
}