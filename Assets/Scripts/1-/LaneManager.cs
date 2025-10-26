using UnityEngine;
using UnityEngine.InputSystem;
using static LaneManager;

public class LaneManager : MonoBehaviour
{
    
    #region VARIABLES

    [Header("Références")]
    [SerializeField] private GameObject playerPrefab; // prefab avec PlayerSucc + PlayerInput + SpriteRenderer

    [System.Serializable]
    public class Lane
    {
        public string laneName = "Lane";
        public Transform playerSpawn;
        public SuccManager manager;     // doit avoir players[] de taille 1 (ou on ignore les extra)
        public HumanAttack human;
        public LaserAttack laser;
        public SuccHUD hud;
        public Camera cam;              // caméra dédiée à la lane
    }

    [Header("Lanes (max 4)")]
    public Lane[] lanes = new Lane[4];

    [Header("Caméras - padding")]
    [SerializeField] private float camViewportPadding = 0f; // 0..0.02 si tu veux des petits espaces

    public static LaneManager Instance { get; private set; }
    private readonly System.Collections.Generic.List<PlayerSucc> _allPlayers = new System.Collections.Generic.List<PlayerSucc>();
    private bool _roundEnded = false;


    #endregion


    #region START/UPDATE

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SetupPlayersOnLanes();
        SetupSplitScreen();

        foreach (var ln in lanes)
        {
            if (ln != null && ln.manager != null)
            {
                // si tu as un IntroFlowUI par lane, laisse-lui StartRound.
                ln.manager.StartRound();
            }
        }
        StartAllLanes(); // CHANGE : on lance toutes les lanes proprement (reset global + start)

    }

    private void OnEnable() // ADD : écouter l’événement “dernier survivant”
    {
        SurvivorRegistry.OnLastSurvivor += OnLastSurvivor;
    }

    private void OnDisable() // ADD : propre désabonnement
    {
        SurvivorRegistry.OnLastSurvivor -= OnLastSurvivor;
    }

    #endregion


    #region OTHER FUNCTIONS


    public void StartAllLanes()
    {
        // Reset global des survivants UNE SEULE FOIS pour ce round
        SurvivorRegistry.Reset();

        // Lancer chaque lane (chaque SuccManager.ResetState() enregistrera le joueur dans le registre)
        foreach (var ln in lanes)
        {
            if (ln != null && ln.manager != null)
            {
                ln.manager.StartRound();
            }
        }
    }

    // ADD : quand il ne reste plus qu’un vivant, on arrête proprement TOUTES les lanes
    private void OnLastSurvivor(PlayerSucc winner)
    {
        foreach (var ln in lanes)
        {
            if (ln != null && ln.manager != null)
            {
                // Chaque SuccManager s’occupe d’afficher la WIN uniquement
                // si le winner appartient à SA lane (via Owns(winner) côté manager).
                ln.manager.EndRoundLocal(winner);
            }
        }
    }

    public void RegisterPlayer(PlayerSucc p)
    {
        if (p == null) return;
        if (!_allPlayers.Contains(p))
        {
            _allPlayers.Add(p);
            // Debug.Log("[LaneManager] RegisterPlayer " + p.name);
        }
    }


    private void SetupPlayersOnLanes()
    {
        // 1) Récupérer les PlayerData qui viennent du hub
        var all = FindObjectsOfType<PlayerData>(includeInactive: true);
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("[LaneManager] Aucun PlayerData trouvé. On ne fait rien.");
            return;
        }

        // On limite à 4
        int count = Mathf.Min(all.Length, lanes.Length);

        for (int i = 0; i < count; i++)
        {
            var lane = lanes[i];
            var pdata = all[i];
            if (lane == null || pdata == null)
            {
                Debug.LogWarning($"[LaneManager] Lane {i} ou PlayerData null.");
                continue;
            }

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
                    controlScheme: null,            // si tu utilises des control schemes, mets le bon nom ici
                    pairWithDevice: device
                ).gameObject;
            }
            else
            {
                // fallback : on instancie normal (clavier partagé par ex.)
                playerGO = Instantiate(playerPrefab);
            }

            // 3) Positionner sur le spawn de la lane
            if (lane.playerSpawn != null)
            {
                playerGO.transform.position = lane.playerSpawn.position;
                playerGO.transform.rotation = lane.playerSpawn.rotation;
            }

            // 4) Habillage visuel depuis PlayerData (sprite, etc.)
            var sr = playerGO.GetComponent<SpriteRenderer>();
            if (sr && pdata.playerSpritePV) sr.sprite = pdata.playerSpritePV;

            // 5) Brancher le PlayerSucc vers le manager de SA lane
            var ps = playerGO.GetComponent<PlayerSucc>();
            if (ps == null)
            {
                Debug.LogError($"[LaneManager] PlayerPrefab sans PlayerSucc !");
                continue;
            }

            // Index local à la lane : 0 (une lane = un joueur)
            ps.Index = 0;
            ps.Manager = lane.manager;

            // 6) Configurer le SuccManager de la lane
            if (lane.manager != null)
            {
                // On s'assure que le tableau a au moins 1 case
                if (lane.manager.players == null || lane.manager.players.Length == 0)
                    lane.manager.players = new PlayerSucc[1];

                lane.manager.players[0] = ps;
                lane.manager.hud = lane.hud;
                lane.manager.human = lane.human;

                // Le LaserAttack connaît le manager pour OnLaserHit()
                if (lane.laser != null) lane.laser.manager = lane.manager;
            }

            // 7) Donner la caméra de lane au joueur si besoin (suivi, culling, etc.)
            if (lane.cam != null)
            {
                // Optionnel : faire suivre la cam au joueur
                // lane.cam.GetComponent<YourFollowScript>()?.SetTarget(playerGO.transform);
            }

            Debug.Log($"[LaneManager] P{i + 1} placé sur {lane.laneName}");
        }
    }

    private void SetupSplitScreen()
    {
        // Compter les lanes effectivement actives (avec camera)
        int activeLanes = 0;
        foreach (var ln in lanes) if (ln != null && ln.cam != null) activeLanes++;

        if (activeLanes <= 0) return;

        // Définir les rects en fonction du nombre de joueurs actifs
        // 1 joueur : plein écran
        // 2 joueurs : horizontal 2 x 1
        // 3-4 joueurs : grille 2 x 2
        Rect[] rects;
        if (activeLanes == 1)
        {
            rects = new Rect[] { PaddedRect(new Rect(0, 0, 1, 1)) };
        }
        else if (activeLanes == 2)
        {
            rects = new Rect[]
            {
                PaddedRect(new Rect(0, 0.5f, 1, 0.5f)), // haut
                PaddedRect(new Rect(0, 0,    1, 0.5f)), // bas
            };
        }
        else
        {
            rects = new Rect[]
            {
                PaddedRect(new Rect(0,   0.5f, 0.5f, 0.5f)), // TL
                PaddedRect(new Rect(0.5f,0.5f, 0.5f, 0.5f)), // TR
                PaddedRect(new Rect(0,   0f,   0.5f, 0.5f)), // BL
                PaddedRect(new Rect(0.5f,0f,   0.5f, 0.5f)), // BR
            };
        }

        // Appliquer aux cams, dans l'ordre des lanes non nulles
        int idx = 0;
        foreach (var ln in lanes)
        {
            if (ln == null || ln.cam == null) continue;
            ln.cam.rect = rects[Mathf.Clamp(idx, 0, rects.Length - 1)];
            idx++;
        }
    }

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

        // Debug.Log($"[LaneManager] NotifyDeath -> alive={alive}");

        if (alive <= 1)
        {
            _roundEnded = true;

            // Stoppe toutes les lanes
            foreach (var ln in lanes)
            {
                if (ln == null || ln.manager == null) continue;
                ln.manager.EndRoundLocal(lastAlive); // on passe le survivant potentiel
            }

            // Option: désactiver tous les inputs pour geler proprement
            var allInputs = FindObjectsOfType<PlayerInput>();
            foreach (var pi in allInputs)
            {
                if (pi) pi.enabled = false;
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
    }


    private Rect PaddedRect(Rect r)
    {
        float p = Mathf.Clamp01(camViewportPadding);
        return new Rect(r.x + p, r.y + p, r.width - 2 * p, r.height - 2 * p);
    }

    #endregion
}
