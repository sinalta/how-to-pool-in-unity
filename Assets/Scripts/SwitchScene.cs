using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    [SerializeField] private string m_sceneName;

    public void Switch()
    {
        SceneManager.LoadScene(m_sceneName);
    }
}
