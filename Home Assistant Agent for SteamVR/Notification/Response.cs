﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home_Assistant_Agent_for_SteamVR.Notification
{
    class Response
    {
        public Response(string nonce, string message, string error) {
            this.nonce = nonce;
            this.message = message;
            this.error = error;
        }

        public string nonce = "";
        public string message = "";
        public string error = "";
    }
}
