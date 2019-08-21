using System;
using System.Collections.Generic;
using System.Text;

namespace GettausBotti.DataTypes
{
    public class GetScore
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public int Score { get; set; }

        public override string ToString()
        {
            return (UserName ?? "no username") + ": " + Score;
        }

        public string ToScoreLine(int length)
        {
            var result = (UserName ?? "no username");
            result = result.PadRight(length);
            result = result.Substring(0, length - Score.ToString().Length - 1);
            result += " ";
            result += Score;

            return result;
        }
    }
}