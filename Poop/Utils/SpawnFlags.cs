using System;

namespace Prefix.Poop.Utils;

[Flags]
public enum SpawnFlags : uint
{
    // Phys prop spawnflags
    PhysPropStartAsleep = 0x000001u,
    PhysPropDontTakePhysicsDamage = 0x000002u,   // This prop can't be damaged by physics collisions
    PhysPropDebris = 0x000004u,   // Don't collide with the player or other debris.
    PhysPropMotionDisabled = 0x000008u,   // Motion disabled at startup (flag only valid in spawn - motion can be enabled via input)
    PhysPropTouch = 0x000010u,   // Can be 'crashed through' by running player (plate glass)
    PhysPropPressure = 0x000020u,   // Can be broken by a player standing on it
    PhysPropEnableOnPhyscannon = 0x000040u,   // Enable motion only if the player grabs it with the physcannon
    PhysPropNoRotorwashPush = 0x000080u,   // The rotorwash doesn't push these
    PhysPropEnablePickupOutput = 0x000100u,   // If set, allow the player to +USE this for the purposes of generating an output
    PhysPropPreventPickup = 0x000200u,   // If set, prevent +USE/Physcannon pickup of this prop
    PhysPropPreventPlayerTouchEnable = 0x000400u,   // If set, the player will not cause the object to enable its motion when bumped into
    PhysPropHasAttachedRagdolls = 0x000800u,   // Need to remove attached ragdolls on enable motion/etc
    PhysPropForceTouchTriggers = 0x001000u,   // Override normal debris behavior and respond to triggers anyway
    PhysPropForceServerSide = 0x002000u,   // Force multiplayer physics object to be serverside
    PhysPropRadiusPickup = 0x004000u,   // For Xbox, makes small objects easier to pick up by allowing them to be found
    PhysPropAlwaysPickUp = 0x100000u,   // Physcannon can always pick this up, no matter what mass or constraints may apply.
    PhysPropNoCollisions = 0x200000u,   // Don't enable collisions on spawn
    PhysPropIsGib = 0x400000u,   // Limit # of active gibs

    // Physbox Spawnflags (Start at 0x01000 to avoid collision with CBreakable's)
    PhysBoxAsleep = 0x01000u,
    PhysBoxIgnoreUse = 0x02000u,
    PhysBoxDebris = 0x04000u,
    PhysBoxMotionDisabled = 0x08000u,
    PhysBoxUsePreferred = 0x10000u,
    PhysBoxEnableOnPhyscannon = 0x20000u,
    PhysBoxNoRotorwashPush = 0x40000u,    // The rotorwash doesn't push these
    PhysBoxEnablePickupOutput = 0x80000u,
    PhysBoxAlwaysPickUp = 0x100000u,   // Physcannon can always pick this up, no matter what mass or constraints may apply.
    PhysBoxNeverPickUp = 0x200000u,   // Physcannon will never be able to pick this up.
    PhysBoxNeverPunt = 0x400000u,   // Physcannon will never be able to punt this object.
    PhysBoxPreventPlayerTouchEnable = 0x800000u,   // If set, the player will not cause the object to enable its motion when bumped into

    // Func breakable spawnflags
    BreakTriggerOnly = 0x0001u,    // May only be broken by trigger
    BreakTouch = 0x0002u,    // Can be 'crashed through' by running player (plate glass)
    BreakPressure = 0x0004u,    // Can be broken by a player standing on it
    BreakPhysicsBreakImmediately = 0x0200u,    // The first physics collision this breakable has will immediately break it
    BreakDontTakePhysicsDamage = 0x0400u,    // This breakable doesn't take damage from physics collisions
    BreakNoBulletPenetration = 0x0800u,    // Don't allow bullets to penetrate

    // Func pushable spawnflags
    PushBreakable = 0x0080u,
    PushNoUse = 0x0100u     // Player cannot +use pickup this entity
}