using System.Collections.Generic;
using net.queuepacked.Assembly.CPU;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class AssemblyTests
    {
        [Test]
        public void TextToBytes()
        {
            byte[] opCodes = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

            List<byte> compiled = AssemblyCompiler.CodesToProgram
            (
                """
                0
                1
                2
                3
                4
                5
                6
                7
                8
                9
                10
                11
                12
                """
            );

            Assert.That(compiled, Is.EqualTo(opCodes));
        }
        [Test]
        public void AsmToOpCode()
        {
            string byteCode =
                """
                0
                1
                2
                3
                4
                5
                6
                7
                8
                9
                10
                11
                12
                """;

            string compiled = AssemblyCompiler.OperationsToCodes
            (
                """
                nop
                rac
                wac
                swp
                add
                sub
                bsl
                bsr
                jmp
                jpz
                jpf
                rio
                wio
                """
            );

            Assert.That(compiled, Is.EqualTo(byteCode));
        }

        [Test]
        public void TranslateReference()
        {
            string intended =
                """
                1
                0
                3
                2
                0
                4
                5
                """;

            string converted = AssemblyCompiler.ReferencesToAddresses
            (
                """
                1:one
                #one
                #two
                2:two
                :four
                #four
                5
                """,
                out string[] markers
            );

            Assert.That(converted, Is.EqualTo(intended));
            Assert.That(markers.Length, Is.EqualTo(7));
            Assert.That(markers, Is.EqualTo(new[] { "one", "#one", "#two", "two", "four", "#four", "" }));
        }

        [Test]
        public void Comments()
        {
            string intended =
                """
                one
                two
                three
                """;

            string converted = AssemblyCompiler.RemoveCommentsAndWhitespace
            (
                """
                one
                      /comment 0
                two/comment 1
                /comment 2
                
                three
                
                /comment 3
                """
            );

            Assert.That(converted, Is.EqualTo(intended));
        }

        [Test]
        public void Sequence()
        {
            string intended =
                """
                one
                two
                three
                """;

            string converted = AssemblyCompiler.SingleLineToSequence
            (
                """
                one two
                three
                """
            );

            Assert.That(converted, Is.EqualTo(intended));
        }
    }
}
