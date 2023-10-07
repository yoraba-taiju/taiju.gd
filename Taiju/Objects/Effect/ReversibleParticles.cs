namespace Taiju.Objects.Effect;
using Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html

public partial class ReversibleParticles : Node3D {
  [Export] private Mesh mesh_;
  [Export] private int meshCount_ = 32;
  [Export] private float maxSpeed_ = 5.0f;
  [Export(PropertyHint.Range, "0.0, 1.0")] private float lifeTimeScale_ = 1.0f / 5.0f;
  [Export] private Texture2D texture_ = ResourceLoader.Load<CompressedTexture2D>("res://Objects/Effect/textures/魔素.png");
  
  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMesh_;
  
  // MeshData
  private double integrateTime_;
  private struct Item {
    public Color Color;
    public float Velocity;
    public Vector3 Angle;
    public float LifeTime;
  }
  private Item[] items_;

  public override void _Ready() {
    var rng = new RandomNumberGenerator();
    multiMesh_ = GetNode<MultiMeshInstance3D>("MultiMesh");
    multiMesh_.Multimesh = new MultiMesh();
    var meshes = multiMesh_.Multimesh;
    meshes.UseColors = true;
    meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
    meshes.Mesh = mesh_;
    meshes.InstanceCount = meshCount_;
    var material = mesh_.SurfaceGetMaterial(0) as StandardMaterial3D;
    material!.AlbedoTexture = texture_;
    items_ = new Item[meshCount_];
    var zero = new Transform3D(Basis.Identity, Vector3.Zero);
    for (var i = 0; i < meshCount_; ++i) {
      ref var item = ref items_[i];
      item.Color = new Color(rng.Randf() / 2.0f + 0.5f, rng.Randf() / 2.0f + 0.5f, rng.Randf() / 2.0f + 0.5f);
      item.Velocity = maxSpeed_ * rng.Randf();
      item.Angle = new Vector3(rng.Randf() * 2.0f - 1.0f, rng.Randf() * 2.0f - 1.0f, 0.0f).Normalized();
      item.LifeTime = item.Velocity * lifeTimeScale_;
      meshes.SetInstanceColor(i, item.Color);
      meshes.SetInstanceTransform(i, zero);
    }

    integrateTime_ = 0.0;
  }

  public override void _Process(double dt) {
    integrateTime_ += dt;
    var meshes = multiMesh_.Multimesh;
    var trans = new Transform3D(Basis.Identity, Vector3.Zero);
    var integrateTime = (float)integrateTime_;
    for (var i = 0; i < meshCount_; ++i) {
      ref var item = ref items_[i];
      if (item.LifeTime >= integrateTime) {
        meshes.SetInstanceColor(i, Colors.Transparent);
        continue;
      }
      var offset = item.Angle * item.Velocity * integrateTime;
      meshes.SetInstanceTransform(i, trans.TranslatedLocal(offset));
    }
  }
}
