using System;
using Godot;
using Taiju.UI.HUD;

namespace Taiju.Objects;

public partial class Player : Node3D {
  [Export] private int numMagicElementsPerTick_ = 1;
  [Export] private int numMinimumMagicElementsToBack_ = 1;
  /* Constants */
  private struct Constant {
    public const string BackButtonName = "time_back";
    public const string FireButtonName = "fire";
    public const string SpellButtonName = "spell";
    public const string MoveRightButtonName = "move_right";
    public const string MoveLeftButtonName = "move_left";
    public const string MoveUpButtonName = "move_up";
    public const string MoveDownButtonName = "move_down";
  }

  /** Clocks **/
  public enum ClockState {
    Normal,
    OnDamage,
    ControlledByEnemy,
  }

  public enum ClockOperation {
    StartForward,
    Forward,
    StartBack,
    Back,
    Leap,
    Stop,
  }
  public enum ClockAction {
    ForwardTick,
    BackTick,
  }

  /** Sora **/
  public struct SoraOperation {
    public bool InvokeFire;
    public bool InvokeSpell;
    public bool InvokeClone;
    public Vector3 Delta;
  }
 
  public struct State {
    public int NumMagicElements;
    public ClockState ClockState;
    public ClockOperation ClockOperation;
    public SoraOperation SoraOperation;
  }
  
  /** Current States **/
  private State state_;
  public ref readonly ClockOperation CurrentClockOperation => ref state_.ClockOperation;
  public ref readonly SoraOperation CurrentSoraOperation => ref state_.SoraOperation;

  /** Other nodes **/
  private Score score_;
  private SpellGauge spellGauge_;

  public override void _Ready() {
    base._Ready();
    state_ = new State {
      NumMagicElements = 0,
      ClockState = ClockState.Normal,
      ClockOperation = ClockOperation.StartForward,
      SoraOperation = new SoraOperation {
        InvokeFire = false,
        InvokeSpell = false,
        InvokeClone = false,
        Delta = Vector3.Zero,
      },
    };
    score_ = GetNode<Score>("/root/Root/Field/HUD/Score")!;
    score_.Set(0);
    spellGauge_ = GetNode<SpellGauge>("/root/Root/Field/HUD/SpellGauge")!;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    ProcessClock();
  }

  // Set CurrentClockOperation
  private void ProcessClock() {
    switch (state_.ClockState) {
      case ClockState.Normal: {
        state_.SoraOperation.InvokeClone = false;
        if (ProcessBackButton()) {
          ProcessNormalClock();
        }
        if (state_.ClockOperation is ClockOperation.StartForward or ClockOperation.Forward) {
          ProcessNormalSora();
        }
      }
        break;
      case ClockState.OnDamage: {
        ProcessDamagedClock();
      }
        break;
      case ClockState.ControlledByEnemy:
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  /***************************************************************************
   ** ClockState.Normal
   ***************************************************************************/
  private bool ProcessBackButton() {
    // Backing started
    if (Input.IsActionJustPressed(Constant.BackButtonName)) {
      if (state_.NumMagicElements < numMinimumMagicElementsToBack_) {
        return true;
      }
      state_.ClockOperation = ClockOperation.StartBack;
      return false;
    }
    // Leaped
    if (Input.IsActionJustReleased(Constant.BackButtonName)) {
      state_.ClockOperation =
        state_.ClockOperation is ClockOperation.StartBack or ClockOperation.Back or ClockOperation.Stop
          ? ClockOperation.Leap
          : ClockOperation.Forward;
      if (state_.ClockOperation == ClockOperation.Leap) {
        state_.SoraOperation.InvokeClone = true;
      }
      return false;
    }
    // Backing
    if (Input.IsActionPressed(Constant.BackButtonName)) {
      switch (state_.ClockOperation) {
        case ClockOperation.StartForward:
        case ClockOperation.Forward:
        case ClockOperation.Leap:
          state_.ClockOperation =
            state_.NumMagicElements < numMagicElementsPerTick_ ?
              ClockOperation.Forward :
              ClockOperation.StartBack;
          break;
        case ClockOperation.StartBack:
        case ClockOperation.Back:
        case ClockOperation.Stop:
          state_.ClockOperation =
            state_.NumMagicElements < numMagicElementsPerTick_ ?
              ClockOperation.Stop :
              ClockOperation.Back;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return false;
    }
    return true;
  }

  private void ProcessNormalClock() {
    switch (CurrentClockOperation) {
      case ClockOperation.StartForward:
        state_.ClockOperation = ClockOperation.Forward;
        break;
      case ClockOperation.Forward:
        break;
      case ClockOperation.StartBack:
        state_.ClockOperation =
          state_.NumMagicElements >= numMinimumMagicElementsToBack_
            ? ClockOperation.Back
            : ClockOperation.Stop;
        break;
      case ClockOperation.Back:
        if (state_.NumMagicElements < numMinimumMagicElementsToBack_) {
          state_.ClockOperation = ClockOperation.Stop;
        } 
        break;
      case ClockOperation.Leap:
        state_.ClockOperation = ClockOperation.Forward;
        break;
      case ClockOperation.Stop:
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  private void ProcessNormalSora() {
    ref var operation = ref state_.SoraOperation;
    { // Fire
      operation.InvokeFire = Input.IsActionPressed(Constant.FireButtonName);
    }
    { // Invoke Spell
      operation.InvokeSpell = Input.IsActionJustPressed(Constant.SpellButtonName);
    }
    { // Position
      var delta = Vector3.Zero;
      var moved = false;
      if (Input.IsActionPressed(Constant.MoveRightButtonName)) {
        delta.X += 1.0f;
        moved = true;
      }
      if (Input.IsActionPressed(Constant.MoveLeftButtonName)) {
        delta.X -= 1.0f;
        moved = true;
      }
      if (Input.IsActionPressed(Constant.MoveUpButtonName)) {
        delta.Y += 1.0f;
        moved = true;
      }
      if (Input.IsActionPressed(Constant.MoveDownButtonName)) {
        delta.Y -= 1.0f;
        moved = true;
      }
      operation.Delta = moved ? delta.Normalized() : Vector3.Zero;
    }
  }

  /***************************************************************************
   * ClockState.OnDamage
   ***************************************************************************/

  private void ProcessDamagedClock() {
    if (state_.NumMagicElements < numMagicElementsPerTick_) {
      state_.ClockOperation = ClockOperation.Leap;
      state_.ClockState = ClockState.Normal;
    } else {
      state_.ClockOperation =
        state_.ClockOperation == ClockOperation.Back ?
          ClockOperation.Back :
          ClockOperation.StartBack;
    }
  }

  /***************************************************************************
   * By Clock
   ***************************************************************************/

  public void OnActionByClock(ClockAction clockAction) {
    switch (clockAction) {
      case ClockAction.ForwardTick:
        break;
      case ClockAction.BackTick:
        if (state_.NumMagicElements >= numMagicElementsPerTick_) {
          var nextNumMagicElements = state_.NumMagicElements - numMagicElementsPerTick_;
          state_.NumMagicElements = Math.Max(0, nextNumMagicElements);
          spellGauge_.SetGauge(state_.NumMagicElements);
        }
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(clockAction), clockAction, null);
    }
  }

  /***************************************************************************
   * By Sora
   ***************************************************************************/

  public void OnDamageBySora() {
    if (state_.NumMagicElements >= numMinimumMagicElementsToBack_) {
      // TODO: GAME OVER
      state_.ClockState = ClockState.OnDamage;
    }
  }
  public void OnAbsorbMagicElementsBySora(int numAbsorbedMagicElements) {
    var nextNumMagicElements = state_.NumMagicElements + numAbsorbedMagicElements;
    state_.NumMagicElements = Math.Min(nextNumMagicElements, SpellGauge.MaxItems);
    spellGauge_.SetGauge(state_.NumMagicElements);
  }

  /***************************************************************************
   * By others
   ***************************************************************************/

  public void OnScoreAdded(long scoreDelta) {
    score_.Add(scoreDelta);
  }
}
