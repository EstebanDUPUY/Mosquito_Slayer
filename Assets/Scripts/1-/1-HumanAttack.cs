using UnityEngine;
using System.Collections;

public class HumanAttack : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] GameObject idleImage;
    [SerializeField] GameObject alertImage;
    [SerializeField] GameObject attackImage;

    [Header("Timings (aléatoires)")]
    [SerializeField] Vector2 preAlertWait = new Vector2(0.15f, 0.6f);  // attente avant alerte
    [SerializeField] Vector2 alertTime = new Vector2(0.35f, 1.1f);  // durée alerte
    [SerializeField] Vector2 fireTime = new Vector2(0.15f, 0.35f); // durée attaque
    [SerializeField] Vector2 cooldownTime = new Vector2(0.4f, 0.9f);   // CD après séquence
    [SerializeField, Range(0f, 1f)] float feintChance = 0.25f;          // % de feinte (alerte puis rien)

    [Header("Gameplay (option)")]
    [SerializeField] SuccManager manager;

    bool onCooldown;
    Coroutine seqCo;

    void Start()
    {
        SetIdle();
        if (!manager) manager = FindObjectOfType<SuccManager>();
    }

    public void PlayAttackSequence(int targetIndex)
    {
        if (onCooldown) return;                 // protège d’une rafale
        if (seqCo != null) StopCoroutine(seqCo);
        seqCo = StartCoroutine(Sequence(targetIndex));
    }

    IEnumerator Sequence(int targetIndex)
    {
        // petit pré-délai random pour casser le rythme
        yield return new WaitForSeconds(Random.Range(preAlertWait.x, preAlertWait.y));

        // ALERTE
        SetAlert();
        yield return new WaitForSeconds(Random.Range(alertTime.x, alertTime.y));

        // FEINTE ?
        if (Random.value < feintChance)
        {
            SetIdle();
            yield return StartCooldown();
            yield break;
        }

        // ATTAQUE
        SetAttack();
        yield return new WaitForSeconds(Random.Range(fireTime.x, fireTime.y));

        // Résolution (le manager vérifiera si le joueur meurt réellement)
        if (manager && targetIndex >= 0 && targetIndex < manager.players.Length && manager.players[targetIndex])
            manager.players[targetIndex].TryKillFromAttack();

        SetIdle();
        yield return StartCooldown();
        seqCo = null;
    }

    IEnumerator StartCooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(Random.Range(cooldownTime.x, cooldownTime.y));
        onCooldown = false;
    }

    void SetIdle() { if (idleImage) idleImage.SetActive(true); if (alertImage) alertImage.SetActive(false); if (attackImage) attackImage.SetActive(false); }
    void SetAlert() { if (idleImage) idleImage.SetActive(false); if (alertImage) alertImage.SetActive(true); if (attackImage) attackImage.SetActive(false); }
    void SetAttack() { if (idleImage) idleImage.SetActive(false); if (alertImage) alertImage.SetActive(false); if (attackImage) attackImage.SetActive(true); }
}
