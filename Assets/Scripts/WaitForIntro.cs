using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class WaitForIntro : MonoBehaviour
{
    [SerializeField] private float waitDuration;
    [SerializeField] private string sceneName;

    private void Start()
    {
        StartCoroutine(WaitCoroutine());
    }

    private IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(waitDuration);

        SceneManager.LoadScene("MainMenu");
    }

}
