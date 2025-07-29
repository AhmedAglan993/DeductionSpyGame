using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI nameText, wordText, waitingText;

    private string room, name;
    private bool wordReceived = false;

    private void Start()
    {
        room = PlayerPrefs.GetString("room");
        name = PlayerPrefs.GetString("name");
        nameText.text = "Welcome, " + name;

        // Call JoinRoom once to write to Firebase
        FirebaseManager.Instance.JoinRoom(room, name, (isImposter, word) =>
        {
            // We delay showing anything until word is actually assigned
        });

        // Start polling for the word
        InvokeRepeating(nameof(CheckWordStatus), 1f, 1f);
        InvokeRepeating(nameof(Onreveal), 1f, 1f);

        // Listen for reveal
        Onreveal();
    }

    private void Onreveal()
    {
        print(FirebaseManager.Instance.RevealedImposter);
        FirebaseManager.Instance.ListenForImposterReveal((RevealedImposter) =>
        {
            waitingText.text = "The imposter was: " + RevealedImposter;
        });
    }

    private void CheckWordStatus()
    {

        FirebaseManager.Instance.JoinRoom(room, name, (isImposter, word) =>
        {
            print(word);
            if (!string.IsNullOrEmpty(word))
            {
                wordText.text = isImposter ? "You are the IMPOSTER!" : $"Your word is: {word}";
                wordReceived = true;
            }
        });
    }
}
