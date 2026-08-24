using System;
using System.Runtime.CompilerServices;

// Tests that big structs (> 8 bytes, passed by reference on s390x) are
// correctly spilled to the stack when they overflow the available argument
// registers (r2-r6), and that spilled struct pointers do not clobber
// caller locals.

struct Big
{
    public long A;
    public long B;
}

struct Big24
{
    public long X;
    public long Y;
    public long Z;
}

class Program
{
    // Test 1: 6 big structs -- all passed by reference (pointer in reg/stack).
    // The 6th pointer must spill to the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sum6(Big a, Big b, Big c, Big d, Big e, Big f)
        => a.A + a.B + b.A + b.B + c.A + c.B
         + d.A + d.B + e.A + e.B + f.A + f.B;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBigStructSpill6()
    {
        long local = 9999;
        Big s1 = new Big { A = 1, B = 100 };
        Big s2 = new Big { A = 2, B = 200 };
        Big s3 = new Big { A = 3, B = 300 };
        Big s4 = new Big { A = 4, B = 400 };
        Big s5 = new Big { A = 5, B = 500 };
        Big s6 = new Big { A = 6, B = 600 };

        long result = Sum6(s1, s2, s3, s4, s5, s6);

        if (local != 9999)
        {
            Console.WriteLine($"FAIL TestBigStructSpill6: local clobbered, got {local}");
            return 1;
        }
        if (result != 2121)
        {
            Console.WriteLine($"FAIL TestBigStructSpill6: expected 2121, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 2: 7 big structs -- 6th and 7th pointers spill.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sum7(Big a, Big b, Big c, Big d, Big e, Big f, Big g)
        => a.A + a.B + b.A + b.B + c.A + c.B
         + d.A + d.B + e.A + e.B + f.A + f.B
         + g.A + g.B;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBigStructSpill7()
    {
        long local1 = 1111;
        long local2 = 2222;
        Big s1 = new Big { A = 1, B = 10 };
        Big s2 = new Big { A = 2, B = 20 };
        Big s3 = new Big { A = 3, B = 30 };
        Big s4 = new Big { A = 4, B = 40 };
        Big s5 = new Big { A = 5, B = 50 };
        Big s6 = new Big { A = 6, B = 60 };
        Big s7 = new Big { A = 7, B = 70 };

        long result = Sum7(s1, s2, s3, s4, s5, s6, s7);

        if (local1 != 1111 || local2 != 2222)
        {
            Console.WriteLine($"FAIL TestBigStructSpill7: locals clobbered, local1={local1} local2={local2}");
            return 1;
        }
        // (1+10)+(2+20)+...+(7+70) = 28+280 = 308
        if (result != 308)
        {
            Console.WriteLine($"FAIL TestBigStructSpill7: expected 308, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 3: Mixed int args + big struct spill.
    // 5 int args fill r2-r6, big struct pointer must spill.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long MixedSum(int a, int b, int c, int d, int e, Big s)
        => a + b + c + d + e + s.A + s.B;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestMixedIntAndBigStructSpill()
    {
        long local = 7777;
        Big s = new Big { A = 100, B = 200 };
        long result = MixedSum(1, 2, 3, 4, 5, s);

        if (local != 7777)
        {
            Console.WriteLine($"FAIL TestMixedIntAndBigStructSpill: local clobbered, got {local}");
            return 1;
        }
        if (result != 315)
        {
            Console.WriteLine($"FAIL TestMixedIntAndBigStructSpill: expected 315, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 4: 24-byte struct (3 longs) -- larger than 8 bytes, by-reference.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Big24Sum(int a, int b, int c, int d, int e, Big24 s)
        => a + b + c + d + e + s.X + s.Y + s.Z;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestBig24StructSpill()
    {
        long local = 4444;
        Big24 s = new Big24 { X = 10, Y = 20, Z = 30 };
        long result = Big24Sum(1, 2, 3, 4, 5, s);

        if (local != 4444)
        {
            Console.WriteLine($"FAIL TestBig24StructSpill: local clobbered, got {local}");
            return 1;
        }
        if (result != 75)
        {
            Console.WriteLine($"FAIL TestBig24StructSpill: expected 75, got {result}");
            return 1;
        }
        return 0;
    }

    // Test 5: Return big struct from callee with spilled arg.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Big ReturnBig(int a, int b, int c, int d, int e, Big s)
        => new Big { A = s.A + a, B = s.B + e };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestReturnBigStruct()
    {
        Big s = new Big { A = 10, B = 20 };
        Big result = ReturnBig(1, 2, 3, 4, 5, s);

        if (result.A != 11 || result.B != 25)
        {
            Console.WriteLine($"FAIL TestReturnBigStruct: expected (11,25), got ({result.A},{result.B})");
            return 1;
        }
        return 0;
    }

    static int Main()
    {
        int failures = 0;

        failures += TestBigStructSpill6();
        failures += TestBigStructSpill7();
        failures += TestMixedIntAndBigStructSpill();
        failures += TestBig24StructSpill();
        failures += TestReturnBigStruct();

        if (failures == 0)
            Console.WriteLine("ALL PASS");
        else
            Console.WriteLine($"FAILURES: {failures}");

        return failures;
    }
}

