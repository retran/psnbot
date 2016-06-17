using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class Message
    {
        [JsonProperty("message_id")]
        public long MessageId;

        [JsonProperty("from")]
        public User From;

        [JsonProperty("date")]
        public long Date;

        [JsonProperty("chat")]
        public Chat Chat;

        [JsonProperty("forward_from")]
        public User ForwardFrom;

        [JsonProperty("reply_to_message")]
        public Message ReplyToMessage;

        [JsonProperty("text")]
        public string Text;

        [JsonProperty("entities")]
        public MessageEntity[] Entities;

        [JsonProperty("audio")]
        public Audio Audio;

        [JsonProperty("document")]
        public Document Document;

        [JsonProperty("photo")]
        public PhotoSize[] Photo;

        [JsonProperty("sticker")]
        public Sticker Sticker;

        [JsonProperty("video")]
        public Video Video;

        [JsonProperty("Voice")]
        public Voice Voice;

        [JsonProperty("caption")]
        public string Caption;

        [JsonProperty("contact")]
        public Contact Contact;

        [JsonProperty("location")]
        public Location Location;

        [JsonProperty("venue")]
        public Venue Venue;

        [JsonProperty("new_chat_member")]
        public User NewChatMember;

        [JsonProperty("left_chat_member")]
        public User LeftChatMember;

        [JsonProperty("new_chat_title")]
        public string NewChatTitle;

        [JsonProperty("new_chat_photo")]
        public PhotoSize[] NewChatPhoto;

        [JsonProperty("delete_chat_photo")]
        public bool? DeleteChatPhoto;

        [JsonProperty("group_chat_created")]
        public bool? GroupChatCreated;

        [JsonProperty("supergroup_chat_created")]
        public bool? SuperGroupChatCreated;

        [JsonProperty("channel_chat_created")]
        public bool? ChannelChatCreated;

        [JsonProperty("migrate_from_chat_id")]
        public long MigrateFromChatId;

        [JsonProperty("pinned_message")]
        public Message PinnedMessage;
    }
}