using System;
using UnityEngine;

namespace GameTool.Assistants.Helper.SelectableString.Scripts
{
    public class SelectableString : PropertyAttribute
    {
        public string[] options;
        public Type[] optionTypes = new Type[0];

        public SelectableString(params string[] options)
        {
            this.options = options;
        }
    
        public SelectableString(params Type[] optionTypes)
        {
            this.optionTypes = optionTypes;
        }
    
        public SelectableString(Type[] optionTypes, string[] options)
        {
            this.optionTypes = optionTypes;
            this.options = options;
        }
    }
}