using System;
using System.Runtime.CompilerServices;

// Tests that small structs (<= 8 bytes, passed by value in register on s390x)
// are correctly spilled to the stack when they overflow the available argument
// registers (r2-r6), and that spilled struct args do not clobber caller locals.

struct Small
{
    public int X;
    public int Y;
}

struct Tiny
{
    public int V;
}

class Program
{
    // Test 1: 6 small structs -- the 6th must spill to the stack.
    // The caller has a local variable 'a' whose frame slot must not overlap
    // with the outgoing argument area.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum6(Small s1, Small s2, Small s3, Small s4, Small s5, Small s6)
        => s1.X + s1.Y + s2.X + s2.Y + s3.X + s3.Y
         + s4.X + s4.Y + s5.X + s5.Y + s6.X + s6.Y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestSmallStructSpill6()
    {
        int a = 42;
        Small p1 = new Small { X = 1, Y = 10 };
        Small p2 = new Small { X = 2, Y = 20 };
        Small p3 = new Small { X = 3, Y = 30 };
        Small p4 = new Small { X = 4, Y = 40 };
        Small p5 = new Small { X = 5, Y = 50 };
        Small p6 = new Small { X = 6, Y = 60 };

        int result = Sum6(p1, p2, p3, p4, p5, p6);

        // 'a' must still be 42 after the call -- if the 6th arg's spill slot
        // overlaps with 'a', this check will fail.
        if (a != 42)
        {
            Console.WriteLine($"FAIL TestSmallStructSpill6: local a clobbered, got {a}");
            return 1;
        }
        if (result != 231)
        {
            Console.WriteLine($"FAIL TestSmallStructSpill6: expected 231, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 2: 7 small structs -- the 6th and 7th must spill.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum7(Small s1, Small s2, Small s3, Small s4, Small s5,
                    Small s6, Small s7)
        => s1.X + s1.Y + s2.X + s2.Y + s3.X + s3.Y
         + s4.X + s4.Y + s5.X + s5.Y + s6.X + s6.Y
         + s7.X + s7.Y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestSmallStructSpill7()
    {
        int a = 99;
        int b = 77;
        Small p1 = new Small { X = 1, Y = 10 };
        Small p2 = new Small { X = 2, Y = 20 };
        Small p3 = new Small { X = 3, Y = 30 };
        Small p4 = new Small { X = 4, Y = 40 };
        Small p5 = new Small { X = 5, Y = 50 };
        Small p6 = new Small { X = 6, Y = 60 };
        Small p7 = new Small { X = 7, Y = 70 };

        int result = Sum7(p1, p2, p3, p4, p5, p6, p7);

        if (a != 99 || b != 77)
        {
            Console.WriteLine($"FAIL TestSmallStructSpill7: locals clobbered, a={a} b={b}");
            return 1;
        }
        // expected: (1+10)+(2+20)+(3+30)+(4+40)+(5+50)+(6+60)+(7+70) = 308
        if (result != 308)
        {
            Console.WriteLine($"FAIL TestSmallStructSpill7: expected 308, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 3: Mixed int args + small struct spill.
    // 5 int args in r2-r6, then a small struct must spill.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MixedSum(int a, int b, int c, int d, int e, Small s)
        => a + b + c + d + e + s.X + s.Y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestMixedIntAndStructSpill()
    {
        int local = 123;
        Small s = new Small { X = 100, Y = 200 };
        int result = MixedSum(1, 2, 3, 4, 5, s);

        if (local != 123)
        {
            Console.WriteLine($"FAIL TestMixedIntAndStructSpill: local clobbered, got {local}");
            return 1;
        }
        if (result != 315)
        {
            Console.WriteLine($"FAIL TestMixedIntAndStructSpill: expected 315, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 4: 4-byte struct (Tiny) as spilled arg.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TinySum(int a, int b, int c, int d, int e, Tiny t)
        => a + b + c + d + e + t.V;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestTinyStructSpill()
    {
        int local = 555;
        Tiny t = new Tiny { V = 50 };
        int result = TinySum(10, 20, 30, 40, 50, t);

        if (local != 555)
        {
            Console.WriteLine($"FAIL TestTinyStructSpill: local clobbered, got {local}");
            return 1;
        }
        if (result != 200)
        {
            Console.WriteLine($"FAIL TestTinyStructSpill: expected 200, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 5: Return value from callee is correct even with struct spill.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Small ReturnSmall(int a, int b, int c, int d, int e, Small s)
        => new Small { X = s.X + a, Y = s.Y + e };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestReturnSmallStruct()
    {
        Small s = new Small { X = 10, Y = 20 };
        Small result = ReturnSmall(1, 2, 3, 4, 5, s);

        if (result.X != 11 || result.Y != 25)
        {
            Console.WriteLine($"FAIL TestReturnSmallStruct: expected (11,25), got ({result.X},{result.Y})");
            return 1;
        }
        return 0;
    }

    static int Main()
    {
        int failures = 0;

        failures += TestSmallStructSpill6();
        failures += TestSmallStructSpill7();
        failures += TestMixedIntAndStructSpill();
        failures += TestTinyStructSpill();
        failures += TestReturnSmallStruct();

        if (failures == 0)
            Console.WriteLine("ALL PASS");
        else
            Console.WriteLine($"FAILURES: {failures}");

        return failures;
    }
}

