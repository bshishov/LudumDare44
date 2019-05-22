using System.Collections;
using Actors;
using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Utils.UI;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UICanvasGroupFader))]
public class UINotificationBar : MonoBehaviour
{
    public Sprite DefaultSprite;
    public Image Icon;
    public TextMeshProUGUI Caption;
    public TextMeshProUGUI Text;

    private UICanvasGroupFader _fader;
    private Coroutine _showRoutine;
    private CharacterState _character;

    void Start()
    {
        _fader = GetComponent<UICanvasGroupFader>();
        _character = LocatePlayer();
        if (_character != null)
        {
            _character.OnItemPickup += (item, stacks) => { ShowItemInfo(item); };
            _character.OnSpellPickup += (item, stacks) => { ShowSpellInfo(item); };
        }
    }

    void SetIcon(Sprite sprite)
    {
        Icon.sprite = sprite == null ? DefaultSprite : sprite;
    }

    IEnumerator HideAfter(float time)
    {
        yield return new WaitForSeconds(time);
        _fader.FadeOut();
    }

    CharacterState LocatePlayer()
    {
        var obj = GameObject.FindGameObjectWithTag(Common.Tags.Player);
        if (obj == null)
            return null;

        return obj.GetComponent<CharacterState>();
    }

    public void ShowItemInfo(Item item, float duration = 2f)
    {
        SetIcon(item.Icon);
        Caption.text = item.Name;
        Text.text = item.Description;
        _fader.FadeIn();

        if(_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(HideAfter(duration));
    }

    public void ShowSpellInfo(Spell spell, float duration = 2f)
    {
        SetIcon(spell.Icon);
        Caption.text = spell.Name;
        Text.text = spell.Description;
        _fader.FadeIn();

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(HideAfter(duration));
    }
}
