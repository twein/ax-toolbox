//#undef for debug purposes
#define USE_COMPRESSED_SERIALIZATION

using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;



namespace AXToolbox.Common.IO
{
    [Serializable]
    class FileSerializableObject
    {
        [NonSerialized]
        protected bool _isDirty = false;

        public void Save(string fileName)
        {
            var fStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
#if USE_COMPRESSED_SERIALIZATION
            var stream = new GZipStream(fStream, CompressionMode.Compress);
#else
            var stream = fStream;
#endif

            try
            {
                IFormatter bfmtr = new BinaryFormatter();
                foreach (FieldInfo fieldI in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (fieldI.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                        bfmtr.Serialize(stream, fieldI.GetValue(this));
            }
            finally
            {
                stream.Close();
#if USE_COMPRESSED_SERIALIZATION
                fStream.Close();
#endif
            }
            _isDirty = false;
        }

        public void Load(string fileName)
        {
            var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
#if USE_COMPRESSED_SERIALIZATION
            var stream = new GZipStream(fStream, CompressionMode.Decompress, false);
#else
            var stream = fStream;
#endif

            try
            {
                IFormatter bfmtr = new BinaryFormatter();
                foreach (FieldInfo fieldI in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (fieldI.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                        fieldI.SetValue(this, bfmtr.Deserialize(stream));
            }
            finally
            {
                stream.Close();
#if USE_COMPRESSED_SERIALIZATION
                fStream.Close();
#endif
            }
            _isDirty = false;
        }
    }
}
