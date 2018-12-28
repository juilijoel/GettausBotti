using GettausBotti.DataTypes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GettausBotti
{
    public static class Helper
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

        public static List<GetTime> GetGetTimes(IConfigurationRoot config)
        {
            return config.GetSection("getTimes").GetChildren().Select(gt => new GetTime
            {
                Hour = int.Parse(gt.Value.Split(":")[0]),
                Minute = int.Parse(gt.Value.Split(":")[1]),
            }).ToList();
        }
    }
}
