using System.Collections;
using System.Collections.Generic;
using Actors;
using Assets.Scripts.Data;
using Assets.Scripts.Utils.Debugger;
using Spells;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Cheats : MonoBehaviour
{
    public Buff[] Buffs;
    public Spell[] Spells;
    public Item[] Items;
    public GameObject[] Enemies;

    private PlayerController _playerController;
    private CharacterState _playerState;
    private SpellbookState _playerSpellbookState;

    void Start()
    {
        _playerController     = FindObjectOfType<PlayerController>();
        _playerState          = _playerController.GetComponent<CharacterState>();
        _playerSpellbookState = _playerController.GetComponent<SpellbookState>();

        if (Buffs != null)
            foreach (var buff in Buffs)
            {
                Debugger.Default.Display($"Cheats/Apply Buffs/{buff.name}", () => { _playerState.ApplyBuff(buff, _playerState, null, 1); });
            }

        foreach (var spell in Spells)
        {
            Debugger.Default.Display($"Cheats/Pickup Spell/{spell.name}", () => { _playerState.Pickup(spell, 1); });

            Debugger.Default.Display($"Cheats/Drop Spell/{spell.name}",
                                     () =>
                                     {
                                         DroppedSpell.InstantiateDroppedSpell(spell,
                                                                              _playerState.GetNodeTransform(CharacterState.NodeRole.Chest).transform.position);
                                     });
        }

        foreach (var item in Items)
        {
            Debugger.Default.Display($"Cheats/Pickup Item/{item.name}", () => { _playerState.Pickup(item, 1); });
        }

        foreach (var enemy in Enemies)
        {
            Debugger.Default.Display($"Cheats/Spawn Enemy/{enemy.name}",
                                     () => { GameObject.Instantiate(enemy, _playerState.transform.position, Quaternion.identity); });
            Debugger.Default.Display($"Cheats/Spawn Enemy/{enemy.name}/x10",
                                     () =>
                                     {
                                         for (var i = 0; i < 10; ++i)
                                         {
                                             GameObject.Instantiate(enemy, _playerState.transform.position, Quaternion.identity);
                                         }
                                     });
        }
    }

    void Update()
    {
#if DEBUG
        if(_playerController == null || _playerSpellbookState == null)
            return;

        var spellSlotIdx = 0;
        foreach (var spellbookStateSpellSlot in _playerSpellbookState.SpellSlots)
        {
            var path = _playerController.gameObject.name + "/SpellbookState/Slot " + spellSlotIdx;
            Debugger.Default.Display(path + "/RemainingCooldown", spellbookStateSpellSlot.RemainingCooldown);
            Debugger.Default.Display(path + "/State", spellbookStateSpellSlot.State.ToString());
            if(spellbookStateSpellSlot.Spell != null)
                Debugger.Default.Display(path + "/Spell", spellbookStateSpellSlot.Spell.name);
            spellSlotIdx++;
        }
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Load all resources")]
    public void LoadAllResources()
    {
        Buffs = FindAssetsByType<Buff>().ToArray();
        Spells = FindAssetsByType<Spell>().ToArray();
        Items = FindAssetsByType<Item>().ToArray();
    }

    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
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
