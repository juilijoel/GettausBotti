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
        static ITelegramBotClient _botClient;
        static List<GetTime> _getTimes;
        static GettingRepository _gr;
        private static IConfigurationRoot _config;
        private static PenaltyBox _pb;

        public static void Main(string[] args)
        {
            //Set config file
#if DEBUG
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings_debug.json");
#else
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
#endif

            //Init private objects
            _config = builder.Build();
            _getTimes = Extensions.GetGetTimes(_config);
            _gr = new GettingRepository();
            _pb = new PenaltyBox();

            //Init bot client
            _botClient = new TelegramBotClient(_config["accessToken"]);
            var me = _botClient.GetMeAsync().Result;

            Console.WriteLine(
              $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );

            _botClient.OnMessage += Bot_OnMessage;
            _botClient.StartReceiving();
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
                        Console.WriteLine($"Get attempt from {e.Message.From.Username}, chatId: {e.Message.Chat.Id}");
                        if (response.ResponseMessage != null)
                        {
                            await _botClient.SendTextMessageAsync(e.Message.Chat, response.ResponseMessage, replyToMessageId: e.Message.MessageId);
                        }
                        break;
                        
                    case "/scores":
                        {
                            var scores = await _gr.GetScores(e.Message.Chat.Id);
                            await _botClient.SendTextMessageAsync(e.Message.Chat, Extensions.ScoresToMessageString(scores));
                        }
                        break;

                    case "/reverse":
                        {
                            await _botClient.SendTextMessageAsync(e.Message.Chat, Extensions.ReverseMessageText(e.Message));
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
            TimeSpan penaltyDuration = _pb.CheckPenalty(message.From.Id, message.Chat.Id, message.Date);

            //If user has active penalty, return current penalty 
            if (penaltyDuration != TimeSpan.Zero)
            {
                return new GetResponse()
                {
                    IsGet = false,
                    ResponseMessage = "Penalty remaining: " + penaltyDuration.Minutes + ":" + penaltyDuration.Seconds
                };
            };

            //If get minute is right, we try if it's the first attempt of current minute
            if(_getTimes.Any(gt => gt.Hour == messageLocalTime.Hour && gt.Minute == messageLocalTime.Minute))
            {
                 return await _gr.SaveIfFirstGetOfMinuteAsync(message);
            }

            //If get minute is wrong, PUNISH
            penaltyDuration = _pb.AddPenalty(message.From.Id, message.Chat.Id, message.Date,
                TimeSpan.FromMinutes(int.Parse(_config["penaltyDuration"])));

            return new GetResponse
            {
                IsGet = false,
                ResponseMessage = "Shit get! Penalty for " + penaltyDuration.TotalMinutes + " minutes."
            };
        }
    }
}
