using AnimatorAsCode.V1;
using UnityEditor;
using UnityEngine;


public class GunSlideAnimatorGeneratorEditor : EditorWindow
{
    Animator gunAnimator;
    Transform animatorRoot;
    Object assetContainer;
    bool includeChargeHandleLayer;
    bool includeChargeHandleGrabLayer;
    bool manualLockOnly;

    // Slide tracking clips
    AnimationClip clipSlideMovingBack;
    AnimationClip clipSlideFullyRear;
    AnimationClip clipSlideLockedBack;
    AnimationClip clipSlideReturning;
    AnimationClip clipSlideForward;
    AnimationClip clipChargeHandleMotion;
    AnimationClip clipChargeHandleGrabbed;
    AnimationClip clipChargeHandleNotGrabbed;

    // Fire cycle clips
    AnimationClip clipFireCycle;     // slide back → forward (ammo remaining)
    AnimationClip clipFireCycleLock; // slide back → stays back (last round)

    // Bullet visibility clips
    AnimationClip clipBulletEnable;
    AnimationClip clipBulletDisable;

    [MenuItem("Tools/Gun Slide/Animator Generator")]
    public static void ShowWindow() => GetWindow<GunSlideAnimatorGeneratorEditor>("Gun Slide Generator");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Animator", EditorStyles.boldLabel);
        gunAnimator = (Animator)EditorGUILayout.ObjectField("Gun Animator", gunAnimator, typeof(Animator), true);
        animatorRoot = (Transform)EditorGUILayout.ObjectField("Animator Root", animatorRoot, typeof(Transform), true);
        assetContainer = EditorGUILayout.ObjectField("Asset Container", assetContainer, typeof(Object), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Slide Tracking Clips", EditorStyles.boldLabel);
        clipSlideMovingBack = (AnimationClip)EditorGUILayout.ObjectField("Slide Moving Back", clipSlideMovingBack, typeof(AnimationClip), false);
        clipSlideFullyRear = (AnimationClip)EditorGUILayout.ObjectField("Slide Fully Rear", clipSlideFullyRear, typeof(AnimationClip), false);
        clipSlideLockedBack = (AnimationClip)EditorGUILayout.ObjectField("Slide Locked Back", clipSlideLockedBack, typeof(AnimationClip), false);
        clipSlideReturning = (AnimationClip)EditorGUILayout.ObjectField("Slide Returning (reverse)", clipSlideReturning, typeof(AnimationClip), false);
        clipSlideForward = (AnimationClip)EditorGUILayout.ObjectField("Slide Forward", clipSlideForward, typeof(AnimationClip), false);
        includeChargeHandleLayer = EditorGUILayout.ToggleLeft("Include Charge Handle Layer (moves only when pulled, not when firing, e.g. M4)", includeChargeHandleLayer);
        if (includeChargeHandleLayer)
        {
            clipChargeHandleMotion = (AnimationClip)EditorGUILayout.ObjectField("Charge Handle Motion", clipChargeHandleMotion, typeof(AnimationClip), false);
        }
        manualLockOnly = EditorGUILayout.ToggleLeft("Manual Lock Only (no fire-lock; the slide only latches when the player racks it, e.g. G3)", manualLockOnly);
        includeChargeHandleGrabLayer = EditorGUILayout.ToggleLeft("Include Charge Handle Grab Layer (pivoting handles, e.g. G3)", includeChargeHandleGrabLayer);
        if (includeChargeHandleGrabLayer)
        {
            clipChargeHandleGrabbed = (AnimationClip)EditorGUILayout.ObjectField("Handle Grabbed", clipChargeHandleGrabbed, typeof(AnimationClip), false);
            clipChargeHandleNotGrabbed = (AnimationClip)EditorGUILayout.ObjectField("Handle Not Grabbed", clipChargeHandleNotGrabbed, typeof(AnimationClip), false);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fire Cycle Clips", EditorStyles.boldLabel);
        clipFireCycle     = (AnimationClip)EditorGUILayout.ObjectField("Fire Cycle (ammo left)", clipFireCycle,     typeof(AnimationClip), false);
        if (!manualLockOnly)
            clipFireCycleLock = (AnimationClip)EditorGUILayout.ObjectField("Fire Cycle Lock (last round)", clipFireCycleLock, typeof(AnimationClip), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bullet Visibility Clips", EditorStyles.boldLabel);
        clipBulletEnable = (AnimationClip)EditorGUILayout.ObjectField("Enable Bullet", clipBulletEnable, typeof(AnimationClip), false);
        clipBulletDisable = (AnimationClip)EditorGUILayout.ObjectField("Disable Bullet", clipBulletDisable, typeof(AnimationClip), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate"))
        {
            if (gunAnimator == null)
            {
                EditorUtility.DisplayDialog("Missing Field", "Assign a Gun Animator.", "OK");
            }
            else if (includeChargeHandleLayer && clipChargeHandleMotion == null)
            {
                EditorUtility.DisplayDialog("Missing Field", "Assign a Charge Handle Motion clip when the charge handle layer is enabled.", "OK");
            }
            else if (includeChargeHandleGrabLayer && (clipChargeHandleGrabbed == null || clipChargeHandleNotGrabbed == null))
            {
                EditorUtility.DisplayDialog("Missing Field", "Assign Handle Grabbed and Handle Not Grabbed clips when the charge handle grab layer is enabled.", "OK");
            }
            else
            {
                Generate();
            }
        }

        if (GUILayout.Button("Populate From Selected GameObject"))
        {
            var go = Selection.activeGameObject;
            if (go)
            {
                var anim = go.GetComponent<Animator>();
                if (anim != null)
                {
                    gunAnimator = anim;
                    animatorRoot = go.transform;
                    EditorUtility.DisplayDialog("Populated", "Copied Animator and root from selection. Assign clips and asset container manually.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Not found", "Selected GameObject doesn't have an Animator component.", "OK");
                }
            }
            else EditorUtility.DisplayDialog("No selection", "Select a GameObject.", "OK");
        }
    }

    void Generate()
    {
        // Disable looping on all input clips so they play once and hold
        SetClipsNonLooping(clipSlideMovingBack, clipSlideFullyRear, clipSlideLockedBack,
            clipSlideReturning, clipSlideForward, clipFireCycle,
            clipBulletEnable, clipBulletDisable);
        if (clipFireCycleLock != null)
            SetClipsNonLooping(clipFireCycleLock);
        if (clipChargeHandleMotion != null)
            SetClipsNonLooping(clipChargeHandleMotion);
        if (clipChargeHandleGrabbed != null)
            SetClipsNonLooping(clipChargeHandleGrabbed);
        if (clipChargeHandleNotGrabbed != null)
            SetClipsNonLooping(clipChargeHandleNotGrabbed);

        var aac = AacV1.Create(new AacConfiguration
        {
            SystemName = "GunSlide",
            AnimatorRoot = animatorRoot,
            DefaultValueRoot = animatorRoot,
            AssetKey = "gun-slide-stable-key", // keep this stable across regenerations
            AssetContainer = assetContainer,
            ContainerMode = AacConfiguration.Container.Everything,
            DefaultsProvider = new AacDefaultsProvider(false) // WD off for world objects
        });

        var ctrl = gunAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        var layer = aac.CreateSupportingArbitraryControllerLayer(ctrl, "SlideLayer");

        // SlideLayer must be weight 1 — Unity layers default to 0
        var slideLayerArr = ctrl.layers;
        slideLayerArr[^1].defaultWeight = 1f;
        ctrl.layers = slideLayerArr;

        // Parameters
        var slideStretch = layer.FloatParameter("SlideStretch"); // driven by physbone natively
        var slideLocked  = layer.BoolParameter("SlideLocked");   // written by Udon

        // Thresholds - tune these to your physbone's stretch range
        const float ejectionThreshold = 0.35f;
        const float fullRearThreshold = 0.95f;

        // States
        var stateForward      = layer.NewState("SlideForward")
            .WithAnimation(clipSlideForward);

        var stateMovingBack   = layer.NewState("SlideMovingBack").RightOf(stateForward)
            .WithAnimation(clipSlideMovingBack)
            .WithMotionTime(slideStretch);

        var statePassedEject  = layer.NewState("SlidePassedEjection").RightOf(stateMovingBack)
            .WithAnimation(clipSlideMovingBack) // same visual clip, this state exists purely for event targeting
            .WithMotionTime(slideStretch);

        var stateFullyRear    = layer.NewState("SlideFullyRear").RightOf(statePassedEject)
            .WithAnimation(clipSlideFullyRear);

        var stateLocked       = layer.NewState("SlideLockedBack").Under(stateFullyRear)
            .WithAnimation(clipSlideLockedBack);

        var stateReturning    = layer.NewState("SlideReturning").Under(stateMovingBack)
            .WithAnimation(clipSlideReturning)
            .WithMotionTime(slideStretch);

        // --- Transitions ---

        // Forward → MovingBack (any rearward movement)
        stateForward.TransitionsTo(stateMovingBack)
            .When(slideStretch.IsGreaterThan(0.05f));

        // MovingBack → PassedEjection (crossed ejection threshold)
        stateMovingBack.TransitionsTo(statePassedEject)
            .When(slideStretch.IsGreaterThan(ejectionThreshold));

        // PassedEjection → Locked (latched before reaching full rear).
        // Checked before the Returning transition so a lock wins over a falling stretch.
        statePassedEject.TransitionsTo(stateLocked)
            .When(slideLocked.IsTrue());

        // PassedEjection → FullyRear
        statePassedEject.TransitionsTo(stateFullyRear)
            .When(slideStretch.IsGreaterThan(fullRearThreshold));

        // FullyRear → Locked (Udon sets SlideLocked true)
        stateFullyRear.TransitionsTo(stateLocked)
            .When(slideLocked.IsTrue());

        // FullyRear → Returning (player let go before lock)
        stateFullyRear.TransitionsTo(stateReturning)
            .When(slideStretch.IsLessThan(fullRearThreshold))
            .And(slideLocked.IsFalse());
                           
        // Locked → Returning (slide release hit, Udon sets SlideLocked false)
        stateLocked.TransitionsTo(stateReturning)
            .When(slideLocked.IsFalse());

        // PassedEjection → Returning (partial pull, dropped below ejection threshold)
        statePassedEject.TransitionsTo(stateReturning)
            .When(slideStretch.IsLessThan(ejectionThreshold));

        // MovingBack → Forward (dropped before ejection threshold, no event needed)
        stateMovingBack.TransitionsTo(stateForward)
            .When(slideStretch.IsLessThan(0.05f));

        // Returning → Forward
        stateReturning.TransitionsTo(stateForward)
            .When(slideStretch.IsLessThan(0.05f));

        if (includeChargeHandleLayer)
        {
            var chargeHandleLayer = aac.CreateSupportingArbitraryControllerLayer(ctrl, "ChargeHandleLayer");

            var chargeHandleLayerArr = ctrl.layers;
            chargeHandleLayerArr[^1].defaultWeight = 1f;
            ctrl.layers = chargeHandleLayerArr;

            var chargeHandleStretch = chargeHandleLayer.FloatParameter("SlideStretch");

            chargeHandleLayer.NewState("ChargeHandle")
                .WithAnimation(clipChargeHandleMotion)
                .WithMotionTime(chargeHandleStretch);
        }

        // Optional separate grab layer driving only the pivoting part of the
        // charging handle (G3-style). It uses its own Handle Grabbed / Handle Not
        // Grabbed clips (separate from the ChargeHandleLayer's motion clip), is
        // driven by the SlideGrabbed bool that UCS_SliderHandler already writes on
        // grab/release, and latches into a locked state when the handle is manually
        // locked (shared SlideLocked).
        if (includeChargeHandleGrabLayer)
        {
            var handleGrabLayer = aac.CreateSupportingArbitraryControllerLayer(ctrl, "ChargeHandleGrabLayer");

            var grabLayerArr = ctrl.layers;
            grabLayerArr[^1].defaultWeight = 1f;
            ctrl.layers = grabLayerArr;

            var slideGrabbedForHandle = handleGrabLayer.BoolParameter("SlideGrabbed");
            var slideLockedForHandle = handleGrabLayer.BoolParameter("SlideLocked");

            var handleIdle = handleGrabLayer.NewState("ChargeHandleIdle")
                .WithAnimation(clipChargeHandleNotGrabbed);
            var handlePivoted = handleGrabLayer.NewState("ChargeHandlePivoted").RightOf(handleIdle)
                .WithAnimation(clipChargeHandleGrabbed);
            var handleLocked = handleGrabLayer.NewState("ChargeHandleLocked").Under(handlePivoted)
                .WithAnimation(clipChargeHandleGrabbed);

            // Grab → pivot the handle out
            handleIdle.TransitionsTo(handlePivoted)
                .When(slideGrabbedForHandle.IsTrue());

            // Manual lock → straight to the locked position
            handleIdle.TransitionsTo(handleLocked)
                .When(slideLockedForHandle.IsTrue());

            // Pulling while grabbed → latch into the locked position
            handlePivoted.TransitionsTo(handleLocked)
                .When(slideLockedForHandle.IsTrue());

            // Let go of an unlatched handle → pivot back in
            handlePivoted.TransitionsTo(handleIdle)
                .When(slideGrabbedForHandle.IsFalse())
                .And(slideLockedForHandle.IsFalse());

            // Unlock → handle returns to the forward pose
            handleLocked.TransitionsTo(handleIdle)
                .When(slideLockedForHandle.IsFalse());
        }

        // --- Fire cycle layer (overrides slide tracking during auto-cycle) ---
        // Keep both entry styles for robustness:
        // 1) bool-driven transitions (IsFiring/IsFiringLock)
        // 2) direct Animator.Play(...) fallback in gun scripts
        var fireLayer = aac.CreateSupportingArbitraryControllerLayer(ctrl, "FireCycleLayer");

        // set layer weight to 1 so it fully overrides SlideLayer when active
        var fireLayers = ctrl.layers;
        fireLayers[^1].defaultWeight = 1f;
        ctrl.layers = fireLayers;

        var fireIdle      = fireLayer.NewState("FireIdle"); // no clip — Layer 0 shows through
        // Played at 1x: these clips are only a few frames long, and any speed-up
        // makes the cycle shorter than a rendered frame.
        var fireCycle     = fireLayer.NewState("FireCycle").RightOf(fireIdle)
            .WithAnimation(clipFireCycle);

        var isFiring = fireLayer.BoolParameter("IsFiring");

        fireIdle.TransitionsTo(fireCycle)
            .When(isFiring.IsTrue());

        // No FireCycle self-transition: the gun scripts already call
        // Animator.Play(FireCycle, layer, 0f) per shot to restart the clip, and a
        // trigger-driven self-loop re-enters the state at frame 0 before it can play.

        // Manual-lock guns never cycle from firing into the locked position, so they
        // have no fire-lock clip and don't need the state at all.
        if (!manualLockOnly)
        {
            // IsFiringLock stays a Bool, not a Trigger: the last-round position is a
            // state the gun sits in until it's unlocked, not a one-shot event.
            var isFiringLock = fireLayer.BoolParameter("IsFiringLock");
            var fireCycleLock = fireLayer.NewState("FireCycleLock").Under(fireIdle)
                .WithAnimation(clipFireCycleLock);

            fireIdle.TransitionsTo(fireCycleLock)
                .When(isFiringLock.IsTrue());
            fireCycle.TransitionsTo(fireCycleLock)
                .When(isFiringLock.IsTrue());
            // Held until the gun unlocks - no exit time.
            fireCycleLock.TransitionsTo(fireIdle)
                .When(isFiringLock.IsFalse());

            // Registered after the lock transition so a locked gun never bounces
            // through FireIdle on its way to FireCycleLock.
            fireCycle.TransitionsTo(fireIdle)
                .AfterAnimationFinishes()
                .When(isFiringLock.IsFalse());
        }
        else
        {
            fireCycle.TransitionsTo(fireIdle)
                .AfterAnimationFinishes();
        }

        // --- Bullet visibility layer (simple one-frame enable / disable clips) ---
        var bulletLayer = aac.CreateSupportingArbitraryControllerLayer(ctrl, "BulletLayer");

        var bulletVisible = bulletLayer.BoolParameter("BulletVisible");

        var bulletDisabled = bulletLayer.NewState("BulletDisabled")
            .WithAnimation(clipBulletDisable);

        var bulletEnabled = bulletLayer.NewState("BulletEnabled").RightOf(bulletDisabled)
            .WithAnimation(clipBulletEnable);

        bulletDisabled.TransitionsTo(bulletEnabled)
            .When(bulletVisible.IsTrue());

        bulletEnabled.TransitionsTo(bulletDisabled)
            .When(bulletVisible.IsFalse());

        // Keep both supporting layers active.
        var layers = ctrl.layers;
        layers[^2].defaultWeight = 1f;
        layers[^1].defaultWeight = 1f;
        ctrl.layers = layers;

        // IsFiring is a one-shot event, not a state: a bool would stay
        // true for the whole CycleTime (re-entering the cycle after the 2x clip ends)
        // and would give no rising edge for the next shot during rapid fire.
        // Must run last — AAC rewrites ctrl.parameters each time it builds a layer.
        MakeTrigger(ctrl, "IsFiring");

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done", "Animator generated and saved.", "OK");
    }

    // AAC only creates Bool parameters; retype them in place once the layer is built.
    static void MakeTrigger(UnityEditor.Animations.AnimatorController ctrl, params string[] names)
    {
        var parameters = ctrl.parameters;
        foreach (var p in parameters)
        {
            foreach (var name in names)
                if (p.name == name) p.type = AnimatorControllerParameterType.Trigger;
        }
        ctrl.parameters = parameters;
    }

    static void SetClipsNonLooping(params AnimationClip[] clips)
    {
        foreach (var clip in clips)
        {
            if (clip == null) continue;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime) continue;
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }
    }
}
