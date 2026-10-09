using System;
using UnityEngine;

namespace DatdevUlts.GetPathResources
{
    public class PathResDef : PropertyAttribute
    {
        private Type _type;

        public Type Type => _type;

        public PathResDef(Type type) {
            _type = type;
        }
    }
}