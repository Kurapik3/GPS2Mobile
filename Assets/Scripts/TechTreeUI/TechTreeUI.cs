using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TechTreeUI : MonoBehaviour
{
    public static TechTreeUI instance;

    public GameObject nodePrefab;
    public Transform nodeParent;
    public GameObject confirmPopup;
    public GameObject infoPopup;
    public GameObject techInfoPopup;   
    public GameObject unitInfoPopup;

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

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OpenConfirmPopup(TechNode node)
    {
        CloseAllPopups();

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
        Debug.Log($"[TechTreeUI] Opening info popup for: {node.techName}");
        CloseAllPopups();

        if (node == null) return;

        bool isUnitTech = IsUnitTech(node.techName);

        if (isUnitTech)
        {
            if (unitInfoPopup == null)
            {
                Debug.LogError("[TechTreeUI] UnitInfoPopup not assigned!");
                return;
            }
            var popup = unitInfoPopup.GetComponent<UnitInfoPopup>();
            if (popup == null)
            {
                Debug.LogError("[TechTreeUI] No UnitInfoPopup component found!");
                return;
            }
            unitInfoPopup.SetActive(true);
            unitInfoPopup.GetComponent<UnitInfoPopup>().Setup(node);
            popup.Setup(node);
        }
        else
        {
            if (techInfoPopup == null)
            {
                Debug.LogError("[TechTreeUI] TechInfoPopup not assigned!");
                return;
            }
            var popup = techInfoPopup.GetComponent<TechInfoPopup>();
            if (popup == null)
            {
                Debug.LogError("[TechTreeUI] No TechInfoPopup component found!");
                return;
            }
            techInfoPopup.SetActive(true);
            techInfoPopup.GetComponent<TechInfoPopup>().Setup(node);
            popup.Setup(node);
        }
    }

    private void CloseAllPopups()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
        if (unitInfoPopup != null) unitInfoPopup.SetActive(false);
        if (techInfoPopup != null) techInfoPopup.SetActive(false);
    }

    private bool IsUnitTech(string techName)
    {
        string[] unitTechNames = {
            "Armor", "Scouting", "Shooter", "Naval Warfare",
            "Tank", "Scout", "Shooter", "Bomber"
        };

        foreach (string name in unitTechNames)
        {
            if (techName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

