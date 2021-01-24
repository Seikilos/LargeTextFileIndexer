using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Seikilos.LargeTextFileIndexerLib
{
    public class LargeFileIndexerAccess : IReadOnlyCollection<string>, IDisposable
    {
        public string this[int i] => GetIndexFromStream(i);
        private readonly Stream _internalIndexStream;

        private readonly Stream _internalInputStream;
        private Stream _inStream;


        private List<long> _lines;
        private StreamReader _streamReader;

        private int _bufferSize = 1024;
        private char[] _buffer;
        private long _bufferPosition = 0;
        private long _bytesRead;


        public LargeFileIndexerAccess(string inputFile, string indexFile, int bufferSize = -1)
        {
            if (File.Exists(inputFile) == false)
            {
                throw new FileNotFoundException(inputFile);
            }

            if (File.Exists(indexFile) == false)
            {
                throw new FileNotFoundException(indexFile);
            }

            _internalInputStream = File.OpenRead(inputFile);
            _internalIndexStream = File.OpenRead(indexFile);

            InitStream(_internalInputStream, _internalIndexStream, bufferSize);
        }


        public LargeFileIndexerAccess(Stream inStream, Stream indexStream, int bufferSize = -1)
        {
            InitStream(inStream, indexStream, bufferSize);
        }

        public void Dispose()
        {
            _internalIndexStream?.Flush();
            _internalIndexStream?.Dispose();

            _internalInputStream?.Flush();
            _internalInputStream?.Dispose();

            _inStream?.Dispose();
            _streamReader.Dispose();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return new AccessorEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get => _lines.Count;
        }

        private void InitStream(Stream inStream, Stream indexStream, int bufferSize)
        {
            if (bufferSize == -1)
            {
                _bufferSize = 1024;
            }
            else
            {
                _bufferSize = bufferSize;
            }
            _inStream = inStream ?? throw new ArgumentNullException(nameof(inStream));
            _streamReader = new StreamReader(_inStream);
            

            _buffer = new char[_bufferSize];

            if (indexStream == null)
            {
                throw new ArgumentNullException(nameof(indexStream));
            }

            BuildIndex(indexStream);

            moveStreamAndRead(0);
        }

        private void BuildIndex(Stream indexStream)
        {
            _lines = new List<long>();
            using (var br = new BinaryReader(indexStream))
            {
                while (true)
                {
                    try
                    {
                        _lines.Add(br.ReadInt64());
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }
        }

        private string GetIndexFromStream(int line)
        {
            if (line >= _lines.Count)
            {
                throw new IndexOutOfRangeException($"Index {line} is out of range. Element count is {_lines.Count}");
            }

            // Check if current buffer already contains the start position
            var lineStart = _lines[line];
            if (lineStart >= _bufferPosition && lineStart < _bufferPosition + _bufferSize)
            {
                // lineStart is in buffer

            }
            else
            {
                // lineStart is not in buffer, advance

                moveStreamAndRead(lineStart);
                
                _bufferPosition = (int) lineStart;

              
            }

            return readFromStream(lineStart);
            // Read stream until \r\n or \n found, mo


            _inStream.Seek(_lines[line], SeekOrigin.Begin);
            
            // This is extremely inefficient
            _streamReader.DiscardBufferedData();

            var str = _streamReader.ReadLine();

            return str;
        }

        private void moveStreamAndRead(long fromPosition)
        {
            _inStream.Seek(fromPosition, SeekOrigin.Begin);
            _bufferPosition = fromPosition;
            _streamReader.DiscardBufferedData();
            _bytesRead += _streamReader.ReadBlock(_buffer, 0, _bufferSize);
        }

        private string readFromStream(long index)
        {
            var sb = new StringBuilder();

            while (index <= _bytesRead)
            {
                if (index >= (_bufferPosition + _bufferSize))
                {
                    moveStreamAndRead(index);
                }

                var nextChar = _buffer[index%_bufferSize];

                ++index;
                

                if (nextChar == '\n' || nextChar == '\0')
                {
                    return sb.ToString();
                }

                if (nextChar != '\r')
                {
                    sb.Append(nextChar);
                }

            }

            return sb.ToString();
        }

        internal class AccessorEnumerator : IEnumerator<string>
        {
            private readonly LargeFileIndexerAccess _access;
            private int _position = -1;

            public AccessorEnumerator(LargeFileIndexerAccess access)
            {
                _access = access ?? throw new ArgumentNullException(nameof(access));
            }

            public bool MoveNext()
            {
                ++_position;
                return _position < _access.Count;
            }

            public void Reset()
            {
                _position = -1;
            }

            public string Current
            {
                get
                {
                    try
                    {
                        return _access[_position];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}