using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;

public class IndexOnlyDetector : MonoBehaviour
{
    [Header("Straightness thresholds")]
    [Range(5f, 35f)] public float straightAngle = 20f; // how straight a finger must be

    [Header("Palm direction gate")]
    [Tooltip("Require the index tip to point roughly 'forward' from the palm/wrist.")]
    public bool usePalmDirectionGate = true;
    [Tooltip("Dot threshold against palm forward; lower = more permissive")]
    [Range(0f, 0.9f)] public float minPalmForwardDot = 0.15f; // slightly looser than 0.25

    [Tooltip("Use a safer forward proxy (max of Wrist->Palm and Wrist->MiddleProximal)")]
    public bool frontSafePalm = true;

    [Header("Thumb handling")]
    [Tooltip("Ignore thumb when deciding 'index-only' to avoid false negatives in front poses.")]
    public bool ignoreThumb = true; // default ON to be robust in front-facing gestures
    [Tooltip("Extra leniency for thumb straightness (higher = thumb less likely counted as extended)")]
    [Range(0f, 25f)] public float thumbExtraAngle = 10f;

    [Header("Debounce")]
    [Tooltip("Jitter prevention time (s) before toggling state")]
    [Range(0f, 0.2f)] public float holdTime = 0.06f;

    [System.Serializable] public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent OnIndexOnlyChanged;

    XRHandSubsystem _hands;
    bool _indexOnly;
    float _timer;

    void OnEnable()
    {
        _hands = FindRunningHands();
        if (_hands != null) _hands.updatedHands += OnUpdatedHands;
    }

    void OnDisable()
    {
        if (_hands != null) _hands.updatedHands -= OnUpdatedHands;
        _hands = null;
    }

    XRHandSubsystem FindRunningHands()
    {
        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        return list.FirstOrDefault(s => s.running);
    }

    void OnUpdatedHands(XRHandSubsystem subsystem,
                        XRHandSubsystem.UpdateSuccessFlags success,
                        XRHandSubsystem.UpdateType updateType)
    {
        var right = subsystem.rightHand;
        bool target = false;

        if (right.isTracked)
        {
            bool index  = IsFingerExtended(right, XRHandJointID.IndexProximal,  XRHandJointID.IndexIntermediate,  XRHandJointID.IndexDistal,  XRHandJointID.IndexTip);
            bool middle = IsFingerExtended(right, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip);
            bool ring   = IsFingerExtended(right, XRHandJointID.RingProximal,   XRHandJointID.RingIntermediate,   XRHandJointID.RingDistal,   XRHandJointID.RingTip);
            bool little = IsFingerExtended(right, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip);

            bool thumb  = ignoreThumb ? false : IsThumbExtended(right);

            target = index && !(middle || ring || little || thumb);
        }

        // debounce
        if (target != _indexOnly)
        {
            _timer += Time.deltaTime;
            if (_timer >= holdTime)
            {
                _indexOnly = target;
                _timer = 0f;
                OnIndexOnlyChanged?.Invoke(_indexOnly);
            }
        }
        else
        {
            _timer = 0f;
        }
    }

    bool IsFingerExtended(XRHand hand, XRHandJointID prox, XRHandJointID mid, XRHandJointID dist, XRHandJointID tip)
    {
        if (!TryPose(hand, prox, out var pA) || !TryPose(hand, mid, out var pB) ||
            !TryPose(hand, dist, out var pC) || !TryPose(hand, tip, out var pT))
            return false;

        var v1 = (pB.position - pA.position).normalized;
        var v2 = (pC.position - pB.position).normalized;
        var v3 = (pT.position - pC.position).normalized;

        float minDot = Mathf.Cos(straightAngle * Mathf.Deg2Rad);
        if (Vector3.Dot(v1, v2) < minDot) return false;
        if (Vector3.Dot(v2, v3) < minDot) return false;

        if (usePalmDirectionGate)
        {
            // safer palm-forward proxy: max(dot) of two forward guesses
            float dot = float.PositiveInfinity;

            if (TryPose(hand, XRHandJointID.Palm, out var pPalm) && TryPose(hand, XRHandJointID.Wrist, out var pWrist))
            {
                var palmForwardA = (pPalm.position - pWrist.position).normalized; // original
                var palmToTip    = (pT.position    - pPalm.position ).normalized;

                float dA = Vector3.Dot(palmForwardA, palmToTip);
                float dB = dA;

                if (frontSafePalm && TryPose(hand, XRHandJointID.MiddleProximal, out var pMidProx))
                {
                    var palmForwardB = (pMidProx.position - pWrist.position).normalized; // alternative
                    dB = Vector3.Dot(palmForwardB, palmToTip);
                }

                dot = Mathf.Max(dA, dB);
            }

            if (!float.IsPositiveInfinity(dot) && dot < minPalmForwardDot)
                return false;
        }

        return true;
    }

    bool IsThumbExtended(XRHand hand)
    {
        if (!TryPose(hand, XRHandJointID.ThumbProximal, out var pP) ||
            !TryPose(hand, XRHandJointID.ThumbDistal,   out var pD) ||
            !TryPose(hand, XRHandJointID.ThumbTip,      out var pT))
            return false;

        var v1 = (pD.position - pP.position).normalized;
        var v2 = (pT.position - pD.position).normalized;

        // Make it harder to count thumb as "extended" by adding slack
        float minDot = Mathf.Cos((straightAngle + thumbExtraAngle) * Mathf.Deg2Rad);
        return Vector3.Dot(v1, v2) >= minDot;
    }

    bool TryPose(XRHand hand, XRHandJointID id, out Pose pose)
        => hand.GetJoint(id).TryGetPose(out pose);

    public bool IndexOnly => _indexOnly;
}
