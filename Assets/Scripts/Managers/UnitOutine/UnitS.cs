using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitS : MonoBehaviour
{
    public static UnitS instance;

    public List<GameObject> allUnitList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    [SerializeField] private LayerMask clickable;
    [SerializeField] private LayerMask ground;
    [SerializeField] private GameObject groundMarker;
    

    private Camera cam;

    private void Awake()
    {
        if (instance != null  && instance !=this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickable))
            {
                SelectByClicking(hit.collider.gameObject);
            }
            else
            {
                DeselectAll();
            }
        }

    }

    private void DeselectAll()
    {
        

        foreach (var unit in unitsSelected)
        {
            TriggerSelectionIndicator(unit, false);
        }

        unitsSelected.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();

        unitsSelected.Add(unit);

        TriggerSelectionIndicator(unit, true);
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        unit.transform.GetChild(0).gameObject.SetActive(isVisible);
    }
}
