using GommeHDnetForumAPI.Models.Entities;

namespace Mitternacht.Modules.Forum.Common
{
    public class RankUpdateItem
    {
        public UserInfo OldUserInfo { get; set; }
        public UserInfo NewUserInfo { get; set; }
        public string OldRank => OldUserInfo?.UserTitle;
        public string NewRank => NewUserInfo?.UserTitle;

        public RankUpdateItem(UserInfo oldUserInfo, UserInfo newUserInfo)
        {
            OldUserInfo = oldUserInfo;
            NewUserInfo = newUserInfo;
        }
    }
}
