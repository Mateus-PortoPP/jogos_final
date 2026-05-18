using System.Collections;
using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// No início da cena, mostra o player, vai até um alvo (ex: de onde os
    /// goblins vêm) e volta — aí a noite "começa de verdade".
    /// </summary>
    public class CameraIntroCue : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Espera (s) depois do load antes de iniciar a panorâmica.")]
        [SerializeField] private float startDelay = 0.4f;

        private IEnumerator Start()
        {
            if (target == null) yield break;
            yield return new WaitForSeconds(startDelay);
            // espera a CameraFocusPan existir/estar livre
            float guard = 0f;
            while ((CameraFocusPan.Instance == null || CameraFocusPan.Instance.IsBusy) && guard < 3f)
            {
                guard += Time.deltaTime;
                yield return null;
            }
            if (CameraFocusPan.Instance != null)
                CameraFocusPan.Instance.FocusOn(target.position);
        }
    }
}
