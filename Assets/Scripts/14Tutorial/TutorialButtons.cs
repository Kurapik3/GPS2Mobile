using UnityEngine;
using UnityEngine.UI;

public class TutorialButtons : MonoBehaviour
{
    [SerializeField] private GameObject TutorialPanel;

    [Header ("button")]
    [SerializeField] private Button ReturnButton;

    public void ClickReturn()
    {
        TutorialPanel.SetActive(false);
    }


}
