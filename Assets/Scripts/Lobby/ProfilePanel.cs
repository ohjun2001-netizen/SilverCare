// Assets/Scripts/Lobby/ProfilePanel.cs
// 담당: 양석원
// TODO: TextMeshPro 설치 후 UI 연결 (Package Manager → TextMeshPro)
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.Lobby
{
    public class ProfilePanel : MonoBehaviour
    {
        public void SaveProfile()
        {
            // TODO: 양석원 - 이름·나이·성별·난이도 입력 UI 연결
            var profile = new PlayerProfile
            {
                playerName = "어르신",
                age        = 65,
                gender     = "",
                difficulty = "easy"
            };
            PlayerDataManager.Instance?.SaveProfile(profile);
            gameObject.SetActive(false);
        }
    }
}
