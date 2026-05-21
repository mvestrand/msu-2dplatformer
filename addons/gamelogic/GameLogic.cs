#if TOOLS
using Godot;
using System;

[Tool]
public class GameLogic : EditorPlugin {



	public readonly struct CustomNodeTypeRecord {
		public string TypeName { get; }
		public string BaseType { get; }
		public string ScriptPath { get; }
		public string IconPath { get; }

		public CustomNodeTypeRecord(string type, string @base, string script_path, string icon_path = "") {
			TypeName = type;
			BaseType = @base;
			ScriptPath = script_path;
			IconPath = icon_path;
		}
	}

	// TYPE DECLARATIONS GO HERE ===================================================
	// new CustomNodeTypeRecord("Example", "Node", "Example.cs", "ExampleIcon.svg"),

	public readonly static string gamelogic = "addons/gamelogic/";
	public readonly static string icons = gamelogic + "icons/";

	public readonly static CustomNodeTypeRecord[] types = new CustomNodeTypeRecord[]{

		new CustomNodeTypeRecord("Hurtbox", "Area2D", gamelogic + "Hurtbox.cs", icons + "Hurtbox.svg"),
		new CustomNodeTypeRecord("Hitbox", "Area2D", gamelogic + "Hitbox.cs", icons + "Hitbox.svg"),
		new CustomNodeTypeRecord("Health", "Node", gamelogic + "Health.cs", icons + "Health.svg")
	};
	//=============================================================================

	public override void _EnterTree() {
        foreach (CustomNodeTypeRecord type in types) {
			var script = GD.Load<Script>(type.ScriptPath);
			Texture icon = null;
            if (!type.IconPath.Empty()) {
				icon = GD.Load<Texture>(type.IconPath);
			}
			AddCustomType(type.TypeName, type.BaseType, script, icon);
		}
    }

	public override void _ExitTree() {
        foreach (CustomNodeTypeRecord type in types) {
			RemoveCustomType(type.TypeName);
		}
	}
}
#endif