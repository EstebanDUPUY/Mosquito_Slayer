/*
using TMPro;
using UnityEngine;

public class LayBoom : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject bombInQuestion;
    private bool playerHasBomb;

    [SerializeField] private float timeLeftBeforeBoom;
    [SerializeField] private bool killPlayer;

    [SerializeField] private TextMeshProUGUI idLabel;
    public int id;

    private void Awake()
    {
        bombInQuestion = GetComponent<GameObject>();
    }
    private void Update()
    {
        if (timeLeftBeforeBoom <= 0)
        {
            killPlayer = true;
        }
        if (killPlayer)
        {
            Destroy(player);
        }
    }

    private void AssignId()
    {
        
    }

    public void SetUp(int id, Sprite sprite)
    {
        this.id = id;
        idLabel.text = "Player_" + id;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    private void AssignFirstPlayerWithBomb()
    {
        // player.id = hasBomb=
    }
    private void SwitchPlayerWithBomb()
    {

    }

    private void KillPlayer()
    {
        if (killPlayer)
        {
            GameObject.Destroy(gameObject);
        }
    }
}
*/

using TMPro;
using UnityEngine;

public class LayBoom : MonoBehaviour
{
    // timeBeforeBoom
    // func.giveBombToPlayer 
    // 
    [SerializeField] private GameObject bomb;
    [SerializeField] private GameObject[] players;

}