using System.Collections;
using UnityEngine;
using SilverCare.Common;

public class GolfStoryIntegrator : MonoBehaviour
{
    private GameObject _resultPanel;

    private IEnumerator Start()
    {
        while (_resultPanel == null)
        {
            GameObject canvas = GameObject.Find("GolfUI_Canvas");
            if (canvas != null)
            {
                Transform resultTransform = canvas.transform.Find("ResultPanel");
                if (resultTransform != null)
                    _resultPanel = resultTransform.gameObject;
            }

            yield return new WaitForSeconds(0.5f);
        }

        while (true)
        {
            if (_resultPanel.activeInHierarchy)
            {
                bool firstClear = StoryProgressManager.Instance != null &&
                                  StoryProgressManager.Instance.TryMarkActivityCleared(StoryProgressManager.StoryActivity.Golf);

                if (firstClear)
                {
                    AudioManager.Instance?.PlayGameClear();
                    StoryProgressManager.Instance?.SpeakClearNarration(StoryProgressManager.StoryActivity.Golf);
                }

                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}
