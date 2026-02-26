using DG.Tweening;
using TMPro;
using UnityEngine;

public class SplashEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private TextMeshPro _textMeshPro;
    [SerializeField] private Vector3 textStartingPos;
    [SerializeField] private Vector3 textEndPos;

    private void Start()
    {
        //billboard the text to face Main Camera
        _textMeshPro.transform.LookAt(Camera.main.transform);
    }

    [ContextMenu("Play Splash Effect")]
    public void Play()
    {
        _particleSystem.Play();

        _textMeshPro.DOKill();

        _textMeshPro.gameObject.SetActive(true);
        
        _textMeshPro.transform.localPosition = textStartingPos;

        _textMeshPro.transform.DOLocalMove(textEndPos, 5f).SetEase(Ease.OutCubic);

        _textMeshPro.color = new Color(_textMeshPro.color.r, _textMeshPro.color.g, _textMeshPro.color.b, 1);

        _textMeshPro.DOFade(0, 5f).SetEase(Ease.OutCubic).SetDelay(0.05f).OnComplete(() =>
        {
            _textMeshPro.gameObject.SetActive(false);
        });

        Destroy(gameObject, 5.5f);
    }
}
