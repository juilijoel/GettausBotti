namespace GettausBotti.Library.Services
{
    using GettausBotti.Data;
    using GettausBotti.Data.Entities;
    using GettausBotti.Interfaces.Models;
    using GettausBotti.Interfaces.Services;
    using GettausBotti.Library.Extensions;
    using GettausBotti.Library.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System;
    using Telegram.Bot.Types;

    public class GettingService : IGettingService
    {
        public GettingService(IConfiguration config,
            GettingContext gettingContext,
            IPenaltyService penaltyService)
        {
            Config = config;
            GettingContext = gettingContext;

            FatBoySlim = new SemaphoreSlim(1);
            PenaltyService = penaltyService;
        }

        private IConfiguration Config { get; }
        private GettingContext GettingContext { get; }
        private IPenaltyService PenaltyService { get; }
        private SemaphoreSlim FatBoySlim { get; }

        public async Task<IGetResponse> TryGetAsync(Message message, CancellationToken cancellationToken)
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(Config["timezone"]);
            var getTimes = Extensions.GetGetTimes(Config);
            var failMessages = Config.GetSection("failMessages").GetChildren().Select(m => m.Value.ToString()).ToList();

            var messageLocalTime = TimeZoneInfo.ConvertTimeFromUtc(message.Date, timeZoneInfo);
            TimeSpan penaltyDuration = PenaltyService.HasPenalty(message.From.Id, message.Chat.Id, messageLocalTime);

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
            if (getTimes.Any(gt => gt.CheckPenalty(messageLocalTime)))
            {
                penaltyDuration = PenaltyService.AddPenalty(message.From.Id, message.Chat.Id, messageLocalTime,
                    TimeSpan.FromSeconds(int.Parse(Config["penaltyDuration"])));

                return new GetResponse
                {
                    IsGet = false,
                    ResponseMessage = failMessages.PickRandom() + ", " + penaltyDuration.TotalSeconds + "s penalty"
                };
            }

            var successfulGetObject = getTimes.FirstOrDefault(gt => gt.CheckGet(messageLocalTime));

            //If get minute is right, we try if it's the first attempt of current minute
            if (successfulGetObject != null)
            {
                return await SaveIfFirstGetOfMinuteAsync(message, successfulGetObject);
            }

            return new GetResponse
            {
                IsGet = false,
                ResponseMessage = failMessages.PickRandom()
            };
        }

        public async Task<IGetResponse> SaveIfFirstGetOfMinuteAsync(Message paMessage, IGetObject paGetObject)
        {
            try
            {
                await FatBoySlim.WaitAsync();

                //return false if a previous get exists in the chat
                if (await GettingContext.GetAttempts.AnyAsync(ga => ga.ChatId == paMessage.Chat.Id
                                                         && ga.TimeStamp.Date == paMessage.Date.Date
                                                         && ga.TimeStamp.Hour == paMessage.Date.Hour
                                                         && ga.TimeStamp.Minute == paMessage.Date.Minute
                                                         && ga.TimeStamp <= paMessage.Date))
                {
                    return new GetResponse()
                    {
                        IsGet = false
                    };
                }

                await GettingContext.GetAttempts.AddAsync(new GetAttempt()
                {
                    ChatId = paMessage.Chat.Id,
                    UserId = paMessage.From.Id,
                    TimeStamp = paMessage.Date,
                    UserName = paMessage.From.Username,
                    IsGet = true
                });

                await GettingContext.SaveChangesAsync();

                return new GetResponse()
                {
                    IsGet = true,
                    ResponseMessage = paGetObject.RandomMessage()
                };
            }
            finally
            {
                FatBoySlim.Release();
            }
        }

        public async Task<bool> SaveFailedGet(Message paMessage)
        {
            await GettingContext.GetAttempts.AddAsync(new GetAttempt()
            {
                ChatId = paMessage.Chat.Id,
                UserId = paMessage.From.Id,
                TimeStamp = paMessage.Date,
                UserName = paMessage.From.Username,
                IsGet = false
            });

            await GettingContext.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<IGetScore>> GetScores(long paChatId, int topCount, int? paYear)
        {
            return await GettingContext.GetAttempts
                .AsNoTracking()
                .Where(ga => ga.ChatId == paChatId && (paYear == null || ga.TimeStamp.Year == paYear))
                .GroupBy(ga => new { ga.ChatId, ga.UserId })
                .Where(ga => ga.Count(gag => gag.IsGet) > 0)
                .Select(ga => new GetScore
                {
                    UserId = ga.Key.UserId,
                    UserName = ga.Where(gag => gag.UserName != null)
                        .OrderByDescending(gag => gag.TimeStamp)
                        .Select(gag => gag.UserName).FirstOrDefault(),
                    Score = ga.Count(gag => gag.IsGet)
                } as IGetScore)
                .OrderByDescending(gs => gs.Score)
                .Take(topCount)
                .ToListAsync();
        }

        public async Task<IEnumerable<IFameRow>> GetHallOfFame(long paChatId, int paStartingYear, int paCurrentYear)
        {
            var getsByYear = await GettingContext.GetAttempts
                .Where(ga => ga.ChatId == paChatId && ga.TimeStamp.Year >= paStartingYear && ga.TimeStamp.Year < paCurrentYear)
                .GroupBy(ga => new { ga.ChatId, ga.UserId, ga.TimeStamp.Year })
                .Select(ga => new FameRow
                {
                    Year = ga.Key.Year,
                    UserId = ga.Key.UserId,
                    UserName = ga.Where(gag => gag.UserName != null)
                        .OrderByDescending(gag => gag.TimeStamp)
                        .Select(gag => gag.UserName).FirstOrDefault(),
                    Score = ga.Count(gag => gag.IsGet)
                }).ToListAsync();

            if (!getsByYear.Any()) return new List<FameRow>();

            var yearDict = new Dictionary<int, FameRow>();
            var tempRow = new FameRow();

            foreach (var g in getsByYear)
            {
                if (yearDict.TryGetValue(g.Year, out tempRow))
                {
                    if (g.Score > tempRow.Score)
                    {
                        yearDict[g.Year] = g;
                    }
                }
                else
                {
                    yearDict.Add(g.Year, g);
                }
            }

            return yearDict.OrderByDescending(fr => fr.Key).Select(fr => fr.Value).ToList();
        }

        public async Task<bool> RemoveScores(long paUserId)
        {
            var toBeRemoved = await GettingContext.GetAttempts.Where(ga => ga.UserId == paUserId).ToListAsync();
            if (toBeRemoved.Count == 0) return false;

            GettingContext.GetAttempts.RemoveRange(toBeRemoved);
            await GettingContext.SaveChangesAsync();

            return true;
        }
    }
}