using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace GettausBotti.DataTypes
{
    public class Command
    {
        public enum CommandEnum
        {
            Get,
            Scores,
            Reverse,
            Time,
            Gdpr
        }
        public Command(Message message, string botName)
        {
            var splitMessage = message.EntityValues.FirstOrDefault().Split(" ");
            var splitCommandKey = splitMessage[0].Split("@");


            if (splitCommandKey.Length == 1)
            {
                CommandWord = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(splitCommandKey[0].ToLower());
            }
            else if (splitCommandKey.Length > 1 && splitCommandKey[1].Trim() == botName)
            {
                CommandWord = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(splitCommandKey[0].ToLower());
            }

            Enum.TryParse(CommandWord, out CommandEnum tempEnum);
            CommandType = tempEnum;

            Params = splitMessage.Skip(1).ToList();
        }

        public CommandEnum CommandType { get; set; }
        public string CommandWord { get; set; }

        public List<string> Params { get; set; }
    }
}