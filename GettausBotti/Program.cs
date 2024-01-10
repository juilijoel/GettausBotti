namespace GettausBotti
{
    using GettausBotti.Data;
    using GettausBotti.Interfaces.Services;
    using GettausBotti.Library.Extensions;
    using GettausBotti.Library.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Telegram.Bot;
    using Telegram.Bot.Exceptions;
    using Telegram.Bot.Polling;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;

    internal class Program
    {
        private static ITelegramBotClient _botClient { get; set; }
        private static IGettingService _gr { get; set; }
        private static User _botUser { get; set; }
        private static IConfigurationRoot _config { get; set; }
        private static TimeZoneInfo _timeZoneInfo { get; set; }

        public static void Main(string[] args)
        {
            //Set config file
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>();

            var configurationRoot = configBuilder.Build();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddConfiguration(configurationRoot);
            builder.Services.AddScoped<GettingContext>();
            builder.Services.AddScoped<IGettingService, GettingService>();
            builder.Services.AddSingleton<IPenaltyService, PenaltyService>();
            using IHost host = builder.Build();

            //Init private objects
            _config = configBuilder.Build();
            _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(_config["timezone"]);
            _gr = host.Services.GetRequiredService<IGettingService>();

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
                        var response = await _gr.TryGetAsync(update.Message, cancellationToken);
                        Console.WriteLine($"Get attempt from {update.Message.From.Username}, chatId: {update.Message.Chat.Id}");
                        if (response.ResponseMessage != null)
                        {
                            await _botClient.SendTextMessageAsync(update.Message.Chat, response.ResponseMessage, replyToMessageId: update.Message.MessageId, cancellationToken: cancellationToken);
                        }
                        break;

                    case "/scores":
                        {
                            var scores = await _gr.GetScores(update.Message.Chat.Id, int.Parse(_config["topCount"]), currentYear);
                            if (!scores.Any())
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
                            if (!rows.Any())
                            {
                                if (currentYear == startingYear)
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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