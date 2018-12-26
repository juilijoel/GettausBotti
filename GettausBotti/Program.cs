using Microsoft.Extensions.Configuration;
using System;
using System.IO;
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

        static void Main(string[] args)
        {
            //Set config file
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            //Init bot client
            botClient = new TelegramBotClient(configuration["accessToken"]);
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
                    Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}.");

                    //Here we handle the user input
                    switch (e.Message.Text)
                    {
                        //User tried to GET
                        case "/get":
                            if (TryGet(e.Message))
                            {
                                await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: "nice", replyToMessageId: e.Message.MessageId);
                            }
                            break;
                    }     
                }
            }
            catch(Exception ex){
                Console.WriteLine(ex.Message);
            }
        }

        private static bool TryGet(Message message)
        {
            var messageLocalTime = message.Date.ToLocalTime();

            return (messageLocalTime.Hour == 16 && messageLocalTime.Minute == 20);
        }
    }
}
