using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Hàm dùng chung cho tất cả các màn chơi
    public void LoadLevel(string sceneName)
    {
        // Reset lại thời gian trước khi chuyển màn (phòng trường hợp màn cũ đang Pause)
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}