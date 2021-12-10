using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Fias.Source
{
	public class FiasReaderFactory : IDisposable
	{
		private readonly ZipFile _fiasZipFile;
		private readonly Dictionary<ZipFileIndex, ZipEntry> _zipFilesIndex = new Dictionary<ZipFileIndex, ZipEntry>();
		private IEnumerable<string> _expectingFileTypeNames = new string[0];

		public FiasReaderFactory(ZipFile fiasZipFile)
		{
			_fiasZipFile = fiasZipFile ?? throw new ArgumentNullException(nameof(fiasZipFile));
			LoadExpectingFileTypes();
			IndexingFileNames();
		}

		private void LoadExpectingFileTypes()
		{
			var currentAssembly = Assembly.GetExecutingAssembly();
			_expectingFileTypeNames = currentAssembly.GetTypes()
			   .Where(t => Attribute.IsDefined(t, typeof(FiasFileAttribute), true))
			   .Select(t => t.GetCustomAttribute<FiasFileAttribute>())
			   .Select(x => x.FiasFileTypeName);
		}

		private void IndexingFileNames()
		{
			foreach(ZipEntry entry in _fiasZipFile)
			{
				var extension = Path.GetExtension(entry.Name);
				if(extension.ToLower() != ".xml")
				{
					continue;
				}

				var directoryName = Path.GetDirectoryName(entry.Name).Trim('\\', '/', ' ');
				var fileName = Path.GetFileName(entry.Name).Trim('\\', '/', ' ');
				var fileTypeName = GetFileTypeName(fileName);
				if(string.IsNullOrWhiteSpace(fileTypeName))
				{
					continue;
				}

				var entryIndexKey = new ZipFileIndex
				{
					RegionCode = directoryName,
					FileTypeName = fileTypeName
				};

				if(!_zipFilesIndex.ContainsKey(entryIndexKey))
				{
					_zipFilesIndex.Add(entryIndexKey, entry);
				}
			}
		}

		private string GetFileTypeName(string fileName)
		{
			var nameParts = fileName.Split('_');
			if(nameParts.Length < 3)
			{
				throw new InvalidOperationException($"Неизвестный файл {fileName}. Невозможно определить тип сущности, " +
					$"оно должно быть указано вначале имени файла. Формат имени файла: ТИП_ДАТА_GUID.XML") ;
			}

			var namePartsList = nameParts.ToList();
			namePartsList.Remove(namePartsList.Last());
			namePartsList.Remove(namePartsList.Last());
			var fileTypeName = string.Join('_', namePartsList);
			if(!_expectingFileTypeNames.Contains(fileTypeName))
			{
				return null;
			}

			return fileTypeName;
		}

		public ElementReader<T> GetReader<T>(int? regionCode = null)
		{
			var type = typeof(T);
			var fiasFileAttribute = type.GetCustomAttributes(typeof(FiasFileAttribute), true).FirstOrDefault() as FiasFileAttribute;
			if(fiasFileAttribute == null)
			{
				throw new InvalidOperationException($"Тип {type.FullName} не содержит обязательный аттрибут {nameof(FiasFileAttribute)}.");
			}

			var region = regionCode == null ? "" : regionCode.ToString();
			var fiasFileTypeName = fiasFileAttribute.FiasFileTypeName;
			var index = new ZipFileIndex(region, fiasFileTypeName);

			if(!_zipFilesIndex.TryGetValue(index, out ZipEntry entry))
			{
				string regionMessage = regionCode == null ? "без региона" : $"для региона ({regionCode})";
				throw new InvalidOperationException($"Невозможно найти файл {index.FileTypeName} {regionMessage}.");
			}

			var stream = _fiasZipFile.GetInputStream(entry);
			return new ElementReader<T>(stream);
		}

		public void Dispose()
		{
			_fiasZipFile?.Close();
		}

		private struct ZipFileIndex
		{
			public string RegionCode { get; set; }
			public string FileTypeName { get; set; }

			public ZipFileIndex(string regionCode, string fileTypeName)
			{
				RegionCode = regionCode;
				FileTypeName = fileTypeName;
			}
		}
	}
}
