using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Fias.Source
{
	public class ElementReader<T> : IDisposable
	{
		private readonly Stream _stream;
		private readonly string _elementName;

		private readonly XmlSerializer _serializer;
		private readonly XmlReaderSettings _readerSettings;
		private XmlReader _reader;
		private bool _initial;
		private bool _isDisposed;
		private T _nextElementBuffer;

		public ElementReader(Stream stream, string defaultElementName = "")
		{
			_stream = stream ?? throw new ArgumentNullException(nameof(stream));

			var elementType = typeof(T);
			XmlTypeAttribute xmlTypeAttribyte = elementType.GetCustomAttributes(typeof(XmlTypeAttribute), true).FirstOrDefault() as XmlTypeAttribute;
			_elementName = defaultElementName;
			if(xmlTypeAttribyte != null)
			{
				_elementName = xmlTypeAttribyte.TypeName;
			}
			if(string.IsNullOrWhiteSpace(_elementName))
			{
				throw new InvalidOperationException($"Имя элемента не определено. Должно быть указано в {nameof(defaultElementName)} " +
					$"аргументе, либо в {nameof(XmlTypeAttribute)} искомого типа");
			}

			_readerSettings = new XmlReaderSettings();
			XmlRootAttribute root = new XmlRootAttribute();
			root.ElementName = _elementName;
			root.IsNullable = true;

			_serializer = new XmlSerializer(elementType, root);
			_reader = XmlReader.Create(_stream, _readerSettings);
			_initial = true;
		}

		public virtual T ReadNext()
		{
			T result = _nextElementBuffer;
			if(result == null)
			{
				result = ReadNextItem();
			}

			if(result == null)
			{
				throw new InvalidOperationException("Больше нет элементов для чтения");
			}

			_nextElementBuffer = ReadNextItem();

			return result;
		}

		protected virtual T ReadNextItem()
		{
			T result = default(T);
			while(!_reader.EOF)
			{
				if(_reader.NodeType == XmlNodeType.Element && _reader.Name == _elementName)
				{
					result = (T)_serializer.Deserialize(_reader);
					break;
				}
				else
				{
					_reader.Read();
				}
			}
			
			_initial = false;
			return result;
		}

		public virtual bool CanReadNext
		{
			get
			{
				if(_initial)
				{
					_nextElementBuffer = ReadNextItem();
				}

				if(_isDisposed)
				{
					return false;
				}

				return !_reader.EOF || _nextElementBuffer != null;
			}
		}

		public virtual void Dispose()
		{
			_isDisposed = true;
			_stream?.Dispose();
			_reader?.Dispose();
		}
	}
}
