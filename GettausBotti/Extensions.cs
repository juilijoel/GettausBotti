using GettausBotti.DataTypes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            return config.GetSection("getTimes").GetChildren().Select(gt => new GetObject
            {
                Hour = int.Parse(gt.Value.Split(":")[0]),
                Minute = int.Parse(gt.Value.Split(":")[1]),
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
    }
}
