using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Seikilos.LargeTextFileIndexerLib;
using Xunit;

namespace Seikilos.LargeTextFileIndexerTests
{
    public class LargeFileIndexerTests
    {
        private async Task<List<long>> IndexFileHelper(string input)
        {
            var sut = new LargeFileIndexer();
            using var inStream = new MemoryStream();
            using var str = new StreamWriter(inStream);
            str.Write(input);
            str.Flush();
            inStream.Position = 0;


            var outStream = new MemoryStream();
         
            await sut.IndexAsync(inStream, outStream).ConfigureAwait(false);

            outStream.Position = 0;

            using var outBinReader = new BinaryReader(outStream);

            var ints = new List<long>();

            while (true)
            {
                try
                {
                    ints.Add(outBinReader.ReadInt64());
                }
                catch (EndOfStreamException)
                {
                    break;
                }

            }


            return ints;
        }


        [Fact]
        public async Task Test_Indexing_Works_For_One_Line()
        {
            // Arrange
            var input = "Hello World";

            // Act
            var result = await IndexFileHelper(input).ConfigureAwait(false);

            // Assert
            result.Count.Should().Be(1);
            result[0].Should().Be(0);

        }

        [Fact]
        public async Task Test_Indexing_Works_For_Single_New_Line()
        {
            // Arrange
            var input = "Hello\nWorld";

            // Act
            var result = await IndexFileHelper(input).ConfigureAwait(false);

            // Assert
            result.Count.Should().Be(2);
            result[0].Should().Be(0);
            result[1].Should().Be(6);
        }

        [Fact]
        public async Task Test_Indexing_Works_For_Return_with_New_Line()
        {
            // Arrange
            var input = "Hello\r\nWorld";

            // Act
            var result = await IndexFileHelper(input).ConfigureAwait(false);

            // Assert
            result.Count.Should().Be(2);
            result[0].Should().Be(0);
            result[1].Should().Be(7);
        }

        [Fact]
        public async Task Test_Indexing_Works_For_Mixed_New_Line_Chars()
        {
            // Arrange
            var input = "Hello\r\nWorld\nand me";

            // Act
            var result = await IndexFileHelper(input).ConfigureAwait(false);

            // Assert
            result.Count.Should().Be(3);
            result[0].Should().Be(0);
            result[1].Should().Be(7);
            result[2].Should().Be(13);
        }

        [Fact]
        public async Task Test_Empty_Data_Is_Supported()
        {
            // Arrange
            var input = "";

            // Act
            var result = await IndexFileHelper(input).ConfigureAwait(false);

            // Assert
            result.Count.Should().Be(0);
           
        }
    }
}
