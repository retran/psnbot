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
        public string Title { get; private set; }

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
            var title = targets.FirstOrDefault(t => t.Type == "TITLE_NAME");

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

            if (title != null)
            {
                Title = title.Meta;
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
            var title = targets.FirstOrDefault(t => t.Type == "TITLE_NAME");

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

            if (title != null)
            {
                Title = title.Meta;
            }
        }

        public string GetTelegramMessage()
        {
            
            StringBuilder sb = new StringBuilder();

            var name = Account.TelegramName;
            if (string.IsNullOrEmpty(name))
            {
                name = Account.PSNName;
            }
            else
            {
                name = "@" + name + " (" + Account.PSNName + ")";
            }

            sb.Append("🎂🎂🎂 Поздравляем пользователя " + name  +" с получением" + (string.IsNullOrEmpty(Name) ? " скрытого" : "") + " приза в игре " + Title + "!");
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
