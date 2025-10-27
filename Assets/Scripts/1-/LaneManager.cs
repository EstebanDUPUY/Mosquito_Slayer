using UnityEngine;
using UnityEngine.InputSystem;

public class LaneManager : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("Références")]
    [SerializeField] private GameObject playerPrefab; // prefab avec PlayerSucc + PlayerInput + SpriteRenderer

    [System.Serializable]
    public class Lane
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

        [Header("Caméra dédiée à la lane")]
        public Camera cam;

        [Header("UI / Canvas")]
        public Canvas laneCanvas;
    }

    [Header("Lanes (max 4)")]
    public Lane[] lanes = new Lane[4];

    [Header("Caméras - padding")]
    [SerializeField] private float camViewportPadding = 0f; // 0..0.02 si tu veux des petits espaces

    public static LaneManager Instance { get; private set; }

    // liste runtime des PlayerSucc en jeu (toutes lanes confondues)
    private readonly System.Collections.Generic.List<PlayerSucc> _allPlayers =
        new System.Collections.Generic.List<PlayerSucc>();

    private bool _roundEnded = false;

    #endregion


    //
    #region START/UPDATE

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // NOTE : Désormais on NE fait plus le setup dans Start() automatiquement,
    // c'est SuccSceneBootstrap qui appelle InitRoundFromPlayers(players[])
    // après le chargement de la scène.
    private void Start()
    {
        // On ne fait rien ici maintenant.
    }

    #endregion


    //
    #region PUBLIC API (appelée par SuccSceneBootstrap)

    /// <summary>
    /// Appelée par SuccSceneBootstrap dès que la scène est chargée.
    /// playersFromGM vient de GameManager.instance.players.
    /// </summary>
    public void InitRoundFromPlayers(PlayerData[] playersFromGM) // NEW
    {
        // sécurité
        if (playersFromGM == null || playersFromGM.Length == 0)
        {
            Debug.LogWarning("[LaneManager] InitRoundFromPlayers: pas de joueurs donnés.");
            return;
        }

        // reset runtime
        _allPlayers.Clear();
        _roundEnded = false;

        // 1) Désactiver toutes les lanes au début
        for (int i = 0; i < lanes.Length; i++)
        {
            var ln = lanes[i];
            if (ln == null) continue;
            if (ln.root) ln.root.SetActive(false);
        }

        // Combien de joueurs on veut spawner ?
        int count = Mathf.Min(playersFromGM.Length, lanes.Length);

        // 2) Pour chaque joueur : activer une lane, y instancier le moustique, brancher le HUD, etc.
        for (int i = 0; i < count; i++)
        {
            var ln = lanes[i];
            var pdata = playersFromGM[i];

            if (ln == null)
            {
                Debug.LogWarning($"[LaneManager] Lane {i} est null.");
                continue;
            }

            if (pdata == null)
            {
                Debug.LogWarning($"[LaneManager] PlayerData {i} est null.");
                continue;
            }

            // activer visuellement la lane
            if (ln.root) ln.root.SetActive(true);

            // 🔴 NEW : avant même de lancer la round, on lie le canvas à SA caméra
            BindCanvasToCamera(ln, i); // i sert juste de sortingOrder différent par lane

            // on spawn le moustique pour CETTE lane
            PlayerSucc ps = SpawnPlayerInLane(pdata, ln);

            if (ps == null)
            {
                Debug.LogError($"[LaneManager] Impossible de spawner le joueur {i} dans {ln.laneName}.");
                continue;
            }

            _allPlayers.Add(ps);
        }


        // 3) Configurer les viewports caméra en fonction du nb de joueurs
        SetupCameraRectsDynamic();

        // 4) Démarrer la manche dans chaque lane active (-> HUD ResetAll, TickLoop, etc.)
        StartAllActiveLanes();

        Debug.Log("[LANE] Lanes actives après init :");
        for (int i = 0; i < lanes.Length; i++)
        {
            var ln = lanes[i];
            if (ln == null) continue;

            bool active = (ln.root && ln.root.activeSelf);
            string camInfo = ln.cam ? ln.cam.rect.ToString() : "no cam";
            Debug.Log($"Lane {i} active={active} camRect={camInfo} hud={(ln.hud ? ln.hud.name : "no hud")}");
        }

    }

    #endregion


    //
    #region SETUP PLAYERS / LANES


    // NEW : force le canvas de la lane à être branché sur SA caméra
    private void BindCanvasToCamera(Lane ln, int sortingOrder)
    {
        if (ln == null) return;
        if (ln.laneCanvas == null) return;
        if (ln.cam == null) return;

        var cv = ln.laneCanvas;

        // On impose le mode écran-par-caméra pour que l'UI ne couvre que la viewport
        cv.renderMode = RenderMode.ScreenSpaceCamera;
        cv.worldCamera = ln.cam;

        // Distance du plan UI devant la caméra (évite qu’il parte derrière)
        cv.planeDistance = 1f;

        // On donne un ordre de rendu différent pour chaque lane, pour éviter les conflits
        cv.sortingOrder = sortingOrder;

        // Petit debug pour vérifier que tout est bien branché
        Debug.Log($"[HUDDBG] Canvas '{cv.name}' lié à cam '{ln.cam.name}' (sortingOrder={sortingOrder})");
    }

    // instancie le PlayerPrefab dans la lane, l'associe à la bonne caméra/HUD/manager etc.
    private PlayerSucc SpawnPlayerInLane(PlayerData pdata, Lane ln)
    {
        // 2) Instancier le Player avec SON device (New Input System)
        GameObject playerGO = null;

        // Essayons de récupérer le premier device lié au PlayerData
        InputDevice device = null;
        if (pdata.playerInputPV != null && pdata.playerInputPV.devices.Count > 0)
            device = pdata.playerInputPV.devices[0];

        if (device != null)
        {
            // Instancie un PlayerPrefab déjà pairé avec ce device
            playerGO = PlayerInput.Instantiate(
                playerPrefab,
                controlScheme: null,             // si tu utilises des control schemes, mets le bon nom ici
                pairWithDevice: device
            ).gameObject;
        }
        else
        {
            // fallback : on instancie normal (clavier partagé par ex.)
            playerGO = Instantiate(playerPrefab);
        }

        // 3) Positionner sur le spawn de la lane
        if (ln.playerSpawn != null)
        {
            playerGO.transform.position = ln.playerSpawn.position;
            playerGO.transform.rotation = ln.playerSpawn.rotation;
        }

        // 4) Habillage visuel depuis PlayerData (sprite, etc.)
        var sr = playerGO.GetComponent<SpriteRenderer>();
        if (sr && pdata.playerSpritePV) sr.sprite = pdata.playerSpritePV;

        // 5) Brancher le PlayerSucc vers le manager de SA lane
        var ps = playerGO.GetComponent<PlayerSucc>();
        if (ps == null)
        {
            Debug.LogError("[LaneManager] PlayerPrefab sans PlayerSucc !");
            return null;
        }

        // Index local à la lane : 0 (une lane = un joueur)
        ps.Index = 0;
        ps.Manager = ln.manager;

        // 6) Configurer le SuccManager de la lane
        if (ln.manager != null)
        {
            // On s'assure que le tableau a au moins 1 case
            if (ln.manager.players == null || ln.manager.players.Length == 0)
                ln.manager.players = new PlayerSucc[1];

            ln.manager.players[0] = ps;
            ln.manager.hud = ln.hud;
            ln.manager.human = ln.human;

            // Le LaserAttack connaît le manager pour OnLaserHit()
            if (ln.laser != null)
                ln.laser.manager = ln.manager;
        }

        // IMPORTANT HUD : si ton HUD est un Canvas en "Screen Space - Camera",
        // assure qu'il pointe sur la bonne cam de la lane
        // === NEW : brancher et configurer le Canvas HUD de cette lane pour qu'il s'affiche vraiment ===
        Canvas assignedCanvas = null; // on va garder une réf pour le debug final

        if (ln.hud != null)
        {
            // On part du principe que ln.hud est un composant dans CETTE lane
            // (ex: SuccHUD sur "HUD_Lane0/CanvasLane0").
            assignedCanvas = ln.hud.GetComponentInParent<Canvas>();

            if (assignedCanvas != null)
            {
                // 1. Forcer le mode Screen Space - Camera (sinon partage chelou, ou pas dans le split)
                assignedCanvas.renderMode = RenderMode.ScreenSpaceCamera;

                // 2. Attacher la caméra de CETTE lane
                if (ln.cam != null)
                {
                    assignedCanvas.worldCamera = ln.cam;
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
                    SetLayerRecursively(assignedCanvas.gameObject, uiLayer);
                }
            }
        }

        // === DEBUG juste après config HUD de la lane ===
        DebugLaneHUD(
            laneIndex: System.Array.IndexOf(lanes, ln),
            ln: ln,
            canvas: assignedCanvas
        );

        // puis on continue normal
        Debug.Log($"[LaneManager] Joueur '{pdata.name}' placé sur {ln.laneName}");

        // NEW : attacher les infos du joueur global à ce runtime GO
        CopyPlayerDataToRuntimePlayer(pdata, playerGO);

        return ps;
    }

    // On configure les Rect des caméras en fonction du nombre de lanes ACTIVES
    private void SetupCameraRectsDynamic() // NEW
    {
        // d'abord, qu'est-ce qui est actif ?
        System.Collections.Generic.List<Camera> activeCams = new System.Collections.Generic.List<Camera>();
        foreach (var ln in lanes)
        {
            if (ln == null) continue;
            if (ln.root == null) continue;
            if (!ln.root.activeSelf) continue;
            if (ln.cam == null) continue;
            activeCams.Add(ln.cam);
        }

        int n = activeCams.Count;
        if (n == 0) return;

        // On fabrique une liste de Rects adaptés.
        // Tu peux ajuster exactement le layout que tu veux ici.
        System.Collections.Generic.List<Rect> rects = new System.Collections.Generic.List<Rect>();

        if (n == 1)
        {
            // plein écran
            rects.Add(MakePaddedRect(new Rect(0f, 0f, 1f, 1f)));
        }
        else if (n == 2)
        {
            // 2 joueurs -> split vertical gauche / droite
            rects.Add(MakePaddedRect(new Rect(0f, 0f, 0.5f, 1f))); // joueur 1 à gauche
            rects.Add(MakePaddedRect(new Rect(0.5f, 0f, 0.5f, 1f))); // joueur 2 à droite
        }
        else if (n == 3)
        {
            // 3 joueurs -> 2 en haut gauche/droite, 1 en bas full
            rects.Add(MakePaddedRect(new Rect(0f, 0.5f, 0.5f, 0.5f))); // TL
            rects.Add(MakePaddedRect(new Rect(0.5f, 0.5f, 0.5f, 0.5f))); // TR
            rects.Add(MakePaddedRect(new Rect(0f, 0f, 1f, 0.5f))); // bottom full
        }
        else // n >= 4
        {
            // 4 joueurs -> grille 2x2
            rects.Add(MakePaddedRect(new Rect(0f, 0.5f, 0.5f, 0.5f))); // TL
            rects.Add(MakePaddedRect(new Rect(0.5f, 0.5f, 0.5f, 0.5f))); // TR
            rects.Add(MakePaddedRect(new Rect(0f, 0f, 0.5f, 0.5f))); // BL
            rects.Add(MakePaddedRect(new Rect(0.5f, 0f, 0.5f, 0.5f))); // BR
        }

        // on applique
        for (int i = 0; i < activeCams.Count; i++)
        {
            Camera cam = activeCams[i];
            Rect r = rects[Mathf.Min(i, rects.Count - 1)];
            cam.rect = r;

            // NEW 👇 : s'assurer que la cam voit aussi l'UI
            int uiLayer = LayerMask.NameToLayer("UI"); // par défaut c'est layer 5
            if (uiLayer >= 0)
            {
                cam.cullingMask |= (1 << uiLayer);
            }

            Debug.Log($"[CAMDBG] Cam '{cam.name}' rect={cam.rect} mask={cam.cullingMask}");
        }
    }

    private Rect MakePaddedRect(Rect r)
    {
        float p = Mathf.Clamp01(camViewportPadding);
        return new Rect(
            r.x + p,
            r.y + p,
            r.width - 2f * p,
            r.height - 2f * p
        );
    }

    // démarre les SuccManager de toutes les lanes actives
    private void StartAllActiveLanes() // NEW
    {
        foreach (var ln in lanes)
        {
            if (ln == null) continue;
            if (ln.root == null || !ln.root.activeSelf) continue;
            if (ln.manager == null) continue;

            // ici, StartRound() fait :
            // - roundOver = false
            // - hud.ResetAll()
            // - manager.players[0].ResetState()
            // - TickLoop() (donc la jauge commence à bouger)
            ln.manager.StartRound();
        }
    }

    // NEW : copie les infos du PlayerData "source" (celui du GameManager)
    // dans un PlayerData sur le prefab runtime. Si le prefab n'en a pas,
    // on en ajoute un pour que la suite du jeu puisse l'identifier.
    private void CopyPlayerDataToRuntimePlayer(PlayerData source, GameObject runtimeGO)
    {
        if (source == null || runtimeGO == null) return;

        // On regarde si le prefab instancié a déjà un PlayerData.
        var dest = runtimeGO.GetComponent<PlayerData>();
        if (dest == null)
        {
            dest = runtimeGO.AddComponent<PlayerData>();
        }

        // On copie les infos importantes.
        dest.playerSpritePV = source.playerSpritePV;
        dest.myDeviceIdPV = source.myDeviceIdPV;
        dest.scorePV = source.scorePV;

        // Si tu veux garder le lien device pour du debug/score/etc :
        dest.playerInputPV = runtimeGO.GetComponent<PlayerInput>();

        // Optionnel : si tu veux aussi que le SpriteRendererRef du runtime
        // pointe sur le SpriteRenderer du prefab instancié.
        var sr = runtimeGO.GetComponent<SpriteRenderer>();
        dest.spriteRendererRef = sr;
    }

    #endregion


    //
    #region RUNTIME (morts / victoire etc.)

    public void NotifyDeath(PlayerSucc justDied)
    {
        if (_roundEnded) return;

        // Compter les vivants
        int alive = 0;
        PlayerSucc lastAlive = null;

        foreach (var p in _allPlayers)
        {
            if (p != null && p.IsAlive)
            {
                alive++;
                lastAlive = p;
            }
        }

        if (alive <= 1)
        {
            _roundEnded = true;

            // Stoppe toutes les lanes et affiche la win du survivant
            foreach (var ln in lanes)
            {
                if (ln == null || ln.manager == null) continue;
                ln.manager.EndRoundLocal(lastAlive); // on passe le survivant potentiel
            }

            // freeze les inputs
            var allInputs = FindObjectsOfType<PlayerInput>();
            foreach (var pi in allInputs)
            {
                if (pi) pi.enabled = false;
            }

            // informe le GameManager global qui a gagné
            if (lastAlive != null)
            {
                var pdata = lastAlive.GetComponent<PlayerData>();
                if (pdata != null && GameManager.instance != null)
                {
                    GameManager.instance.AddScoreToPlayer(pdata);
                }
            }
        }
    }

    public void ForceWin(PlayerSucc winner)
    {
        if (_roundEnded) return;
        _roundEnded = true;

        foreach (var ln in lanes)
        {
            if (ln == null || ln.manager == null) continue;
            ln.manager.EndRoundLocal(winner);
        }

        // freeze les inputs
        var allInputs = FindObjectsOfType<PlayerInput>();
        foreach (var pi in allInputs)
        {
            if (pi) pi.enabled = false;
        }

        // score
        if (winner != null)
        {
            var pdata = winner.GetComponent<PlayerData>();
            if (pdata != null && GameManager.instance != null)
            {
                GameManager.instance.AddScoreToPlayer(pdata);
            }
        }
    }

    #endregion


    // === DEBUG NEW ===
    // Donne un état complet de la lane côté HUD / Canvas / Camera
    private void DebugLaneHUD(int laneIndex, Lane ln, Canvas canvas)
    {
        if (ln == null)
        {
            Debug.Log($"[HUDDBG] Lane {laneIndex} = null");
            return;
        }

        string rootActive = (ln.root ? ln.root.activeSelf.ToString() : "null");
        string camName = (ln.cam ? ln.cam.name : "null");
        string camRect = (ln.cam ? ln.cam.rect.ToString() : "null");
        string camMask = (ln.cam ? ln.cam.cullingMask.ToString() : "null");

        string hudName = (ln.hud ? ln.hud.name : "null");
        bool hudActive = (ln.hud ? ln.hud.gameObject.activeInHierarchy : false);
        string hudLayer = (ln.hud ? LayerMask.LayerToName(ln.hud.gameObject.layer) : "null");

        string canvasName = (canvas ? canvas.name : "null");
        bool canvasActive = (canvas ? canvas.gameObject.activeInHierarchy : false);
        string canvasMode = (canvas ? canvas.renderMode.ToString() : "null");
        string canvasWCam = (canvas ? (canvas.worldCamera ? canvas.worldCamera.name : "null") : "null");
        int canvasOrder = (canvas ? canvas.sortingOrder : -999);
        float canvasPlaneDist = (canvas ? canvas.planeDistance : -999f);
        string canvasLayer = (canvas ? LayerMask.LayerToName(canvas.gameObject.layer) : "null");

        Debug.Log(
            $"[HUDDBG] Lane {laneIndex}:\n" +
            $"  root.active={rootActive}\n" +
            $"  cam={camName} rect={camRect} mask={camMask}\n" +
            $"  hud={hudName} hudActive={hudActive} hudLayer={hudLayer}\n" +
            $"  canvas={canvasName} canvasActive={canvasActive}\n" +
            $"     mode={canvasMode} worldCam={canvasWCam}\n" +
            $"     sortingOrder={canvasOrder} planeDist={canvasPlaneDist} canvasLayer={canvasLayer}"
        );
    }

    // Force un layer (genre "UI") sur ce GO et tous ses enfants
    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (!go) return;
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i);
            if (child != null) SetLayerRecursively(child.gameObject, layer);
        }
    }

}
