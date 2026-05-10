// Assets/Scripts/GoStop/GoStopUIManager.cs
// UI는 GoStopGameManager가 직접 생성·관리합니다. 이 파일은 호환성 유지용 stub입니다.
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.GoStop
{
    public class GoStopUIManager : MonoBehaviour
    {
        public void RefreshAll(List<HwatooCard> hand, List<HwatooCard> floor, List<HwatooCard> captured) { }
        public void ShowGoStopChoice() { }
        public void ShowFinalResult(int score) { }
        public void UpdateScore(int score) { }
    }
}
