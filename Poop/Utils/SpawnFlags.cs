// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;

namespace Prefix.Poop.Utils;

[Flags]
public enum SpawnFlags : uint
{
    // Phys prop spawnflags
    SF_PHYSPROP_START_ASLEEP = 0x000001u,
    SF_PHYSPROP_DONT_TAKE_PHYSICS_DAMAGE = 0x000002u,   // This prop can't be damaged by physics collisions
    SF_PHYSPROP_DEBRIS = 0x000004u,   // Don't collide with the player or other debris.
    SF_PHYSPROP_MOTIONDISABLED = 0x000008u,   // Motion disabled at startup (flag only valid in spawn - motion can be enabled via input)
    SF_PHYSPROP_TOUCH = 0x000010u,   // Can be 'crashed through' by running player (plate glass)
    SF_PHYSPROP_PRESSURE = 0x000020u,   // Can be broken by a player standing on it
    SF_PHYSPROP_ENABLE_ON_PHYSCANNON = 0x000040u,   // Enable motion only if the player grabs it with the physcannon
    SF_PHYSPROP_NO_ROTORWASH_PUSH = 0x000080u,   // The rotorwash doesn't push these
    SF_PHYSPROP_ENABLE_PICKUP_OUTPUT = 0x000100u,   // If set, allow the player to +USE this for the purposes of generating an output
    SF_PHYSPROP_PREVENT_PICKUP = 0x000200u,   // If set, prevent +USE/Physcannon pickup of this prop
    SF_PHYSPROP_PREVENT_PLAYER_TOUCH_ENABLE = 0x000400u,   // If set, the player will not cause the object to enable its motion when bumped into
    SF_PHYSPROP_HAS_ATTACHED_RAGDOLLS = 0x000800u,   // Need to remove attached ragdolls on enable motion/etc
    SF_PHYSPROP_FORCE_TOUCH_TRIGGERS = 0x001000u,   // Override normal debris behavior and respond to triggers anyway
    SF_PHYSPROP_FORCE_SERVER_SIDE = 0x002000u,   // Force multiplayer physics object to be serverside
    SF_PHYSPROP_RADIUS_PICKUP = 0x004000u,   // For Xbox, makes small objects easier to pick up by allowing them to be found
    SF_PHYSPROP_ALWAYS_PICK_UP = 0x100000u,   // Physcannon can always pick this up, no matter what mass or constraints may apply.
    SF_PHYSPROP_NO_COLLISIONS = 0x200000u,   // Don't enable collisions on spawn
    SF_PHYSPROP_IS_GIB = 0x400000u,   // Limit # of active gibs

    // Physbox Spawnflags (Start at 0x01000 to avoid collision with CBreakable's)
    SF_PHYSBOX_ASLEEP = 0x01000u,
    SF_PHYSBOX_IGNOREUSE = 0x02000u,
    SF_PHYSBOX_DEBRIS = 0x04000u,
    SF_PHYSBOX_MOTIONDISABLED = 0x08000u,
    SF_PHYSBOX_USEPREFERRED = 0x10000u,
    SF_PHYSBOX_ENABLE_ON_PHYSCANNON = 0x20000u,
    SF_PHYSBOX_NO_ROTORWASH_PUSH = 0x40000u,    // The rotorwash doesn't push these
    SF_PHYSBOX_ENABLE_PICKUP_OUTPUT = 0x80000u,
    SF_PHYSBOX_ALWAYS_PICK_UP = 0x100000u,   // Physcannon can always pick this up, no matter what mass or constraints may apply.
    SF_PHYSBOX_NEVER_PICK_UP = 0x200000u,   // Physcannon will never be able to pick this up.
    SF_PHYSBOX_NEVER_PUNT = 0x400000u,   // Physcannon will never be able to punt this object.
    SF_PHYSBOX_PREVENT_PLAYER_TOUCH_ENABLE = 0x800000u,   // If set, the player will not cause the object to enable its motion when bumped into

    // Func breakable spawnflags
    SF_BREAK_TRIGGER_ONLY = 0x0001u,    // May only be broken by trigger
    SF_BREAK_TOUCH = 0x0002u,    // Can be 'crashed through' by running player (plate glass)
    SF_BREAK_PRESSURE = 0x0004u,    // Can be broken by a player standing on it
    SF_BREAK_PHYSICS_BREAK_IMMEDIATELY = 0x0200u,    // The first physics collision this breakable has will immediately break it
    SF_BREAK_DONT_TAKE_PHYSICS_DAMAGE = 0x0400u,    // This breakable doesn't take damage from physics collisions
    SF_BREAK_NO_BULLET_PENETRATION = 0x0800u,    // Don't allow bullets to penetrate

    // Func pushable spawnflags
    SF_PUSH_BREAKABLE = 0x0080u,
    SF_PUSH_NO_USE = 0x0100u     // Player cannot +use pickup this entity
}