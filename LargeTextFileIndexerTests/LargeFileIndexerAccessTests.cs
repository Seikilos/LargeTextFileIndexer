using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Seikilos.LargeTextFileIndexerLib;
using Xunit;

namespace Seikilos.LargeTextFileIndexerTests
{
    public class LargeFileIndexerAccessTests
    {

        private async Task<(Stream, Stream)> MakeStreamFromData(string rawData)
        {
             var inStream = new MemoryStream();
            var str = new StreamWriter(inStream);
            str.Write(rawData);
            str.Flush();
            inStream.Position = 0;


            var outStream = new MemoryStream();
            var sut = new LargeFileIndexer();
            await sut.IndexAsync(inStream, outStream).ConfigureAwait(false);

            inStream.Position = 0;
            outStream.Position = 0;

            return (inStream, outStream);
        }

       [Fact]
        public void Test_Access_Throws_On_Null_Stream_Input()
        {
            // Arrange
           
            // Act
            Action action = () => new LargeFileIndexerAccess(null, new MemoryStream());

            // Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Test_Access_Throws_On_Null_Index_Stream_Input()
        {
            // Arrange

            // Act
            Action action = () => new LargeFileIndexerAccess(new MemoryStream(), null);

            // Assert
            action.Should().Throw<ArgumentNullException>();
        }


        [Fact]
        public async Task Test_Access_Accesses_Stream()
        {
            // Arrange
            var (inStream, indexStream) = await this.MakeStreamFromData("Hello\nWorld\nString").ConfigureAwait(false);

            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream);

            // Assert
            sut[0].Should().Be("Hello");
            sut[1].Should().Be("World");
            sut[2].Should().Be("String");

            inStream.Dispose();
            indexStream.Dispose();
        }

        [Fact]
        public async Task Test_Access_Accesses_Stream_Repeatedly()
        {
            // Arrange
            var (inStream, indexStream) = await this.MakeStreamFromData("Hello\nWorld\nString").ConfigureAwait(false);

            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream);

            // Assert
            sut[0].Should().Be("Hello");
            sut[1].Should().Be("World");
            sut[2].Should().Be("String");
            
            sut[0].Should().Be("Hello");
            sut[2].Should().Be("String");


            inStream.Dispose();
            indexStream.Dispose();
        }



        [Fact]
        public async Task Test_Access_Accesses_Stream_Small_Buffer()
        {
            // Arrange
            var (inStream, indexStream) = await this.MakeStreamFromData("Hello\nWorld\nString").ConfigureAwait(false);

            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream, 4);

            // Assert
            sut[0].Should().Be("Hello");
            sut[1].Should().Be("World");
            sut[2].Should().Be("String");

            inStream.Dispose();
            indexStream.Dispose();
        }



        [Fact]
        public async Task Test_Access_Accesses_Stream_Repeatedly_Small_Buffer()
        {
            // Arrange
            var (inStream, indexStream) = await this.MakeStreamFromData("Hello\nWorld\nString").ConfigureAwait(false);

            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream, 4);

            // Assert
            sut[0].Should().Be("Hello");
            sut[1].Should().Be("World");
            sut[2].Should().Be("String");

            sut[0].Should().Be("Hello");
            sut[2].Should().Be("String");


            inStream.Dispose();
            indexStream.Dispose();
        }


        [Fact]
        public async Task Test_Access_Accesses_Stream_Small_Buffer_Mixed_New_Lines()
        {
            // Arrange
            var (inStream, indexStream) = await this.MakeStreamFromData("Hello\r\nWorld\nString").ConfigureAwait(false);

            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream, 4);

            // Assert
            sut[0].Should().Be("Hello");
            sut[1].Should().Be("World");
            sut[2].Should().Be("String");

            inStream.Dispose();
            indexStream.Dispose();
        }


        [Fact]
        public async Task Test_Access_Accesses_File()
        {
            // Arrange
            File.WriteAllText("sample.txt", "Hello\nWorld\nString");
            var indexer = new LargeFileIndexer();
            await indexer.IndexAsync("sample.txt", "sample.index").ConfigureAwait(false);

            // Act
            var sut = new LargeFileIndexerAccess("sample.txt", "sample.index");
            
            // Assert
            sut[0].Should().Be("Hello");
            sut[2].Should().Be("String");

        }

        [Fact]
        public async Task Test_Access_Has_Count()
        {
            // Arrange
            var (inStream, indexStream) = await this.MakeStreamFromData("Hello\nWorld\nString").ConfigureAwait(false);

            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream);

            // Assert
            sut.Count.Should().Be(3);
          

            inStream.Dispose();
            indexStream.Dispose();
        }

        [Fact]
        public async Task Test_Access_Supports_Linq()
        {
            
            // Arrange
            var (inStream, indexStream) = await this.MakeStreamFromData("Hello\nWorld\nString").ConfigureAwait(false);

            var refList = new string[] { "Hello", "World", "String" };

            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream);


            // Assert
            var index = 0;
            foreach (var s in sut)
            {
                s.Should().Be(refList[index++]);
            }


            inStream.Dispose();
            indexStream.Dispose();
        }


        [InlineData(30)]
        [InlineData(199999)]
        [Theory]
        public async Task Test_Large_File(int lines)
        {
            // Arrange
            var rand = new Random(12323);
            var data = new List<string>();
          
            for (var i = 0; i < lines; ++i)
            {
                data.Add(TestUtils.RandomString(rand, rand.Next(40,1000)));
            }

            var sb = new StringBuilder();
            for (var i = 0; i < lines - 1; ++i)
            {
                sb.Append(data[i]);
                if (rand.NextDouble() < 0.5)
                {
                    sb.Append("\r\n");
                }
                else
                {
                    sb.Append("\n");
                }
            }

            sb.Append(data.Last());

            var (inStream, indexStream) = await this.MakeStreamFromData(string.Join(Environment.NewLine, data)).ConfigureAwait(false);

           
            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream);


            // Assert
            for (var i = 0; i < lines; ++i)
            {
                sut[i].Should().Be(data[i], $"Line {i} must be equal");
            }
            

        }


    }
}
