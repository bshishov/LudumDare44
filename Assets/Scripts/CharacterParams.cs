using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;
using System.Linq;

public class CharacterParams : MonoBehaviour
{
    public CharacterConfig character;
    private float Health;    
    private float Speed;

    public float Damage;
    private float DropRate;
    private Spell[] DropSpells;
    private Spell[] UseSpells;

    private Dictionary<Buff, float> BuffsOn;
  


    // Start is called before the first frame update
    void Start()
    {
        Health = character.Health;
        Speed = character.Speed;
        Damage = character.Damage;
        DropRate = character.DropRate;
        UseSpells = character.UseSpells;
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Buff buff in BuffsOn.Keys.ToList())
        {
            if (!buff.Permanent)
            {
                BuffsOn[buff] -= Time.deltaTime;
            }

            if (BuffsOn[buff] > 0) {

            }
            else
            {
                BuffsOn.Remove(buff);
            }            
        }
    }

    void CastBuff(Buff buff)
    {
        if (BuffsOn.ContainsKey(buff))
        {
            BuffsOn[buff] = buff.Duration;
        }
        else
        {
            BuffsOn.Add(buff, buff.Duration);
        }
    }
}
