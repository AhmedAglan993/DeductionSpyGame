using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EntryManager : MonoBehaviour
{
    public TMP_Dropdown roleDropdown;
    public TMP_InputField nameInput;
    public TMP_InputField roomInput;

    public void Join()
    {
        PlayerPrefs.SetString("role", roleDropdown.options[roleDropdown.value].text.ToLower());
        PlayerPrefs.SetString("name", nameInput.text);
        PlayerPrefs.SetString("room", roomInput.text);

        if (roleDropdown.value == 0)
            SceneManager.LoadScene("PlayerScene");
        else
            SceneManager.LoadScene("RefereeScene");
    }
}
