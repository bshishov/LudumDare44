using Assets.Scripts.Data;
using Assets.Scripts.Utils.Debugger;
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

        public CharacterState ActiveTarget;
        public CharacterState ActiveAllyTarget; // For AI that will operate on allies (e.g. buffer)
        public float LastAttackTime;
        public float LastSpellCastTime;

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
    }
}