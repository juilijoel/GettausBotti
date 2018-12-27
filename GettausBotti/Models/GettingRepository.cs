using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;

namespace GettausBotti.Models
{
    public class GettingRepository
    {
        public async System.Threading.Tasks.Task<bool> SaveIfFirstGetOfMinuteAsync(Message paMessage)
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
                        TimeStamp = paMessage.Date
                    });

                    await ctx.SaveChangesAsync();

                    return true;
                }

                return false;
            }
        }
    }
}
