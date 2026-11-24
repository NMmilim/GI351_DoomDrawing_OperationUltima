using UnityEngine;

public class FoleySoundEvents : MonoBehaviour
{
  
    public void ClothMove()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        SoundManager.Instance.PlaySound3D("ClothMove", transform.position);
    }
    public void WeaponRattle()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        SoundManager.Instance.PlaySound3D("WeaponRattle", transform.position);
    }

    public void MagPouch()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        SoundManager.Instance.PlaySound3D("MagPouch", transform.position);
    }
    public void Velcro()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        SoundManager.Instance.PlaySound3D("Velcro", transform.position);
    }
}