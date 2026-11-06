using System.Collections.Generic;
using HearTheTide.Core;
using UnityEngine;

namespace HearTheTide.Battle
{
    public abstract class Actor : MonoBehaviour
    {
        public BattleController Battle;
        public CombatStats Stats;
        public float Hp;
        public bool IsDead { get { return Hp <= 0f; } }
        protected Renderer BodyRenderer;
        private Color _baseColor;
        private float _flash;

        public void SetupVisual(GameObject visual)
        {
            BodyRenderer = visual.GetComponentInChildren<Renderer>();
            if (BodyRenderer != null) _baseColor = BodyRenderer.material.color;
        }

        public virtual void TakeDamage(float amount, Color textColor)
        {
            if (IsDead) return;
            Hp = Mathf.Max(0f, Hp - amount);
            _flash = 0.12f;
            Battle.Hud.Float(transform.position + Vector3.up * 2.4f, Mathf.RoundToInt(amount).ToString(), textColor);
            if (IsDead) OnDeath();
        }

        protected virtual void OnDeath() { }

        protected virtual void Update()
        {
            if (BodyRenderer == null) return;
            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                BodyRenderer.material.color = _flash > 0f ? Color.white : _baseColor;
            }
        }

        protected void FaceTowards(Vector3 target)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        protected void ClampToArena()
        {
            var p = transform.position;
            p.y = 0f;
            if (p.magnitude > BattleController.ArenaRadius) p = p.normalized * BattleController.ArenaRadius;
            transform.position = p;
        }
    }

    // ====================================================================== 女主（玩家）

    public sealed class PlayerActor : Actor
    {
        public const float MoveSpeed = 6.5f;
        public const float AttackRange = 2.8f;
        public const float DodgeDistance = 4.5f;
        public const float DodgeInvuln = 0.35f;
        public const int MaxDodgeCharges = 3;
        public const float DodgeRecharge = 2.5f;
        public const float SkillCooldown = 6f;
        public const float AssistCooldown = 10f;
        private static readonly float[] ComboMultipliers = { 1f, 1.1f, 1.25f, 1.7f };

        public Loadout Loadout;
        public Vector2 MoveInput;
        public float LastParryPressTime = -99f;
        public float LastDodgeTime = -99f;
        public bool Guarding;
        public float SkillReadyAt;
        public float AssistReadyAt;
        public int DodgeCharges = MaxDodgeCharges;

        private int _combo;
        private float _lastAttackTime = -99f;
        private float _dodgeRechargeTimer;
        private Vector3 _dashFrom, _dashTo;
        private float _dashT = 1f;

        public bool Invulnerable { get { return Time.time - LastDodgeTime < DodgeInvuln; } }

        protected override void Update()
        {
            base.Update();
            if (IsDead || Battle.Finished) return;
            ReadKeyboard();

            if (_dashT < 1f)
            {
                _dashT = Mathf.Min(1f, _dashT + Time.deltaTime / 0.2f);
                transform.position = Vector3.Lerp(_dashFrom, _dashTo, _dashT);
            }
            else if (!Guarding)
            {
                var move = new Vector3(MoveInput.x, 0f, MoveInput.y);
                if (move.sqrMagnitude > 1f) move.Normalize();
                if (move.sqrMagnitude > 0.01f)
                {
                    transform.position += move * MoveSpeed * Time.deltaTime;
                    transform.rotation = Quaternion.LookRotation(move);
                }
            }
            ClampToArena();

            if (DodgeCharges < MaxDodgeCharges)
            {
                _dodgeRechargeTimer += Time.deltaTime;
                if (_dodgeRechargeTimer >= DodgeRecharge) { _dodgeRechargeTimer = 0f; DodgeCharges++; }
            }
        }

        private void ReadKeyboard()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            // 编辑器 / PC 调试：WASD 移动，J 攻击，K 或空格 闪避，L 格挡（按住），U 技能，I 协攻，O 共鸣技
            var kb = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) kb.y += 1;
            if (Input.GetKey(KeyCode.S)) kb.y -= 1;
            if (Input.GetKey(KeyCode.A)) kb.x -= 1;
            if (Input.GetKey(KeyCode.D)) kb.x += 1;
            if (kb != Vector2.zero) MoveInput = kb;
            else if (Battle.Hud.Joystick != null) MoveInput = Battle.Hud.Joystick.Value;
            if (Input.GetKeyDown(KeyCode.J)) Attack();
            if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Space)) Dodge();
            if (Input.GetKeyDown(KeyCode.L)) ParryDown();
            if (Input.GetKeyUp(KeyCode.L)) ParryUp();
            if (Input.GetKeyDown(KeyCode.U)) Skill();
            if (Input.GetKeyDown(KeyCode.I)) Assist();
            if (Input.GetKeyDown(KeyCode.O)) Resonance();
#else
            if (Battle.Hud.Joystick != null) MoveInput = Battle.Hud.Joystick.Value;
#endif
        }

        public void Attack()
        {
            if (IsDead || Battle.Finished || Guarding) return;
            if (Time.time - _lastAttackTime < 0.3f) return;
            _combo = Time.time - _lastAttackTime < 0.9f ? (_combo + 1) % ComboMultipliers.Length : 0;
            _lastAttackTime = Time.time;
            var target = Battle.NearestEnemy(transform.position, AttackRange + 1.5f);
            if (target != null) FaceTowards(target.transform.position);
            float mul = ComboMultipliers[_combo];
            bool hit = false;
            foreach (var e in Battle.EnemiesInFront(transform, AttackRange))
            {
                e.TakeDamage(DamageFormula.Compute(Stats.Attack, mul, e.Stats.Defense), Color.white);
                hit = true;
            }
            if (hit) Battle.Resonance.Add(ResonanceGauge.HitGain);
        }

        public void Dodge()
        {
            if (IsDead || Battle.Finished || DodgeCharges <= 0) return;
            DodgeCharges--;
            LastDodgeTime = Time.time;
            var dir = new Vector3(MoveInput.x, 0, MoveInput.y);
            if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
            _dashFrom = transform.position;
            _dashTo = transform.position + dir.normalized * DodgeDistance;
            _dashT = 0f;
            Battle.OnPlayerResponse(TideResponse.Dodge);
        }

        public void ParryDown()
        {
            if (IsDead || Battle.Finished) return;
            Guarding = true;
            LastParryPressTime = Time.time;
            Battle.OnPlayerResponse(TideResponse.Parry);
        }

        public void ParryUp() { Guarding = false; }

        public void Skill()
        {
            if (IsDead || Battle.Finished || Time.time < SkillReadyAt) return;
            SkillReadyAt = Time.time + SkillCooldown;
            Battle.Hud.Float(transform.position + Vector3.up * 3f, "调律·震荡", UIColors.Skill);
            foreach (var e in Battle.EnemiesInRadius(transform.position, 4.5f))
                e.TakeDamage(DamageFormula.Compute(Stats.Attack, Loadout.SkillMultiplier, e.Stats.Defense), UIColors.Skill);
            Battle.Resonance.Add(ResonanceGauge.SkillGain);
        }

        public void Assist()
        {
            if (IsDead || Battle.Finished || Time.time < AssistReadyAt) return;
            AssistReadyAt = Time.time + AssistCooldown;
            Battle.Partner.DoAssist();
        }

        public void Resonance()
        {
            if (IsDead || Battle.Finished) return;
            if (!Battle.Resonance.TryConsume()) return;
            Battle.PlayResonanceSkill();
        }

        public void Heal(float amount)
        {
            Hp = Mathf.Min(Stats.MaxHp, Hp + amount);
            Battle.Hud.Float(transform.position + Vector3.up * 2.4f, "+" + Mathf.RoundToInt(amount), UIColors.Heal);
        }

        protected override void OnDeath() { Battle.OnPlayerDefeated(); }
    }

    // ====================================================================== 男主（AI 搭档）

    public sealed class PartnerActor : Actor
    {
        public CharacterDefinition Definition;
        private float _nextAttack;

        protected override void Update()
        {
            base.Update();
            if (Battle.Finished) return;
            var player = Battle.Player;
            var target = Battle.NearestEnemy(transform.position, 9f);
            Vector3 goal;
            if (target != null)
            {
                goal = target.transform.position;
                FaceTowards(goal);
                if (Vector3.Distance(transform.position, goal) < 2.4f)
                {
                    if (Time.time >= _nextAttack)
                    {
                        _nextAttack = Time.time + 1.2f;
                        target.TakeDamage(DamageFormula.Compute(Stats.Attack, 0.8f, target.Stats.Defense), UIColors.Partner);
                    }
                    return;
                }
            }
            else
            {
                goal = player.transform.position - player.transform.right * 1.8f;
                if (Vector3.Distance(transform.position, goal) < 0.6f) return;
                FaceTowards(goal);
            }
            transform.position = Vector3.MoveTowards(transform.position, goal, 6f * Time.deltaTime);
            ClampToArena();
        }

        /// <summary>协攻：优先打断红潮，否则释放男主专属技（玩法框架 4.4）。</summary>
        public void DoAssist()
        {
            var red = Battle.FindRedWindup();
            if (red != null)
            {
                transform.position = red.transform.position - red.transform.forward * 1.5f;
                FaceTowards(red.transform.position);
                red.Interrupt();
                Battle.Hud.Float(red.transform.position + Vector3.up * 3f, "打断！", UIColors.Rebound);
                Battle.Resonance.Add(10f);
                return;
            }

            Battle.Hud.Float(transform.position + Vector3.up * 3f, Definition.AssistSkillName, UIColors.Partner);
            switch (Definition.Role)
            {
                case CombatRole.Control:
                    foreach (var e in Battle.EnemiesInRadius(transform.position, 6f))
                    {
                        e.Stun(2.5f);
                        e.TakeDamage(DamageFormula.Compute(Stats.Attack, 1.5f, e.Stats.Defense), UIColors.Partner);
                    }
                    break;
                case CombatRole.Burst:
                    var t = Battle.NearestEnemy(transform.position, 20f);
                    if (t != null)
                    {
                        transform.position = t.transform.position - (t.transform.position - transform.position).normalized * 1.2f;
                        t.TakeDamage(DamageFormula.Compute(Stats.Attack, 4f, t.Stats.Defense), UIColors.Partner);
                    }
                    break;
                case CombatRole.Support:
                    Battle.Player.Heal(Battle.Player.Stats.MaxHp * 0.2f);
                    break;
                default:
                    foreach (var e in Battle.EnemiesInRadius(transform.position, 5f))
                        e.TakeDamage(DamageFormula.Compute(Stats.Attack, 2.5f, 0f), UIColors.Partner); // 破防：无视防御
                    break;
            }
        }

        public override void TakeDamage(float amount, Color textColor) { } // 原型阶段男主不受伤
    }

    public static class UIColors
    {
        public static readonly Color Skill = new Color(0.55f, 0.85f, 1f);
        public static readonly Color Partner = new Color(1f, 0.75f, 0.4f);
        public static readonly Color Heal = new Color(0.4f, 1f, 0.5f);
        public static readonly Color Rebound = new Color(1f, 0.85f, 0.3f);
        public static readonly Color Hurt = new Color(1f, 0.35f, 0.35f);

        public static Color Cue(TideCue cue)
        {
            switch (cue)
            {
                case TideCue.Blue: return new Color(0.25f, 0.55f, 1f);
                case TideCue.White: return new Color(0.95f, 0.95f, 0.95f);
                default: return new Color(1f, 0.2f, 0.2f);
            }
        }

        public static string CueHint(TideCue cue)
        {
            switch (cue)
            {
                case TideCue.Blue: return "蓝潮 · 踩点格挡";
                case TideCue.White: return "白潮 · 踩点闪避";
                default: return "红潮 · 呼叫协攻打断！";
            }
        }
    }
}
