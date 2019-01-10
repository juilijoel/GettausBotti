using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace GettausBotti.DataTypes
{
    public class Penalty
    {
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public DateTime TimeStamp { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class PenaltyBox
    {
        private readonly Dictionary<Tuple<long, long>, Penalty> _penalties;

        public PenaltyBox()
        {
            _penalties = new Dictionary<Tuple<long, long>, Penalty>();
        }

        public TimeSpan AddPenalty(long userId, long chatId, DateTime timeStamp, TimeSpan duration)
        {
            CleanPenalties(timeStamp);
            var key = new Tuple<long, long>(chatId, userId);

            if (_penalties.ContainsKey(key))
            {
                return _penalties[key].Duration += duration;
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

        public TimeSpan CheckPenalty(long userId, long chatId, DateTime timeStamp)
        {
            CleanPenalties(timeStamp);
            var key = new Tuple<long, long>(chatId, userId);

            if (_penalties.ContainsKey(key))
            {
                return _penalties[key].Duration - (_penalties[key].TimeStamp - timeStamp);
            }

            return TimeSpan.Zero;
        }

        private void CleanPenalties(DateTime currentTimeStamp)
        {
            var toRemove = _penalties.Where(p => p.Value.TimeStamp.Add(p.Value.Duration) < currentTimeStamp).Select(p => p.Key);

            foreach (var key in toRemove)
            {
                _penalties.Remove(key);
            }
        }
    }


}