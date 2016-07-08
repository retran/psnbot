using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class GetUpdatesQuery
    {
        [JsonProperty("offset")]
        public long? Offset;

        [JsonProperty("limit")]
        public long? Limit;

        [JsonProperty("timeout")]
        public long? Timeout;
    }
}