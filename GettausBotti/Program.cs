using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace GettausBotti
{
    class Program
    {
        static ITelegramBotClient botClient;
        static List<GetTime> getTimes;

        static public void Main(string[] args)
        {
            //Set config file
#if DEBUG
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings_debug.json");
#else
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
#endif
            var config = builder.Build();

            //Set getting times
            getTimes = config.GetSection("getTimes").GetChildren().Select(gt => new GetTime
            {
                Hour = int.Parse(gt.Value.Split(":")[0]),
                Minute = int.Parse(gt.Value.Split(":")[1]),
            }).ToList();

            //Init bot client
            botClient = new TelegramBotClient(config["accessToken"]);
            var me = botClient.GetMeAsync().Result;

            Console.WriteLine(
              $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );

            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();
            Thread.Sleep(int.MaxValue);
        }


        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Message.Text != null)
                {
                    Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}. Message universal time: {e.Message.Date.ToUniversalTime()}");

                    //Here we handle the user input
                    switch (e.Message.Text)
                    {
                        //User tried to GET
                        case "/get":
                            if (TryGet(e.Message))
                            {
                                await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: "nice", replyToMessageId: e.Message.MessageId);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: "shit get", replyToMessageId: e.Message.MessageId);
                            }

                            break;
                    }     
                }
            }
            catch(Exception ex){
                Console.WriteLine(ex.Message);
            }
        }

        static bool TryGet(Message message)
        {
            var messageLocalTime = message.Date.ToUniversalTime();

            return getTimes.Any(gt => gt.Hour == messageLocalTime.Hour && gt.Minute == messageLocalTime.Minute);
        }
    }

    public class GetTime
    {
        public int Hour { get; set; }
        public int Minute { get; set; }
    }
}
