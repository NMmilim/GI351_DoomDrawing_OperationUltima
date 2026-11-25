using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public bool start = false;
    public AnimationCurve curve;
    public float duration = 1f;

    private Coroutine _shakeRoutine;

    private void Update()
    {
        if (start)
        {
            start = false;
            Shake();
        }
    }

    public void Shake()
    {
        Shake(1f, duration);
    }

    public void Shake(float strengthMultiplier, float shakeDuration)
    {
        if (curve == null || shakeDuration <= 0f)
        {
            return;
        }

        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
        }

        _shakeRoutine = StartCoroutine(
            Shaking(strengthMultiplier, shakeDuration));
    }

    private IEnumerator Shaking(
        float strengthMultiplier,
        float shakeDuration)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / shakeDuration);
            float strength = curve.Evaluate(progress) * strengthMultiplier;

            transform.position =
                startPosition +
                Random.insideUnitSphere * strength;

            yield return null;
        }

        transform.position = startPosition;
        _shakeRoutine = null;
    }
}