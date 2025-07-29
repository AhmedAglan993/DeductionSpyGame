using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    private const string firebaseUrl = "https://deductiongame-fd6dd-default-rtdb.firebaseio.com/";
    private string room, playerName;
    private bool isImposter = false;
    public string RevealedImposter = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }

    public void JoinRoom(string roomCode, string name, Action<bool, string> callback)
    {
        room = roomCode;
        playerName = name;
        StartCoroutine(JoinIfNotExists(callback));
    }

    IEnumerator JoinIfNotExists(Action<bool, string> callback)
    {
        string playerUrl = $"{firebaseUrl}rooms/{room}/players/{playerName}.json";
        UnityWebRequest checkReq = UnityWebRequest.Get(playerUrl);
        yield return checkReq.SendWebRequest();

        if (checkReq.result == UnityWebRequest.Result.Success && checkReq.downloadHandler.text != "null")
        {
            // Player already exists → don't overwrite, just get word and role
            Debug.Log("Player already exists, skipping re-add.");
        }
        else
        {
            // Player not found → add them with just playerName (no isImposter)
            var player = new Player { playerName = playerName };
            string json = JsonConvert.SerializeObject(player);
            UnityWebRequest addReq = UnityWebRequest.Put(playerUrl, json);
            addReq.SetRequestHeader("Content-Type", "application/json");
            yield return addReq.SendWebRequest();

            if (addReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to add player: " + addReq.error);
            }
        }

        // Start polling for word and role regardless
        StartCoroutine(GetWordAndRole(callback));
    }

    IEnumerator GetWordAndRole(Action<bool, string> callback)
    {
        while (true)
        {
            UnityWebRequest req = UnityWebRequest.Get($"{firebaseUrl}rooms/{room}.json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var json = req.downloadHandler.text;
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (data != null && data.ContainsKey("players") && data.ContainsKey("word"))
                {
                    var playersJson = data["players"].ToString();
                    var playersDict = JsonConvert.DeserializeObject<Dictionary<string, Player>>(playersJson);

                    if (playersDict.TryGetValue(playerName, out var player))
                    {
                        isImposter = player.isImposter;
                        callback(isImposter, data["word"].ToString().Trim('"'));
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(1);
        }
    }

    public void ListenForPlayers(string room, Action<List<Player>> callback)
    {
        StartCoroutine(PlayerPolling(room, callback));
    }

    IEnumerator PlayerPolling(string room, Action<List<Player>> callback)
    {
        while (true)
        {
            UnityWebRequest req = UnityWebRequest.Get($"{firebaseUrl}rooms/{room}/players.json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var jsonText = req.downloadHandler.text;

                if (string.IsNullOrEmpty(jsonText) || jsonText == "null")
                {
                    callback(null);
                }
                else
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Player>>(jsonText);
                    var players = dict.Select(kv =>
                    {
                        kv.Value.playerName = kv.Key;
                        return kv.Value;
                    }).ToList();
                    callback(players);
                }
            }

            yield return new WaitForSeconds(1);
        }
    }

    public void StartGame(string room, string word)
    {
        StartCoroutine(StartGameRoutine(room, word));
    }

    IEnumerator StartGameRoutine(string room, string word)
    {
        string url = $"{firebaseUrl}rooms/{room}/players.json";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var json = req.downloadHandler.text;
            Debug.Log(json);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, Player>>(json);
            var players = dict.Select(kv =>
            {
                kv.Value.playerName = kv.Key;
                return kv.Value;
            }).ToList();

            if (players == null || players.Count == 0)
            {
                Debug.LogError("No players found to start the game.");
                yield break;
            }

            var imposterName = players[UnityEngine.Random.Range(0, players.Count)].playerName;

            foreach (var player in players)
            {
                player.isImposter = player.playerName == imposterName;
                yield return Put($"rooms/{room}/players/{player.playerName}.json", JsonConvert.SerializeObject(player));
            }

            yield return Put($"rooms/{room}/word.json", JsonConvert.SerializeObject(word));
        }
        else
        {
            Debug.LogError("Failed to fetch players: " + req.error);
        }
    }

    public void RevealImposter()
    {
        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        string url = $"{firebaseUrl}rooms/{PlayerPrefs.GetString("room")}/players.json";
        print(url);
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var json = req.downloadHandler.text;
            Debug.Log("Received JSON: " + json);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, Player>>(json);

            if (dict == null)
            {
                Debug.LogError("Failed to deserialize player data.");
                yield break;
            }

            foreach (var kv in dict)
            {
                Debug.Log($"Checking player {kv.Key}, isImposter: {kv.Value.isImposter}");
            }

            var imposter = dict.FirstOrDefault(kv => kv.Value.isImposter);
            RevealedImposter = imposter.Key;

            Debug.Log("Imposter is: " + RevealedImposter);
        }
        else
        {
            Debug.LogError("Failed to fetch player data: " + req.error);
        }
    }


    IEnumerator Put(string path, string json)
    {
        var req = UnityWebRequest.Put(firebaseUrl + path, json);
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }

    public void OnReveal(Action callback)
    {
        StartCoroutine(RevealListener(callback));
    }

    IEnumerator RevealListener(Action callback)
    {
        while (string.IsNullOrEmpty(RevealedImposter))
        {
            yield return new WaitForSeconds(1);
        }
        callback();
    }
}
