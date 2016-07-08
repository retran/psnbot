using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class CallbackQuery
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("from")]
        public User From;

        [JsonProperty("message")]
        public Message Message;

        [JsonProperty("inline_message_id")]
        public string InlineMessageId;

        [JsonProperty("data")]
        public string Data;
    }
}
