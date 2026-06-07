using UnityEngine;

/// <summary>
/// Section-based procedural road curve generator designed for drift gameplay.
///
/// Instead of per-node random yaw (which creates jittery bumpy roads), the road
/// is built as a sequence of deliberate SECTIONS — each section commits to a
/// direction and intensity for long enough that the player can read the road,
/// set up a drift, and carry it through the apex.
///
/// Section flow (repeating rhythm):
///   [Straight] → [Sweeping Curve] → [Straight] → [Tighter Curve] → ...
///
/// No per-node pitch noise. Vertical stays flat — suitable for drifting.
/// Biome-specific width/curvature multipliers are applied from outside.
/// </summary>
public class RoadCurveGenerator
{
    // ── Section types ────────────────────────────────────────────────────
    public enum SectionType
    {
        Straight,       // build speed, road points straight
        GentleCurve,    // sweeping, 4-7° yaw/node — long drift setup
        MediumCurve,    // committed, 7-11° yaw/node — main drift section
        TightCurve,     // hard corner, 11-15° yaw/node — late-game only
        ChicaneL,       // left-right-left — tests control
        ChicaneR,       // right-left-right
    }

    // ── Inspector-exposed settings ────────────────────────────────────────
    [System.Serializable]
    public struct Settings
    {
        [Header("Road Width")]
        public float BaseRoadHalfWidth;

        [Header("Banking")]
        public float BankPerDegreeYaw;   // auto-bank coefficient
        public float MaxBankAngle;

        [Header("Straight section length (nodes)")]
        public int StraightMinNodes;
        public int StraightMaxNodes;

        [Header("Curve section length (nodes)")]
        public int CurveMinNodes;
        public int CurveMaxNodes;

        [Header("Yaw per node, by section type (degrees)")]
        public float GentleYaw;   // 4-7
        public float MediumYaw;   // 7-11
        public float TightYaw;    // 11-15

        [Header("Transition smoothing")]
        [Range(0.05f, 0.4f)]
        public float YawSmoothing;    // lerp factor toward section target

        [Header("Spiral prevention")]
        public float MaxAccumulatedYaw;

        // ── Defaults tuned for drift gameplay ─────────────────────────────
        public static Settings Default => new Settings
        {
            // Narrower road — forces commitment to the drift line
            BaseRoadHalfWidth  = 4f,       // 8 m total (was 10 m)

            BankPerDegreeYaw   = 0.65f,    // stronger camber feel on tight bends
            MaxBankAngle       = 20f,

            // Very short straights — just enough to set up the next corner
            StraightMinNodes   = 3,        // 24 m
            StraightMaxNodes   = 6,        // 48 m

            // Medium-length curve sections — sustained enough to drift through
            CurveMinNodes      = 8,        // 64 m
            CurveMaxNodes      = 15,       // 120 m

            // Yaw redesigned for real drift radii:
            // r ≈ nodeSpacing / sin(yaw°)  (nodeSpacing = 8 m)
            GentleYaw          = 11f,      // r ≈ 42 m  — sweeping drift
            MediumYaw          = 16f,      // r ≈ 29 m  — committed drift
            TightYaw           = 22f,      // r ≈ 21 m  — hard hairpin drift

            YawSmoothing       = 0.15f,    // slightly snappier entry
            MaxAccumulatedYaw  = 85f,
        };
    }

    // ── State ─────────────────────────────────────────────────────────────
    Settings   s;
    float      currentYaw;        // active yaw delta per node (smoothed)
    float      targetYaw;         // yaw goal for this section
    float      worldYaw;          // absolute heading in world degrees
    float      accumulatedYaw;    // running total for spiral prevention
    int        sectionNodesLeft;  // remaining nodes in current section
    SectionType currentSection;
    int        sectionCount;      // how many sections generated (for sequencing)
    Vector3    lastPosition;

    public float DifficultyT      { get; set; }  // 0..1, set by game state
    public float WidthMultiplier  { get; set; } = 1f;  // per-biome

    // ── Construction ─────────────────────────────────────────────────────
    public RoadCurveGenerator(Settings settings, int seed, Vector3 startPos, float startYaw = 0f)
    {
        s            = settings;
        lastPosition = startPos;
        worldYaw     = startYaw;
        Random.InitState(seed);
        sectionNodesLeft = 0; // force section pick on first node
    }

    // ── Public API ────────────────────────────────────────────────────────
    public SplineRoadBuilder.SplineNode NextNode(float nodeSpacing)
    {
        if (sectionNodesLeft <= 0) PickNextSection();

        sectionNodesLeft--;

        // Smooth yaw toward section target (slow lerp = long committed curves)
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, s.YawSmoothing);
        worldYaw  += currentYaw;

        // Spiral prevention — if we've wound too far, force a section reset
        accumulatedYaw += currentYaw;
        if (Mathf.Abs(accumulatedYaw) > s.MaxAccumulatedYaw)
        {
            sectionNodesLeft = 0; // next call will pick counter-direction
        }

        // Auto-bank from curvature
        float banking = Mathf.Clamp(-currentYaw * s.BankPerDegreeYaw,
                                     -s.MaxBankAngle, s.MaxBankAngle);

        // No per-node pitch noise — flat road for drifting
        Quaternion rotation = Quaternion.Euler(0f, worldYaw, banking);
        Vector3    forward  = rotation * Vector3.forward;
        Vector3    pos      = lastPosition + forward * nodeSpacing;
        lastPosition = pos;

        return new SplineRoadBuilder.SplineNode(
            pos, rotation,
            s.BaseRoadHalfWidth * WidthMultiplier,
            banking, 0f);
    }

    // ── Section picker ───────────────────────────────────────────────────
    void PickNextSection()
    {
        sectionCount++;

        // If accumulated yaw is high, force a straight to unwind
        bool windedUp = Mathf.Abs(accumulatedYaw) > s.MaxAccumulatedYaw * 0.65f;
        if (windedUp)
        {
            StartStraight();
            accumulatedYaw *= 0.4f;  // partially unwind counter
            return;
        }

        float roll = Random.value;

        // Drift rhythm: ~75% curves at all times.
        // Straights are short breathers between corners, not long driving sections.
        // Straight chance drops slightly as difficulty rises (more relentless).
        float straightChance = Mathf.Lerp(0.25f, 0.15f, DifficultyT);

        // Force a short straight every 4 sections minimum so the player can breathe
        bool forceBreather = (sectionCount % 4 == 0);

        if (forceBreather || roll < straightChance)
        {
            StartStraight();
            return;
        }

        // Pick curve intensity — difficulty progressively unlocks tighter corners
        float curveRoll = Random.value;
        SectionType type;

        if (DifficultyT < 0.20f)
        {
            // Intro: gentle sweepers only
            type = SectionType.GentleCurve;
        }
        else if (DifficultyT < 0.45f)
        {
            // Early: gentle + medium
            type = curveRoll < 0.45f ? SectionType.GentleCurve : SectionType.MediumCurve;
        }
        else if (DifficultyT < 0.70f)
        {
            // Mid: medium-heavy, start introducing tight
            if (curveRoll < 0.25f)      type = SectionType.GentleCurve;
            else if (curveRoll < 0.70f) type = SectionType.MediumCurve;
            else                        type = SectionType.TightCurve;
        }
        else
        {
            // Late: mostly medium/tight + chicanes for chaos
            if (curveRoll < 0.15f)      type = SectionType.GentleCurve;
            else if (curveRoll < 0.50f) type = SectionType.MediumCurve;
            else if (curveRoll < 0.78f) type = SectionType.TightCurve;
            else                        type = Random.value < 0.5f ? SectionType.ChicaneL : SectionType.ChicaneR;
        }

        StartCurve(type);
    }

    void StartStraight()
    {
        currentSection   = SectionType.Straight;
        targetYaw        = 0f;
        sectionNodesLeft = Random.Range(s.StraightMinNodes, s.StraightMaxNodes + 1);
    }

    void StartCurve(SectionType type)
    {
        currentSection = type;

        float baseYaw = type switch
        {
            SectionType.GentleCurve => Random.Range(s.GentleYaw * 0.8f, s.GentleYaw * 1.2f),
            SectionType.MediumCurve => Random.Range(s.MediumYaw * 0.8f, s.MediumYaw * 1.2f),
            SectionType.TightCurve  => Random.Range(s.TightYaw  * 0.8f, s.TightYaw  * 1.2f),
            SectionType.ChicaneL    => s.GentleYaw,
            SectionType.ChicaneR    => s.GentleYaw,
            _                       => s.GentleYaw,
        };

        // Chicanes: direction flips mid-section — handled by scheduling two half-sections.
        // For simplicity, chicanes just pick a direction and use short length;
        // the spiral prevention and opposite-direction next section creates the S naturally.
        float sign = (type == SectionType.ChicaneR)
            ? 1f
            : (accumulatedYaw > 0f ? -1f : 1f);  // prefer counter to current lean

        targetYaw        = sign * baseYaw;
        sectionNodesLeft = Random.Range(s.CurveMinNodes, s.CurveMaxNodes + 1);
    }
}
