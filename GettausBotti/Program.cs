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


        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Message.Text != null)
                {
                    Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}. Message universal time: {e.Message.Date.ToUniversalTime()}");

                    //Here we handle the user input
                    switch (e.Message.Text.Split(" ")[0])
                    {
                        //User tried to GET
                        case "/get":
                            if (await TryGetAsync(e.Message))
                            {
                                await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: "nice", replyToMessageId: e.Message.MessageId);
                                Console.WriteLine($"Successful get attempt from {e.Message.From.Username}, chatId: {e.Message.Chat}");
                            }
                            else
                            {
                                Console.WriteLine($"Failed get attempt from {e.Message.From.Username}, chatId: {e.Message.Chat.Id}");
                            }
                            break;

                        case "/scores":
                            {
                                var scores = await gr.GetScores(e.Message.Chat.Id);
                                var scoresString = "";

                                foreach(var score in scores)
                                {
                                    scoresString += score.ToString();
                                    scoresString += "\n";
                                }

                                await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: scoresString);
                            }
                            break;
                    }     
                }
            }
            catch(Exception ex){
                Console.WriteLine(ex.Message);
            }
        }

        static async Task<bool> TryGetAsync(Message message)
        {
            var messageLocalTime = message.Date.ToUniversalTime();

            //If get minute is right, we try if it's the first attempt of current minute
            if(getTimes.Any(gt => gt.Hour == messageLocalTime.Hour && gt.Minute == messageLocalTime.Minute))
            {
                return await gr.SaveIfFirstGetOfMinuteAsync(message);
            }

            //If get minute is wrong, we save a failed attempt
            await gr.SaveFailedGet(message);
            return false;
        }
    }
}
