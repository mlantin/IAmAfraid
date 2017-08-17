using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IAAController {
	public static bool ClickButtonUp {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return GvrController.ClickButtonUp;
			#else
			return false;
			#endif
		}
	}
	public static bool ClickButtonDown {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return GvrController.ClickButtonDown;
			#else
			return false;
			#endif
		}
	}
	public static bool IsPressed {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return GvrController.ClickButton;
			#else
			return false;
			#endif
		}
	}
	public static bool TouchUp {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return GvrController.TouchUp;
			#else
			return false;
			#endif
		}
	}
	public static bool TouchDown {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return GvrController.TouchDown;
			#else
			return false;
			#endif
		}
	}
	public static bool IsTouching {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return GvrController.IsTouching;
			#else
			return false;
			#endif
		}
	}
	public static Vector2 Position {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return GvrController.TouchPos;
			#else
			return Vector2.zero;
			#endif
		}
	}
	/// <summary>
	/// If touch position is at center and the button is pressed down.
	/// </summary>
	public static bool IsCenterPress {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return ((Position - Vector2.one / 2f).sqrMagnitude < .09  && IsPressed);
			#else
			return Vector2.zero;
			#endif
		}
	}
	/// <summary>
	/// If touch position is at right edge and the button is pressed down.
	/// </summary>
	public static bool IsRightPress {
		get {
			#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
			return (Position.x > .85f && IsPressed);
			#else
			return Vector2.zero;
			#endif
		}
	}
}
