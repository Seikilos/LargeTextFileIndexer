using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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


        public LargeFileIndexerAccess(string inputFile, string indexFile)
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

            InitStream(_internalInputStream, _internalIndexStream);
        }


        public LargeFileIndexerAccess(Stream inStream, Stream indexStream)
        {
            InitStream(inStream, indexStream);
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

        private void InitStream(Stream inStream, Stream indexStream)
        {
            _inStream = inStream ?? throw new ArgumentNullException(nameof(inStream));
            _streamReader = new StreamReader(_inStream);

            if (indexStream == null)
            {
                throw new ArgumentNullException(nameof(indexStream));
            }

            BuildIndex(indexStream);
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

            _inStream.Seek(_lines[line], SeekOrigin.Begin);
            
            // This is extremely inefficient
            _streamReader.DiscardBufferedData();

            var str = _streamReader.ReadLine();

            return str;
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