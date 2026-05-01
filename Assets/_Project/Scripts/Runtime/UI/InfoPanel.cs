using System;

namespace Markyu.LastKernel
{
    public enum InfoPriority
    {
        Hover,      // Lowest — mouse hover tooltips
        Sequence,   // Mid — vendor unlocks, non-critical events
        Modal       // High — game-pausing events like end-of-day
    }

    // Structured card data for rich hover tooltips.
    // Built by CardInstance.GetCardInfo() and consumed by InfoPanelController.
    public readonly struct CardInfoData
    {
        public readonly CardCategory Category;
        public readonly CombatType   Combat;
        public readonly int          CurrentHP;
        public readonly int          MaxHP;
        public readonly string       FormattedStats;
        public readonly int          SellPrice;
        public readonly int          Nutrition;
        public readonly int          UsesLeft;

        public bool HasHP        => MaxHP > 0;
        public bool HasCombat    => Combat != CombatType.None && !string.IsNullOrEmpty(FormattedStats);
        public bool HasSell      => SellPrice > 0;
        public bool HasNutrition => Nutrition > 0;
        public bool HasUses      => UsesLeft > 0;

        public CardInfoData(CardCategory category, CombatType combat, int currentHP, int maxHP,
            string formattedStats, int sellPrice, int nutrition, int usesLeft = 0)
        {
            Category       = category;
            Combat         = combat;
            CurrentHP      = currentHP;
            MaxHP          = maxHP;
            FormattedStats = formattedStats;
            SellPrice      = sellPrice;
            Nutrition      = nutrition;
            UsesLeft       = usesLeft;
        }
    }

    // Static relay — always accessible from Runtime without a scene object.
    // InfoPanelController.OnBind() registers the real UIToolkit implementation.
    public sealed class InfoPanel
    {
        public static readonly InfoPanel Instance = new InfoPanel();

        private static Action<object, InfoPriority, (string, string), string, Action> _requestImpl;
        private static Action<object>                           _clearImpl;
        private static Action<(string, string)>                _registerHoverImpl;
        private static Action                                   _unregisterHoverImpl;
        private static Action<(string, string), CardInfoData?>  _registerCardHoverImpl;

        private InfoPanel() { }

        public static void Register(
            Action<object, InfoPriority, (string, string), string, Action> requestImpl,
            Action<object> clearImpl,
            Action<(string, string)> registerHoverImpl,
            Action unregisterHoverImpl,
            Action<(string, string), CardInfoData?> registerCardHoverImpl = null)
        {
            _requestImpl           = requestImpl;
            _clearImpl             = clearImpl;
            _registerHoverImpl     = registerHoverImpl;
            _unregisterHoverImpl   = unregisterHoverImpl;
            _registerCardHoverImpl = registerCardHoverImpl;
        }

        public static void Unregister()
        {
            _requestImpl           = null;
            _clearImpl             = null;
            _registerHoverImpl     = null;
            _unregisterHoverImpl   = null;
            _registerCardHoverImpl = null;
        }

        public void RequestInfoDisplay(
            object requester,
            InfoPriority priority,
            (string header, string body) info,
            string buttonLabel = null,
            Action buttonAction = null)
            => _requestImpl?.Invoke(requester, priority, info, buttonLabel, buttonAction);

        public void ClearInfoRequest(object requester) => _clearImpl?.Invoke(requester);

        public void RegisterHover((string header, string body) info) => _registerHoverImpl?.Invoke(info);

        public void UnregisterHover() => _unregisterHoverImpl?.Invoke();

        public void RegisterCardHover((string header, string body) info, CardInfoData? cardInfo)
            => _registerCardHoverImpl?.Invoke(info, cardInfo);
    }
}
