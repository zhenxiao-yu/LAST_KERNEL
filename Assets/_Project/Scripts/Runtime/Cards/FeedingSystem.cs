// End-of-day feeding coroutine.
//
// DayCycleManager orchestrates WHEN feeding happens; FeedingSystem handles HOW.
// Kept separate from CardManager because it needs direct access to CardSettings
// (HungerPerCharacter) and the camera controller — exposing those through
// ICardService would widen the interface without real benefit.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    internal sealed class FeedingSystem
    {
        private readonly Func<IEnumerable<CardInstance>> getAllCards;
        private readonly CardSettings cardSettings;
        private readonly Action notifyStatsChanged;

        public FeedingSystem(
            Func<IEnumerable<CardInstance>> getAllCards,
            CardSettings cardSettings,
            Action notifyStatsChanged)
        {
            this.getAllCards = getAllCards;
            this.cardSettings = cardSettings;
            this.notifyStatsChanged = notifyStatsChanged;
        }

        public IEnumerator FeedCharacters()
        {
            var allCards = getAllCards().ToList();

            var characterCards = allCards
                .Where(card => card.Definition.Category == CardCategory.Character)
                .ToList();

            var consumableCards = allCards
                .Where(card => card.Definition.Category == CardCategory.Consumable && card.CurrentNutrition > 0)
                .ToList();

            foreach (var character in characterCards)
            {
                if (character == null) continue;

                if (TryGetCameraController(out var cam))
                    yield return cam.MoveTo(character.transform.position);

                int hungerLeft = cardSettings.HungerPerCharacter;

                while (hungerLeft > 0)
                {
                    if (consumableCards.Count == 0)
                    {
                        character.Kill();
                        break;
                    }

                    var nearestConsumable = consumableCards
                        .OrderBy(c => Vector3.Distance(character.transform.position, c.transform.position))
                        .First();

                    consumableCards.Remove(nearestConsumable);

                    yield return nearestConsumable.Consume(
                        character,
                        hungerLeft,
                        nutrition =>
                        {
                            hungerLeft -= nutrition;

                            float healFraction = (float)nutrition / cardSettings.HungerPerCharacter;
                            int maxHealth = character.Stats.MaxHealth.Value;
                            int healAmount = Mathf.RoundToInt(maxHealth * 0.5f * healFraction);
                            int maxPossibleHeal = maxHealth - character.CurrentHealth;
                            character.Heal(Mathf.Min(healAmount, maxPossibleHeal));
                        }
                    );

                    if (nearestConsumable != null && nearestConsumable.CurrentNutrition > 0)
                        consumableCards.Add(nearestConsumable);

                    notifyStatsChanged();
                }

                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private static bool TryGetCameraController(out CameraController cameraController)
        {
            cameraController = null;
            Camera mainCamera = Camera.main;
            if (mainCamera == null || mainCamera.transform.parent == null) return false;
            return mainCamera.transform.parent.TryGetComponent(out cameraController);
        }
    }
}
