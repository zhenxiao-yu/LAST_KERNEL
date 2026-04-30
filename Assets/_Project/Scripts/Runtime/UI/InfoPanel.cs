using System;

namespace Markyu.LastKernel
{
    public enum InfoPriority
    {
        Hover,      // Lowest — mouse hover tooltips
        Sequence,   // Mid — vendor unlocks, non-critical events
        Modal       // High — game-pausing events like end-of-day
    }

    // Static relay — always accessible from Runtime without a scene object.
    // InfoPanelController.OnBind() registers the real UIToolkit implementation.
    public sealed class InfoPanel
    {
        public static readonly InfoPanel Instance = new InfoPanel();

        private static Action<object, InfoPriority, (string, string), string, Action> _requestImpl;
        private static Action<object>           _clearImpl;
        private static Action<(string, string)> _registerHoverImpl;
        private static Action                   _unregisterHoverImpl;

        private InfoPanel() { }

        public static void Register(
            Action<object, InfoPriority, (string, string), string, Action> requestImpl,
            Action<object> clearImpl,
            Action<(string, string)> registerHoverImpl,
            Action unregisterHoverImpl)
        {
            _requestImpl        = requestImpl;
            _clearImpl          = clearImpl;
            _registerHoverImpl  = registerHoverImpl;
            _unregisterHoverImpl = unregisterHoverImpl;
        }

        public static void Unregister()
        {
            _requestImpl        = null;
            _clearImpl          = null;
            _registerHoverImpl  = null;
            _unregisterHoverImpl = null;
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
    }
}
