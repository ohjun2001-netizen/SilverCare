// Assets/Scripts/Lobby/ProfilePanel.cs
// 담당: 양석원
// TODO: TextMeshPro 설치 후 UI 연결 (Package Manager → TextMeshPro)
using UnityEngine;
using UnityEngine.UI;
using SilverCare.Common;

namespace SilverCare.Lobby
{
    public class ProfilePanel : MonoBehaviour
    {
        [Header("Input UI")]
        [SerializeField] private InputField nameInputField;
        [SerializeField] private InputField ageInputField;

        public void SaveProfile()
        {
            // 나이 파싱 로직
            int parsedAge = 65;
            if (ageInputField != null && int.TryParse(ageInputField.text, out int age))
            {
                parsedAge = age;
            }

            // 이름 설정 로직
            string inputName = "어르신";
            if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
            {
                inputName = nameInputField.text;
            }

            var profile = new PlayerProfile
            {
                playerName = inputName,
                age        = parsedAge,
                gender     = "None", // D-1 MVP 생략
                difficulty = "easy"  // D-1 MVP 생략
            };
            
            PlayerDataManager.Instance?.SaveProfile(profile);
            gameObject.SetActive(false);
        }
    }
}
        
        /*
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
        */
