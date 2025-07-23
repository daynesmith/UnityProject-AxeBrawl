using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image foregroundImage; // Green fill
    public Image backgroundImage; // Red background (optional)
    private Transform camTransform;

    public void SetHealth(float current, float max)
    {
        float fillAmount = Mathf.Clamp01(current / max);
        foregroundImage.fillAmount = fillAmount;
    }

    void LateUpdate()
    {
        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;

        if (camTransform != null)
            transform.LookAt(transform.position + camTransform.forward);
    }
}

