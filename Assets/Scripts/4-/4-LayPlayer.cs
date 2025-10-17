using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LayPlayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI idLabel;
    public int id;

    public void SetUp(int id, Sprite sprite)
    {
        this.id = id;
        idLabel.text = "Player_" + id;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }
    //
    /*
    [SerializeField] private TextMeshProUGUI playerEggScore;
    [SerializeField] private TextMeshProUGUI bestEggScore;
    [SerializeField] private int playerScore;

    private void Start()
    {
        playerEggScore.text = playerScore.ToString() + "Eggs";
        bestEggScore.text = playerScore.ToString() + "Eggs";
    }
    */
}
