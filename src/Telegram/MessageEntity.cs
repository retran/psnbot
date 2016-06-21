using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class MessageEntity
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("offset")]
        public long Offset;

        [JsonProperty("length")]
        public long Length;

        [JsonProperty("url")]
        public string Url;
    }
}