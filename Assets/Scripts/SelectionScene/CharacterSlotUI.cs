using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISelectHandler
{
    [Header("Quel skin ce slot représente ?")]
    public int skinIndex; // doit matcher l’index dans playerSkin[] du manager

    [Header("Références UI")]
    [SerializeField] private GameObject bloodClaimPanel; // tache de sang (feedback quand choisi)

    private bool isLocked = false; // déjà définitivement pris ?
    public PlayerJoinManager joinManager;

    void Awake()
    {
        if (!joinManager) joinManager = FindObjectOfType<PlayerJoinManager>();
        if (bloodClaimPanel) bloodClaimPanel.SetActive(false);
    }

    // appelé PAR le manager quand le joueur a validé ce slot
    public void LockThisChoice()
    {
        isLocked = true;
        if (bloodClaimPanel) bloodClaimPanel.SetActive(true);
    }

    // ===== SOURIS HOVER =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (joinManager == null) return;
        // dire au manager "le focus (highlight) est maintenant sur moi"
        joinManager.SetFocusedSlot(this);

        // IMPORTANT : on force aussi le focus UI EventSystem sur ce bouton,
        // comme si la manette était dessus.
        var selectable = GetComponent<Selectable>();
        if (selectable && EventSystem.current)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }

    // ===== SOURIS CLICK =====
    public void OnPointerClick(PointerEventData eventData)
    {
        TrySelectMe();
    }

    // ===== MANETTE / CLAVIER FOCUS =====
    public void OnSelect(BaseEventData eventData)
    {
        if (joinManager == null) return;
        // quand le pad navigue jusqu'à moi, je deviens le slot "focus logique"
        joinManager.SetFocusedSlot(this);
    }

    // ===== VALIDATION INTERNE =====
    public bool TrySelectMe()
    {
        if (isLocked) return false;
        if (joinManager == null) return false;

        // On demande au manager de tenter de valider ce slot.
        bool ok = joinManager.TrySelectSlot(this);

        // si ok == true, LockThisChoice() sera appelé par le manager.
        return ok;
    }
}
