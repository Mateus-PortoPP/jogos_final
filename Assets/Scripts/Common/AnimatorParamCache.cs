using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Common
{
    /// <summary>
    /// Wrapper em volta do Animator que só aplica SetFloat/SetBool/SetTrigger
    /// se o parâmetro EXISTIR no AnimatorController atual.
    /// Evita warnings de "parameter does not exist" e permite que o script
    /// funcione mesmo com Animator parcial ou ausente.
    ///
    /// Cacheia os hashes pra performance e refresca se o controller mudar.
    /// </summary>
    public class AnimatorParamCache
    {
        private readonly Animator animator;
        private RuntimeAnimatorController lastController;
        private readonly Dictionary<string, AnimatorControllerParameterType> known
            = new Dictionary<string, AnimatorControllerParameterType>();

        public AnimatorParamCache(Animator animator)
        {
            this.animator = animator;
            Refresh();
        }

        private void Refresh()
        {
            known.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            lastController = animator.runtimeAnimatorController;
            foreach (var p in animator.parameters)
            {
                known[p.name] = p.type;
            }
        }

        private bool Has(string name, AnimatorControllerParameterType type)
        {
            if (animator == null) return false;
            // Refresca cache se o controller foi trocado em runtime
            if (animator.runtimeAnimatorController != lastController) Refresh();
            return !string.IsNullOrEmpty(name)
                && known.TryGetValue(name, out var t)
                && t == type;
        }

        public void SetFloat(string name, float value)
        {
            if (Has(name, AnimatorControllerParameterType.Float)) animator.SetFloat(name, value);
        }

        public void SetBool(string name, bool value)
        {
            if (Has(name, AnimatorControllerParameterType.Bool)) animator.SetBool(name, value);
        }

        public void SetTrigger(string name)
        {
            if (Has(name, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(name);
        }
    }
}
