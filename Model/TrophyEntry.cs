using System;
using System.Linq;
using System.Net;
using System.Text;
using PsnLib.Entities;

namespace PSNBot.Model
{
    public class TrophyEntry
    {
        private TrophyDetailEntity.Trophy _trophy;
        public string Source { get; set; }

        public DateTime TimeStamp { get; set; }
        public string Name { get; set; }
        public string Detail { get; set; }
        public string Image { get; set; }
        public Account Account { get; set; }
        public string Title { get; private set; }

        public string NpComm { get; private set; }

        public TrophyEntry(Account account, TrophyDetailEntity.Trophy trophy, DateTime stamp, string game, string npcomm)
        {
            Account = account;
            Name = trophy.TrophyName;
            TimeStamp = stamp;
            Detail = trophy.TrophyDetail;
            Image = trophy.TrophyIconUrl;
            Title = game;
            if (account != null)
            {
                Source = account.PSNName;
            }
            _trophy = trophy;
            NpComm = npcomm;
        }

        public string GetTelegramMessage()
        {
            var name = Account.TelegramName;
            if (string.IsNullOrEmpty(name))
            {
                name = Account.PSNName;
            }
            else
            {
                name = "@" + name + " (" + Account.PSNName + ")";
            }

            if (string.IsNullOrEmpty(Name))
            {
                return string.Format("{0} получил скрытый трофей в игре <b>{1}</b>", name, Title);
            }
            else
            {
                var link = string.Format("http://psnbot.corvusalba.ru/trophy/{0}/{1}", WebUtility.UrlEncode(_trophy.TrophyId.ToString()), WebUtility.UrlEncode(NpComm));
                return string.Format("{0} получил <a href=\"{1}\">трофей</a> в игре <b>{2}</b>", name, link, Title);
            }
        }
    }
}
