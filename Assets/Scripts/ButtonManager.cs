using UnityEngine;
using UnityEngine.SceneManagement;


public class ButtonManager : MonoBehaviour
{
    public void LoadGame(string sceneName)
    {
        SceneManager.LoadScene("Kasper");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void MainMenu(string sceneName)
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
