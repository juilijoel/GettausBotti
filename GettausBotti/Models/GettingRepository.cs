using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GettausBotti.Models
{
    public class GettingRepository
    {
        public async System.Threading.Tasks.Task<bool> IsFirstGetOfMinuteAsync(DateTime paTimeStamp)
        {
            using (var ctx = new GettingContext())
            {
                return !await ctx.GetAttempts.AnyAsync(ga => ga.TimeStamp.Date == paTimeStamp.Date
                    && ga.TimeStamp.Hour == paTimeStamp.Hour
                    && ga.TimeStamp.Minute == paTimeStamp.Minute
                    && ga.TimeStamp < paTimeStamp);
            }
        }
    }
}
