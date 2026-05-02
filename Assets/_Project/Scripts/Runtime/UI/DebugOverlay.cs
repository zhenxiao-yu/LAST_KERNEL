#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// In-game debug overlay. Toggle with F1 or backtick.
    /// Only compiled in Editor or Development Build.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private bool _visible = false;
        private float _fps;
        private float _fpsSampleTimer;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private bool _stylesReady;

        private const float FpsSampleInterval = 0.5f;
        private const KeyCode ToggleKey1 = KeyCode.F1;
        private const KeyCode ToggleKey2 = KeyCode.BackQuote;

        private void Update()
        {
            InputManager input = InputManager.Instance;
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool togglePressed = input != null
                ? input.WasKeyPressedThisFrame(ToggleKey1) || input.WasKeyPressedThisFrame(ToggleKey2)
                : keyboard != null &&
                  (keyboard.f1Key.wasPressedThisFrame || keyboard.backquoteKey.wasPressedThisFrame);
#else
            bool togglePressed = input != null
                ? input.WasKeyPressedThisFrame(ToggleKey1) || input.WasKeyPressedThisFrame(ToggleKey2)
                : Input.GetKeyDown(ToggleKey1) || Input.GetKeyDown(ToggleKey2);
#endif

            if (togglePressed)
                _visible = !_visible;

            if (!_visible) return;

            _fpsSampleTimer += Time.unscaledDeltaTime;
            if (_fpsSampleTimer >= FpsSampleInterval)
            {
                _fps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
                _fpsSampleTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            string content = BuildContent();
            GUIContent guiContent = new GUIContent(content);
            Vector2 size = _labelStyle.CalcSize(guiContent);
            size.x += 20f;
            size.y += 16f;

            Rect rect = new Rect(10f, 10f, size.x, size.y);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, size.x - 16f, size.y - 12f), content, _labelStyle);
        }

        private string BuildContent()
        {
            // CardInstance is a MonoBehaviour — FindObjectsByType works fine.
            // CardStack / CraftingTask / CombatTask are plain C# classes;
            // query them through their manager singletons instead.
            CardInstance[] allCards = FindObjectsByType<CardInstance>(FindObjectsInactive.Exclude);
            int cardCount = allCards.Length;
            int stackCount = allCards.Select(c => c.Stack).Where(s => s != null).Distinct().Count();

            int craftingCount = CraftingManager.Instance != null
                ? GetCraftingTaskCount()
                : 0;
            int combatCount = CombatManager.Instance != null
                ? CombatManager.Instance.ActiveCombats.Count()
                : 0;

            CardInstance dragged = System.Array.Find(allCards, c => c.IsBeingDragged);

            return
                $"FPS: {_fps:F0}\n" +
                $"Cards: {cardCount}  Stacks: {stackCount}\n" +
                $"Crafting: {craftingCount}  Combat: {combatCount}\n" +
                $"Language: {GameLocalization.CurrentLanguage}\n" +
                $"Dragging: {(dragged != null ? dragged.Definition?.name ?? "—" : "none")}\n" +
                "[F1 / ` to toggle]";
        }

        private static int GetCraftingTaskCount()
        {
            // activeCraftingTasks is private; reflect to avoid modifying CraftingManager.
            var field = typeof(CraftingManager).GetField(
                "activeCraftingTasks",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) return 0;
            var list = field.GetValue(CraftingManager.Instance) as System.Collections.ICollection;
            return list?.Count ?? 0;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _boxStyle = new GUIStyle(GUI.skin.box);
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
            bgTex.Apply();
            _boxStyle.normal.background = bgTex;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                richText = false
            };
            _labelStyle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _stylesReady = true;
        }
    }
}
#endif
