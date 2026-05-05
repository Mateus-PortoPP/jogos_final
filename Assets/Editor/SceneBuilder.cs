using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using TowerDefense.Common;
using TowerDefense.Player;
using TowerDefense.Enemies;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Monta a cena inicial do Sprint 2 com 1 clique e gera os AnimatorControllers prontos.
    /// Idempotente: roda de novo sem duplicar.
    /// </summary>
    public static class SceneBuilder
    {
        private const string KnightPath = "Assets/Sprites/Player/knight 1 sword 1.png";
        private const string GoblinPath = "Assets/Sprites/Enimies/goblin spritesheet calciumtrice.png";
        private const string BackgroundPath = "Assets/Sprites/Enviroment/B1013-3.png";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        private const string PlayerControllerPath = "Assets/Animations/Player/Player.controller";
        private const string GoblinControllerPath = "Assets/Animations/Enimies/Goblin.controller";

        [MenuItem("Tower Defense/Setup Initial Scene")]
        public static void SetupScene()
        {
            EnsureTagExists("Player");
            EnsureTagExists("Enemy");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "inicial")
            {
                bool ok = EditorUtility.DisplayDialog(
                    "Setup Initial Scene",
                    $"A cena ativa é '{scene.name}', não 'inicial'. Continuar mesmo assim?",
                    "Continuar", "Cancelar");
                if (!ok) return;
            }

            // Garante AnimatorControllers (não destrutivo: mantém clips já atribuídos)
            var playerCtrl = EnsurePlayerAnimatorController();
            var goblinCtrl = EnsureGoblinAnimatorController();

            // Idempotência: remove os GameObjects gerenciados antes de recriar
            DestroyIfExists("Background");
            DestroyIfExists("Ground");
            DestroyIfExists("Player");
            DestroyIfExists("Goblin");

            ConfigureCamera();
            CreateBackground();
            CreateGround();
            CreatePlayer(playerCtrl);
            CreateGoblin(goblinCtrl);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Tower Defense] Cena montada e AnimatorControllers prontos em Assets/Animations/.");
        }

        // ========== GameObjects ==========

        private static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        }

        private static void CreateBackground()
        {
            var sprite = LoadFirstSprite(BackgroundPath);
            if (sprite == null)
            {
                Debug.LogError($"[SceneBuilder] Sprite do background não encontrado em {BackgroundPath}");
                return;
            }

            var go = new GameObject("Background");
            go.transform.position = new Vector3(-8f, -4f, 10f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;
        }

        private static void CreateGround()
        {
            var go = new GameObject("Ground");
            go.transform.position = new Vector3(0f, -3.5f, 0f);
            go.transform.localScale = new Vector3(20f, 1f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSolidSprite();
            sr.color = new Color(0.30f, 0.20f, 0.10f);
            sr.sortingOrder = -50;

            go.AddComponent<BoxCollider2D>();
        }

        private static void CreatePlayer(AnimatorController controller)
        {
            var sprite = LoadFirstSprite(KnightPath);
            if (sprite == null)
            {
                Debug.LogError($"[SceneBuilder] Sprite do knight não encontrado em {KnightPath}");
                return;
            }

            var go = new GameObject("Player");
            go.tag = "Player";
            go.transform.position = new Vector3(-5f, -2f, 0f);
            go.transform.localScale = new Vector3(2f, 2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            go.AddComponent<BoxCollider2D>();

            var input = go.AddComponent<PlayerInput>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions != null)
            {
                input.actions = actions;
                input.defaultActionMap = "Player";
                input.notificationBehavior = PlayerNotifications.SendMessages;
            }
            else
            {
                Debug.LogWarning($"[SceneBuilder] InputActions não encontradas em {InputActionsPath}.");
            }

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerCombat>();

            var health = go.AddComponent<Health>();
            // HP do Guardião = 100 (definido no GDD)
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").intValue = 100;
            so.ApplyModifiedProperties();
        }

        private static void CreateGoblin(AnimatorController controller)
        {
            var sprite = LoadFirstSprite(GoblinPath);
            if (sprite == null)
            {
                Debug.LogError($"[SceneBuilder] Sprite do goblin não encontrado em {GoblinPath}");
                return;
            }

            var go = new GameObject("Goblin");
            go.tag = "Enemy";
            go.transform.position = new Vector3(7f, -2f, 0f);
            go.transform.localScale = new Vector3(2f, 2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            go.AddComponent<BoxCollider2D>();

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            go.AddComponent<Goblin>();

            var health = go.AddComponent<Health>();
            // HP do Goblin = 40 (GDD)
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").intValue = 40;
            so.ApplyModifiedProperties();
        }

        // ========== Animator Controllers ==========

        private static AnimatorController EnsurePlayerAnimatorController()
        {
            EnsureFolder("Assets/Animations/Player");
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (existing != null) return existing; // não sobrescreve, mantém clips já atribuídos

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle", new Vector3(250, 0, 0));
            var walk = sm.AddState("Walk", new Vector3(450, 0, 0));
            var jump = sm.AddState("Jump", new Vector3(450, 100, 0));
            var attack = sm.AddState("Attack", new Vector3(250, 200, 0));
            var death = sm.AddState("Death", new Vector3(450, 200, 0));
            sm.defaultState = idle;

            // Idle ↔ Walk via Speed
            var idleToWalk = idle.AddTransition(walk);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.05f;
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var walkToIdle = walk.AddTransition(idle);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.05f;
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Any → Attack (trigger)
            var anyToAttack = sm.AddAnyStateTransition(attack);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.02f;
            anyToAttack.canTransitionToSelf = false;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            // Attack → Idle (após exit time)
            var attackToIdle = attack.AddTransition(idle);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.05f;

            // Jump quando sai do chão
            var idleToJump = idle.AddTransition(jump);
            idleToJump.hasExitTime = false;
            idleToJump.duration = 0.02f;
            idleToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

            var walkToJump = walk.AddTransition(jump);
            walkToJump.hasExitTime = false;
            walkToJump.duration = 0.02f;
            walkToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

            // Jump → Idle quando volta ao chão
            var jumpToIdle = jump.AddTransition(idle);
            jumpToIdle.hasExitTime = false;
            jumpToIdle.duration = 0.05f;
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

            // Any → Death (trigger)
            var anyToDeath = sm.AddAnyStateTransition(death);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.02f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");

            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static AnimatorController EnsureGoblinAnimatorController()
        {
            EnsureFolder("Assets/Animations/Enimies");
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(GoblinControllerPath);
            if (existing != null) return existing;

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(GoblinControllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle", new Vector3(250, 0, 0));
            var walk = sm.AddState("Walk", new Vector3(450, 0, 0));
            var attack = sm.AddState("Attack", new Vector3(250, 150, 0));
            var death = sm.AddState("Death", new Vector3(450, 150, 0));
            sm.defaultState = walk;

            var idleToWalk = idle.AddTransition(walk);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.05f;
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var walkToIdle = walk.AddTransition(idle);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.05f;
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var anyToAttack = sm.AddAnyStateTransition(attack);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.02f;
            anyToAttack.canTransitionToSelf = false;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            var attackToIdle = attack.AddTransition(idle);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.05f;

            var anyToDeath = sm.AddAnyStateTransition(death);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.02f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");

            AssetDatabase.SaveAssets();
            return ctrl;
        }

        // ========== assets / utils ==========

        private static Sprite LoadFirstSprite(string path)
        {
            var single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (single != null) return single;
            var subs = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var s in subs)
            {
                if (s is Sprite sp) return sp;
            }
            return null;
        }

        private static Sprite cachedSolidSprite;
        private static Sprite GetOrCreateSolidSprite()
        {
            if (cachedSolidSprite != null) return cachedSolidSprite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            cachedSolidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            cachedSolidSprite.hideFlags = HideFlags.HideAndDontSave;
            return cachedSolidSprite;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void EnsureTagExists(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            var so = new SerializedObject(assets[0]);
            var tags = so.FindProperty("tags");
            if (tags == null) return;

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }
    }
}
