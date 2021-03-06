﻿using GettausBotti.DataTypes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Telegram.Bot.Types;

namespace GettausBotti
{
    public static class Extensions
    {
        public static string ScoresToMessageString(List<GetScore> scores, string header, int lineLength, int? year)
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

        public static string HallOfFameToString(List<FameRow> rows, string header, int lineLength)
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

        public static List<GetObject> GetGetTimes(IConfigurationRoot config)
        {
            return config.GetSection("gets").GetChildren().Select(g => new GetObject
            {
                Hour = int.Parse(g.GetSection("time").Value.Split(":")[0]),
                Minute = int.Parse(g.GetSection("time").Value.Split(":")[1]),
                Messages = g.GetSection("messages").GetChildren().Select(m => m.Value.ToString()).ToList()
            }).ToList();
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

        public static string CommandFromMessage(Message message, string botName)
        {
            var splitted = message.EntityValues.FirstOrDefault().Split("@");

            if (splitted.Length == 1)
            {
                return splitted[0].ToLower();
            }
            if (splitted.Length > 1 && splitted[1].Trim() == botName)
            {
                return splitted[0].ToLower();
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
