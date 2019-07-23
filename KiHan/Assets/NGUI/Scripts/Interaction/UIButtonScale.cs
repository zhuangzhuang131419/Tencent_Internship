//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Simple example script of how a button can be scaled visibly when the mouse hovers over it or it gets pressed.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Button Scale")]
public class UIButtonScale : MonoBehaviour
{
	public Transform tweenTarget;
	public Vector3 hover = new Vector3(1.0f, 1.0f, 1.0f);
	public Vector3 pressed = new Vector3(0.9f, 0.9f, 0.9f);
	public float duration = 0.1f;
	public float durationUp = 0.4f;
	public AnimationCurve curve = AnimationCurve.Linear(0f,0f, 1f,1f);
	public UITweener.Method method = UITweener.Method.EaseInOut;
	public bool isHardCode = true;

	Vector3 mScale;
	bool mStarted = false;

	void Start ()
	{
		if (!mStarted)
		{
			mStarted = true;
			if (tweenTarget == null) tweenTarget = transform;
			mScale = tweenTarget.localScale;
			if (isHardCode)
			{
				// 此处hard code 全局应用， 避免每次修改参数，需要每个按钮一次调整
				hover = new Vector3(1.0f, 1.0f, 1.0f);
				pressed = new Vector3(0.8f, 0.8f, 0.8f);
				duration = 0.1f;
				durationUp = 0.3f;
				curve = new AnimationCurve();
				curve.AddKey(0f, 0f);
				curve.AddKey(0.2f, 1.2f);
				curve.AddKey(0.5f, 0.7f);
				curve.AddKey(1f, 1f);
			}
		}
	}

	//void OnEnable () { if (mStarted) OnHover(UICamera.IsHighlighted(gameObject)); }

	void OnDisable ()
	{
		if (mStarted && tweenTarget != null)
		{
			TweenScale tc = tweenTarget.GetComponent<TweenScale>();

			if (tc != null)
			{
				tc.value = mScale;
				tc.enabled = false;
			}
		}
	}

	void OnPress (bool isPressed)
	{
		if (enabled)
		{
			if (!mStarted) Start();
			TweenScale ts = TweenScale.Begin(tweenTarget.gameObject, isPressed ? duration : durationUp, isPressed ? Vector3.Scale(mScale, pressed) :
			                                 Vector3.Scale(mScale, hover));
				//(UICamera.IsHighlighted(gameObject) ? Vector3.Scale(mScale, hover) : mScale));
			if (!isPressed)
			{
				ts.animationCurve = curve;
				ts.method = method;
			}
		}
	}

	/*void OnHover (bool isOver)
	{
		if (enabled)
		{
			if (!mStarted) Start();
			TweenScale.Begin(tweenTarget.gameObject, duration, isOver ? Vector3.Scale(mScale, hover) : mScale).method = UITweener.Method.EaseInOut;
		}
	}

	void OnSelect (bool isSelected)
	{
		if (enabled && (!isSelected || UICamera.currentScheme == UICamera.ControlScheme.Controller))
			OnHover(isSelected);
	}*/
}
