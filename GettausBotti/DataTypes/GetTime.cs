using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GettausBotti.DataTypes
{
    public class GetTime
    {
        public int Hour { get; set; }
        public int Minute { get; set; }

        //PenaltyZone means previous minute before GetTime
        public GetTime PenaltyZone()
        {
            if (Minute == 0)
            {
                if (Hour == 0)
                {
                    return new GetTime()
                    {
                        Hour = 23,
                        Minute = 59
                    };
                }
                else
                {
                    return new GetTime()
                    {
                        Hour = this.Hour - 1,
                        Minute = 59
                    };
                }
            }

            return new GetTime()
            {
                Hour = this.Hour,
                Minute = this.Minute - 1
            };
        }
    }
}
