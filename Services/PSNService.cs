using PSNBot.Model;
using PsnLib.Entities;
using PsnLib.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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
        private TrophyManager _trophyManager;
        private TrophyDetailManager _trophyDetailManager;

        public PSNService()
        {
            _authManager = new AuthenticationManager();
            _recentActivityManager = new RecentActivityManager();
            _friendManager = new FriendManager();
            _messageManager = new MessageManager();
            _userManager = new UserManager();
            _trophyManager = new TrophyManager();
            _trophyDetailManager = new TrophyDetailManager();
        }

        public async Task<bool> Login(string username, string password)
        {
            _userAccountEntity = await _authManager.Authenticate(username, password);
            return _userAccountEntity != null;
        }

        public async Task<IEnumerable<TrophyEntry>> GetTrophies(Account account, DateTime lastUpdatedStamp)
        {
            var results = new List<TrophyEntry>();
            var trophies = await _trophyManager.GetTrophyList(account.PSNName, 0, _userAccountEntity);
	    int flag = 0;
	    while (flag < 10)
	    {
            if (trophies != null && trophies.TrophyTitles != null && trophies.TrophyTitles.Any())
            {
                foreach (var trophy in trophies.TrophyTitles.Where(tt => tt.ComparedUser != null && (DateTime.Parse(tt.ComparedUser.LastUpdateDate).ToUniversalTime() > lastUpdatedStamp)))
                {
		    int f = 0;
			while (f < 10)
			{
                    var details = await _trophyDetailManager.GetTrophyDetailList(trophy.NpCommunicationId, account.PSNName, true, _userAccountEntity);
                    if (details != null && details.Trophies != null && details.Trophies.Any())
                    {
                        foreach (var detail in details.Trophies.Where(t => t.ComparedUser.Earned && DateTime.Parse(t.ComparedUser.EarnedDate).ToUniversalTime() > account.LastPolledTrophy))
                        {
                            results.Add(new TrophyEntry(account, detail, DateTime.Parse(detail.ComparedUser.EarnedDate).ToUniversalTime(), trophy.TrophyTitleName, trophy.NpCommunicationId));                           
                        }
			f = 10;
                    }                        
		    else
		    {
                f++;
			Console.WriteLine("{0} Can't fetch details", DateTime.Now);	
			Thread.Sleep(5000);
		    }
			}
                }                    
		flag = 10;
            }                
	    else
	    {
            flag++;
		Console.WriteLine("{0} Can't fetch achievments for user {1}", DateTime.Now, account.PSNName);	
		Thread.Sleep(5000);
	    }
	    }
            return results.OrderBy(r => r.TimeStamp);
        }

        public async Task<TrophyEntry> GetTrophy(string npComm, int id)
        {
            var details = await _trophyDetailManager.GetTrophyDetailList(npComm, _userAccountEntity.Entity.OnlineId, true, _userAccountEntity);
            if (details != null && details.Trophies != null && details.Trophies.Any())
            {
                var detail = details.Trophies.FirstOrDefault(t => t.TrophyId == id);
                if (detail != null)
                {
                    return new TrophyEntry(null, detail, DateTime.Now, null, null);
                }                           
            }    
            return null;                    
        }

        public async Task<bool> CheckFriend(string pSNName)
        {
            int offset = 0;
            while (true)
            {
                var list = await _friendManager.GetFriendsList(_userAccountEntity.Entity.OnlineId, offset, false, false, false, true, false, false, false, _userAccountEntity);
                if (list.FriendList == null)
                {
                    return false;
                }
                offset = offset + list.FriendList.Count();
                if (list.FriendList.Any(f => string.Equals(f.OnlineId, pSNName, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                if (offset >= list.TotalResults)
                {
                    return false;
                }
            }
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
    }
}
