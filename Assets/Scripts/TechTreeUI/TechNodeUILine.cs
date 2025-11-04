//using UnityEngine;
//using UnityEngine.UI;

//public class TechNodeUILine : MonoBehaviour
//{
//    private TechNode fromNode;
//    private TechNode toNode;
//    private Image lineImage;
//    private Sprite lockedSprite;
//    private Sprite unlockedSprite;

//    public void Setup(TechNode from, TechNode to, Sprite locked, Sprite unlocked)
//    {
//        fromNode = from;
//        toNode = to;
//        lockedSprite = locked;
//        unlockedSprite = unlocked;

//        lineImage = GetComponent<Image>();
//        if (lineImage == null)
//        {
//            lineImage = gameObject.AddComponent<Image>();
//        }

//        UpdateLineState();
//    }

//    public void UpdateLineState()
//    {
//        if (fromNode == null || toNode == null || lineImage == null)
//            return;

//        // Line is unlocked if the prerequisite (fromNode) is learned
//        if (fromNode.state == TechState.Learned)
//        {
//            // Show unlocked line
//            if (unlockedSprite != null)
//            {
//                lineImage.sprite = unlockedSprite;
//            }
//            lineImage.color = new Color(0.5f, 1f, 0.5f, 1f); // Green tint
//        }
//        else
//        {
//            // Show locked line
//            if (lockedSprite != null)
//            {
//                lineImage.sprite = lockedSprite;
//            }
//            lineImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Dark gray, semi-transparent
//        }
//    }
