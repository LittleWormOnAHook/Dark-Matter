using System.Collections.Generic;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Building
{
    /// <summary>
    /// 0926-move: in build mode, placed Equipment and surface items (Build Hub, generator, storage crate, lights, signs...)
    /// can be picked up and re-placed. Aim at one (it glows gold) and press Middle Mouse (pad: Right Shoulder) to grab it.
    /// The real object stays alive but hidden (renderers, colliders and lights off; a hub's zone stays at its anchor) while the
    /// normal placement ghost for its catalog part follows the crosshair under every usual rule. LMB, Middle Mouse or RB
    /// puts the ORIGINAL object at the new pose: components and state are untouched, no cost, no refund.
    /// RMB, B, Esc, D-Pad Down or leaving build mode puts it back exactly where it was.
    /// </summary>
    public sealed partial class DMBuildingPlacementController
    {
        public const string MoveKeyLabel = "MMB";
        public const string MovePadLabel = "RB";

        static readonly int HoverBaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int HoverColorId = Shader.PropertyToID("_Color");
        static readonly int HoverEmissiveId = Shader.PropertyToID("_EmissiveColor");
        static readonly Color HoverTint = new Color(1f, 0.8f, 0.35f, 1f);
        static readonly Color HoverEmission = new Color(0.55f, 0.36f, 0.08f, 1f);

        sealed class MoveState
        {
            public DMBuildingGhost Ghost;
            public DMBuildingPiece Piece;
            public Vector3 Position;
            public Quaternion Rotation;
            public DMBuildingGhost Host;
            public Renderer[] Renderers;
            public bool[] RendererOn;
            public Collider[] Colliders;
            public bool[] ColliderOn;
            public Light[] Lights;
            public bool[] LightOn;
            public Behaviour[] Paused;
            public bool[] PausedOn;
        }

        static MoveState carried;
        static DMBuildingGhost moveHover;
        static Renderer[] hoverRenderers;
        static MaterialPropertyBlock hoverBlock;
        static bool moveSuppressLeft;
        static bool moveSuppressRight;
        static string moveHint;
        static int moveHintFrame = -10;

        /// <summary>True while a placed piece is picked up.</summary>
        public static bool IsMovingPiece => carried != null && carried.Ghost != null;

        /// <summary>Move prompt for the build hotbar this frame ("MMB / RB: Move Generator", "Moving ..."), or null.</summary>
        public static string MoveHint => Time.frameCount - moveHintFrame <= 1 ? moveHint : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMoveStatics()
        {
            carried = null;
            moveHover = null;
            hoverRenderers = null;
            hoverBlock = null;
            moveSuppressLeft = false;
            moveSuppressRight = false;
            moveHint = null;
            moveHintFrame = -10;
        }

        /// <summary>The part the aim works for: the carried piece while moving, else the hotbar selection.</summary>
        static DMBuildingPiece AimPiece()
        {
            return carried != null && carried.Piece != null ? carried.Piece : DMBuildingMode.SelectedPiece;
        }

        /// <summary>Moving is free: the cost check only applies to new pieces.</summary>
        static bool HasCostOrMoving(DMBuildingPiece piece)
        {
            return IsMovingPiece || DMBuildingCatalog.HasCost(piece);
        }

        /// <summary>0927-zone-anchor: the Build Hub being carried, or null. Its zone does not move with it.</summary>
        static DMBuildHub MovingHub()
        {
            return IsMovingPiece && carried.Ghost.TryGetComponent(out DMBuildHub hub) ? hub : null;
        }

        static bool IsMovable(DMBuildingPiece piece)
        {
            return piece != null && (IsEquipmentPiece(piece) || piece.Snap == DMBuildingSnap.Surface);
        }

        /// <summary>Esc: puts a carried piece back. True when something was being carried.</summary>
        public static bool TryCancelMove()
        {
            if (!IsMovingPiece)
            {
                carried = null;
                return false;
            }

            CancelMove();
            return true;
        }

        /// <summary>Runs every build-mode frame before the build/destroy input. True = this frame is handled here.</summary>
        bool UpdateMoveEquipment(bool overBar, bool leftHeld, bool rightHeld)
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (carried != null && carried.Ghost == null)
                carried = null; // destroyed meanwhile (save load)

            if (carried == null)
            {
                // After a place or cancel, swallow that click until it is released so it neither builds nor destroys.
                if (moveSuppressLeft && !leftHeld)
                    moveSuppressLeft = false;
                if (moveSuppressRight && !rightHeld)
                    moveSuppressRight = false;
                if (moveSuppressLeft || moveSuppressRight)
                {
                    SetMoveHover(null);
                    QuietBuildInput();
                    return true;
                }

                DMBuildingGhost hovered = !overBar && !leftHeld && !rightHeld && !DMBuildingMode.MaterialsOpen
                    ? MovableUnderCrosshair()
                    : null;
                SetMoveHover(hovered);
                if (hovered == null)
                    return false;

                SetMoveHint(MoveKeyLabel + " / " + MovePadLabel + ": Move " + NameOf(hovered));
                bool grab = (mouse != null && mouse.middleButton.wasPressedThisFrame)
                    || (pad != null && pad.rightShoulder.wasPressedThisFrame);
                if (!grab)
                    return false;

                SetMoveHover(null);
                BeginMove(hovered);
                QuietBuildInput();
                return true;
            }

            SetMoveHover(null);
            buildHold = 0f;
            destroyHold = 0f;
            destroyFocus = null;
            DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);

            bool cancel = (mouse != null && mouse.rightButton.wasPressedThisFrame)
                || (keyboard != null && keyboard.bKey.wasPressedThisFrame)
                || (pad != null && pad.dpad.down.wasPressedThisFrame);
            if (cancel)
            {
                CancelMove();
                moveSuppressRight = rightHeld;
                QuietBuildInput();
                return true;
            }

            SetMoveHint("Moving " + NameOf(carried.Ghost) + " \u2014 LMB place, RMB cancel");
            DMBuildingPiece aimedPiece = null;
            Vector3 aimedPosition = default;
            Quaternion aimedRotation = Quaternion.identity;
            bool canCommit = false;
            bool seated = !overBar && TryAim(out aimedPiece, out aimedPosition, out aimedRotation, out canCommit);
            DMBuildingGhost host = seated && aimedPiece != null && aimedPiece.Snap == DMBuildingSnap.Surface ? surfaceHost : null;
            if (seated && HostChainContains(host, carried.Ghost))
                canCommit = false; // never onto something that hangs on the carried piece

            bool place = (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
                || (pad != null && pad.rightShoulder.wasPressedThisFrame);
            if (place && seated && canCommit && aimedPiece == carried.Piece)
            {
                FinishMove(aimedPosition, aimedRotation, host);
                moveSuppressLeft = leftHeld;
                QuietBuildInput();
                return true;
            }

            if (!seated)
            {
                if (preview != null)
                    preview.SetActive(false);
                return true;
            }

            ShowMovePreview(aimedPiece, aimedPosition, aimedRotation, canCommit);
            return true;
        }

        void QuietBuildInput()
        {
            buildHold = 0f;
            destroyHold = 0f;
            destroyFocus = null;
            if (preview != null)
                preview.SetActive(false);
            DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
        }

        void ShowMovePreview(DMBuildingPiece piece, Vector3 position, Quaternion rotation, bool canCommit)
        {
            EnsurePreview(piece.Id);
            if (preview == null)
                return;
            preview.SetActive(true);
            preview.transform.SetPositionAndRotation(position, rotation);
            Material tint = canCommit ? GhostMaterial() : BlockedMaterial();
            if (tint != previewTintMaterial)
            {
                ApplyTint(preview, tint, keepGlass: true);
                previewTintMaterial = tint;
            }
        }

        static void SetMoveHint(string text)
        {
            moveHint = text;
            moveHintFrame = Time.frameCount;
        }

        static string NameOf(DMBuildingGhost ghost)
        {
            DMBuildingPiece piece = ghost != null ? DMBuildingCatalog.Find(ghost.PieceId) : null;
            if (piece != null && !string.IsNullOrEmpty(piece.DisplayName))
                return piece.DisplayName;
            return ghost != null ? ghost.PieceId : string.Empty;
        }

        static DMBuildingGhost MovableUnderCrosshair()
        {
            if (!TryHitGhost(out DMBuildingGhost ghost) || ghost == null || !ghost.Built)
                return null;
            return IsMovable(DMBuildingCatalog.Find(ghost.PieceId)) ? ghost : null;
        }

        static bool HostChainContains(DMBuildingGhost host, DMBuildingGhost target)
        {
            for (int i = 0; host != null && i < 16; i++)
            {
                if (host == target)
                    return true;
                host = host.Host;
            }

            return false;
        }

        // ---- Pick up / put down ----

        void BeginMove(DMBuildingGhost ghost)
        {
            DMBuildingPiece piece = ghost != null ? DMBuildingCatalog.Find(ghost.PieceId) : null;
            if (piece == null)
                return;

            // Settle a running creation bounce first so the rest pose is the pose we keep and restore.
            DMBuildingCreationFx fx = ghost.GetComponent<DMBuildingCreationFx>();
            if (fx != null)
            {
                Vector3 rest = DMBuildingCreationFx.RestPosition(ghost);
                fx.enabled = false;
                UnityEngine.Object.Destroy(fx);
                ghost.transform.position = rest;
                ghost.transform.localScale = Vector3.one;
            }

            var state = new MoveState
            {
                Ghost = ghost,
                Piece = piece,
                Position = ghost.transform.position,
                Rotation = ghost.transform.rotation,
                Host = ghost.Host,
            };

            state.Renderers = ghost.GetComponentsInChildren<Renderer>(true);
            state.RendererOn = new bool[state.Renderers.Length];
            for (int i = 0; i < state.Renderers.Length; i++)
            {
                state.RendererOn[i] = state.Renderers[i].enabled;
                state.Renderers[i].enabled = false;
            }

            state.Colliders = ghost.GetComponentsInChildren<Collider>(true);
            state.ColliderOn = new bool[state.Colliders.Length];
            for (int i = 0; i < state.Colliders.Length; i++)
            {
                state.ColliderOn[i] = state.Colliders[i].enabled;
                state.Colliders[i].enabled = false;
            }

            state.Lights = ghost.GetComponentsInChildren<Light>(true);
            state.LightOn = new bool[state.Lights.Length];
            for (int i = 0; i < state.Lights.Length; i++)
                state.LightOn[i] = state.Lights[i].enabled;

            // 0927-zone-anchor: a carried hub stays enabled, so its zone (anchored where it was first placed) keeps
            // counting and its zone visual stays put. Powered lights stop switching until the piece is put down.
            var paused = new List<Behaviour>(2);
            paused.AddRange(ghost.GetComponentsInChildren<DMBasePoweredLights>(true)); // 0927-power-lights: may sit on a model child
            state.Paused = paused.ToArray();
            state.PausedOn = new bool[state.Paused.Length];
            for (int i = 0; i < state.Paused.Length; i++)
            {
                state.PausedOn[i] = state.Paused[i].enabled;
                state.Paused[i].enabled = false;
            }

            for (int i = 0; i < state.Lights.Length; i++)
                state.Lights[i].enabled = false;

            carried = state;
            yawNotches = 0;
            heightOffset = 0f;
            AfterMove(ghost);
        }

        static void FinishMove(Vector3 position, Quaternion rotation, DMBuildingGhost host)
        {
            MoveState state = carried;
            carried = null;
            DMBuildingGhost ghost = state.Ghost;
            if (ghost == null)
                return;

            ghost.transform.SetPositionAndRotation(position, rotation);
            ghost.Host = host;
            MoveDependents(ghost, state.Position, state.Rotation, position, rotation);
            RekeyBuiltCrate(ghost);
            RestoreHidden(state);
            AfterMove(ghost);
        }

        static void CancelMove()
        {
            MoveState state = carried;
            carried = null;
            if (state == null || state.Ghost == null)
                return;

            state.Ghost.transform.SetPositionAndRotation(state.Position, state.Rotation);
            state.Ghost.Host = state.Host;
            RestoreHidden(state);
            AfterMove(state.Ghost);
        }

        static void RestoreHidden(MoveState state)
        {
            for (int i = 0; i < state.Renderers.Length; i++)
            {
                if (state.Renderers[i] != null)
                    state.Renderers[i].enabled = state.RendererOn[i];
            }

            for (int i = 0; i < state.Colliders.Length; i++)
            {
                if (state.Colliders[i] != null)
                    state.Colliders[i].enabled = state.ColliderOn[i];
            }

            for (int i = 0; i < state.Lights.Length; i++)
            {
                if (state.Lights[i] != null)
                    state.Lights[i].enabled = state.LightOn[i];
            }

            // Paused behaviours last (powered lights). A hub is never paused: its zone stays at its anchor.
            for (int i = 0; i < state.Paused.Length; i++)
            {
                if (state.Paused[i] != null)
                    state.Paused[i].enabled = state.PausedOn[i];
            }
        }

        /// <summary>Power squares, generator load, built-piece caches and the clear-zone check see the new pose.</summary>
        static void AfterMove(DMBuildingGhost ghost)
        {
            InvalidateBuiltGhostCache();
            DMBasePower.MarkDirty();
            if (ghost != null && ghost.TryGetComponent(out DMBaseGenerator generator) && generator.isActiveAndEnabled)
                generator.RefreshLoad();
            ResetZoneClearCache();
        }

        /// <summary>Items stuck to the moved piece (a light on a crate) ride along.</summary>
        static void MoveDependents(DMBuildingGhost moved, Vector3 oldPosition, Quaternion oldRotation, Vector3 newPosition, Quaternion newRotation)
        {
            Quaternion delta = newRotation * Quaternion.Inverse(oldRotation);
            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || ghost == moved || ghost.Host != moved)
                    continue;
                Transform t = ghost.transform;
                t.SetPositionAndRotation(newPosition + delta * (t.position - oldPosition), delta * t.rotation);
                RekeyBuiltCrate(ghost);
            }
        }

        /// <summary>Built crate ids come from their position; the contents follow the crate to its new id.</summary>
        static void RekeyBuiltCrate(DMBuildingGhost ghost)
        {
            Project.Storage.DMStorageCrate crate = ghost != null ? ghost.GetComponentInChildren<Project.Storage.DMStorageCrate>(true) : null;
            if (crate == null || !crate.IsBuiltCrate)
                return;

            string oldId = crate.CrateId;
            string newId = BuiltCrateId(ghost.transform.position);
            if (string.Equals(oldId, newId, System.StringComparison.Ordinal))
                return;
            if (Project.Storage.DMStorageCrateRuntime.Rekey(oldId, newId))
                crate.AssignCrateId(newId);
            else
                Debug.LogWarning("[DM Building] Moved crate kept id " + oldId + " because " + newId + " is taken; its contents may not follow it after a reload.");
        }

        // ---- Hover glow ----

        static void SetMoveHover(DMBuildingGhost ghost)
        {
            if (ghost == moveHover && (ghost == null || hoverRenderers != null))
                return;

            if (hoverRenderers != null)
            {
                for (int i = 0; i < hoverRenderers.Length; i++)
                {
                    if (hoverRenderers[i] != null)
                        hoverRenderers[i].SetPropertyBlock(null);
                }
            }

            hoverRenderers = null;
            moveHover = ghost;
            if (ghost == null)
                return;

            if (hoverBlock == null)
                hoverBlock = new MaterialPropertyBlock();
            Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>(false);
            var tinted = new List<Renderer>(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r is ParticleSystemRenderer || r.HasPropertyBlock())
                    continue; // never clobber a block someone else owns
                Material material = r.sharedMaterial;
                if (material == null)
                    continue;

                hoverBlock.Clear();
                bool any = false;
                if (material.HasProperty(HoverEmissiveId))
                {
                    hoverBlock.SetColor(HoverEmissiveId, HoverEmission);
                    any = true;
                }

                if (material.HasProperty(HoverBaseColorId))
                {
                    hoverBlock.SetColor(HoverBaseColorId, Color.Lerp(material.GetColor(HoverBaseColorId), HoverTint, 0.45f));
                    any = true;
                }
                else if (material.HasProperty(HoverColorId))
                {
                    hoverBlock.SetColor(HoverColorId, Color.Lerp(material.GetColor(HoverColorId), HoverTint, 0.45f));
                    any = true;
                }

                if (!any)
                    continue;
                r.SetPropertyBlock(hoverBlock);
                tinted.Add(r);
            }

            hoverRenderers = tinted.ToArray();
        }
    }
}
