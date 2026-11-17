using UnityEngine;
using UnityEngine.InputSystem;

public class Game : MonoBehaviour
{
    void Start()
    {
        // Automatically ensure the Wrath Boss encounter and combat systems are spawned
        if (GetComponent<WrathBossSpawner>() == null)
        {
            gameObject.AddComponent<WrathBossSpawner>();
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            // Toggle freeze time (pause / unpause)
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
            else
            {
                Time.timeScale = 0f;
            }
        }
    }
}
