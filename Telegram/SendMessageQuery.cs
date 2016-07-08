using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class SendMessageQuery
    {
        [JsonProperty("chat_id")]
        public long ChatId;

        [JsonProperty("text")]
        public string Text;

        [JsonProperty("parse_mode", NullValueHandling = NullValueHandling.Ignore)]
        public string ParseMode;

        [JsonProperty("disable_web_page_preview")]
        public bool? DisableWebPagePreview;

        [JsonProperty("disable_notification")]
        public bool? DisableNotification;

        [JsonProperty("reply_to_message_id")]
        public long ReplyToMessageId;

        [JsonProperty("reply_markup", NullValueHandling = NullValueHandling.Ignore)]
        public ReplyMarkup ReplyMarkup;
    }

    public class SendPhotoQuery
    {
        [JsonProperty("chat_id")]
        public long ChatId;

        [JsonProperty("photo")]
        public string Photo;

        [JsonProperty("reply_to_message_id")]
        public long ReplyToMessageId;

        [JsonProperty("reply_markup", NullValueHandling = NullValueHandling.Ignore)]
        public ReplyMarkup ReplyMarkup;
    }

}