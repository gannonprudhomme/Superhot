using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class ReticleUI : MonoBehaviour  {
    [Tooltip("Image that is shown when we're not hovering")]
    public Image? ReticleImage;

    [Tooltip("RectTransform for the reticle, which we use to animate it")]
    public RectTransform? ReticleRectTransform;
    
    [Tooltip("Image that is shown when we're hovering")]
    public Image? HoveringImage;

    [Tooltip("Event we subscribe to when we're hovering over a pickup")]
    public PickupHoveringEvent? PickupHoveringEvent;
    
    [Tooltip("Event we subscribe to when we're reloading")]
    public ReloadEvent? ReloadEvent;
    
    private void Start() {
        ReticleImage!.enabled = true;
        HoveringImage!.enabled = false;
        
        PickupHoveringEvent!.OnHovering += OnHover;
        PickupHoveringEvent!.OnNotHovering += OnNotHover;
        
        ReloadEvent!.ReloadStart += OnReloadStart;
    }

    private void OnHover(PickupHoveringEvent.HoverType hoverType) {
        ReticleImage!.enabled = false;
        HoveringImage!.enabled = true;
    }
    
    private void OnNotHover() {
        ReticleImage!.enabled = true;
        HoveringImage!.enabled = false;
    }
    
    private void OnReloadStart(float duration) {
        StartCoroutine(AnimateReload(duration));
    }

    private const float reloadScale = 0.7f;
    private const float normalScale = 1f;
    private float currentRotation = 0f;
    private const float rotationIncrement = 90f;
    
    private IEnumerator AnimateReload(float duration) {
        // It first shrinks a bit (super fast)
        // then rotates 90 deg

        const float shrinkDuration = 0.1f;
        var scaleTween = ReticleRectTransform
            .DOScale(endValue: reloadScale, duration: shrinkDuration)
            .SetEase(Ease.OutSine);

        float rotationDuration = duration - shrinkDuration;
        var rotateTween = ReticleRectTransform
            .DORotate(new Vector3(0, 0, ReticleRectTransform!.rotation.eulerAngles.z + rotationIncrement), duration - shrinkDuration)
            .SetEase(Ease.OutSine);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(scaleTween);
        sequence.Append(rotateTween);

        yield return sequence.WaitForCompletion();
        
        Debug.Log("Done!");
        yield return AnimateReloadCompletion();
    }

    private IEnumerator AnimateReloadCompletion() {
        var scaleBackTween = ReticleRectTransform!
            .DOScale(endValue: normalScale, duration: 0.1f)
            .SetEase(Ease.OutElastic)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true); // Ignore timescale
        
        yield return null;
    }
}
