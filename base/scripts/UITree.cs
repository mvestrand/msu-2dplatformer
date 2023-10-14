using Godot;
using System;

public class UITree : Control {

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
    }

    public void SetActive(string pageName) {
        foreach (var child in GetChildren()) {
            if (child is Control control) {
                control.Visible = (control.Name == pageName);
            }
        }
    }
    
}
