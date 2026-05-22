using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

/// <summary>
/// Reads steering input from the Unity Input System.
/// Mobile: horizontal finger drag.
/// Desktop: A / D keys.
/// </summary>
[DisallowMultipleComponent]
public class DriftInputSystemReader : MonoBehaviour
{
    [Header("Touch Input")]
    [SerializeField] float fullDragDistancePixels = 320f;
    [SerializeField] float touchSteerSensitivity = 1f;
    [SerializeField] bool invertTouch;

    [Header("Keyboard Input")]
    [SerializeField] float keyboardStrength = 1f;

    [Header("Filtering")]
    [SerializeField] float steerRiseSpeed = 8f;
    [SerializeField] float steerFallSpeed = 10f;

    public float Steering { get; private set; }
    public bool IsSteeringInputActive { get; private set; }

#if ENABLE_INPUT_SYSTEM
    int activeTouchId = -1;
    Vector2 touchStartScreenPos;
    bool touchIsActive;
#endif

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Enable();
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Disable();
        activeTouchId = -1;
        touchIsActive = false;
#endif
        Steering = 0f;
        IsSteeringInputActive = false;
    }

    void Update()
    {
        float targetSteer = ReadSteeringRaw(out bool hasInput);
        float speed = Mathf.Abs(targetSteer) > Mathf.Abs(Steering) ? steerRiseSpeed : steerFallSpeed;
        Steering = Mathf.MoveTowards(Steering, targetSteer, speed * Time.deltaTime);
        Steering = Mathf.Clamp(Steering, -1f, 1f);
        IsSteeringInputActive = hasInput || Mathf.Abs(Steering) > 0.01f;
    }

    float ReadSteeringRaw(out bool hasInput)
    {
        float keyboard = ReadKeyboardSteering(out bool keyboardActive);
        float touch = ReadTouchSteering(out bool touchActive);

        hasInput = keyboardActive || touchActive;

        // Touch input takes priority while dragging.
        return touchActive ? touch : keyboard;
    }

    float ReadKeyboardSteering(out bool hasInput)
    {
#if ENABLE_INPUT_SYSTEM
        float steer = 0f;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed)
            {
                steer -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                steer += 1f;
            }
        }

        steer *= keyboardStrength;
        hasInput = Mathf.Abs(steer) > 0.0001f;
        return Mathf.Clamp(steer, -1f, 1f);
#else
        float steer = Input.GetAxisRaw("Horizontal");
        hasInput = Mathf.Abs(steer) > 0.0001f;
        return Mathf.Clamp(steer, -1f, 1f);
#endif
    }

    float ReadTouchSteering(out bool hasInput)
    {
#if ENABLE_INPUT_SYSTEM
        hasInput = false;

        if (!touchIsActive)
        {
            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                    t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    t.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    touchIsActive = true;
                    activeTouchId = t.touchId;
                    touchStartScreenPos = t.screenPosition;
                    break;
                }
            }
        }

        if (!touchIsActive)
        {
            return 0f;
        }

        var active = Touch.activeTouches;
        for (int i = 0; i < active.Count; i++)
        {
            var t = active[i];
            if (t.touchId != activeTouchId)
            {
                continue;
            }

            if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                touchIsActive = false;
                activeTouchId = -1;
                return 0f;
            }

            float sign = invertTouch ? -1f : 1f;
            float dragX = (t.screenPosition.x - touchStartScreenPos.x) * sign;
            float normalized = (dragX / Mathf.Max(32f, fullDragDistancePixels)) * touchSteerSensitivity;
            hasInput = Mathf.Abs(normalized) > 0.005f;

            // Keep control responsive during long drags.
            touchStartScreenPos = Vector2.Lerp(touchStartScreenPos, t.screenPosition, 0.08f);
            return Mathf.Clamp(normalized, -1f, 1f);
        }

        // Active touch disappeared (e.g. focus change)
        touchIsActive = false;
        activeTouchId = -1;
        return 0f;
#else
        hasInput = false;
        return 0f;
#endif
    }
}
