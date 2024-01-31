namespace Home_Assistant_Agent_for_SteamVR.Notification
{
    class Response
    {
        public Response(string nonce, bool success = true, string errorCode = "", string errorMessage = "",
            string type = "result")
        {
            this.nonce = nonce;
            this.type = type;
            this.success = success;
            this.error.code = errorCode;
        }

        public string nonce;
        public string type;
        public bool success;
        public Error error = new Error();

        public class Error
        {
            public string code = "";
            public string message = "";
        }
    }

    class ResponseWithSessionId
    {
        public ResponseWithSessionId(string sessionID, Response response) {
            this.sessionID = sessionID;
            this.response = response;
        }

        public string sessionID;
        public Response response;
    }
}