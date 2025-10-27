using UnityEngine;
using UnityEngine.InputSystem;

public class Lane : MonoBehaviour
{
    public string laneName = "Lane";

    [Header("Hiérarchie")]
    public GameObject root;              // NEW : le GameObject parent de TOUTE la lane (caméra, bras, HUD...)
                                         // -> on va l'activer/désactiver selon le nb de joueurs

    public Transform playerSpawn;        // où on pose le moustique

    [Header("Gameplay refs")]
    public SuccManager manager;          // doit avoir players[] de taille 1 pour cette lane
    public HumanAttack human;
    public LaserAttack laser;
    public SuccHUD hud;
    public PlayerSucc playerSucc;

    [Header("Caméra dédiée à la lane")]
    public Camera cam;

    [Header("UI / Canvas")]
    public Canvas laneCanvas;



    public PlayerSucc SpawnPlayerInLane(PlayerData pdata)
    {
        
        // 3) Positionner sur le spawn de la lane
        if (playerSpawn != null)
        {
            pdata.transform.position = playerSpawn.position;
        }


        // Index local à la lane : 0 (une lane = un joueur)
        playerSucc.Index = 0;
        playerSucc.Manager = manager;

        // 6) Configurer le SuccManager de la lane
        if (manager != null)
        {
            // On s'assure que le tableau a au moins 1 case
            if (manager.players == null || manager.players.Length == 0)
                manager.players = new PlayerSucc[1];

            manager.players[0] = playerSucc;
            manager.hud = hud;
            manager.human = human;

            // Le LaserAttack connaît le manager pour OnLaserHit()
            if (laser != null)
                laser.manager = manager;
        }

        playerSucc.LinkInput(pdata);

        //SetCanvas();
        laneCanvas.worldCamera = pdata.playerCamera;

        // puis on continue normal
        Debug.Log($"[LaneManager] Joueur '{pdata.name}' placé sur {name}");


        return playerSucc;
    }

    private void SetCanvas()
    {
        // IMPORTANT HUD : si ton HUD est un Canvas en "Screen Space - Camera",
        // assure qu'il pointe sur la bonne cam de la lane
        // === NEW : brancher et configurer le Canvas HUD de cette lane pour qu'il s'affiche vraiment ===
        Canvas assignedCanvas = null; // on va garder une réf pour le debug final

        if (hud != null)
        {
            // On part du principe que ln.hud est un composant dans CETTE lane
            // (ex: SuccHUD sur "HUD_Lane0/CanvasLane0").
            assignedCanvas = hud.GetComponentInParent<Canvas>();

            if (assignedCanvas != null)
            {
                // 1. Forcer le mode Screen Space - Camera (sinon partage chelou, ou pas dans le split)
                assignedCanvas.renderMode = RenderMode.ScreenSpaceCamera;

                // 2. Attacher la caméra de CETTE lane
                if (cam != null)
                {
                    assignedCanvas.worldCamera = cam;
                }

                // 3. S'assurer qu'il passe DEVANT le bras/monstre
                assignedCanvas.sortingOrder = 1000;
                assignedCanvas.planeDistance = 1f;

                // 4. Activer le GO du Canvas
                assignedCanvas.gameObject.SetActive(true);

                // 5. IMPORTANT : on force aussi le layer du Canvas ET de tous ses enfants en "UI"
                //    (si jamais en scene ça a atterri dans Default etc).
                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0)
                {
                    //SetLayerRecursively(assignedCanvas.gameObject, uiLayer);
                }
            }
        }
    }
}
