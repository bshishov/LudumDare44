using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Utils.Debugger;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Cheats : MonoBehaviour
{
    public Buff[] Buffs;
    public Spell[] Spells;

    private PlayerController _playerController;
    private CharacterState _playerState;

    void Start()
    {
        _playerController = FindObjectOfType<PlayerController>();
        _playerState = _playerController.GetComponent<CharacterState>();

        foreach (var buff in Buffs)
        {
            Debugger.Default.Display(string.Format("Cheats/Apply Buffs/{0}", buff.name), () =>
            {
                _playerState.CastBuff(buff);
            });
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Load all resources")]
    public void LoadAllResources()
    {
        Buffs = FindAssetsByType<Buff>().ToArray();
        Spells = FindAssetsByType<Spell>().ToArray();
    }

    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }
        return assets;
    }
#endif
}
