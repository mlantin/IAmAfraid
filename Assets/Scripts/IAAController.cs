using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IAAController {
	public static bool ClickButtonUp {
		get {
			return GvrController.ClickButtonUp;
		}
	}
	public static bool ClickButtonDown {
		get {
			return GvrController.ClickButtonDown;
		}
	}
	public static bool IsPressed {
		get {
			return GvrController.ClickButton;
		}
	}
	public static bool TouchUp {
		get {
			return GvrController.TouchUp;
		}
	}
	public static bool TouchDown {
		get {
			return GvrController.TouchDown;
		}
	}
	public static bool IsTouching {
		get {
			return GvrController.IsTouching;
		}
	}
	public static Vector2 Position {
		get {
			return GvrController.TouchPos;
		}
	}
	/// <summary>
	/// If touch position is at center and the button is pressed down.
	/// </summary>
	public static bool IsCenterPress {
		get {
			return ((Position - Vector2.one / 2f).sqrMagnitude < .09  && IsPressed);
		}
	}
	/// <summary>
	/// If touch position is at right edge and the button is pressed down.
	/// </summary>
	public static bool IsRightPress {
		get {
			return (Position.x > .85f && IsPressed);
		}
	}
}
