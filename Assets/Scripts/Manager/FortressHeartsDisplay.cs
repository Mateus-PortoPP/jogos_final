using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.Manager
{
    /// <summary>
    /// HUD da fortaleza usando corações em fila. Cada coração representa
    /// uma porção do HP máximo (default: 10 HP por coração).
    ///
    /// Comportamento: o coração inteiro DESAPARECE quando aquele "slot" zera.
    /// Ex: 60 HP / 10 por coração = 6 corações cheios. Cada 10 de dano remove
    /// 1 coração da direita pra esquerda. Se a fortaleza for curada no futuro,
    /// os corações voltam.
    /// </summary>
    public class FortressHeartsDisplay : MonoBehaviour
    {
        [Header("Configuração")]
        [Tooltip("Quantos pontos de HP cada coração representa. Ex: 60 HP / 10 = 6 corações.")]
        [SerializeField] private int hpPerHeart = 10;

        [Tooltip("Tamanho de cada coração na UI (pixels).")]
        [SerializeField] private float heartSize = 40f;

        [Tooltip("Espaço entre corações (pixels).")]
        [SerializeField] private float spacing = 4f;

        [Header("Sprite")]
        [Tooltip("Sprite do coração cheio. Os corações simplesmente somem ao perder HP, não trocam pra versão danificada.")]
        [SerializeField] private Sprite heartSprite;

        private List<GameObject> hearts = new List<GameObject>();
        private GameManager game;

        private void Awake()
        {
            // Garante o HorizontalLayoutGroup pra alinhar corações lado a lado.
            // Quando um coração é desativado (SetActive false), os outros mantêm
            // a posição (o slot some, o resto fica fixo à esquerda).
            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogWarning("[FortressHeartsDisplay] GameManager.Instance é null. Coloque um GameManager na cena.");
                return;
            }

            SpawnHearts(game.MaxFortressHP);
            game.OnFortressHPChanged += UpdateHearts;
            UpdateHearts(game.CurrentFortressHP, game.MaxFortressHP);
        }

        private void OnDestroy()
        {
            if (game != null) game.OnFortressHPChanged -= UpdateHearts;
        }

        private void SpawnHearts(int maxHP)
        {
            int heartCount = Mathf.Max(1, Mathf.CeilToInt((float)maxHP / hpPerHeart));
            for (int i = 0; i < heartCount; i++)
            {
                var go = new GameObject($"FortressHeart_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(heartSize, heartSize);

                var img = go.GetComponent<Image>();
                img.sprite = heartSprite;
                hearts.Add(go);
            }
        }

        private void UpdateHearts(int current, int max)
        {
            // Quantos corações ainda devem aparecer? Usamos Ceil pra que o coração
            // só suma quando o slot dele chegar realmente a zero.
            // Ex: HP 51, hpPerHeart 10 → ceil(5.1) = 6 corações (último ainda tem 1 HP)
            //     HP 50 → ceil(5.0) = 5 corações (último coração sumiu)
            int visible = Mathf.Max(0, Mathf.CeilToInt((float)current / hpPerHeart));

            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].SetActive(i < visible);
            }
        }
    }
}
