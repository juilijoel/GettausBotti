using GettausBotti.DataTypes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GettausBotti.Models
{
    public class GettingRepository
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public async Task<GetResponse> SaveIfFirstGetOfMinuteAsync(Message paMessage, GetObject paGetObject)
        {
            try
            {
                await _semaphore.WaitAsync();
                using (var ctx = new GettingContext())
                {
                    //return false if a previous get exists in the chat
                    if (await ctx.GetAttempts.AnyAsync(ga => ga.ChatId == paMessage.Chat.Id
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

                    await ctx.GetAttempts.AddAsync(new GetAttempt()
                    {
                        ChatId = paMessage.Chat.Id,
                        UserId = paMessage.From.Id,
                        TimeStamp = paMessage.Date,
                        UserName = paMessage.From.Username,
                        IsGet = true
                    });

                    await ctx.SaveChangesAsync();
                    return new GetResponse()
                    {
                        IsGet = true,
                        ResponseMessage = paGetObject.RandomMessage()
                    };
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> SaveFailedGet(Message paMessage)
        {
            using (var ctx = new GettingContext())
            {
                await ctx.GetAttempts.AddAsync(new GetAttempt()
                {
                    ChatId = paMessage.Chat.Id,
                    UserId = paMessage.From.Id,
                    TimeStamp = paMessage.Date,
                    UserName = paMessage.From.Username,
                    IsGet = false
                });

                await ctx.SaveChangesAsync();

                return true;
            }
        }

        public async Task<List<GetScore>> GetScores(long paChatId, int topCount)
        {
            using (var ctx = new GettingContext())
            {
                return await ctx.GetAttempts
                    .Where(ga => ga.ChatId == paChatId)
                    .GroupBy(ga => new { ga.ChatId, ga.UserId })
                    .Where(ga => ga.Count(gag => gag.IsGet) > 0)
                    .Select(ga => new GetScore
                    {
                        UserId = ga.Key.UserId,
                        UserName = ga.Where(gag => gag.UserName != null)
                            .OrderByDescending(gag => gag.TimeStamp)
                            .Select(gag => gag.UserName).FirstOrDefault(),
                        Score = ga.Count(gag => gag.IsGet)
                    })
                    .OrderByDescending(gs => gs.Score)
                    .Take(topCount)
                    .ToListAsync();
            }
        }

        public async Task<GetScore> GetScore(long paChatId, long paUserId)
        {
            using (var ctx = new GettingContext())
            {
                return await ctx.GetAttempts
                    .Where(ga =>
                        ga.ChatId == paChatId
                        && ga.UserId == paUserId
                        && ga.IsGet)
                    .GroupBy(ga => ga.UserId)
                    .Select(gag => new GetScore()
                    {
                        UserId = gag.FirstOrDefault().UserId,
                        Score = gag.Count(),
                        UserName = gag.FirstOrDefault().UserName
                    }).FirstOrDefaultAsync();
            }
        }
    }
}
