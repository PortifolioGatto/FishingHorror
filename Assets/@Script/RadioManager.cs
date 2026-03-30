using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class RadioManager : MonoBehaviour
{
    public RadioDialogue testDialogue;

    public GameObject radioObj;

    [Header("Input")]
    [SerializeField] private InputActionReference skipTextInput;

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

    private bool skipPressed;

    public static RadioManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        dialoguePanel.SetActive(false);

        skipTextInput.action.Enable();
        skipTextInput.action.performed += _ => skipPressed = true;
        skipTextInput.action.canceled += _ => skipPressed = false;
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
            skipPressed = false;

            RadioDialogue.DialogueLine line = dialogue.dialogueLines[lineIndex];

            if(line.audioClip != null)
            {
                AudioManager.Instance.PlaySFX(line.audioClip, radioObj.transform.position, 1f, 0f);
            }

            string localizedName = line.name_.GetLocalizedString();
            string localizedText = line.text_.GetLocalizedString();

            dialogueText.text = (localizedText == string.Empty) ? "" : localizedName + ": ";
            foreach (char c in localizedText)
            {
                dialogueText.text += c;

                if(skipPressed)
                {
                    dialogueText.text = ((localizedName == string.Empty) ? "" : localizedName + ": ") + localizedText;
                    break;
                }

                yield return new WaitForSeconds(typewriterSpeed);
            }

            skipPressed = false;

            while (!skipPressed)
            {
                yield return null;
            }

            lineIndex++;
        }

        yield return dialogueCanvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();

        whiteNoise.Stop();

        dialoguePanel.SetActive(false);
    }
}
