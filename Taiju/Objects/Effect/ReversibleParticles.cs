using System;

namespace Taiju.Objects.Effect;
using Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html

public partial class ReversibleParticles : Node3D {
  [Export] private Mesh mesh_;
  [Export] private int meshCount_ = 16;
  [Export] private float maxSpeed_ = 10.0f;
  [Export(PropertyHint.Range, "0.0, 1.0")] private float lifeTimeScale_ = 1.0f / 10.0f / 2.0f;
  [Export] private Texture2D texture_ = ResourceLoader.Load<CompressedTexture2D>("res://Objects/Effect/textures/魔素.png");
  
  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMesh_;
  
  // MeshData
  private double integrateTime_;
  private struct Item {
    public Color Color;
    public float Velocity;
    public Vector2 Angle;
    public float LifeTime;
  }
  private Item[] items_;

  public override void _Ready() {
    var rng = new RandomNumberGenerator();
    multiMesh_ = GetNode<MultiMeshInstance3D>("MultiMesh");
    multiMesh_.Multimesh = new MultiMesh();
    var meshes = multiMesh_.Multimesh;
    meshes.UseColors = true;
    meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    meshes.Mesh = mesh_;
    meshes.InstanceCount = meshCount_;
    var material = mesh_.SurfaceGetMaterial(0) as StandardMaterial3D;
    material!.AlbedoTexture = texture_;
    material.VertexColorUseAsAlbedo = true;
    items_ = new Item[meshCount_];
    var zero = Transform2D.Identity;
    for (var i = 0; i < meshCount_; ++i) {
      ref var item = ref items_[i];
      item.Color = new Color(rng.Randf() * 0.7f + 0.3f, rng.Randf() * 0.7f + 0.3f, rng.Randf() * 0.7f + 0.3f);
      item.Velocity = maxSpeed_ * (rng.Randf() / 2.0f + 0.5f);
      item.Angle = new Vector2(rng.Randf() * 2.0f - 1.0f, rng.Randf() * 2.0f - 1.0f).Normalized();
      item.LifeTime = item.Velocity * lifeTimeScale_;
      meshes.SetInstanceColor(i, item.Color);
      meshes.SetInstanceTransform2D(i, zero);
    }

    integrateTime_ = 0.0;
  }

  public override void _Process(double dt) {
    integrateTime_ += dt;
    var meshes = multiMesh_.Multimesh;
    var trans = Transform2D.Identity;
    var nan = new Transform2D().TranslatedLocal(new Vector2(Single.NaN, Single.NaN));
    var integrateTime = (float)integrateTime_;
    for (var i = 0; i < meshCount_; ++i) {
      ref var item = ref items_[i];
      if (item.LifeTime <= integrateTime) {
        meshes.SetInstanceTransform2D(i, nan);
        continue;
      }
      var v = item.Velocity;
      var lifeTime = item.LifeTime;
      var leftTime = lifeTime - integrateTime;
      var offset = item.Angle * (v * integrateTime - integrateTime * integrateTime * v * 0.2f);
      meshes.SetInstanceTransform2D(i, trans.TranslatedLocal(offset).ScaledLocal(Vector2.One * (leftTime / lifeTime)));
    }
  }
}
