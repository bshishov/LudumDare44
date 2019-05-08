using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Debugger;
using UnityEngine;
using UnityEngine.AI;



public class AnimationController : MonoBehaviour
{
    public Animator Animator;
    public float SpeedMultiplier = 1f;
    private float _timeUntilDestroy = 5f;

    [Header("Material configuration")]
    public float ImpactDecayTime = 0.1f;
    public float DissolveTime = 1f;
    public Renderer[] Renderers;

    [Header("Animator configuration")]
    public bool DisableAnimationsAfterDeath = true;
    public bool UseMaterialAnimations;
    public bool AutoTrackSpeed = true;
    public float SmoothTime = 0.1f;
    public bool SeparateLegs = true;
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
    private float _impactTime;
    private Vector3 _toHell = new Vector3(0f, 0.01f, 0f);

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
        _impactTime = ImpactDecayTime;
    }

    public void PlayAttackAnimation()
    {
        if(_disabled)
            return;
        
        Animator.SetTrigger(Attack);
    }

    public void PlayHitImpactAnimation()
    {
        if (_disabled)
            return;

        _impactTime = ImpactDecayTime;
        if (TakeDamageVariations == null || TakeDamageVariations.Length == 0)
        {
            if(!string.IsNullOrEmpty(TakeDamage))
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

    private void Update()
    {
        if (_disabled && _timeUntilDestroy>0f)
        {
            _timeUntilDestroy -= Time.deltaTime;
            GetComponent<NavMeshAgent>().enabled = false;
        }
        if (_disabled && _timeUntilDestroy <= 0f)
        {            
            transform.position -= _toHell;
        }
        if (transform.position.y < -1f)
            Destroy(gameObject);

        _impactTime = Mathf.Max(_impactTime - Time.deltaTime, 0);
        if (Renderers != null)
        {
            var impactVal = _impactTime / ImpactDecayTime;
            foreach (var rndr in Renderers)
            {
                rndr.material.SetFloat("_TintMultiplier", impactVal * 0.3f);
            }
        }

        if (AutoTrackSpeed)
        {
            var moveDirection = transform.position - _lastPosition;
            moveDirection.y = 0;
            var speed = moveDirection.magnitude / Time.deltaTime;
            var dir = transform.InverseTransformDirection(moveDirection);
            var dir2d = new Vector2(dir.x, dir.z) * SpeedMultiplier * speed;

            _moveDir = Vector2.SmoothDamp(_moveDir, dir2d, ref _dirVelocity, SmoothTime);

            if (SeparateLegs)
            {
                Animator.SetFloat("SpeedX", _moveDir.x);
                Animator.SetFloat("SpeedZ", _moveDir.y);
            }

            Animator.SetFloat("Speed", _moveDir.magnitude);

            _lastPosition = transform.position;
        }
    }

    public void PlayCastAnimation()
    {
        if(_disabled)
            return;

        Animator.SetTrigger(CastTarget);
    }
}
