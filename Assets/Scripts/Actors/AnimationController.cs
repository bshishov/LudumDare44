﻿using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Debugger;
using UnityEngine;


public class AnimationController : MonoBehaviour
{
    public Animator Animator;
    public float SpeedMultiplier = 1f;

    public bool DisableAnimationsAfterDeath = true;
    public bool UseMaterialAnimations;
    public bool AutoTrackSpeed = true;
    public float SmoothTime = 0.1f;

    [Header("Animator configuration")]
    public string DeathTrigger = "Death";
    public string TakeDamage = "TakeDamage";
    public string SpeedVariable = "Speed";
    public string CastAoE = "CastAoE";
    public string CastTarget = "CastTarget";
    public string CastProjectile = "CastProjectile";
    public string Attack = "CastTarget";
    public string Pickup = "Pickup";

    public string[] DeathVariations;
    public string[] TakeDamageVariations;

    // TODO: IK ?

    private bool _disabled = false;
    private Vector3 _lastPosition;
    private Vector2 _dirVelocity;
    private Vector2 _moveDir;


    void Start()
    {
        if (Animator == null)
        {
            Animator = GetComponent<Animator>();
        }

        if (Animator == null)
        {
            Debug.LogErrorFormat("Animation controller requires Animator component: {0}", gameObject.name);
        }

        Debugger.Default.Display("Animation/Play Death", PlayDeathAnimation);
        Debugger.Default.Display("Animation/Play Attack", PlayAttackAnimation);
        //Debugger.Default.Display("Animation/Play Cast AoE", () =>
        //{
        //    PlayCastAnimation(SubSpell.SpellTypes.Aoe);
        //});
        //Debugger.Default.Display("Animation/Play Cast Projectile", () =>
        //{
        //    PlayCastAnimation(SubSpell.SpellTypes.Projectile);
        //});
        //Debugger.Default.Display("Animation/Play Cast Raycast", () =>
        //{
        //    PlayCastAnimation(SubSpell.SpellTypes.Raycast);
        //});
        Debugger.Default.Display("Animation/Force enable", () => { _disabled = false; });

        _lastPosition = transform.position;
    }

    public void PlayDeathAnimation()
    {
        if(_disabled)
            return;

        if (DisableAnimationsAfterDeath)
            _disabled = true;
        if (DeathVariations == null || DeathVariations.Length == 0)
        {
            Animator.SetTrigger(DeathTrigger);
        }
        else
        {
            Animator.SetTrigger(RandomUtils.Choice(DeathVariations));
        }
    }

    public void PlayAttackAnimation()
    {
        if(_disabled)
            return;
        
        Animator.SetTrigger(Attack);
    }

    //public void PlayCastAnimation(Spell.SpellTypes spellType = SubSpell.SpellTypes.Raycast)
    //{
    //    if (_disabled)
    //        return;

    //    switch (spellType)
    //    {
    //        case SubSpell.SpellTypes.Raycast:
    //            Animator.SetTrigger(CastTarget);
    //            break;
    //        case SubSpell.SpellTypes.Aoe:
    //            Animator.SetTrigger(CastAoE);
    //            break;
    //        case SubSpell.SpellTypes.Projectile:
    //            Animator.SetTrigger(CastProjectile);
    //            break;
    //    }
    //}

    public void PlayHitImpactAnimation()
    {
        if (_disabled)
            return;

        if (TakeDamageVariations == null || TakeDamageVariations.Length == 0)
        {
            Animator.SetTrigger(TakeDamage);
        }
        else
        {
            Animator.SetTrigger(RandomUtils.Choice(TakeDamageVariations));
        }
    }

    public void SetSpeed(float speed)
    {
        Animator.SetFloat(SpeedVariable, speed * SpeedMultiplier);
    }

    public void PlayPickupAnimation()
    {
        if(_disabled)
            return;

        Animator.SetTrigger(Pickup);
    }

    void Update()
    {
        if (AutoTrackSpeed)
        {
            var moveDirection = transform.position - _lastPosition;
            moveDirection.y = 0;
            var speed = moveDirection.magnitude / Time.deltaTime;
            var dir = transform.InverseTransformDirection(moveDirection);
            var dir2d = new Vector2(dir.x, dir.z) * SpeedMultiplier * speed;

            _moveDir = Vector2.SmoothDamp(_moveDir, dir2d, ref _dirVelocity, SmoothTime);

            Animator.SetFloat("SpeedX", _moveDir.x);
            Animator.SetFloat("SpeedZ", _moveDir.y);

            _lastPosition = transform.position;
        }
    }
}
