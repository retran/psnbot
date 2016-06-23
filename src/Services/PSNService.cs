using PSNBot.Model;
using PsnLib.Entities;
using PsnLib.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Services
{
    public class PSNService
    {
        private AuthenticationManager _authManager;
        private FriendManager _friendManager;
        private RecentActivityManager _recentActivityManager;
        private UserAccountEntity _userAccountEntity;
        private MessageManager _messageManager;
        private UserManager _userManager;

        public PSNService()
        {
            _authManager = new AuthenticationManager();
            _recentActivityManager = new RecentActivityManager();
            _friendManager = new FriendManager();
            _messageManager = new MessageManager();
            _userManager = new UserManager();
        }

        public async Task<bool> Login(string username, string password)
        {
            _userAccountEntity = await _authManager.Authenticate(username, password);
            return _userAccountEntity != null;
        }

        public async Task<IEnumerable<TrophyEntry>> GetTrophies(IEnumerable<Account> accounts)
        {
            var tasks = accounts.AsParallel().Select(async account =>
            {
                var activity = await _recentActivityManager.GetActivityFeed(account.PSNName, 0, false, false, _userAccountEntity);

                if (activity != null)
                {
                    return GetTrophiesImpl(activity.feed, account);
                }

                return new TrophyEntry[] { };
            }).ToArray();
            return (await Task.WhenAll(tasks)).SelectMany(_ => _);
        }

        public async Task<bool> CheckFriend(string pSNName)
        {
            var list = await _friendManager.GetFriendsList(_userAccountEntity.Entity.OnlineId, 0, false, false, false, true, false, false, false, _userAccountEntity);
            return list.FriendList != null && list.FriendList.Any(f => string.Equals(f.OnlineId, pSNName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> RemoveFriend(string psnName)
        {
            return await _friendManager.DeleteFriend(psnName, _userAccountEntity);
        }

        public async Task<bool> SendFriendRequest(string psnId)
        {
            return await _friendManager.SendFriendRequest(psnId, Messages.FriendRequestMessage, _userAccountEntity);
        }

        public async Task<UserEntry> GetUser(string psnId)
        {
            try
            {
                return new UserEntry(await _userManager.GetUser(psnId, _userAccountEntity));
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Image>> GetImages(DateTime timestamp)
        {
            try
            {
                var groups = await _messageManager.GetMessageGroup(_userAccountEntity.Entity.OnlineId, _userAccountEntity);
                if (groups == null || groups.MessageGroups == null || !groups.MessageGroups.Any())
                {
                    return new Image[] { };
                }
                var tasks = groups.MessageGroups.AsParallel().Select(async mg =>
                {
                    var result = new List<Image>();
                    var id = mg.MessageGroupId;
                    var conversation = await _messageManager.GetGroupConversation(id, _userAccountEntity);
                    if (conversation.messages != null)
                    {
                        foreach (var msg in conversation.messages.Where(m => m.contentKeys.Any(k => string.Equals(k, "image-data-0", StringComparison.OrdinalIgnoreCase))))
                        {
                            var date = DateTime.Parse(msg.receivedDate, CultureInfo.InvariantCulture).ToUniversalTime();
                            if (date > timestamp)
                            {
                                var image = await _messageManager.GetImageMessageContent(id, msg, _userAccountEntity);
                                byte[] data = new byte[image.Length];
                                image.Read(data, 0, (int)image.Length);
                                result.Add(new Image()
                                {
                                    Data = data,
                                    Source = msg.senderOnlineId,
                                    TimeStamp = date
                                });
                            }
                        }
                    }
                    return result;
                }).ToArray();
                return (await Task.WhenAll(tasks)).SelectMany(_ => _);
            }
            catch
            {
                return new Image[] { };
            }
        }

        private static IEnumerable<TrophyEntry> GetTrophiesImpl(List<RecentActivityEntity.Feed> feed, Account account)
        {
            if (feed != null)
            {
                foreach (var entry in feed.Where(e => e.StoryType == "TROPHY"))
                {
                    if (entry.CondensedStories != null && entry.CondensedStories.Any())
                    {
                        foreach (var story in entry.CondensedStories)
                        {
                            yield return new TrophyEntry(account, story);
                        }
                    }
                    else
                    {
                        yield return new TrophyEntry(account, entry);
                    }
                }
            }
        }
    }
}
