// Assets/Scripts/Baduk/Replay/KifuLoader.cs
// JSON 파일 위치:
//   Assets/Resources/Data/kifu_sample.json
//   Assets/Resources/Data/npc_comments.json
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Baduk.Data;

namespace Baduk.Replay
{
    public class KifuLoader : MonoBehaviour
    {
        const string KIFU_PATH     = "Data/kifu_sample";
        const string COMMENTS_PATH = "Data/npc_comments";

        KifuDatabase     _kifuDb;
        NpcCommentPool   _comments;

        public int TotalKifus => _kifuDb?.total ?? 0;
        public NpcCommentPool Comments => _comments;

        void Awake()
        {
            LoadKifuDatabase();
            LoadCommentPool();
        }

        void LoadKifuDatabase()
        {
            var json = Resources.Load<TextAsset>(KIFU_PATH);
            if (json == null)
            {
                Debug.LogError($"[KifuLoader] 기보 JSON 없음: Resources/{KIFU_PATH}");
                return;
            }
            _kifuDb = JsonUtility.FromJson<KifuDatabase>(json.text);
            Debug.Log($"[KifuLoader] 기보 {_kifuDb.total}개 로드 (ver {_kifuDb.version})");
        }

        void LoadCommentPool()
        {
            var json = Resources.Load<TextAsset>(COMMENTS_PATH);
            if (json == null)
            {
                Debug.LogWarning($"[KifuLoader] 코멘트 JSON 없음: Resources/{COMMENTS_PATH} → 빈 풀로 진행");
                _comments = new NpcCommentPool();
                return;
            }
            _comments = JsonUtility.FromJson<NpcCommentPool>(json.text);
        }

        public Kifu GetKifuById(string id)
            => _kifuDb?.kifus.FirstOrDefault(k => k.id == id);

        public Kifu GetKifuByIndex(int index)
        {
            if (_kifuDb == null || _kifuDb.kifus == null) return null;
            if (index < 0 || index >= _kifuDb.kifus.Count) return null;
            return _kifuDb.kifus[index];
        }

        public List<Kifu> AllKifus => _kifuDb?.kifus ?? new List<Kifu>();
    }
}
