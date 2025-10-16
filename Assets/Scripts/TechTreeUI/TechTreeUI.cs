using UnityEngine;

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
        GenerateTree();
    }

    void GenerateTree()
    {
        CreateNode("Scouting", 5, new Vector2(0, 0));
        CreateNode("Fishing", 4, new Vector2(200, 100));
        CreateNode("Metal Scrap", 6, new Vector2(400, 100));
        CreateNode("Mutualism", 4, new Vector2(0, -100));
    }

    void CreateNode(string name, int cost, Vector2 pos)
    {
        var node = Instantiate(nodePrefab, nodeParent);
        node.GetComponent<RectTransform>().anchoredPosition = pos;
        var script = node.GetComponent<TechNode>();
        script.Setup(name, cost, new Color(0.6f, 0.9f, 0.95f));
    }

    public void OpenConfirmPopup(TechNode node)
    {
        confirmPopup.SetActive(true);
        confirmPopup.GetComponent<ConfirmPopup>().Setup(node);
    }
}
