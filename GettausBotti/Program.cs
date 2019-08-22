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
using Telegram.Bot.Types.Enums;

namespace GettausBotti
{
    class Program
    {
        static ITelegramBotClient _botClient;
        static List<GetObject> _getTimes;
        static List<string> _failMessages;
        static GettingRepository _gr;
        private static User _botUser;
        private static IConfigurationRoot _config;
        private static TimeZoneInfo _timeZoneInfo;
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
            _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(_config["timezone"]);
            _getTimes = Extensions.GetGetTimes(_config);
            _failMessages = _config.GetSection("failMessages").GetChildren().Select(fm => fm.Value.ToString()).ToList();
            _gr = new GettingRepository();
            _pb = new PenaltyBox();

            //Init bot client
            _botClient = new TelegramBotClient(_config["accessToken"]);
            _botUser = _botClient.GetMeAsync().Result;

            Console.WriteLine(
              $"Hello, World! I am user {_botUser.Id} and my name is {_botUser.FirstName}."
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

                var command = Extensions.CommandFromMessage(e.Message, _botUser.Username);

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
                            var scores = await _gr.GetScores(e.Message.Chat.Id, int.Parse(_config["topCount"]));
                            await _botClient.SendTextMessageAsync(e.Message.Chat, Extensions.ScoresToMessageString(scores, _config["topHeader"], int.Parse(_config["lineLenght"])), parseMode: ParseMode.Markdown);
                        }
                        break;

                    case "/reverse":
                        {
                            await _botClient.SendTextMessageAsync(e.Message.Chat, Extensions.ReverseMessageText(e.Message));
                        }
                        break;

                    case "/time":
                        {
                            await _botClient.SendTextMessageAsync(e.Message.Chat, Extensions.TimeMessage(_timeZoneInfo, e.Message.Date));
                        }   
                        break;

                    case "/gdpr":
                        {
                            bool removeEntries = Extensions.EnsureGdprRequest(e.Message, _botUser.Username);

                            if (removeEntries)
                            {
                                if (await _gr.RemoveScores(e.Message.Contact.UserId))
                                {
                                    await _botClient.SendTextMessageAsync(e.Message.Chat,
                                        "Your data has been removed.",
                                        replyToMessageId: e.Message.MessageId);
                                }
                                else
                                {
                                    await _botClient.SendTextMessageAsync(e.Message.Chat,
                                        "No data to be removed.",
                                        replyToMessageId: e.Message.MessageId);
                                }
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(e.Message.Chat, 
                                    "Type \"/gdpr delete\" to remove all of your getting data.\nThis action is permanent.", 
                                    replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Markdown);
                            }
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
            var messageLocalTime = TimeZoneInfo.ConvertTimeFromUtc(message.Date, _timeZoneInfo);
            TimeSpan penaltyDuration = _pb.HasPenalty(message.From.Id, message.Chat.Id, messageLocalTime);

            //If user has active penalty, return current penalty 
            if (penaltyDuration != TimeSpan.Zero)
            {
                return new GetResponse()
                {
                    IsGet = false,
                    ResponseMessage = penaltyDuration.Seconds + "s penalty remaining"
                };
            };

            //If get minute is on penalty zone (one minute before GetObject), then punish
            if (_getTimes.Any(gt => gt.CheckPenalty(messageLocalTime)))
            {
                penaltyDuration = _pb.AddPenalty(message.From.Id, message.Chat.Id, messageLocalTime,
                    TimeSpan.FromSeconds(int.Parse(_config["penaltyDuration"])));

                return new GetResponse
                {
                    IsGet = false,
                    ResponseMessage = _failMessages.PickRandom() + ", " + penaltyDuration.TotalSeconds + "s penalty"
                };
            }

            var successfulGetObject = _getTimes.FirstOrDefault(gt => gt.CheckGet(messageLocalTime));

            //If get minute is right, we try if it's the first attempt of current minute
            if (successfulGetObject != null)
            {
                 return await _gr.SaveIfFirstGetOfMinuteAsync(message, successfulGetObject);
            }

            return new GetResponse
            {
                IsGet = false,
                ResponseMessage = _failMessages.PickRandom()
            };
        }
    }
}
