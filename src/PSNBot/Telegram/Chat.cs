using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class Chat
    {
        [JsonProperty("id")]
        public long Id;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("username")]
        public string Username;

        [JsonProperty("first_name")]
        public string FirstName;

        [JsonProperty("last_name")]
        public string LastName;
    }
}