using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class RefereeUI : MonoBehaviour
{
    public TMP_InputField wordInput;
    public Button startButton, revealButton;
    public TextMeshProUGUI playersText, statusText;

    private string room;

    void Start()
    {
        room = PlayerPrefs.GetString("room");
        statusText.text = $"Room: {room}";

        ListOfPlayers();

        startButton.onClick.AddListener(() =>
        {
            string word = wordInput.text;
            FirebaseManager.Instance.StartGame(room, word);
            statusText.text = "Game Started";
        });

        revealButton.onClick.AddListener(() =>
        {
            FirebaseManager.Instance.RevealImposter();
        });
    }

    private void ListOfPlayers()
    {
        print("list");
        FirebaseManager.Instance.ListenForPlayers(room, (players) =>
        {
            if (players == null || players.Count == 0)
            {
                playersText.text = "Waiting for players...";
            }
            else
            {
                playersText.text = "Players:\n" + string.Join("\n", players.Select(p => $"{p.playerName} - {(p.isImposter ? "Imposter" : "Not Imposter")}"));
            }
        });
    }
}
