namespace Taiju.Objects.Effect;
using Godot;

public partial class ReversibleParticles : Node3D {
  [Export] private Mesh mesh_;
  [Export] private int meshCount_ = 32;
  [Export] private Texture2D texture_ = ResourceLoader.Load<CompressedTexture2D>("res://Objects/Effect/textures/魔素.png");
  
  private MultiMeshInstance3D multiMesh_;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    multiMesh_ = GetNode<MultiMeshInstance3D>("MultiMesh");
    multiMesh_.Multimesh = new MultiMesh();
    var meshes = multiMesh_.Multimesh;
    meshes.Mesh = mesh_;
    var material = meshes.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
    material!.AlbedoTexture = texture_;
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
    
  }
}