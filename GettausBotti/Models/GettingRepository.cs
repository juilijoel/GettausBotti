using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GettausBotti.Models
{
    public class GettingRepository
    {
        public async Task<bool> SaveIfFirstGetOfMinuteAsync(Message paMessage)
        {
            using (var ctx = new GettingContext())
            {
                if(!await ctx.GetAttempts.AnyAsync(ga => ga.TimeStamp.Date == paMessage.Date.Date
                     && ga.TimeStamp.Hour == paMessage.Date.Hour
                     && ga.TimeStamp.Minute == paMessage.Date.Minute
                     && ga.TimeStamp < paMessage.Date))
                {
                    await ctx.GetAttempts.AddAsync(new GetAttempt()
                    {
                        ChatId = paMessage.Chat.Id,
                        UserId = paMessage.From.Id,
                        TimeStamp = paMessage.Date,
                        UserName = paMessage.From.Username,
                        IsGet = true
                    });

                    await ctx.SaveChangesAsync();

                    return true;
                }
                return false;
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
    }
}
