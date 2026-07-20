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

        [Header("Lane Width Variation")]
        public float LaneWidth;            // metres per lane (used to size 2/4/8-lane roads)
        public float WidthChangePerNode;   // max half-width change per node (smooth taper)
        public int   WidthMinNodes;        // length of a constant-width section
        public int   WidthMaxNodes;

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

        [Header("Self-intersection guard (flat road — a loop closing on itself is unrecoverable)")]
        [Tooltip("Minimum world-space distance kept between the current node and any earlier, non-adjacent node. Must clear the widest possible combined road/shoulder reach.")]
        public float MinSelfClearance;
        [Tooltip("How many of the most-recent nodes are exempt from the clearance check (they're naturally close — that's just the curve we're currently drawing).")]
        public int SelfCheckIgnoreRecentNodes;
        [Tooltip("How many nodes of position history to retain for the check. Only needs to cover the largest realistic loop, not the whole run.")]
        public int SelfCheckHistoryNodes;

        // ── Defaults tuned for drift gameplay ─────────────────────────────
        public static Settings Default => new Settings
        {
            // Narrower road — forces commitment to the drift line
            BaseRoadHalfWidth  = 4f,       // 8 m total (was 10 m)

            // Width varies in long sections: rarely 2-lane, often 4-lane, often 8-lane.
            LaneWidth          = 3.5f,     // 2-lane≈7m, 4-lane≈14m, 8-lane≈28m total
            WidthChangePerNode = 0.5f,     // gentle taper between width sections
            WidthMinNodes      = 12,       // ~96 m
            WidthMaxNodes      = 26,       // ~208 m

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
            MaxAccumulatedYaw  = 65f,      // tightened from 85° — see PickNextSection's unwind

            // 50m clears the worst-case combined half-width + fence/prop reach (~45m,
            // see RoadGeometrySafety) with margin. History only needs to span the
            // largest loop a single wind-up could trace — a 42m-radius GentleCurve
            // closing a full circle is ~264m of arc, i.e. ~33 nodes; 60 is generous.
            MinSelfClearance          = 50f,
            SelfCheckIgnoreRecentNodes = 24,  // ~192m — comfortably more than one tight hairpin's own circumference
            SelfCheckHistoryNodes      = 60,
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

    // Width-section state (independent cadence from curve sections)
    float      currentHalfWidth;  // smoothed half-width actually emitted
    float      targetHalfWidth;   // half-width goal for this width section
    int        widthNodesLeft;    // remaining nodes in current width section

    // Self-intersection guard state
    readonly System.Collections.Generic.List<Vector3> positionHistory = new System.Collections.Generic.List<Vector3>(128);
    int selfIntersectCooldown; // nodes remaining where PickNextSection is forced to pick only Straight/Gentle

    public float DifficultyT      { get; set; }  // 0..1, set by game state
    public float WidthMultiplier  { get; set; } = 1f;  // per-biome
    public float CurrentHalfWidth => currentHalfWidth;  // last emitted (pre-biome-mult)

    // ── Construction ─────────────────────────────────────────────────────
    public RoadCurveGenerator(Settings settings, int seed, Vector3 startPos, float startYaw = 0f)
    {
        s            = settings;
        lastPosition = startPos;
        worldYaw     = startYaw;
        Random.InitState(seed);
        sectionNodesLeft = 0; // force section pick on first node

        currentHalfWidth = settings.BaseRoadHalfWidth;
        targetHalfWidth  = settings.BaseRoadHalfWidth;
        widthNodesLeft   = 0; // force width pick on first node
    }

    // ── Public API ────────────────────────────────────────────────────────
    public SplineRoadBuilder.SplineNode NextNode(float nodeSpacing)
    {
        if (sectionNodesLeft <= 0) PickNextSection();
        if (widthNodesLeft   <= 0) PickNextWidth();

        sectionNodesLeft--;
        widthNodesLeft--;

        // Smooth, predictable width taper toward the section's target half-width.
        currentHalfWidth = Mathf.MoveTowards(currentHalfWidth, targetHalfWidth,
                                             Mathf.Max(0.01f, s.WidthChangePerNode));

        // Smooth yaw toward section target (slow lerp = long committed curves)
        float candidateYaw = Mathf.Lerp(currentYaw, targetYaw, s.YawSmoothing);
        float candidateWorldYaw = worldYaw + candidateYaw;
        Vector3 candidatePos = lastPosition + (Quaternion.Euler(0f, candidateWorldYaw, 0f) * Vector3.forward) * nodeSpacing;

        // Hard geometric guard: the road is flat (no bridges), so a loop that closes
        // on itself is unrecoverable — the car would have to drive through solid road.
        // Heuristic spiral-prevention below reduces how often this is even approached;
        // this is the actual guarantee. If the candidate node would land too close to
        // an earlier, non-adjacent part of the road, reject the turn for this node —
        // go straight instead — and force a real breather before curving resumes.
        if (IsUnsafeSelfIntersection(candidatePos))
        {
            const float maxEmergencyYawPerNode = 24f; // comparable to a normal curve step — never an instant snap that could kink the extruded mesh into a degenerate ring

            // First response: bleed off whatever curvature was already in progress
            // smoothly rather than instantly canceling it to zero.
            candidateYaw = Mathf.MoveTowards(currentYaw, 0f, maxEmergencyYawPerNode);
            targetYaw = 0f;
            candidateWorldYaw = worldYaw + candidateYaw;
            candidatePos = lastPosition + (Quaternion.Euler(0f, candidateWorldYaw, 0f) * Vector3.forward) * nodeSpacing;
            sectionNodesLeft = 0;
            accumulatedYaw = 0f;
            selfIntersectCooldown = Mathf.Max(selfIntersectCooldown, s.CurveMaxNodes);

            // Bleeding off curvature is usually enough, but if the road is already deep
            // into a near-closed loop, that alone can still land in the danger zone —
            // "less curvature" isn't automatically "moving away". In that rarer case,
            // steer toward whichever history point is closest and away from it, bounded
            // to the same smooth per-node turn — re-evaluated every subsequent node, so
            // it converges over a few nodes if one step isn't enough.
            if (IsUnsafeSelfIntersection(candidatePos))
            {
                Vector3 nearest = FindNearestHistoryPoint(candidatePos);
                Vector3 away = lastPosition - nearest;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f)
                {
                    float desiredWorldYaw = Quaternion.LookRotation(away.normalized, Vector3.up).eulerAngles.y;
                    float boundedDelta = Mathf.Clamp(Mathf.DeltaAngle(worldYaw, desiredWorldYaw), -maxEmergencyYawPerNode, maxEmergencyYawPerNode);
                    candidateYaw = boundedDelta;
                    candidateWorldYaw = worldYaw + boundedDelta;
                    candidatePos = lastPosition + (Quaternion.Euler(0f, candidateWorldYaw, 0f) * Vector3.forward) * nodeSpacing;
                }
            }
        }

        currentYaw = candidateYaw;
        worldYaw = candidateWorldYaw;
        lastPosition = candidatePos;

        // Spiral prevention — if we've wound too far, force a section reset
        accumulatedYaw += currentYaw;
        if (Mathf.Abs(accumulatedYaw) > s.MaxAccumulatedYaw)
        {
            sectionNodesLeft = 0; // next call will pick counter-direction
        }

        RecordPositionHistory(lastPosition);

        // Auto-bank from curvature
        float banking = Mathf.Clamp(-currentYaw * s.BankPerDegreeYaw,
                                     -s.MaxBankAngle, s.MaxBankAngle);

        // No per-node pitch noise — flat road for drifting
        Quaternion rotation = Quaternion.Euler(0f, worldYaw, banking);

        return new SplineRoadBuilder.SplineNode(
            lastPosition, rotation,
            currentHalfWidth * WidthMultiplier,
            banking, 0f);
    }

    bool IsUnsafeSelfIntersection(Vector3 candidatePos)
    {
        int checkUpTo = positionHistory.Count - Mathf.Max(0, s.SelfCheckIgnoreRecentNodes);
        float minSqr = s.MinSelfClearance * s.MinSelfClearance;
        for (int i = 0; i < checkUpTo; i++)
        {
            if ((positionHistory[i] - candidatePos).sqrMagnitude < minSqr) return true;
        }
        return false;
    }

    Vector3 FindNearestHistoryPoint(Vector3 candidatePos)
    {
        int checkUpTo = positionHistory.Count - Mathf.Max(0, s.SelfCheckIgnoreRecentNodes);
        float bestSqr = float.MaxValue;
        Vector3 best = candidatePos;
        for (int i = 0; i < checkUpTo; i++)
        {
            float d = (positionHistory[i] - candidatePos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = positionHistory[i]; }
        }
        return best;
    }

    void RecordPositionHistory(Vector3 pos)
    {
        positionHistory.Add(pos);
        int cap = Mathf.Max(s.SelfCheckIgnoreRecentNodes + 1, s.SelfCheckHistoryNodes);
        if (positionHistory.Count > cap) positionHistory.RemoveAt(0);
    }

    // ── Width-section picker ─────────────────────────────────────────────────
    // Rarely 2-lane (tight, oncoming right at you), often 4-lane, often 8-lane.
    void PickNextWidth()
    {
        float lane = s.LaneWidth > 0.1f ? s.LaneWidth : 3.5f;

        float roll = Random.value;
        int lanes;
        if (roll < 0.15f)      lanes = 2;   // rare: 2-lane (one each way)
        else if (roll < 0.65f) lanes = 4;   // common: 4-lane
        else                   lanes = 8;   // common: 8-lane

        targetHalfWidth = lane * lanes * 0.5f;

        int minN = Mathf.Max(2, s.WidthMinNodes);
        int maxN = Mathf.Max(minN, s.WidthMaxNodes);
        widthNodesLeft = Random.Range(minN, maxN + 1);
    }

    // ── Section picker ───────────────────────────────────────────────────
    void PickNextSection()
    {
        sectionCount++;

        // Forced breather after the self-intersection guard fires (see NextNode) —
        // only mild sections allowed until the road has had room to visibly diverge.
        if (selfIntersectCooldown > 0)
        {
            bool gentleOk = selfIntersectCooldown < s.CurveMaxNodes / 2;
            selfIntersectCooldown = Mathf.Max(0, selfIntersectCooldown - s.CurveMinNodes);
            if (gentleOk && Random.value < 0.5f) StartCurve(SectionType.GentleCurve);
            else StartStraight();
            return;
        }

        // If accumulated yaw is high, force a straight to unwind. Fully zeroed (not
        // just reduced) — a partial unwind let repeated same-direction wind-ups
        // compound across several sections into a full loop (see StartCurve's
        // ChicaneR fix below for the other half of that bug).
        bool windedUp = Mathf.Abs(accumulatedYaw) > s.MaxAccumulatedYaw * 0.65f;
        if (windedUp)
        {
            StartStraight();
            accumulatedYaw = 0f;
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
        // Every section type (ChicaneR included — it used to be hardcoded to always turn
        // the same absolute way, which let repeated ChicaneR picks compound net rotation
        // in one direction across many sections into a full self-crossing loop) prefers
        // turning counter to whatever the road is currently leaning.
        float sign = accumulatedYaw > 0f ? -1f : 1f;

        targetYaw        = sign * baseYaw;
        sectionNodesLeft = Random.Range(s.CurveMinNodes, s.CurveMaxNodes + 1);
    }
}
