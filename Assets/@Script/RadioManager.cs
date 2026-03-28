using DG.Tweening;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    public RadioDialogue testDialogue;

    public GameObject radioObj;

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
        dialogueText.text = "";

        dialogueCanvasGroup.alpha = 0f;
        dialoguePanel.SetActive(true);

        whiteNoise.Play();
        whiteNoise.time = Random.Range(0f, whiteNoise.clip.length);


        yield return dialogueCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();

        

        while (lineIndex < dialogue.dialogueLines.Length)
        {
            RadioDialogue.DialogueLine line = dialogue.dialogueLines[lineIndex];

            if(line.audioClip != null)
            {
                AudioManager.Instance.PlaySFX(line.audioClip, radioObj.transform.position, 1f, 0f);
                Debug.Log($"Playing audio clip: {line.audioClip.name}");
            }

            dialogueText.text = (line.name_.GetLocalizedString() == string.Empty) ? "" : line.name_.GetLocalizedString() + ": ";
            foreach (char c in line.text_.GetLocalizedString())
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
            yield return new WaitForSeconds((line.delay <= 0 ? 3f : line.delay)); // Wait before showing the next line
            lineIndex++;
        }

        yield return dialogueCanvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();

        whiteNoise.Stop();

        dialoguePanel.SetActive(false);
    }
}
