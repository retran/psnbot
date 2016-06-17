using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Telegram
{
    public class Update
    {
        [JsonProperty("update_id")]
        public long UpdateId;

        [JsonProperty("message")]
        public Message Message;

        [JsonProperty("inline_query")]
        public InlineQuery InlineQuery;

        [JsonProperty("chosen_inline_result")]
        public ChosenInlineResult ChosenInlineResult;

        [JsonProperty("callback_query")]
        public CallbackQuery CallbackQuery;
    }
}
