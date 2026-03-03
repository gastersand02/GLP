using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DialogueChoice
{
    [Tooltip("Displayed option text")]
    public string text;
    [Tooltip("Index of next line in the Dialogue.lines array. Use -1 to end.")]
    public int nextLineIndex = -1;
    public UnityEvent onSelect;
}

[Serializable]
public class DialogueLine
{
    [Tooltip("Name of the speaking character")]
    public string speakerName;
    [Tooltip("Optional portrait sprite")]
    public Sprite portrait;
    [TextArea(2,6)]
    [Tooltip("The line text (use \\n for new lines)")]
    public string text;
    [Tooltip("If set, the dialogue will present these choices and pause. Each choice can point to a nextLineIndex.")]
    public DialogueChoice[] choices;
    [Tooltip("If >=0, jump to this index after this line when no choice is used. Otherwise goes to next index.")]
    public int nextLineIndex = -1;
}

/// <summary>
/// Attach this component to a GameObject to configure dialogue in the Inspector.
/// DialogueManager and DialogueTrigger accept references to this component.
/// </summary>
public class DialogueData : MonoBehaviour
{
    [Tooltip("Lines for this dialogue. Each line can contain text, portrait and choices.")]
    public DialogueLine[] lines = new DialogueLine[0];
}
