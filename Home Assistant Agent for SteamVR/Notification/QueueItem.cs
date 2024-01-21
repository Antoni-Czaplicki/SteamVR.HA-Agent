﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home_Assistant_Agent_for_SteamVR.Notification
{
    class QueueItem
    {
        public string sessionId = "";
        public Payload payload = new Payload();

        public QueueItem(string sessionId, Payload payload) {
            this.sessionId = sessionId;
            this.payload = payload;
        }
    }
}
