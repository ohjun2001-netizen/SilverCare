// Assets/Scripts/Baduk/Replay/KifuLoader.cs
// JSON 파일 위치:
//   Assets/Resources/Data/kifu_*.json  (kifu_ 로 시작하는 파일 전부 자동 로드)
//   Assets/Resources/Data/npc_comments.json
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Baduk.Data;

namespace Baduk.Replay
{
    public class KifuLoader : MonoBehaviour
    {
        const string KIFU_FOLDER   = "Data";
        const string KIFU_PREFIX   = "kifu_";
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
            var assets = Resources.LoadAll<TextAsset>(KIFU_FOLDER)
                                  .Where(a => a.name.StartsWith(KIFU_PREFIX))
                                  .ToArray();

            if (assets.Length == 0)
            {
                Debug.LogError($"[KifuLoader] kifu_*.json 없음: Resources/{KIFU_FOLDER}/");
                _kifuDb = new KifuDatabase { kifus = new List<Kifu>() };
                return;
            }

            var all = new List<Kifu>();
            foreach (var asset in assets)
            {
                var db = JsonUtility.FromJson<KifuDatabase>(asset.text);
                if (db?.kifus != null) all.AddRange(db.kifus);
            }
            _kifuDb = new KifuDatabase { version = "0.4", total = all.Count, kifus = all };
            Debug.Log($"[KifuLoader] 기보 {all.Count}개 로드 ({assets.Length}개 파일)");
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
