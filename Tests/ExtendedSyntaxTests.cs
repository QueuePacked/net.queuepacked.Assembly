using System;
using net.queuepacked.Assembly.CPU;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class ExtendedSyntaxTests
    {
        [TestCase
        (
            """
            define a
            """, 
            """
            JMP #∹1
            0:a
            NOP:∹1
            """
        )]
        [TestCase
        (
            """
            define a=1
            define b
            """, 
            """
            JMP #∹1
            1:a
            NOP:∹1
            JMP #∹2
            0:b
            NOP:∹2
            """
        )]
        [TestCase
        (
            """
            define a,b
            define c=1,d
            define e,f=2
            define 5=3,h=4
            """, 
            """
            JMP #∹1
            0:a
            0:b
            NOP:∹1
            JMP #∹2
            1:c
            0:d
            NOP:∹2
            JMP #∹3
            0:e
            2:f
            NOP:∹3
            JMP #∹4
            3:5
            4:h
            NOP:∹4
            """
        )]
        public void Define(string code, string expected)
        {
            string expanded = AssemblyCompiler.ExpandSyntax(code);
            Assert.That(expanded, Is.EqualTo(expected.ReplaceLineEndings()));
        }

        [TestCase
        (
            """
            set a to 1
            """, 
            """
            WAC #⨀1
            RAC #a
            JMP #∷
            1:⨀1
            NOP:∷
            """
        )]
        [TestCase
        (
            """
            set a to 1
            set b to 2
            """, 
            """
            WAC #⨀1
            RAC #a
            WAC #⨀2
            RAC #b
            JMP #∷
            1:⨀1
            2:⨀2
            NOP:∷
            """
        )]
        [TestCase
        (
            """
            set a to 10 + 2
            set b to c - 4
            set d to e - f
            set g to h < 1
            set i to j > 2
            """, 
            """
            WAC #⨀10
            ADD #⨀2
            RAC #a
            WAC #c
            SUB #⨀4
            RAC #b
            WAC #e
            SUB #f
            RAC #d
            WAC #h
            BSL
            RAC #g
            WAC #j
            BSR
            BSR
            RAC #i
            JMP #∷
            2:⨀2
            4:⨀4
            10:⨀10
            NOP:∷
            """
        )]
        public void Set(string code, string expected)
        {
            string expanded = AssemblyCompiler.ExpandSyntax(code);
            Assert.That(expanded, Is.EqualTo(expected.ReplaceLineEndings()));
        }

        [TestCase
        (
            """
            skip if flag
                <code>
            end of skip
            """,
            """
            JPF #↲1
                <code>
            NOP:↲1
            """
        )]
        [TestCase
        (
            """
            skip if flag
                <codeA>
            end of skip
            skip unless flag
                <codeB>
            end of skip
            """,
            """
            JPF #↲1
                <codeA>
            NOP:↲1
            JPF #↲◘2
            JMP #↲2
            NOP:↲◘2
                <codeB>
            NOP:↲2
            """
        )]
        [TestCase
        (
            """
            skip if a is zero
                <codeA>
            end of skip
            skip unless a is zero
                <codeB>
            end of skip
            """,
            """
            WAC #a
            JPZ #↲1
                <codeA>
            NOP:↲1
            WAC #a
            JPZ #↲◘2
            JMP #↲2
            NOP:↲◘2
                <codeB>
            NOP:↲2
            """
        )]
        public void Skip(string code, string expected)
        {
            string expanded = AssemblyCompiler.ExpandSyntax(code);
            Assert.That(expanded, Is.EqualTo(expected.ReplaceLineEndings()));
        }

        [TestCase
        (
            """
            repeat while flag
                <code>
            end of repeat
            """,
            """
            JPF:↺◚1 #↺◘1
            JMP #↺◛1
            NOP:↺◘1
                <code>
            JMP #↺◚1
            NOP:↺◛1
            """
        )]
        [TestCase
        (
            """
            repeat while flag
                <codeA>
            end of repeat
            repeat until flag
                <codeB>
            end of repeat
            """,
            """
            JPF:↺◚1 #↺◘1
            JMP #↺◛1
            NOP:↺◘1
                <codeA>
            JMP #↺◚1
            NOP:↺◛1
            JPF:↺◚2 #↺◛2
                <codeB>
            JMP #↺◚2
            NOP:↺◛2
            """
        )]
        [TestCase
        (
            """
            repeat while a is zero
                <codeA>
            end of repeat
            repeat until a is zero
                <codeB>
            end of repeat
            """,
            """
            WAC:↺◚1 #a
            JPZ #↺◘1
            JMP #↺◛1
            NOP:↺◘1
                <codeA>
            JMP #↺◚1
            NOP:↺◛1
            WAC:↺◚2 #a
            JPZ #↺◛2
                <codeB>
            JMP #↺◚2
            NOP:↺◛2
            """
        )]
        public void Repeat(string code, string expected)
        {
            string expanded = AssemblyCompiler.ExpandSyntax(code);
            Assert.That(expanded, Is.EqualTo(expected.ReplaceLineEndings()));
        }

        [TestCase
        (
            """
            function ForTest
                <code>
            end of function
            """,
            """
            JMP #⨋ForTest
            #εForTest:⇲ForTest
            ADD:εForTest #⨀1
            RAC #↸ForTest
                <code>
            JMP
            :↸ForTest
            NOP:⨋ForTest
            JMP #∷
            1:⨀1
            NOP:∷
            """
        )]
        [TestCase
        (
            """
            call ForTest
            """,
            """
            WAC #⇲ForTest
            SWP
            """
        )]
        [TestCase
        (
            """
            function ForTest
                <code>
            end of function
            call ForTest
            """,
            """
            JMP #⨋ForTest
            #εForTest:⇲ForTest
            ADD:εForTest #⨀1
            RAC #↸ForTest
                <code>
            JMP
            :↸ForTest
            NOP:⨋ForTest
            WAC #⇲ForTest
            SWP
            JMP #∷
            1:⨀1
            NOP:∷
            """
        )]
        public void FunctionCall(string code, string expected)
        {
            string expanded = AssemblyCompiler.ExpandSyntax(code);
            Assert.That(expanded, Is.EqualTo(expected.ReplaceLineEndings()));
        }
    }
}
