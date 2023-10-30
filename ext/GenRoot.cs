using Godot;
using System;


public interface GenSeed { }

public interface GenContext { }

public interface GenBoundaries { }

public interface GenNode {}

[Tool]
public partial class GenRoot : Node2D
{


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
