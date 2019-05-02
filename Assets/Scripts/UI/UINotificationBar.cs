using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UICanvasGroupFader))]
public class UINotificationBar : Singleton<UINotificationBar>
{
    public Sprite DefaultSprite;
    public Image Icon;
    public TextMeshProUGUI Caption;
    public TextMeshProUGUI Text;

    private UICanvasGroupFader _fader;
    private Coroutine _showRoutine;

    void Start()
    {
        _fader = GetComponent<UICanvasGroupFader>();
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
