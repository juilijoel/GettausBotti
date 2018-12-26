using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Telegram.Bot;

namespace GettausBotti
{
    class Program
    {
        static void Main(string[] args)
        {
            //Set config file
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            //Init bot client
            var botClient = new TelegramBotClient(configuration["accessToken"]);
            var me = botClient.GetMeAsync().Result;

            Console.WriteLine(
              $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );
        }
    }
}
