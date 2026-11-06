using HearTheTide.Core;
using UnityEngine;

namespace HearTheTide.Battle
{
    /// <summary>
    /// 蚀影（敌人）。轻攻击无提示；重攻击前有潮音提示（地面圆盘逐渐填满，填满即命中）。
    /// </summary>
    public sealed class EnemyActor : Actor
    {
        private enum State { Chase, Windup, Recover, Stunned }

        public bool IsBoss;
        public float MoveSpeed = 3.2f;
        public float AttackRange = 2.2f;
        public float HeavyRadius = 3f;
        public float HeavyChance = 0.45f;
        public float LightWindup = 0.5f;
        public float HeavyWindup = 1.1f;

        private State _state = State.Chase;
        private float _stateUntil;
        private bool _heavy;
        private TideCue _cue;
        private float _impactTime;
        private TideTelegraph _telegraph;

        public bool IsWindingUpRed { get { return _state == State.Windup && _heavy && _cue == TideCue.Red; } }
        public bool IsWindingUpHeavy { get { return _state == State.Windup && _heavy; } }
        public TideCue CurrentCue { get { return _cue; } }

        protected override void Update()
        {
            base.Update();
            if (IsDead || Battle.Finished) return;
            var player = Battle.Player;
            switch (_state)
            {
                case State.Chase:
                    FaceTowards(player.transform.position);
                    float dist = Vector3.Distance(transform.position, player.transform.position);
                    if (dist > AttackRange)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, player.transform.position, MoveSpeed * Time.deltaTime);
                        ClampToArena();
                    }
                    else BeginAttack();
                    break;

                case State.Windup:
                    if (_telegraph != null) _telegraph.SetProgress(1f - (_impactTime - Time.time) / HeavyWindup);
                    if (Time.time >= _impactTime) Impact();
                    break;

                case State.Recover:
                case State.Stunned:
                    if (Time.time >= _stateUntil) _state = State.Chase;
                    break;
            }
        }

        private void BeginAttack()
        {
            _heavy = Random.value < HeavyChance;
            _state = State.Windup;
            if (!_heavy)
            {
                _impactTime = Time.time + LightWindup;
                return;
            }
            float r = Random.value;
            _cue = r < 0.5f ? TideCue.Blue : r < 0.8f ? TideCue.White : TideCue.Red;
            _impactTime = Time.time + HeavyWindup;
            _telegraph = TideTelegraph.Create(transform.position, HeavyRadius, _cue);
            Battle.Hud.ShowCue(_cue);
        }

        private void Impact()
        {
            ClearTelegraph();
            _state = State.Recover;
            _stateUntil = Time.time + (IsBoss ? 0.6f : 0.9f);
            if (_heavy)
            {
                Battle.ResolveHeavyAttack(this, _cue, _impactTime, HeavyRadius, Stats.Attack * 2.5f);
            }
            else
            {
                var player = Battle.Player;
                if (Vector3.Distance(transform.position, player.transform.position) <= AttackRange + 0.4f && !player.Invulnerable)
                {
                    float dmg = DamageFormula.Compute(Stats.Attack, 1f, player.Stats.Defense);
                    if (player.Guarding) dmg *= 0.3f;
                    player.TakeDamage(dmg, UIColors.Hurt);
                }
            }
        }

        /// <summary>被打断（红潮 + 协攻）。</summary>
        public void Interrupt()
        {
            if (_state != State.Windup) return;
            ClearTelegraph();
            Stun(2f);
        }

        public void Stun(float seconds)
        {
            if (IsDead) return;
            ClearTelegraph();
            _state = State.Stunned;
            _stateUntil = Time.time + seconds;
        }

        private void ClearTelegraph()
        {
            if (_telegraph != null) Destroy(_telegraph.gameObject);
            _telegraph = null;
        }

        protected override void OnDeath()
        {
            ClearTelegraph();
            Battle.OnEnemyDied(this);
            Destroy(gameObject, 0.05f);
        }

        private void OnDestroy() { ClearTelegraph(); }
    }

    /// <summary>潮音提示：外圈为攻击范围，内圈从中心扩散，扩满即命中。</summary>
    public sealed class TideTelegraph : MonoBehaviour
    {
        private Transform _fill;
        private float _radius;

        public static TideTelegraph Create(Vector3 position, float radius, TideCue cue)
        {
            var go = new GameObject("TideCue_" + cue);
            go.transform.position = new Vector3(position.x, 0.02f, position.z);
            var t = go.AddComponent<TideTelegraph>();
            t._radius = radius;
            var color = UIColors.Cue(cue);
            var outer = Placeholder.Primitive(PrimitiveType.Cylinder, "Range", Color.Lerp(color, Color.black, 0.65f), go.transform);
            outer.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            var fill = Placeholder.Primitive(PrimitiveType.Cylinder, "Fill", color, go.transform);
            fill.transform.localPosition = new Vector3(0, 0.01f, 0);
            fill.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            t._fill = fill.transform;
            return t;
        }

        public void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            float d = Mathf.Max(0.01f, _radius * 2f * p);
            _fill.localScale = new Vector3(d, 0.01f, d);
        }
    }
}
