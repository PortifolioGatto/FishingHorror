using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "RadioDialogue", menuName = "ScriptableObjects/RadioDialogue", order = 2)]
public class RadioDialogue : ScriptableObject
{
    public DialogueLine[] dialogueLines;

    [System.Serializable]
    public class DialogueLine
    {
        public LocalizedString name_;
        public LocalizedString text_;
        [Header("Settings")]
        public float delay;
        public AudioClip audioClip;
        public DialogueLine(float delay, AudioClip audioClip)
        {
            this.delay = delay;
            this.audioClip = audioClip;
        }
    }
}
