using System;
using Godot;
using Taiju.UI.HUD;

namespace Taiju.Objects;

public partial class Player : Node3D {
  [Export] private int numMagicElementsPerTick_ = 1;
  [Export] private int numMinimumMagicElementsToBack_ = 1;
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

  public struct State {
    public int NumMagicElements;
    public ClockState ClockState;
    public ClockOperation ClockOperation;
  }
  
  /** Current States **/
  private State state_;
  public ClockOperation CurrentClockOperation => state_.ClockOperation;

  public int NumMagicElements {
    get => state_.NumMagicElements;
    set {
      state_.NumMagicElements = value;
      spellGauge_.SetGauge(value);
    }
  }

  /** Other nodes **/
  private SpellGauge spellGauge_;

  public override void _Ready() {
    base._Ready();
    state_ = new State {
      NumMagicElements = 0,
      ClockState = ClockState.Normal,
    };
    spellGauge_ = GetNode<SpellGauge>("/root/Root/Field/HUD/SpellGauge")!;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    ProcessClock();
  }

  // Set CurrentClockOperation
  private void ProcessClock() {
    switch (state_.ClockState) {
      case ClockState.Normal:
        if (ProcessBackButton()) {
          ProcessNormalClock();
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
    if (Input.IsActionJustPressed("time_back")) {
      state_.ClockOperation =
        state_.NumMagicElements >= numMinimumMagicElementsToBack_
          ? ClockOperation.StartBack
          : ClockOperation.Stop;
      return false;
    }
    // Leaped
    if (Input.IsActionJustReleased("time_back")) {
      state_.ClockOperation =
        CurrentClockOperation is ClockOperation.StartBack or ClockOperation.Back
          ? ClockOperation.Leap
          : ClockOperation.Forward;
      return false;
    }
    // Backing
    if (Input.IsActionPressed("time_back")) {
      state_.ClockOperation =
        state_.NumMagicElements < numMagicElementsPerTick_ ?
          ClockOperation.Stop :
          ClockOperation.Back;
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
          NumMagicElements = Math.Max(0, NumMagicElements - numMagicElementsPerTick_);
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
    NumMagicElements = Math.Min(NumMagicElements + numAbsorbedMagicElements, SpellGauge.ItemsPerGauge);
  }
}
