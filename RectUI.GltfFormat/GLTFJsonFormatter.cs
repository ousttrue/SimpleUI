﻿using System.Collections.Generic;
using RectUI.JSON;


namespace UniGLTF
{
    public class GLTFJsonFormatter: JsonFormatter
    {
        public void GLTFValue(JsonSerializableBase s)
        {
            CommaCheck();
            Store.Write(s.ToJson());
        }

        public void GLTFValue<T>(IEnumerable<T> values) where T : JsonSerializableBase
        {
            BeginList();
            foreach (var value in values)
            {
                GLTFValue(value);
            }
            EndList();
        }

        public void GLTFValue(List<string> values)
        {
            BeginList();
            foreach (var value in values)
            {
                this.Value(value);
            }
            EndList();
        }
    }
}
