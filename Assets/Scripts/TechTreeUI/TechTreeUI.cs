using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TechTreeUI : MonoBehaviour
{
    public static TechTreeUI instance;

    public GameObject nodePrefab;
    public Transform nodeParent;
    public GameObject confirmPopup;
    public GameObject infoPopup;

    private void Awake() => instance = this;

    void Start()
    {
        //GenerateTree();
        foreach (var node in FindObjectsOfType<TechNode>(true))
        {
            node.Initialize();
        }

        if (confirmPopup != null) confirmPopup.SetActive(false);
        if (infoPopup != null) infoPopup.SetActive(false);
    }

    public void OpenConfirmPopup(TechNode node)
    {
        if (confirmPopup == null)
        {
            Debug.LogError("[TechTreeUI] ConfirmPopup not assigned in Inspector!");
            return;
        }

        var popup = confirmPopup.GetComponent<ConfirmPopup>();
        if (popup == null)
        {
            Debug.LogError("[TechTreeUI] No ConfirmPopup component found on ConfirmPopup GameObject!");
            return;
        }
        confirmPopup.SetActive(true);
        confirmPopup.GetComponent<ConfirmPopup>().Setup(node);
    }

    public void OpenInfoPopUp(TechNode node)
    {
        infoPopup.SetActive(true);
        infoPopup.GetComponent<InfoPopUp>().Setup(node);
    }
}
