using System.Threading.Tasks;
using PsnLib.Entities;
using System;
using System.Globalization;

namespace PSNBot.Model
{
    public class UserEntry
    {
        private UserEntity.TrophySummary _trophies;
        private UserEntity _userEntity;

        public UserEntry(UserEntity userEntity)
        {
            _userEntity = userEntity;

            _trophies = userEntity.trophySummary;
        }

        public long GetRating()
        {
            if (_trophies != null)
            {
                return _trophies.EarnedTrophies.Platinum * 500 + _trophies.EarnedTrophies.Gold * 100 + _trophies.EarnedTrophies.Silver * 50 + _trophies.EarnedTrophies.Bronze * 10;
            }
            return 0;
        }

        public string GetStatus()
        {
            try
            {
                var user = _userEntity;

                if (user != null && user.presence != null)
                {
                    var presence = user.presence;
                    var status = "Status: " + presence.PrimaryInfo.OnlineStatus;
                    if (presence.PrimaryInfo.GameTitleInfo != null)
                    {
                        status += " (" + presence.PrimaryInfo.GameTitleInfo.TitleName + " - " + presence.PrimaryInfo.GameStatus + ")";
                    }

                    status += "\nPS Plus: " + (user.Plus ? "да" : "нет");

                    if (presence.PrimaryInfo.LastOnlineDate != null)
                    {
                        status += "\nLast seen: " + DateTime.Parse(presence.PrimaryInfo.LastOnlineDate, CultureInfo.InvariantCulture).ToString(CultureInfo.GetCultureInfo("ru-RU"));
                    }

                    //if (user.trophySummary != null)
                    //{
                    //    var trophies = user.trophySummary;
                    //    status += string.Format("\n\nBronze: {0}\n", trophies.EarnedTrophies.Bronze);
                    //    status += string.Format("Silver: {0}\n", trophies.EarnedTrophies.Silver);
                    //    status += string.Format("Gold: {0}\n", trophies.EarnedTrophies.Gold);
                    //    status += string.Format("Platinum: {0}", trophies.EarnedTrophies.Platinum);
                    //}

                    return status;
                }
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool HasPlus()
        {
            return _userEntity.Plus;
        }

        public bool GetOnline()
        {
            return _userEntity.presence != null && _userEntity.presence.PrimaryInfo != null && _userEntity.presence.PrimaryInfo.OnlineStatus == "online";
        }

        public string GetStatusLine()
        {
            var presence = _userEntity.presence;

            if (presence == null || presence.PrimaryInfo == null)
            {
                return string.Empty;
            }

            if (presence.PrimaryInfo.GameTitleInfo != null)
            {
                return  presence.PrimaryInfo.GameTitleInfo.TitleName + " - " + presence.PrimaryInfo.GameStatus;
            }

            return string.Empty;
        }

        public string GetTrophyLine()
        {
            if (_trophies != null)
            {
                return string.Format("[P {0}] [G {1}] [S {2}] [B {3}]", _trophies.EarnedTrophies.Platinum, _trophies.EarnedTrophies.Gold, _trophies.EarnedTrophies.Silver, _trophies.EarnedTrophies.Bronze);
            }
            return string.Empty;
        }

    }
}