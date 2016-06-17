using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class ReplyKeyboardMarkup : ReplyMarkup
    {
        [JsonProperty("keyboard")]
        public KeyboardButton[][] Keyboard;

        [JsonProperty("resize_keyboard")]
        public bool ResizeKeyboard;

        [JsonProperty("one_time_keyboard")]
        public bool OneTimeKeyboard;

        [JsonProperty("selective")]
        public bool Selective;
    }
}
