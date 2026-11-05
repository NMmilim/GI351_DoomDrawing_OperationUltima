using UnityEngine;
using UnityEngine.InputSystem;

public class Game : MonoBehaviour
{
    void Start()
    {

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
