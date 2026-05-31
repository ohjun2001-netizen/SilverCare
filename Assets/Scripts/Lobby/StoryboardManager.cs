using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro 전용 네임스페이스
using SilverCare.Common; // 팀의 TTSManager 호출용

[RequireComponent(typeof(CanvasGroup))] // 페이드아웃 효과를 위해 강제 추가
public class StoryboardManager : MonoBehaviour
{
    [Header("스토리 데이터 (씬 진입 시 자동 출력)")]
    [TextArea(3, 5)]
    [SerializeField] private string introMessage;
    [Tooltip("텍스트가 화면에 머무는 시간 (초)")]
    [SerializeField] private float displayDuration = 6.0f;

    [Header("VR UI 자동 위치 설정")]
    [SerializeField] private float distanceFromPlayer = 2.0f;
    [SerializeField] private float heightOffset = 1.4f;

    private TextMeshProUGUI _storyText;
    private CanvasGroup _canvasGroup;

    private IEnumerator Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _storyText = GetComponentInChildren<TextMeshProUGUI>();

        if (_storyText == null)
        {
            Debug.LogError("[System] StoryboardManager 하위에 TextMeshProUGUI가 없습니다.");
            yield break;
        }

        // 1. VR 카메라 로드 대기 (씬 로딩 직후 카메라를 못 찾는 버그 원천 차단)
        Transform camT = null;
        while (camT == null)
        {
            if (Camera.main != null) camT = Camera.main.transform;
            yield return new WaitForSeconds(0.1f);
        }

        // 2. 어르신의 정면 시야각(FOV)에 맞춰 월드 캔버스 자동 고정
        Vector3 forward = camT.forward;
        forward.y = 0f; // 캔버스가 기울어지지 않도록 수평 유지
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 targetPos = camT.position + (forward * distanceFromPlayer);
        targetPos.y = heightOffset;
        
        transform.position = targetPos;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        // 3. 텍스트 설정 및 팀 표준 TTS 재생 (서버 연동)
        _storyText.text = introMessage;
        if (!string.IsNullOrEmpty(introMessage))
        {
            TTSManager.Instance?.Speak(introMessage);
        }

        // 4. 대사 시간만큼 대기 후 UI를 부드럽게 투명하게 만듦 (Fade-Out)
        yield return new WaitForSeconds(displayDuration);
        
        float fadeTime = 1.5f;
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        
        // 시야를 가리지 않도록 캔버스 자체를 완전 비활성화
        gameObject.SetActive(false);
    }
}