using DG.Tweening;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    public RadioDialogue testDialogue;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
    [Space]
    [SerializeField] private float fadeDuration = 0.5f;
    [Space]
    [SerializeField] private float typewriterSpeed = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioSource whiteNoise;


    public static RadioManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        dialoguePanel.SetActive(false);
    }


    public void PlayDialogue(RadioDialogue dialogue)
    {
        StartCoroutine(EPlayDialogue(dialogue));
    }

    private System.Collections.IEnumerator EPlayDialogue(RadioDialogue dialogue)
    {
        int lineIndex = 0;
        dialogueText.text = (dialogue.name == string.Empty) ? "" : dialogue.name + ": " ;

        dialogueCanvasGroup.alpha = 0f;
        dialoguePanel.SetActive(true);

        whiteNoise.Play();
        whiteNoise.time = Random.Range(0f, whiteNoise.clip.length);


        yield return dialogueCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();

        while (lineIndex < dialogue.lines.Length)
        {
            string line = dialogue.lines[lineIndex];
            dialogueText.text = (dialogue.name == string.Empty) ? "" : dialogue.name + ": ";
            foreach (char c in line)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
            yield return new WaitForSeconds(3f); // Wait before showing the next line
            lineIndex++;
        }

        yield return dialogueCanvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();

        whiteNoise.Stop();

        dialoguePanel.SetActive(false);
    }
}
