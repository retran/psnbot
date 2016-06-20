using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot
{
    public class AchievementEntry
    {
        public string Source { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Event { get; set; }
        public string Name { get; set; }
        public string Detail { get; set; }
        public string Image { get; set; }
        public Account Account { get; set; }

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
