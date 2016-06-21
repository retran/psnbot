using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class Response<T>
    {
        [JsonProperty("ok")]
        public bool Ok;

        [JsonProperty("error_code")]
        public long ErrorCode;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("result")]
        public T Result;
    }
}