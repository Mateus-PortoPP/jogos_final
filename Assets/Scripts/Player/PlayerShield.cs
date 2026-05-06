using UnityEngine;
using UnityEngine.InputSystem;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// Defesa do Guardião:
    /// - Segura o botão Shield → entra em estado de defesa.
    /// - Enquanto defendendo: bloqueia 100% do dano (via IDamageBlocker), trava o
    ///   movimento (via PlayerController) e desativa o ataque.
    /// - Soltar o botão → sai da defesa.
    ///
    /// Parâmetros do Animator:
    ///   - Shield (bool) — true enquanto defendendo
    /// </summary>
    public class PlayerShield : MonoBehaviour, IDamageBlocker
    {
        public enum ShieldButton { LeftMouse, RightMouse, MiddleMouse }

        [Header("Input")]
        [Tooltip("Botão do mouse pra defender (segurar).")]
        [SerializeField] private ShieldButton mouseButton = ShieldButton.RightMouse;

        [Header("Animator")]
        [SerializeField] private string shieldBoolParam = "Shield";

        [Header("Som")]
        [Tooltip("Som tocado quando o escudo absorve um ataque.")]
        [SerializeField] private AudioClip blockSound;
        [SerializeField, Range(0f, 1f)] private float blockVolume = 1f;

        private Animator animator;
        private AnimatorParamCache animParams;
        private PlayerCombat combat;
        private Health health;

        public bool IsShielding { get; private set; }
        public bool BlocksDamage => IsShielding && (health == null || !health.IsDead);

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animParams = new AnimatorParamCache(animator);
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            // Morto: força sair da defesa e ignora input
            if (health != null && health.IsDead)
            {
                if (IsShielding) SetShielding(false);
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            bool wants = mouseButton switch
            {
                ShieldButton.LeftMouse => mouse.leftButton.isPressed,
                ShieldButton.RightMouse => mouse.rightButton.isPressed,
                ShieldButton.MiddleMouse => mouse.middleButton.isPressed,
                _ => false,
            };
            if (wants != IsShielding) SetShielding(wants);
        }

        private void SetShielding(bool active)
        {
            IsShielding = active;
            animParams.SetBool(shieldBoolParam, active);

            // Bloqueia ataque enquanto defende
            if (combat != null) combat.enabled = !active;
        }

        public void OnDamageBlocked(int amount)
        {
            if (blockSound != null)
                AudioSource.PlayClipAtPoint(blockSound, transform.position, blockVolume);
        }
    }
}
