using Actors;
using Data;
using Spells;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using Utils.Debugger;
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
        _playerController = FindObjectOfType<PlayerController>();
        _playerState = _playerController.GetComponent<CharacterState>();
        _playerSpellbookState = _playerController.GetComponent<SpellbookState>();

        Debugger.Default.Display($"Cheats/GOD mode", () =>
            {
                _playerState.ApplyModifier(ModificationParameter.HpFlat, 10000, 1, 1, _playerState, null);
                _playerState.ApplyModifier(ModificationParameter.MaxHpFlat, 10000, 1, 1, _playerState, null);
            });

        Debugger.Default.Display($"Cheats/WTF mode", () =>
            {
                _playerSpellbookState.NoCooldowns = !_playerSpellbookState.NoCooldowns;
            });

        if (Buffs != null)
            foreach (var buff in Buffs)
            {
                if (buff == null)
                    continue;

                Debugger.Default.Display($"Cheats/Apply Buffs/{buff.name}",
                    () => { _playerState.ApplyBuff(buff, _playerState, null, 1); });
            }

        foreach (var spell in Spells)
        {
            if (spell == null)
                continue;

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
            if (item == null)
                continue;

            Debugger.Default.Display($"Cheats/Pickup Item/{item.name}", () => { _playerState.Pickup(item, 1); });
        }

        foreach (var enemy in Enemies)
        {
            if (enemy == null)
                continue;

            Debugger.Default.Display($"Cheats/Spawn Enemy/{enemy.name}",
                () => { GameObject.Instantiate(enemy, _playerState.transform.position, Quaternion.identity); });
            Debugger.Default.Display($"Cheats/Spawn Enemy/{enemy.name}/x10",
                () =>
                {
                    for (var i = 0; i < 10; ++i)
                    {
                        var offset = Random.onUnitSphere;
                        offset.y = 0;
                        offset = offset.normalized;
                        GameObject.Instantiate(enemy, _playerState.transform.position + offset * 2f, Quaternion.identity);
                    }
                });
        }

#if UNITY_EDITOR
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                Debugger.Default.Display($"Cheats/Load scene (Editor)/{scene.path}", () =>
                {
                    SceneManager.LoadScene(scene.path);
                });
            }

            
        }
#endif

        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debugger.Default.Display($"Cheats/Load Scene/{i} {sceneName}", () =>
            {
                SceneManager.LoadScene(sceneName);
            });
        }
    }

    void Update()
    {
#if DEBUG
        if (_playerController == null || _playerSpellbookState == null)
            return;

        var spellSlotIdx = 0;
        foreach (var spellbookStateSpellSlot in _playerSpellbookState.SpellSlots)
        {
            var path = _playerController.gameObject.name + "/SpellbookState/Slot " + spellSlotIdx;
            Debugger.Default.Display(path + "/RemainingCooldown", spellbookStateSpellSlot.RemainingCooldown);
            Debugger.Default.Display(path + "/State", spellbookStateSpellSlot.State.ToString());
            if (spellbookStateSpellSlot.Spell != null)
                Debugger.Default.Display(path + "/Spell", spellbookStateSpellSlot.Spell.name);
            spellSlotIdx++;
        }
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Load all resources")]
    public void LoadAllResources()
    {
        Buffs = AssetUtility.FindAssetsOfType<Buff>().ToArray();
        Spells = AssetUtility.FindAssetsOfType<Spell>().ToArray();
        Items = AssetUtility.FindAssetsOfType<Item>().ToArray();
    }
#endif
}