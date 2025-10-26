using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Quel skin ce slot représente ?")]
    public int skinIndex; // doit matcher l’index dans playerSkin[] du manager

    [Header("Références UI")]
    [SerializeField] private GameObject bloodClaimPanel; // tache de sang
    [SerializeField] private Image outlineImage;         // contour highlight

    private bool isLocked = false; // déjà choisi définitivement ?
    public PlayerJoinManager joinManager;

    void Awake()
    {
        joinManager = FindObjectOfType<PlayerJoinManager>();

        if (bloodClaimPanel != null)
            bloodClaimPanel.SetActive(false); // au début, pas choisi

        HideOutline();
    }

    // appelé PAR le manager quand le joueur a validé ce slot
    public void LockThisChoice()
    {
        isLocked = true;
        if (bloodClaimPanel != null)
            bloodClaimPanel.SetActive(true); // affiche la tache de sang
        HideOutline();
    }

    void ShowOutline()
    {
        if (outlineImage == null) return;
        outlineImage.enabled = true;
    }

    void HideOutline()
    {
        if (outlineImage == null) return;
        outlineImage.enabled = false;
    }

    void UpdateOutlineColorFromCurrentPlayer()
    {
        if (outlineImage == null) return;
        if (joinManager == null) return;
        if (!joinManager.HasCurrentPlayer()) return;

        // couleur en fonction du joueur qui choisit en ce moment
        outlineImage.color = joinManager.GetCurrentPlayerColor();
    }

    // ============ SOURIS ============
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked) return;
        if (joinManager == null) return;
        if (!joinManager.HasCurrentPlayer()) return;
        if (joinManager.IsSkinAlreadyTaken(skinIndex)) return;

        UpdateOutlineColorFromCurrentPlayer();
        ShowOutline();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isLocked) return;
        HideOutline();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TrySelectMe();
    }

    // ============ MANETTE / CLAVIER ============
    public void OnSelect(BaseEventData eventData)
    {
        if (isLocked) return;
        if (joinManager == null) return;
        if (!joinManager.HasCurrentPlayer()) return;
        if (joinManager.IsSkinAlreadyTaken(skinIndex)) return;

        UpdateOutlineColorFromCurrentPlayer();
        ShowOutline();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (isLocked) return;
        HideOutline();
    }

    // ============ VALIDATION ============
    private void TrySelectMe()
    {
        if (isLocked) return;
        if (joinManager == null) return;

        bool ok = joinManager.TrySelectSlot(this);

        if (ok)
        {
            // TrySelectSlot() a appelé ApplySkin(), qui a appelé LockThisChoice() sur moi.
            // Donc là isLocked = true, la tache de sang est visible,
            // on n'a rien d'autre à faire.
        }
    }
}
