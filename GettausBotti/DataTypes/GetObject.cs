using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GettausBotti.DataTypes
{
    public class GetObject
    {
        public int Hour { get; set; }
        public int Minute { get; set; }
        private GetObject _penaltyZone;
    
        //Custom response message of the get
        public string Message { get; set; }

        public bool CheckGet(DateTime paTimeStamp)
        {
            return Hour == paTimeStamp.Hour && Minute == paTimeStamp.Minute;
        }

        public bool CheckPenalty(DateTime paTimeStamp)
        {
            if (_penaltyZone == null)
            {
                _penaltyZone = PenaltyZone();
            }

            return _penaltyZone.CheckGet(paTimeStamp);
        }

        //PenaltyZone means previous minute before GetObject
        private GetObject PenaltyZone()
        {
            if (Minute == 0)
            {
                if (Hour == 0)
                {
                    return new GetObject()
                    {
                        Hour = 23,
                        Minute = 59
                    };
                }
                else
                {
                    return new GetObject()
                    {
                        Hour = this.Hour - 1,
                        Minute = 59
                    };
                }
            }

            return new GetObject()
            {
                Hour = this.Hour,
                Minute = this.Minute - 1
            };
        }
    }
}
