using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class StartScene : MonoBehaviour
{
    public void OnClickGameStart()  // GAME START
    {
        SceneManager.LoadScene("MainMenu");
    }

}
