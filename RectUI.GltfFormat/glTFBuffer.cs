﻿using System;
using System.Linq;
using RectUI.JSON;

namespace UniGLTF
{
    [Serializable]
    public class glTFBuffer : JsonSerializableBase
    {
        public glTFBuffer()
        {
            // for GLB
        }

        public glTFBuffer(string _uri)
        {
            this.uri = _uri;
        }
        public string uri;

        //[JsonSchema(Required = true, Minimum = 1)]
        public int byteLength;

        // empty schemas
        public object extensions;
        public object extras;
        public string name;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                f.KeyValue(() => uri);
            }
            f.KeyValue(() => byteLength);
        }
    }

    [Serializable]
    public class glTFBufferView : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int buffer;

        [JsonSchema(Minimum = 0)]
        public int byteOffset;

        [JsonSchema(Required = true, Minimum = 1)]
        public int byteLength;

        [JsonSchema(Minimum = 4, Maximum = 252, MultipleOf = 4)]
        public int byteStride;

        [JsonSchema(EnumSerializationType = EnumSerializationType.AsInt, EnumExcludes = new object[] { glBufferTarget.NONE })]
        public glBufferTarget target;

        // empty schemas
        public object extensions;
        public object extras;
        public string name;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => buffer);
            f.KeyValue(() => byteOffset);
            f.KeyValue(() => byteLength);
            if (target != glBufferTarget.NONE)
            {
                f.Key("target"); f.Value((int)target);
            }
            /* When this is not defined, data is tightly packed. When two or more accessors use the same bufferView, this field must be defined.
            if (byteStride >= 4)
            {
                f.KeyValue(() => byteStride);
            }
            */
        }
    }

    [Serializable]
    public class glTFSparseIndices : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int bufferView = -1;

        [JsonSchema(Minimum = 0)]
        public int byteOffset;

        [JsonSchema(Required = true, EnumValues = new object[] { 5121, 5123, 5125 })]
        public glComponentType componentType;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => bufferView);
            f.KeyValue(() => byteOffset);
            f.Key("componentType"); f.Value((int)componentType);
        }
    }

    [Serializable]
    public class glTFSparseValues : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int bufferView = -1;

        [JsonSchema(Minimum = 0)]
        public int byteOffset;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => bufferView);
            f.KeyValue(() => byteOffset);
        }
    }

    [Serializable]
    public class glTFSparse : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 1)]
        public int count;

        [JsonSchema(Required = true)]
        public glTFSparseIndices indices;

        [JsonSchema(Required = true)]
        public glTFSparseValues values;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => count);
            f.KeyValue(() => indices);
            f.KeyValue(() => values);
        }
    }

    [Serializable]
    public class glTFAccessor : JsonSerializableBase
    {
        [JsonSchema(Minimum = 0)]
        public int bufferView = -1;

        [JsonSchema(Minimum = 0, Dependencies = new string[] { "bufferView" })]
        public int byteOffset;

        [JsonSchema(Required = true, EnumValues = new object[] { "SCALAR", "VEC2", "VEC3", "VEC4", "MAT2", "MAT3", "MAT4" }, EnumSerializationType = EnumSerializationType.AsString)]
        public string type;

        public int TypeCount
        {
            get
            {
                switch (type)
                {
                    case "SCALAR":
                        return 1;
                    case "VEC2":
                        return 2;
                    case "VEC3":
                        return 3;
                    case "VEC4":
                    case "MAT2":
                        return 4;
                    case "MAT3":
                        return 9;
                    case "MAT4":
                        return 16;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        [JsonSchema(Required = true, EnumSerializationType = EnumSerializationType.AsInt)]
        public glComponentType componentType;

        [JsonSchema(Required = true, Minimum = 1)]
        public int count;

        public int ElementSize
        {
            get
            {
                switch (componentType)
                {
                    case glComponentType.UNSIGNED_BYTE:
                    case glComponentType.BYTE:
                        return TypeCount;

                    case glComponentType.UNSIGNED_SHORT:
                    case glComponentType.SHORT:
                        return TypeCount * 2;

                    case glComponentType.UNSIGNED_INT:
                    case glComponentType.FLOAT:
                        return TypeCount * 4;
                }

                throw new NotImplementedException();
            }
        }

        [JsonSchema(MinItems = 1, MaxItems = 16)]
        public float[] max;

        [JsonSchema(MinItems = 1, MaxItems = 16)]
        public float[] min;

        public bool normalized;
        public glTFSparse sparse;

        // empty schemas
        public string name;

        public object extensions;

        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => bufferView);
            f.KeyValue(() => byteOffset);
            f.KeyValue(() => type);
            f.Key("componentType"); f.Value((int)componentType);
            f.KeyValue(() => count);
            if (max != null && max.Any())
            {
                f.KeyValue(() => max);
            }
            if (min != null && min.Any())
            {
                f.KeyValue(() => min);
            }

            if (sparse != null && sparse.count > 0)
            {
                f.KeyValue(() => sparse);
            }

            f.KeyValue(() => normalized);
            f.KeyValue(() => name);
        }
    }
}
