namespace GettausBotti
{
    using GettausBotti.DataTypes;
    using GettausBotti.Models;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Telegram.Bot;
    using Telegram.Bot.Exceptions;
    using Telegram.Bot.Polling;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;

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
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");


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

            using CancellationTokenSource cts = new();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = [] // receive all update types except ChatMember related updates
            };


            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            Thread.Sleep(int.MaxValue);
        }


        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                //Do nothing if no command in message
                if (update.Message.EntityValues == null) return;

                Console.WriteLine($"Received a command in chat {update.Message.Chat.Id}. Message universal time: {update.Message.Date.ToUniversalTime()}");

                var command = Extensions.CommandFromMessage(update.Message, _botUser.Username);
                var currentYear = TimeZoneInfo.ConvertTimeFromUtc(update.Message.Date, _timeZoneInfo).Year;

                //Here we handle the user input
                switch (command)
                {
                    //User tried to GET
                    case "/get":
                        var response = await TryGetAsync(update.Message, cancellationToken);
                        Console.WriteLine($"Get attempt from {update.Message.From.Username}, chatId: {update.Message.Chat.Id}");
                        if (response.ResponseMessage != null)
                        {
                            await _botClient.SendTextMessageAsync(update.Message.Chat, response.ResponseMessage, replyToMessageId: update.Message.MessageId, cancellationToken: cancellationToken);
                        }
                        break;

                    case "/scores":
                        {
                            var scores = await _gr.GetScores(update.Message.Chat.Id, int.Parse(_config["topCount"]), currentYear);
                            if (scores.Count == 0)
                            {
                                await _botClient.SendTextMessageAsync(update.Message.Chat, $"No scores for {currentYear} yet :(", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                                break;
                            }
                            await _botClient.SendTextMessageAsync(update.Message.Chat, 
                                Extensions.ScoresToMessageString(scores, 
                                    _config["topHeader"], 
                                    int.Parse(_config["lineLenght"]), 
                                    currentYear), 
                                parseMode: ParseMode.Markdown);
                        }
                        break;

                    case "/alltime":
                        {
                            var scores = await _gr.GetScores(update.Message.Chat.Id, int.Parse(_config["topCount"]), null);
                            await _botClient.SendTextMessageAsync(update.Message.Chat, Extensions.ScoresToMessageString(scores, 
                                    _config["allTimeHeader"], 
                                    int.Parse(_config["lineLenght"]), null), parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                        }
                        break;

                    case "/halloffame":
                        {
                            var startingYear = int.Parse(_config["startingYear"]);
                            var rows = await _gr.GetHallOfFame(update.Message.Chat.Id, startingYear, currentYear);
                            if (rows.Count == 0)
                            {
                                if(currentYear == startingYear)
                                {
                                    await _botClient.SendTextMessageAsync(update.Message.Chat, $"🏆 The Hall of Fame opens in {startingYear + 1} 🏆", cancellationToken: cancellationToken);
                                }
                                break;
                            }
                            await _botClient.SendTextMessageAsync(update.Message.Chat, Extensions.HallOfFameToString(rows, 
                                    _config["hallOfFameHeader"], 
                                    int.Parse(_config["hallOfFameLineLength"])), parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                        }
                        break;

                    case "/reverse":
                        {
                            await _botClient.SendTextMessageAsync(update.Message.Chat, Extensions.ReverseMessageText(update.Message), cancellationToken: cancellationToken);
                        }
                        break;

                    case "/time":
                        {
                            await _botClient.SendTextMessageAsync(update.Message.Chat, Extensions.TimeMessage(_timeZoneInfo, update.Message.Date), cancellationToken: cancellationToken);
                        }   
                        break;

                    case "/gdpr":
                        {
                            bool removeEntries = Extensions.EnsureGdprRequest(update.Message, _botUser.Username);

                            if (removeEntries)
                            {
                                if (await _gr.RemoveScores(update.Message.From.Id))
                                {
                                    await _botClient.SendTextMessageAsync(update.Message.Chat, "Your data has been removed.", replyToMessageId: update.Message.MessageId, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    await _botClient.SendTextMessageAsync(update.Message.Chat, "No data to be removed.", replyToMessageId: update.Message.MessageId, cancellationToken: cancellationToken);
                                }
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(update.Message.Chat, "Type \"/gdpr delete\" to remove all of your getting data.\nThis action is permanent.", replyToMessageId: update.Message.MessageId, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
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

        private static async Task<GetResponse> TryGetAsync(Message message, CancellationToken cancellationToken)
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

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
