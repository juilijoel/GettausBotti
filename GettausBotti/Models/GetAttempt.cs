﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GettausBotti.Models
{
    public class GetAttempt
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
