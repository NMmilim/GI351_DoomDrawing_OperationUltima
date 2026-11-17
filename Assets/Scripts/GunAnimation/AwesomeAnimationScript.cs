using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AwesomeAnimationScript : MonoBehaviour
{
    public Animator anim1;
    public Animator anim2;
    //placeholder animation script
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            anim1.SetTrigger("Shoot");
            anim2.SetTrigger("Shoot");
        }
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) //reload debug
        {
            anim1.SetTrigger("Reload");
            anim2.SetTrigger("Reload");
        }

        if (Keyboard.current != null) //sprint
        {
            bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
            anim1.SetBool("isSprinting", isSprinting);
            anim2.SetBool("isSprinting", isSprinting);
        }
    }
}
