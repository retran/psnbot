using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PsnLib.Managers;
using PsnLib.Entities;
using System.Threading;
using System.Globalization;

namespace PSNBot
{
    public class ImageMessage
    {
        public DateTime TimeStamp { get; set; }

        public byte[] Data { get; set; }
        public string Source { get; set; }
    }

    public class PSNClient
    {
        private AuthenticationManager _authManager;
        private FriendManager _friendManager;
        private RecentActivityManager _recentActivityManager;
        private UserAccountEntity _userAccountEntity;
        private MessageManager _messageManager;
        private UserManager _userManager;

        public PSNClient()
        {
            _authManager = new AuthenticationManager();
            _recentActivityManager = new RecentActivityManager();
            _friendManager = new FriendManager();
            _messageManager = new MessageManager();
            _userManager = new UserManager();
        }

        public void Login(string username, string password)
        {
            var task = _authManager.Authenticate(username, password);
            task.Wait();
            _userAccountEntity = task.Result;
        }

        public Task<UserEntity> GetUser(string psnName)
        {
            try
            {
                return _userManager.GetUser(psnName, _userAccountEntity);
            }
            catch
            {
                return null;
            }
        }

        public long GetRating(UserEntity.TrophySummary throphies)
        {
            return throphies.EarnedTrophies.Platinum * 500 + throphies.EarnedTrophies.Gold * 100 + throphies.EarnedTrophies.Silver * 50 + throphies.EarnedTrophies.Bronze * 10;
        }

        public long GetRating(UserEntity user)
        {
            var throphies = user.trophySummary;
            if (throphies != null)
            {
                return GetRating(throphies);
            }
            return 0;
        }

        public async Task<string> GetStatus(string psnName)
        {
            try
            {
                var user = await _userManager.GetUser(psnName, _userAccountEntity);

                if (user != null && user.presence != null)
                {
                    var presence = user.presence;
                    var status = "Status: " + presence.PrimaryInfo.OnlineStatus;
                    if (presence.PrimaryInfo.GameTitleInfo != null)
                    {
                        status += " (" + presence.PrimaryInfo.GameTitleInfo.TitleName + " - " + presence.PrimaryInfo.GameStatus + ")";
                    }

                    if (presence.PrimaryInfo.LastOnlineDate != null)
                    {
                        status += "\nLast seen: " + DateTime.Parse(presence.PrimaryInfo.LastOnlineDate, CultureInfo.InvariantCulture).ToString(CultureInfo.GetCultureInfo("ru-RU"));
                    }

                    if (user.trophySummary != null)
                    {
                        var trophies = user.trophySummary;
                        status += string.Format("\n\nBronze: {0}\n", trophies.EarnedTrophies.Bronze);
                        status += string.Format("Silver: {0}\n", trophies.EarnedTrophies.Silver);
                        status += string.Format("Gold: {0}\n", trophies.EarnedTrophies.Gold);
                        status += string.Format("Platinum: {0}\n", trophies.EarnedTrophies.Platinum);
                        status += string.Format("\n<b>Rating: {0}</b>\n", GetRating(trophies));
                    }

                    return status;
                }
            }
            catch (Exception e)
            {
                ;
            }
            return null;
        }

        public IEnumerable<ImageMessage> GetMessages(DateTime timestamp)
        {
            var result = new List<ImageMessage>();

            var messageGroup = _messageManager.GetMessageGroup(_userAccountEntity.Entity.OnlineId, _userAccountEntity);
            messageGroup.Wait();


            foreach (var mg in messageGroup.Result.MessageGroups)
            {
                var id = mg.MessageGroupId;
                var conversation = _messageManager.GetGroupConversation(id, _userAccountEntity);
                conversation.Wait();

                foreach (var msg in conversation.Result.messages.Where(m => m.contentKeys.Any(k => string.Equals(k, "image-data-0", StringComparison.OrdinalIgnoreCase))))
                {
                    var date = DateTime.Parse(msg.receivedDate, CultureInfo.InvariantCulture).ToUniversalTime();
                    if (date > timestamp)
                    {
                        var image = _messageManager.GetImageMessageContent(id, msg, _userAccountEntity);
                        image.Wait();
                        byte[] data = new byte[image.Result.Length];
                        image.Result.Read(data, 0, (int)image.Result.Length);
                        result.Add(new ImageMessage()
                        {
                            Data = data,
                            Source = msg.senderOnlineId,
                            TimeStamp = date
                        });
                    }
                }
            }
            return result;
        }

        public bool SendFriendRequest(string name)
        {
            var task = _friendManager.SendFriendRequest(name, "Привет! Это Кланк из чата PS4RUS!", _userAccountEntity);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<AchievementEntry>> GetAchievements(IEnumerable<Account> accounts)
        {
            var achievements = new List<AchievementEntry>();

            foreach (var account in accounts)
            {
                var activity = await _recentActivityManager.GetActivityFeed(account.PSNName, 0, false, false, _userAccountEntity);

                if (activity != null)
                {
                    achievements.AddRange(GetAchievementsImpl(activity.feed, account));
                }
            }

            return achievements.OrderBy(a => a.TimeStamp);
        }

        private static IEnumerable<AchievementEntry> GetAchievementsImpl(List<RecentActivityEntity.Feed> feed, Account account)
        {
            if (feed != null)
            {
                foreach (var entry in feed.Where(e => e.StoryType == "TROPHY"))
                {
                    List<RecentActivityEntity.Target> targets = null;

                    if (entry.CondensedStories != null && entry.CondensedStories.Any())
                    {
                        foreach (var story in entry.CondensedStories)
                        {
                            AchievementEntry achievementEntry = new AchievementEntry();

                            achievementEntry.Account = account;
                            achievementEntry.TimeStamp = story.Date.ToUniversalTime();
                            achievementEntry.Event = story.Caption;
                            achievementEntry.Source = story.Source.Meta;

                            targets = story.Targets;

                            var name = targets.FirstOrDefault(t => t.Type == "TROPHY_NAME");
                            var detail = targets.FirstOrDefault(t => t.Type == "TROPHY_DETAIL");
                            var url = targets.FirstOrDefault(t => t.Type == "TROPHY_IMAGE_URL");

                            if (name != null)
                            {
                                achievementEntry.Name = name.Meta;
                            }

                            if (detail != null)
                            {
                                achievementEntry.Detail = detail.Meta;
                            }

                            if (url != null)
                            {
                                achievementEntry.Image = url.Meta;
                            }

                            yield return achievementEntry;
                        }
                    }
                    else
                    {
                        AchievementEntry achievementEntry = new AchievementEntry();

                        achievementEntry.Account = account;
                        achievementEntry.TimeStamp = entry.Date.ToUniversalTime();
                        achievementEntry.Event = entry.Caption;
                        achievementEntry.Source = entry.Source.Meta;
                        targets = entry.Targets;

                        var name = targets.FirstOrDefault(t => t.Type == "TROPHY_NAME");
                        var detail = targets.FirstOrDefault(t => t.Type == "TROPHY_DETAIL");
                        var url = targets.FirstOrDefault(t => t.Type == "TROPHY_IMAGE_URL");

                        if (name != null)
                        {
                            achievementEntry.Name = name.Meta;
                        }

                        if (detail != null)
                        {
                            achievementEntry.Detail = detail.Meta;
                        }

                        if (url != null)
                        {
                            achievementEntry.Image = url.Meta;
                        }

                        yield return achievementEntry;
                    }
                }
            }
        }

        internal string GetTrophyLine(UserEntity user)
        { 
            var throphies = user.trophySummary;
            if (throphies != null)
            {
                return string.Format("[P {0}] [G {1}] [S {2}] [B {3}]", throphies.EarnedTrophies.Platinum, throphies.EarnedTrophies.Gold, throphies.EarnedTrophies.Silver, throphies.EarnedTrophies.Bronze);
            }
            return string.Empty;
        }
    }
}
