// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        br %r14
LEAF_END JIT_PatchedCodeStart, _TEXT


// There is an even more optimized version of these helpers possible which takes
// advantage of knowledge of which way the ephemeral heap is growing to only do 1/2
// that check (this is more significant in the JIT_WriteBarrier case).
//
// Additionally we can look into providing helpers which will take the src/dest from
// specific registers (like x86) which _could_ (??) make for easier register allocation
// for the JIT64, however it might lead to having to have some nasty code that treats
// these guys really special like... :(.
//
// Version that does the move, checks whether or not it's in the GC and whether or not
// it needs to have it's card updated
//
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
LEAF_ENTRY JIT_CheckedWriteBarrier, _TEXT
// NOT YET IMPLEMENTED
.long 0x31
LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT


// This is used by the mechanism to hold either the JIT_WriteBarrier_PreGrow
// or JIT_WriteBarrier_PostGrow code (depending on the state of the GC). It _WILL_
// change at runtime as the GC changes. Initially it should simply be a copy of the
// larger of the two functions (JIT_WriteBarrier_PostGrow) to ensure we have created
// enough space to copy that code in.
.balign 16
LEAF_ENTRY JIT_WriteBarrier, _TEXT
// NOT YET IMPLEMENTED
.long 0x32
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        br %r14
LEAF_END JIT_PatchedCodeLast, _TEXT

//copy one pointer and walk dest/src.
// No card-table update yet.
//
// On entry:
//   r2 = dest slot address
//   r3 = source slot address
// On exit:
//   *r2 = *r3
//   r2 += 8
//   r3 += 8
//   r1 trashed (same scratch LSRA already kills for this helper)
LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
        lg      %r1, 0(%r3)
        stg     %r1, 0(%r2)
        aghi    %r3, 8
        aghi    %r2, 8
        br      %r14
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
.balign 16
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
stg     %r3, 0(%r2)
br      %r14
LEAF_END JIT_WriteBarrier_Callable, _TEXT
