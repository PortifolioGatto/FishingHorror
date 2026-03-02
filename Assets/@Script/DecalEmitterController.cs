using DG.Tweening;
using UnityEngine;

public class DecalEmitterController : MonoBehaviour
{
    private UnderwaterDecalEmitter _decalEmitter;

    private void Awake()
    {
        _decalEmitter = GetComponent<UnderwaterDecalEmitter>();
    }

    public void TweenOpacity(float targetOpacity)
    {
        DOTween.To(() => _decalEmitter.opacity, x => _decalEmitter.opacity = x, targetOpacity, .33f)
            .SetEase(Ease.InOutSine).SetDelay(.33f);

        DOTween.To(() => _decalEmitter.waveDistortion, x => _decalEmitter.waveDistortion = x, 1f, .33f)
            .SetEase(Ease.InOutSine).SetDelay(.33f);

        transform.DOMoveY(transform.position.y - 10f, .33f).SetEase(Ease.InOutSine).SetDelay(.33f);

        AudioManager.Instance.PlaySFX("splashinsmall", transform.position, .5f);
        AudioManager.Instance.PlaySFX("creppylaugh", transform.position, .25f, .5f);

        Destroy(gameObject, 2f);
    }
}
