namespace GettausBotti.Library.Extensions
{
    using GettausBotti.Interfaces.Models;
    using GettausBotti.Library.Models;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Telegram.Bot.Types;

    public static class Extensions
    {
        public static string ScoresToMessageString(IEnumerable<IGetScore> scores, string header, int lineLength, int? year)
        {
            var yearString = year != null ? year.ToString() : "";
            header = header.Replace("{year}", yearString);
            var resultString = $"** {header} **\n";

            //Monospace markdown
            resultString += "```\n";

            foreach (var score in scores)
            {
                resultString += score.ToStringRow(lineLength);
                resultString += "\n";
            }

            //End monospace
            resultString += "```";

            return resultString;
        }

        public static string HallOfFameToString(IEnumerable<IFameRow> rows, string header, int lineLength)
        {
            var resultString = $"** {header} **\n";

            //Monospace markdown
            resultString += "```\n";

            foreach (var row in rows)
            {
                resultString += row.ToStringRow(lineLength);
                resultString += "\n";
            }

            //End monospace
            resultString += "```";

            return resultString;
        }

        public static List<IGetObject> GetGetTimes(IConfiguration config)
        {
            return config.GetSection("gets").GetChildren().Select(g => new GetObject
            {
                Hour = int.Parse(g.GetSection("time").Value.Split(":")[0]),
                Minute = int.Parse(g.GetSection("time").Value.Split(":")[1]),
                Messages = g.GetSection("messages").GetChildren().Select(m => m.Value.ToString()).ToList()
            }).Select(x => x as IGetObject).ToList();
        }

        public static string ReverseMessageText(Message message)
        {
            return ReverseGraphemeClusters(message.Text.Substring(message.EntityValues.FirstOrDefault().Length));
        }

        private static IEnumerable<string> GraphemeClusters(this string s)
        {
            var enumerator = StringInfo.GetTextElementEnumerator(s);
            while (enumerator.MoveNext())
            {
                yield return (string)enumerator.Current;
            }
        }

        private static string ReverseGraphemeClusters(this string s)
        {
            return string.Join("", s.GraphemeClusters().Reverse().ToArray());
        }

        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }

        public static string TimeMessage(TimeZoneInfo paTimeZoneInfo, DateTime paDateTime)
        {
            var resultTime = TimeZoneInfo.ConvertTimeFromUtc(paDateTime, paTimeZoneInfo);
            return $"time is: {resultTime:HH:mm:ss}";
        }

        public static string? CommandFromMessage(Message message, string botName)
        {
            var split = message.EntityValues.FirstOrDefault().Split("@");

            if (split.Length == 1)
            {
                return split[0].ToLower();
            }
            if (split.Length > 1 && split[1].Trim() == botName)
            {
                return split[0].ToLower();
            }

            return null;
        }

        public static bool EnsureGdprRequest(Message message, string botName)
        {
            var trimmedMessage = message.Text.ToLower().Trim();

            return trimmedMessage == "/gdpr delete" || trimmedMessage == $"/gdpr@{botName.ToLower()} delete";
        }
    }
}