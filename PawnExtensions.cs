using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

public static class PawnExtensions
{
    // Resets MOVETYPE_NOCLIP to MOVETYPE_WALK so that plugin-initiated
    // teleports (e.g. .last / .spawn) don't leave the player noclipping
    // at the destination. Leaves other move types (e.g. MOVETYPE_NONE
    // for coaches / freeze frames) untouched.
    public static void ResetNoclipToWalk(this CBasePlayerPawn? pawn)
    {
        if (pawn == null) return;
        if (pawn.MoveType != MoveType_t.MOVETYPE_NOCLIP) return;
        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
        pawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }

    // Teleports a player/bot to a position while keeping the model upright.
    //
    // A CS2 game update made the player model honour the pitch (X) and roll (Z)
    // of the pawn's body rotation. CBaseEntity.Teleport writes the whole QAngle
    // into that body rotation, so teleporting to a saved eye-angle (.loadnade,
    // .last/.back, .loadpos, .boost/.crouchboost, spawns) now tilts the entire
    // hull instead of only facing the saved direction. It is most obvious on a
    // frozen boost bot, which stays leaning.
    // See: https://github.com/sivert-io/MatchZy-Enhanced/issues/10
    //
    // We still Teleport with the full angle so the engine's fixangle snap aims
    // the player's VIEW at the saved pitch+yaw (the reliable way to snap the
    // camera), then flatten the BODY rotation back to yaw-only. Flattening only
    // touches the scene-node rotation (m_angRotation), not the view angles
    // (m_angEyeAngles), so the camera keeps its pitch while the model stands
    // upright -- restoring the pre-update behaviour.
    public static void TeleportKeepingModelUpright(this CBasePlayerPawn? pawn, Vector? position, QAngle? angle, Vector? velocity)
    {
        if (pawn == null) return;

        pawn.Teleport(position, angle, velocity);

        // No rotation was applied, so there is nothing to flatten.
        if (angle == null) return;

        var sceneNode = pawn.CBodyComponent?.SceneNode;
        if (sceneNode == null) return;

        // Keep yaw (Y); zero pitch (X) and roll (Z) so the model stands upright.
        // m_angRotation is the networked local rotation the client renders;
        // m_angAbsRotation is flattened too as belt-and-suspenders.
        // If a frozen boost bot ever still tilts (the Teleport write winning on
        // the same tick), wrap the block below in Server.NextFrame(...).
        sceneNode.Rotation.X = 0f;
        sceneNode.Rotation.Y = angle.Y;
        sceneNode.Rotation.Z = 0f;
        sceneNode.AbsRotation.X = 0f;
        sceneNode.AbsRotation.Y = angle.Y;
        sceneNode.AbsRotation.Z = 0f;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
    }
}
