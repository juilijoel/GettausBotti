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
        public static string ScoresToMessageString(List<GetScore> scores)
        {
            var resultString = "";

            foreach (var score in scores)
            {
                resultString += score.ToString();
                resultString += "\n";
            }

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
            return $"time is: {resultTime:T}";
        }

        public static string CommandFromMessage(Message message, string botName)
        {
            var splitted = message.EntityValues.FirstOrDefault().Split("@");

            if (splitted.Length == 1)
            {
                return splitted[0];
            }
            if (splitted.Length > 1 && splitted[1].Trim() == botName)
            {
                return splitted[0];
            }

            return null;
        } 
    }
}
