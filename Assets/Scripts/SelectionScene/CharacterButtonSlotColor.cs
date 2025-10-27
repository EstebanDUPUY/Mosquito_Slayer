using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterButtonSlotColor : MonoBehaviour
{
    [Header("Référence bouton UI sur ce slot")]
    [SerializeField] private Button button;

    void Reset()
    {
        // auto-référence si tu ajoutes le script dans l'Editor
        button = GetComponent<Button>();
    }

    // Appelé par le PlayerJoinManager pour dire :
    // "Ce slot est en train d'être regardé par le joueur X -> mets sa couleur"
    public void SetSelectedColor(Color c)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.selectedColor = c;
        colors.highlightedColor = c * 1.1f; // petit glow proche
        button.colors = colors;
    }

    // Quand on n’a plus de joueur en focus, on remet une couleur neutre
    public void ResetColors(Color neutral)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.selectedColor = neutral;
        colors.highlightedColor = neutral;
        button.colors = colors;
    }
}
