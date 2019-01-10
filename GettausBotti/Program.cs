using GettausBotti.DataTypes;
using GettausBotti.Models;
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
        static GettingRepository gr;

        public static void Main(string[] args)
        {
            //Set config file
#if DEBUG
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings_debug.json");
#else
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
#endif
            var config = builder.Build();

            //Set getting times
            getTimes = Extensions.GetGetTimes(config);

            //Init database access
            gr = new GettingRepository();

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


        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                //Do nothing if no command in message
                if (e.Message.EntityValues == null) return;

                Console.WriteLine($"Received a command in chat {e.Message.Chat.Id}. Message universal time: {e.Message.Date.ToUniversalTime()}");

                var command = e.Message.EntityValues.FirstOrDefault().Split("@")[0];

                //Here we handle the user input
                switch (command)
                {
                    //User tried to GET
                    case "/get":
                        var response = await TryGetAsync(e.Message);
                        if (response.IsGet)
                        {
                            await botClient.SendTextMessageAsync(e.Message.Chat, response.ResponseMessage, replyToMessageId: e.Message.MessageId);
                            Console.WriteLine($"Successful get attempt from {e.Message.From.Username}, chatId: {e.Message.Chat.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed get attempt from {e.Message.From.Username}, chatId: {e.Message.Chat.Id}");
                            if (response.ResponseMessage != null)
                            {
                                await botClient.SendTextMessageAsync(e.Message.Chat, response.ResponseMessage, replyToMessageId: e.Message.MessageId);
                            }
                        }
                        break;
                        
                    case "/scores":
                    {
                        var scores = await gr.GetScores(e.Message.Chat.Id);
                        await botClient.SendTextMessageAsync(e.Message.Chat, Extensions.ScoresToMessageString(scores));
                    }
                        break;

                    case "/reverse":
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat, Extensions.ReverseMessageText(e.Message));
                    }
                        break;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task<GetResponse> TryGetAsync(Message message)
        {
            var messageLocalTime = message.Date.ToUniversalTime();

            //If get minute is right, we try if it's the first attempt of current minute
            if(getTimes.Any(gt => gt.Hour == messageLocalTime.Hour && gt.Minute == messageLocalTime.Minute))
            {
                 return await gr.SaveIfFirstGetOfMinuteAsync(message);
            }

            //If get minute is wrong, we save a failed attempt
            await gr.SaveFailedGet(message);
            return new GetResponse
            {
                IsGet = false,
                ResponseMessage = null
            };
        }
    }
}
