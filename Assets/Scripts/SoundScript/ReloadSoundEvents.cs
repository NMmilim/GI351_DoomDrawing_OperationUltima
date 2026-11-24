using UnityEngine;

public class ReloadSoundEvents : MonoBehaviour
{
    public void MagOut()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        SoundManager.Instance.PlaySound3D("MagOut",transform.position);
    }
    public void MagIn()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        SoundManager.Instance.PlaySound3D("MagIn", transform.position);
    }
    public void SlideRelease()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        SoundManager.Instance.PlaySound3D("SlideRelease", transform.position);
    }
}