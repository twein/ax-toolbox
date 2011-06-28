using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Xml;

namespace AXToolbox.Common.IO
{
    /// <summary> Serialization format types.
    /// </summary>
    public enum SerializationFormat
    {
        /// <summary>Binary serialization format.
        /// </summary>
        Binary,
        /// <summary>Compressed binary serialization format.
        /// </summary>
        CompressedBinary,
        /// <summary>Data Contract Serialization format (xml by ref).
        /// </summary>
        DataContract,
        /// <summary>Document serialization format (xml by value).
        /// </summary>
        XML
    }

    /// <summary>
    /// Facade to XML serialization and deserialization of strongly typed objects to/from an XML file.
    /// 
    /// References: 
    /// XML Serialization at  http://samples.gotdotnet.com/QuickStart/howto/default.aspx?url=/quickstart/howto/doc/xmlserialization/rwobjfromxml.aspx
    /// DataContract serialization at http://stackoverflow.com/questions/1617528/net-xml-serialization-storing-reference-instead-of-object-copy
    /// 
    /// CompressedBinary serialization format by toni@balloonerds.com 06/2010
    /// DataContract serialization format by toni@balloonerds.com 06/2011
    /// </summary>
    public static class ObjectSerializer<T> where T : class // Specify that T must be a class.
    {
        /// <summary>
        /// Loads an object from an XML file using a specified serialized format.
        /// </summary>
        /// <example>
        /// <code>
        /// serializableObject = ObjectXMLSerializer<SerializableObject>.Load(@"C:\XMLObjects.xml", SerializedFormat.Binary);
        /// </code>
        /// </example>		
        /// <param name="path">Path of the file to load the object from.</param>
        /// <param name="serializedFormat">XML serialized format used to load the object.</param>
        /// <returns>Object loaded from an XML file using the specified serialized format.</returns>
        public static T Load(string path, SerializationFormat serializedFormat)
        {
            T serializableObject = null;

            switch (serializedFormat)
            {
                case SerializationFormat.Binary:
                    serializableObject = LoadFromBinaryFormat(path);
                    break;

                case SerializationFormat.CompressedBinary:
                    serializableObject = LoadFromCompressedBinaryFormat(path);
                    break;

                case SerializationFormat.DataContract:
                    serializableObject = LoadFromDataContractFormat(path);
                    break;

                case SerializationFormat.XML:
                default:
                    serializableObject = LoadFromXmlFormat(path);
                    break;
            }

            return serializableObject;
        }
        /// <summary>
        /// Saves an object to an XML file using a specified serialized format.
        /// </summary>
        /// <example>
        /// <code>
        /// SerializableObject serializableObject = new SerializableObject();
        /// 
        /// ObjectXMLSerializer<SerializableObject>.Save(serializableObject, @"C:\XMLObjects.xml", SerializedFormat.Binary);
        /// </code>
        /// </example>
        /// <param name="serializableObject">Serializable object to be saved to file.</param>
        /// <param name="path">Path of the file to save the object to.</param>
        /// <param name="serializedFormat">XML serialized format used to save the object.</param>
        public static void Save(T serializableObject, string path, SerializationFormat serializedFormat)
        {
            switch (serializedFormat)
            {
                case SerializationFormat.Binary:
                    SaveToBinaryFormat(serializableObject, path);
                    break;

                case SerializationFormat.CompressedBinary:
                    SaveToCompressedBinaryFormat(serializableObject, path);
                    break;

                case SerializationFormat.DataContract:
                default:
                    SaveToDataContractFormat(serializableObject, path);
                    break;

                case SerializationFormat.XML:
                    SaveToXmlFormat(serializableObject, path);
                    break;
            }
        }

        private static T LoadFromBinaryFormat(string path)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate))
            {
                var binaryFormatter = new BinaryFormatter();
                var serializableObject = binaryFormatter.Deserialize(fileStream) as T;
                return serializableObject;
            }
        }
        private static T LoadFromCompressedBinaryFormat(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.OpenOrCreate))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            {
                var binaryFormatter = new BinaryFormatter();
                var serializableObject = binaryFormatter.Deserialize(gzipStream) as T;
                return serializableObject;
            }
        }
        private static T LoadFromDataContractFormat(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.OpenOrCreate))
            {
                var dataContractSerializer = new DataContractSerializer(typeof(T), null, int.MaxValue, true, true, null);
                var serializableObject = dataContractSerializer.ReadObject(fileStream) as T;
                return serializableObject;
            }
        }
        private static T LoadFromXmlFormat(string path)
        {
            using (var textReader = new StreamReader(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                var serializableObject = xmlSerializer.Deserialize(textReader) as T;
                return serializableObject;
            }
        }

        private static void SaveToBinaryFormat(T serializableObject, string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(fileStream, serializableObject);
            }
        }
        private static void SaveToCompressedBinaryFormat(T serializableObject, string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(gzipStream, serializableObject);
            }
        }
        private static void SaveToDataContractFormat(T serializableObject, string path)
        {
            var xmlSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t",
                NewLineHandling = NewLineHandling.Entitize
            };

            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(fileStream, xmlSettings))
            {
                var dataContractSerializer = new DataContractSerializer(typeof(T), null, int.MaxValue, true, true, null);
                dataContractSerializer.WriteObject(xmlWriter, serializableObject);
            }
        }
        private static void SaveToXmlFormat(T serializableObject, string path)
        {
            using (var textWriter = new StreamWriter(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                xmlSerializer.Serialize(textWriter, serializableObject);
            }
        }
    }
}

