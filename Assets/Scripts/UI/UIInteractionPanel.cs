using Actors;
using Assets.Scripts;
using Assets.Scripts.Utils.UI;
using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(UICanvasGroupFader))]
    public class UIInteractionPanel : MonoBehaviour
    {
        public GameObject InteractionLinePrefab;
        public Transform LineContainer;

        private UICanvasGroupFader _fader;
        private Interactor _interactor;

        void Start()
        {
            _fader = GetComponent<UICanvasGroupFader>();
            _fader.StateChanged += StateChanged;
            _interactor = GameObject.FindGameObjectWithTag(Common.Tags.Player).GetComponent<Interactor>();
            if (_interactor != null)
                _interactor.OnInteractableChanged += OnInteractableChanged;
        }

        private void StateChanged()
        {
            if (_fader.State == UICanvasGroupFader.FaderState.FadedOut)
            {
            }
        }

        public void Hide()
        {
            if (_fader.IsShowing)
                _fader.FadeOut();
        }

       
        private void AddLine(string text)
        {
            var go = GameObject.Instantiate(InteractionLinePrefab, LineContainer, false);
            var txt = go.GetComponent<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = text;
            }
        }

        private void OnInteractableChanged(IInteractable oldInteractable, IInteractable newInteractable)
        {
            foreach (Transform child in LineContainer)
                GameObject.Destroy(child.gameObject);

            if (newInteractable != null)
            {
                if (newInteractable.Type == InteractableType.DroppedSpell)
                {
                    AddLine($"<<color=red>E</color>> {newInteractable.GetInteractionText(Interaction.Pick)}");
                    AddLine($"<<color=red>Q</color>> {newInteractable.GetInteractionText(Interaction.Dismantle)}");
                }

                if (newInteractable.Type == InteractableType.DroppedItem)
                {
                    AddLine($"<<color=red>E</color>> {newInteractable.GetInteractionText(Interaction.Pick)}");
                }

                if (newInteractable.Type == InteractableType.Shop)
                {
                    AddLine($"<<color=red>E</color>> {newInteractable.GetInteractionText(Interaction.Buy)}");
                }

                _fader.FadeIn();
            }
            else
            {
                _fader.FadeOut();
            }
        }
    }
}
