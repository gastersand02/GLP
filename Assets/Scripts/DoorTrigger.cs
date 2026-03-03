using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{

    public Animator dooranim;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dooranim.SetBool("IsOpen", true);
            Destroy(gameObject);
        }    
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
