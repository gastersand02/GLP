using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject panel; // root panel to enable/disable
    public TMP_Text nameText;
    public TMP_Text contentText;
    public Image portraitImage;

    [Header("Choices")]
    public Transform choicesContainer;
    public Button choiceButtonPrefab;

    [Header("Settings")]
    public float showPanelFade = 0.05f;

    void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
        ClearChoices();
        SetContent("");
        SetName("");
        SetPortrait(null);
    }

    public void SetName(string text)
    {
        if (nameText != null) nameText.text = text ?? "";
    }

    public void SetContent(string text)
    {
        if (contentText != null) contentText.text = text ?? "";
    }

    public void SetPortrait(Sprite s)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = s;
            portraitImage.enabled = s != null;
        }
    }

    public void ClearChoices()
    {
        if (choicesContainer == null) return;
        foreach (Transform t in choicesContainer)
            Destroy(t.gameObject);
    }

    public Button CreateChoice(string text)
    {
        if (choiceButtonPrefab == null || choicesContainer == null) return null;
        var btn = Instantiate(choiceButtonPrefab, choicesContainer);
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = text;
        btn.gameObject.SetActive(true);
        return btn;
    }
}