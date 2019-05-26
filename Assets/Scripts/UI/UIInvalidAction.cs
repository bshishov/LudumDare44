using System.Collections;
using Assets.Scripts.Utils.UI;
using TMPro;
using UnityEngine;
using Utils;

namespace UI
{
    [RequireComponent(typeof(UICanvasGroupFader))]
    public class UIInvalidAction : Singleton<UIInvalidAction>
    {
        public TextMeshProUGUI Text;
        public float ShowDuration = 1f;

        private UICanvasGroupFader _fader;
        private Coroutine _showRoutine;

        public enum InvalidAction
        {
            InvalidTarget,
            NotEnoughBlood,
            CantCastSpell,
            SpellIsNotReady
        }

        void Start()
        {
            _fader = GetComponent<UICanvasGroupFader>();
        }
        
        public void Show(InvalidAction invalidAction)
        {
            switch (invalidAction)
            {
                case InvalidAction.InvalidTarget:
                    Text.text = "Invalid Target";
                    break;
                case InvalidAction.NotEnoughBlood:
                    Text.text = "Not enough blood";
                    break;
                case InvalidAction.CantCastSpell:
                    Text.text = "Can't cast";
                    break;
                case InvalidAction.SpellIsNotReady:
                    Text.text = "Not ready";
                    break;
            }
            _fader.FadeIn();
            if (_showRoutine != null)
                StopCoroutine(_showRoutine);

            _showRoutine = StartCoroutine(HideAfter(ShowDuration));
        }

        IEnumerator HideAfter(float time)
        {
            yield return new WaitForSeconds(time);
            _fader.FadeOut();
        }
    }
}
