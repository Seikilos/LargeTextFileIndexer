using System;
using System.Collections.Generic;
using System.IO;
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


        [Fact]
        public async Task Test_Large_File()
        {
            // Arrange
            var rand = new Random(12323);
            var data = new List<string>();
            var lines = 199999;
            for (var i = 0; i < lines; ++i)
            {
                data.Add(TestUtils.RandomString(rand, rand.Next(40,1000)));
            }

            var (inStream, indexStream) = await this.MakeStreamFromData(string.Join(Environment.NewLine, data)).ConfigureAwait(false);

           
            // Act
            var sut = new LargeFileIndexerAccess(inStream, indexStream);


            // Assert
            for (var i = 0; i < lines; ++i)
            {
                sut[i].Should().Be(data[i]);
            }
            

        }


    }
}
