using System;
using System.Collections.Generic;
using Godot;
using Taiju.Objects.Effect;
using Taiju.Objects.Enemy;
using Taiju.UI.HUD;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Witch;

public partial class Sora : ReversibleRigidBody3D {
  /* Constants */
  private struct Constant {
    public const string FireButtonName = "fire";
    public const string SpellButtonName = "spell";
    public const string MoveRightButtonName = "move_right";
    public const string MoveLeftButtonName = "move_left";
    public const string MoveUpButtonName = "move_up";
    public const string MoveDownButtonName = "move_down";
    public const double MoveDelta = 16.0;
  }

  /* Assets */
  [Export(PropertyHint.Range, "0,0.2,")] private double nextBulletDuration_ = 0.08;
  private Node3D witchField_;
  private SoraBulletServer bulletServer_;
  private Node3D bulletNode_;
  private PackedScene arrowAsset_;
  private SpellGauge spellGauge_;

  /* Spell Related Types */
  private struct SpellConstant {
    public const double InvokingTime = 4.0;
  }
  internal record struct SpellRecord {
    public bool Invoking;
    public double InvocationLeftTime;
  }

  internal record struct WitchRecord {
    public bool Chitose;
    public bool Momiji;
    public bool Kaede;
  }

  /* Record Type */
  internal record struct Record {
    public Vector3 Position;
    public double SpiritRot;
    public double AfterFire;
    // Spell
    public SpellRecord Spell;
    // Other Witches.
    public WitchRecord Witch;
  }

  internal record struct ShotRange {
    public double BeginTime;
    public double EndTime;
    public uint EndTick;
  }

  internal enum CloneType {
    Momiji,
    Kaede,
  }

  internal record struct MetaRecord {
    public int NumMagicElements;
    public CloneType CloneType;
  }

  /* ******************************************************************************************************************
   * Statuses
   * ******************************************************************************************************************/

  private Dense<Record> record_;
  private LinkedList<ShotRange> shotRanges_;
  private MetaRecord meta_;
  public bool IsCollisionEnabled => !record_.Ref.Spell.Invoking;

  /* ******************************************************************************************************************
   * Child nodes
   * ******************************************************************************************************************/

  private CollisionShape3D collisionShape_;
  private Node3D soraModel_;
  private Node3D soraSoul_;
  private Node3D chitoseModel_;
  private AudioStreamPlayer3D arrowSoundPlayer_;

  public override void _Ready() {
    base._Ready();
    witchField_ = GetNode<Node3D>("/root/Root/Field/Witch")!;
    bulletServer_ = GetNode<SoraBulletServer>("/root/Root/Field/WitchBullet/SoraBulletServer")!;
    bulletNode_ = GetNode<Node3D>("/root/Root/Field/WitchBullet")!;
    arrowAsset_ = ResourceLoader.Load<PackedScene>("res://Objects/Effect/Arrow.tscn")!;
    spellGauge_ = GetNode<SpellGauge>("/root/Root/Field/HUD/SpellGauge")!;
    // Initial State
    record_ = new Dense<Record>(Clock, new Record {
      Position = Position,
      SpiritRot = 0.0,
      AfterFire = 0.0,
      // Spell
      Spell = new SpellRecord {
        Invoking = false,
        InvocationLeftTime = 0.0,
      },
      // Other Witches.
      Witch = new WitchRecord {
        Chitose = true,
        Momiji = true,
        Kaede = true,
      },
    });
    shotRanges_ = [];
    meta_ = new MetaRecord {
      NumMagicElements = 0,
      CloneType = CloneType.Momiji,
    };
    collisionShape_ = GetNode<CollisionShape3D>("Shape");
    soraModel_ = GetNode<Node3D>("SoraModel")!;
    soraSoul_ = soraModel_.GetNode<Node3D>("Soul")!;
    chitoseModel_ = GetNode<Node3D>("ChitoseModel")!;
    arrowSoundPlayer_ = GetNode<AudioStreamPlayer3D>("Sounds/Arrow")!;
    ContactMonitor = true;
    MaxContactsReported = 1;
    BodyEntered += OnBodyEntered;
  }

  private void OnBodyEntered(Node node) {
    if (node is not EnemyBase) {
      return;
    }
    Hit();
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    // Keeping record clean
    while (shotRanges_.First != null) {
      var value = shotRanges_.First!.Value;
      if (value.EndTick < record_.HistoryBegin) {
        shotRanges_.RemoveFirst();
      } else {
        break;
      }
    }

    /*  Start forwarding */
    ref var rec = ref record_.Mut;

    ref var pos = ref rec.Position;
    { // Position
      var deltaPos = Vector3.Zero;
      var moved = false;
      if (Input.IsActionPressed(Constant.MoveRightButtonName)) {
        deltaPos.X += 1.0f;
        moved = true;
      }
      if (Input.IsActionPressed(Constant.MoveLeftButtonName)) {
        deltaPos.X -= 1.0f;
        moved = true;
      }
      if (Input.IsActionPressed(Constant.MoveUpButtonName)) {
        deltaPos.Y += 1.0f;
        moved = true;
      }
      if (Input.IsActionPressed(Constant.MoveDownButtonName)) {
        deltaPos.Y -= 1.0f;
        moved = true;
      }

      if (moved) {
        deltaPos = deltaPos.Normalized() * (float)(dt * Constant.MoveDelta);
        pos += deltaPos;
        pos.X = Mathf.Clamp(pos.X, -21.0f, 21.0f);
        pos.Y = Mathf.Clamp(pos.Y, -11.5f, 11.5f);
      }
    }

    ref var rot = ref rec.SpiritRot;
    { // Spirit rot
      rot += dt;
    }

    // Handle shot
    InvokeShot(ref rec, integrateTime, dt);

    // Handle Spell
    ProcessSpell(ref rec, integrateTime, dt);

    { // Update using current value.
      Position = pos;
      soraSoul_.Rotation = new Vector3(0.0f, (float)rot, 0.0f);
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus();
  }

  public override bool _ProcessLeap(double integrateTime) {
    var clone = Clone(integrateTime);
    witchField_.AddChild(clone);
    // Cleanup shot ranges.
    var cur = shotRanges_.Last;
    while(cur != null) {
      var value = cur.Value;
      if (value.EndTime < integrateTime) {
        break;
      }
      cur = cur.Previous;
      shotRanges_.RemoveLast();
    }
    return base._ProcessLeap(integrateTime);
  }

  /**
   * Invoke shot button behaviour.
   */
  private void InvokeShot(ref Record rec, double integrateTime, double dt) {
    ref var afterFire = ref rec.AfterFire;
    ref readonly var invoking = ref rec.Spell.Invoking;
    if (Input.IsActionPressed(Constant.FireButtonName)) {
      if (double.IsNaN(afterFire)) {
        // Case 1. Beginning.
        shotRanges_.AddLast(new ShotRange {
          BeginTime = integrateTime,
          EndTick = uint.MaxValue,
          EndTime = double.MaxValue,
        });
        if (!invoking) {
          bulletServer_.Shot(rec.Position + Vector3.Right * 2.0f);
        }
        afterFire = nextBulletDuration_ * 1.3;
      } else {
        // Case 2. Double shot mode.
        afterFire -= dt;
        if (afterFire < 0.0) {
          if (!invoking) {
            bulletServer_.ShotDouble(rec.Position + Vector3.Right * 2.0f);
          }
          afterFire += nextBulletDuration_;
        }
      }
    } else {
      if (!double.IsNaN(afterFire)) {
        // Case 3. Just Released.
        var lastShotEvent = shotRanges_.Last;
        if (lastShotEvent != null) {
          var value = lastShotEvent.Value;
          lastShotEvent.Value = value with {
            EndTick = Clock.CurrentTick,
            EndTime = integrateTime,
          };
        }
        afterFire = double.NaN;
      } else {
        // Case 4. Released.
        afterFire = double.NaN;
      }
    }
  }

  private void ProcessSpell(ref Record rec, double integrateTime, double dt) {
    ref var spell = ref rec.Spell;
    ref var invoking = ref spell.Invoking;
    ref var leftTime = ref spell.InvocationLeftTime;
    ref var witch = ref rec.Witch;
    if (!invoking) {
      // Not invoked yet.
      if (Input.IsActionJustPressed(Constant.SpellButtonName)) {
        // Invoke Chitose.
        if (witch.Chitose) {
          // Invoke Chitose.
          invoking = true;
          leftTime = SpellConstant.InvokingTime;
        } else {
          // Chitose is not alive...
        }
      }
    } else {
      // Invoking now.
      leftTime -= dt;
      if (leftTime <= 0.0) {
        // Switched to Sora back (normal case).
        invoking = false;
        leftTime = 0.0;
      }else if (Input.IsActionJustPressed(Constant.SpellButtonName)) {
        // Invoke back (force case).
        invoking = false;
        leftTime = 0.0;
      }
    }
    SetCurrentWitch(in spell);
  }

  private void InvokeArrow() {
    var arrow = arrowAsset_.Instantiate<Arrow>();
    arrow.InitialPosition = Position;
    arrow.InitialVelocity = Vector3.Left * Arrow.DefaultSpeed;
    bulletNode_.AddChild(arrow);
    arrowSoundPlayer_.Play();
  }

  private bool LoadCurrentStatus() {
    ref readonly var rec = ref record_.Ref;
    ref readonly var pos = ref rec.Position;
    ref readonly var rot = ref rec.SpiritRot;
    Position = pos;
    soraSoul_.Rotation = new Vector3(0.0f, (float)rot, 0.0f);
    ref readonly var spell = ref rec.Spell ;
    SetCurrentWitch(in spell);
    return true;
  }

  private void SetCurrentWitch(in SpellRecord spell) {
    if (!spell.Invoking || double.IsNaN(spell.InvocationLeftTime)) {
      soraModel_.Visible = true;
      soraSoul_.Visible = true;
      chitoseModel_.Visible = false;
      collisionShape_.Disabled = false;
      return;
    }
    collisionShape_.Disabled = true;
    switch (spell.InvocationLeftTime) {
      case >= 1.1:
        soraModel_.Visible = false;
        chitoseModel_.Visible = true;
        break;
      case >= 0.6: // 1.1 ~ 0.6
        soraModel_.Visible = true;
        soraSoul_.Visible = Math.Round(spell.InvocationLeftTime * 10.0) % 3 == 0;
        chitoseModel_.Visible = false;
        break;
      case >= 0.1: // 0.6 ~ 0.1
        soraModel_.Visible = true;
        soraSoul_.Visible = Math.Round(spell.InvocationLeftTime * 10.0) % 2 == 0;
        chitoseModel_.Visible = false;
        break;
      default: // 0.1 ~ 0.0
        soraModel_.Visible = true;
        soraSoul_.Visible = true;
        chitoseModel_.Visible = false;
        break;
    }
  }

  private SoraClone Clone(double integrateTime) {
    var asset = ResourceLoader.Load<PackedScene>("res://Objects/Witch/SoraClone.tscn")!;
    var soraClone = asset.Instantiate<SoraClone>();
    soraClone.Position = Position;
    // Copy replay status
    soraClone.CloneType = meta_.CloneType;
    soraClone.Replay = record_.Clone();
    soraClone.IntegrateTimeOffset = integrateTime;
    // Fill shot ranges
    var shotRanges = new LinkedList<ShotRange>();
    var cur = shotRanges_.Last;
    while(cur != null) {
      var value = cur.Value;
      if (value.EndTime < integrateTime) {
        break;
      }
      shotRanges.AddFirst(value);
      cur = cur.Previous;
    }
    soraClone.ShotRanges = shotRanges;
    return soraClone;
  }

  public void Hit() {
    ref var rec = ref record_.Mut;
    Console.WriteLine("Hit");
  }

  public void AbsorbMagicElement() {
    ref var meta = ref meta_;
    ref var numMagicElements = ref meta.NumMagicElements;
    if (numMagicElements < 8 * 12) {
      numMagicElements++;
    }
    spellGauge_.SetGauge(numMagicElements);
  }
}
