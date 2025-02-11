﻿using System;
using System.Xml.Linq;

namespace Vulkan.Build.Codegen
{
    public class ParameterDefinition
    {
        public string Name { get; }
        public TypeSpec Type { get; }
        public ParameterModifier Modifier { get; }
        public bool IsOptional { get; }
        public string API { get; }

        public ParameterDefinition(string name, TypeSpec type, ParameterModifier modifier, bool isOptional, string api)
        {
            Name = name;
            Type = type;
            Modifier = modifier;
            IsOptional = isOptional;
            API = api;
        }

        public static ParameterDefinition CreateFromXml(XElement xe)
        {
            string name = xe.Element("name").Value;
            var optionalAttr = xe.Attribute("optional");
            bool isOptional = optionalAttr != null && optionalAttr.Value == "true";
            string typeName = xe.Element("type").Value;
            int pointerLevel = 0;
            if (xe.Value.Contains($"{typeName}**") || xe.Value.Contains($"{typeName}* const*"))
            {
                pointerLevel = 2;
            }
            else if (xe.Value.Contains($"{typeName}*"))
            {
                pointerLevel = 1;
            }

            string api = xe.Attribute("api")?.Value;

            TypeSpec type = new TypeSpec(typeName, pointerLevel);

            return new ParameterDefinition(name, type, ParameterModifier.None, isOptional, api);
        }

        public string GetMappedAndNormalizedString(TypeNameMappings tnm)
        {
            return $"{GetModifierString()}{Type.MapTypeSpec(tnm)} {CodegenUtil.NormalizeFieldName(Name)}";
        }

        public string GetModifierString()
        {
            if (Modifier == ParameterModifier.Ref)
            {
                return "ref ";
            }
            else if (Modifier == ParameterModifier.Out)
            {
                return "out ";
            }
            else
            {
                return string.Empty;
            }
        }

        public override string ToString()
        {
            return $"{GetModifierString()}{Type} {Name}";
        }
    }

    public enum ParameterModifier
    {
        None,
        Ref,
        Out
    }
}
