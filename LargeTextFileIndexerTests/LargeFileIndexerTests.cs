using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
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

            return ParseStream(outStream);

        }

        private List<long> ParseStream(Stream outStream)
        {
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

        [Fact]
        public void Test_Input_File_Must_Exist()
        {
            // Arrange
            

            // Act
            Func<Task> a = async () => await new LargeFileIndexer().IndexAsync("do_not_Exist", "output_file").ConfigureAwait(false);

            // Assert
            a.Should().Throw<FileNotFoundException>().WithMessage("do_not_Exist");
        }

        [Fact]
        public async Task Test_Output_File_Has_Non_Zero_Size()
        {
            // Arrange
            File.WriteAllText("input.txt", "some content");

            // Act
            await new LargeFileIndexer().IndexAsync("input.txt", "output.bin").ConfigureAwait(false);
            var fi = new FileInfo("output.bin");

            // Assert
            fi.Length.Should().BeGreaterThan(0);
        }


        [Fact]
        public void Test_Output_File_Is_Overwritten()
        {
            // Arrange
            File.WriteAllText("input.txt", "some content");

            // Act
            Func<Task> a = async () =>
            {
                await new LargeFileIndexer().IndexAsync("input.txt", "output.bin").ConfigureAwait(false);
                await new LargeFileIndexer().IndexAsync("input.txt", "output.bin").ConfigureAwait(false);

            };

            // Assert
            a.Should().NotThrow();
        }

        [InlineData(12356)]
        [Theory]
        public async Task Test_Large_Files(int seed)
        {
            // Arrange
            var rand = new Random(seed);
            var lines = rand.Next(3163,134353);
            var str = makeLargeString(rand, lines);

            var sut = new LargeFileIndexer();
            await using var outIndexStream = new MemoryStream();

            // Act
            await sut.IndexAsync(str, outIndexStream).ConfigureAwait(false);
            outIndexStream.Position = 0;
            var linesParsed = ParseStream(outIndexStream);

            // Assert
            linesParsed.Count.Should().Be(lines);

        }

        private Stream makeLargeString(Random rand, int lines)
        {
            var stringStream = new MemoryStream();
            var sw = new StreamWriter(stringStream);
            for (var i = 0; i < lines; ++i)
            {
                sw.Write(RandomString(rand, rand.Next(1,4949)));
                if (i < lines - 1)
                {
                    if (rand.NextDouble() < 0.5)
                    {
                        sw.Write("\r\n");
                    }
                    else
                    {
                        sw.Write("\n");
                    }
                }
            }

            sw.Flush();

            stringStream.Position = 0;
            return stringStream;

        }

        public static string RandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
