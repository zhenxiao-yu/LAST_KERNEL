using UnityEngine;

namespace Markyu.LastKernel
{
    public class PackInstance : CardInstance, IClickable
    {
        public new PackDefinition Definition
        {
            get => (PackDefinition)base.Definition;
            protected set => base.Definition = value;
        }

        public bool OnClick(Vector3 clickPosition)
        {
            if (Stack != null) Stack.IsLocked = true;

            PullFromNextSlot();

            if (Stack != null)
            {
                Vector3 groundPos = Stack.TargetPosition.Flatten();
                Stack.SetTargetPosition(groundPos);
                Stack.IsLocked = false;
            }

            return true; // We handled the click
        }

        private void PullFromNextSlot()
        {
            if (UsesLeft <= 0)
            {
                Debug.LogWarning("No more slots left in this pack.");
                return;
            }

            var slot = Definition.Slots[Definition.Slots.Count - UsesLeft];
            var cardDefinition = slot.GetRandomCard();

            Vector3 spawnPos = Stack.TargetPosition + new Vector3(Size.x + 0.1f, 0f, 0f);
            if (Board.Instance != null)
                spawnPos = Board.Instance.EnforcePlacementRules(spawnPos, null);
            CardManager.Instance?.CreateCardInstance(cardDefinition, spawnPos.Flatten(), Stack);

            Use();

            if (UsesLeft <= 0)
            {
                TradeManager.Instance?.NotifyPackOpened(Definition);
                Kill();
            }
        }
    }
}

