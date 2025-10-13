/* 
 * Ce script est un repère. 
 * Utilisez la nomenclature comme indiquée ci-dessous dans les exemples. 
 * Organisez vos scripts de la même manière que celui ci. Merci
*/
using UnityEngine;
using UnityEngine.InputSystem;
public class ExampleScript : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("Always Use Headers For variables")]
    [HideInInspector] public int hiddenPublicVariable; // When you need a public variable but don't want to have it in the inspector
    [SerializeField] private float hiddenPrivateVariable; // When you need a private variable and want to have it in the inspector
    public bool PublicVariable; // When you need a public variable and want to have it in the inspector
    private string PrivateVariable; // When you need a private variable but don't want to have it in the inspector

    #endregion

    //
    #region START, UPDATE, ETC . . .

    private void Awake()
    {
        
    }

    private void Start()
    {
        
    }
    private void Update()
    {
        
    }

    private void FixedUpdate()
    {
        
    }

    #endregion

    // 
    #region INPUT

    public void LolInput(InputAction.CallbackContext ctx) // Lol = name of the variable | Input = suffix
    {

    }

    #endregion

    //
    #region OTHER FUNCTIONS

    // Any other functions you might need

    #endregion
}
