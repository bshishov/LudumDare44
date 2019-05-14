using System;
using Actors;
using Assets.Scripts.Data;
using Assets.Scripts.Utils.Debugger;
using Data;
using Spells;
using UnityEngine;
using Logger = Assets.Scripts.Utils.Debugger.Logger;

namespace AI
{
    /// <summary>
    /// All the necessary data required for the agent to behave
    /// i.e. agent's memory.
    /// </summary>
    public class AIAgent
    {
        public readonly EnemyController EnemyController;
        public readonly CharacterState CharacterState;
        public readonly MovementController Movement;
        public readonly AnimationController AnimationController;
        public readonly SpellbookState SpellBook;
        public readonly CharacterConfig Config;
        public readonly Logger Logger;
        public readonly Transform transform;

        // Operational memory
        public CharacterState ActiveTarget;
        public CharacterState ActiveAllyTarget; // For AI that will operate on allies (e.g. buffer)
        public float LastAttackTime;
        public float LastSpellCastTime;
        public AIConfig.AISlotConfig IntendedSpell;

        public AIAgent(EnemyController enemyController)
        {
            // Cache everything once and for all
            Logger = Debugger.Default.GetLogger(enemyController.gameObject.name + "/AI Log", unityLog: false);
            EnemyController = enemyController;
            transform = enemyController.gameObject.transform;
            CharacterState = enemyController.GetComponent<CharacterState>();
            Movement = enemyController.GetComponent<MovementController>();
            AnimationController = enemyController.GetComponent<AnimationController>();
            SpellBook = enemyController.GetComponent<SpellbookState>();
            Config = CharacterState.character;
        }

        public float GetLineDistanceToTarget()
        {
            return Vector3.Distance(transform.position, ActiveTarget.transform.position);
        }

        public bool HasTarget()
        {
            return ActiveTarget != null && ActiveTarget.IsAlive;
        }

        public bool IsBetweenFearAndAggro()
        {
            var distance = Vector3.Distance(transform.position, ActiveTarget.transform.position);
            if (distance < Config.AI.FearRange)
                return false;
            if (distance > Config.AI.AggroRange)
                return false;
            return true;
        }

        public bool IsAlive()
        {
            return CharacterState.IsAlive;
        }

        public bool CanCast()
        {
            // No target to cast on
            if (!HasTarget())
                return false;

            // Cooldown has not passed yet
            if (Time.time < Config.AI.SpellCastingCooldown + LastSpellCastTime)
                return false;

            // Too far
            //if (GetLineDistanceToTarget() > Config.AI.MaxCastRange)
                //return false;

            return true;
        }

        public float GetNavigationDistanceToTarget()
        {
            throw new NotImplementedException();
        }
    }
}