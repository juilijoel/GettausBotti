namespace GettausBotti.Library.Services
{
    using GettausBotti.Interfaces.Services;
    using System;

    public class PenaltyService : IPenaltyService
    {
        public PenaltyService()
        {
            _penalties = [];
        }

        private readonly Dictionary<Tuple<long, long>, Penalty> _penalties;

        public TimeSpan AddPenalty(long userId, long chatId, DateTime timeStamp, TimeSpan duration)
        {
            var key = new Tuple<long, long>(chatId, userId);

            if (_penalties.TryGetValue(key, out Penalty value))
            {
                value.TimeStamp = timeStamp;
                value.Duration = duration;
                return duration;
            }

            _penalties.Add(key, new Penalty()
            {
                UserId = userId,
                ChatId = chatId,
                TimeStamp = timeStamp,
                Duration = duration
            });

            return duration;
        }

        public TimeSpan HasPenalty(long userId, long chatId, DateTime timeStamp)
        {
            var key = new Tuple<long, long>(chatId, userId);

            if (_penalties.TryGetValue(key, out Penalty value))
            {
                var penaltyEndTime = value.TimeStamp.Add(value.Duration);

                if (penaltyEndTime > timeStamp)
                    return penaltyEndTime - timeStamp;
            }

            return TimeSpan.Zero;
        }

        private class Penalty
        {
            public long UserId { get; set; }
            public long ChatId { get; set; }
            public DateTime TimeStamp { get; set; }
            public TimeSpan Duration { get; set; }
        }
    }
}