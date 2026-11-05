using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }
    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
 
    // Update is called once per frame
    void Update()
    {
        
    }
}
