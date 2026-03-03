using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public Image healthBar;
    public float healthamount = 100f;

    public TMP_Text ammotext;
    public int AmmoCount = 100;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
  

        ammotext.text = "Ammo Left: " + AmmoCount;
    }

    public void TakeDamage (float damage)
    {
        healthamount -= damage;
        healthBar.fillAmount = healthamount / 100f;
    }

    public void Heal(float Healamount)
    {
        healthamount += Healamount;
        healthamount = Mathf.Clamp(healthamount, 0, 100);
        healthBar.fillAmount = healthamount / 100f;
    }

    public void AmmoUp(int ammoup)
    {
        AmmoCount += ammoup;
    }

}
