using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PsnLib.Entities;

namespace PSNBot.Model
{
    public class TrophyEntry
    {
        private RecentActivityEntity.Feed entry;

        public string Source { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Event { get; set; }
        public string Name { get; set; }
        public string Detail { get; set; }
        public string Image { get; set; }
        public Account Account { get; set; }

        public TrophyEntry(Account account, RecentActivityEntity.CondensedStory story)
        {
            Account = account;
            TimeStamp = story.Date.ToUniversalTime();
            Event = story.Caption;
            Source = story.Source.Meta;

            var targets = story.Targets;

            var name = targets.FirstOrDefault(t => t.Type == "TROPHY_NAME");
            var detail = targets.FirstOrDefault(t => t.Type == "TROPHY_DETAIL");
            var url = targets.FirstOrDefault(t => t.Type == "TROPHY_IMAGE_URL");

            if (name != null)
            {
                Name = name.Meta;
            }

            if (detail != null)
            {
                Detail = detail.Meta;
            }

            if (url != null)
            {
                Image = url.Meta;
            }
        }

        public TrophyEntry(Account account, RecentActivityEntity.Feed entry)
        {
            Account = account;
            Account = account;
            TimeStamp = entry.Date.ToUniversalTime();
            Event = entry.Caption;
            Source = entry.Source.Meta;
            var targets = entry.Targets;

            var name = targets.FirstOrDefault(t => t.Type == "TROPHY_NAME");
            var detail = targets.FirstOrDefault(t => t.Type == "TROPHY_DETAIL");
            var url = targets.FirstOrDefault(t => t.Type == "TROPHY_IMAGE_URL");

            if (name != null)
            {
                Name = name.Meta;
            }

            if (detail != null)
            {
                Detail = detail.Meta;
            }

            if (url != null)
            {
                Image = url.Meta;
            }
        }

        public string GetTelegramMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("👻");
            sb.Append(Event);
            sb.Append(string.Format(" ({0})", Account.TelegramName));
            if (!string.IsNullOrEmpty(Name))
            {
                sb.Append("\n\n");
                sb.Append("<b>\"" + Name + "\"</b>");
                sb.Append("\n");
                sb.Append(Detail);
            }
            return sb.ToString();
        }
    }
}
