﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vulkan.Build.Codegen
{
    public static class EnumHelpers
    {
        private static readonly Dictionary<string, string> s_knownEnumPrefixes = new Dictionary<string, string>
        {
            { "VkResult", "VK" },
        };

        private static readonly Dictionary<string, string> s_knownEnumValueNames = new Dictionary<string, string>
        {
            {  "VK_STENCIL_FRONT_AND_BACK", "FrontAndBack" },
            // VkStructureType
            { "VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_FLAGS_INFO", "MemoryAllocateFlagsInfo" },

            // VkSampleCountFlagBits
            {  "VK_SAMPLE_COUNT_1_BIT", "Count1" },
            {  "VK_SAMPLE_COUNT_2_BIT", "Count2" },
            {  "VK_SAMPLE_COUNT_4_BIT", "Count4" },
            {  "VK_SAMPLE_COUNT_8_BIT", "Count8" },
            {  "VK_SAMPLE_COUNT_16_BIT", "Count16" },
            {  "VK_SAMPLE_COUNT_32_BIT", "Count32" },
            {  "VK_SAMPLE_COUNT_64_BIT", "Count64" },
            // VkImageType
            {  "VK_IMAGE_TYPE_1D", "Image1D" },
            {  "VK_IMAGE_TYPE_2D", "Image2D" },
            {  "VK_IMAGE_TYPE_3D", "Image3D" },
            // VkImageViewType
            {  "VK_IMAGE_VIEW_TYPE_1D", "Image1D" },
            {  "VK_IMAGE_VIEW_TYPE_2D", "Image2D" },
            {  "VK_IMAGE_VIEW_TYPE_3D", "Image3D" },
            {  "VK_IMAGE_VIEW_TYPE_CUBE", "ImageCube" },
            {  "VK_IMAGE_VIEW_TYPE_1D_ARRAY", "Image1DArray" },
            {  "VK_IMAGE_VIEW_TYPE_2D_ARRAY", "Image2DArray" },
            {  "VK_IMAGE_VIEW_TYPE_CUBE_ARRAY", "ImageCubeArray" },
        };

        private static readonly HashSet<string> s_ignoredParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flags",
            "bit"
        };

        public static void WriteEnum(CsCodeWriter cw, EnumDefinition enumDef, TypeNameMappings tnm)
        {
            if (enumDef.Type == EnumType.Bitmask)
            {
                cw.WriteLine("[Flags]");
            }

            string extendFrom = enumDef.Bitwidth switch
            {
                32 => "int",
                64 => "long",
                _ => throw new ArgumentException($"Unknown bitwidth '{enumDef.Bitwidth}'.")
            };

            string mappedName = tnm.GetMappedName(enumDef.Name);
            string enumNamePrefix = GetEnumNamePrefix(mappedName);
            using (cw.PushBlock($"public enum {mappedName} : {extendFrom}"))
            {
                if (enumDef.Type == EnumType.Bitmask && !enumDef.Values.Any(ev => GetPrettyEnumName(ev.Name, enumNamePrefix) == "None"))
                {
                    cw.WriteLine($"None = 0,");
                }
                foreach (var value in enumDef.Values)
                {
                    if (!string.IsNullOrEmpty(value.Comment))
                    {
                        cw.WriteLine($"///<summary>{value.Comment}</summary>");
                    }

                    string prettyName = GetPrettyEnumName(value.Name, enumNamePrefix);
                    cw.WriteLine($"{prettyName} = {value.Value},");
                }
            }

            using (cw.PushBlock("public static partial class RawConstants"))
            {
                foreach (var value in enumDef.Values)
                {
                    if (!string.IsNullOrEmpty(value.Comment))
                    {
                        cw.WriteLine($"///<summary>{value.Comment}</summary>");
                    }

                    string prettyName = GetPrettyEnumName(value.Name, enumNamePrefix);
                    cw.WriteLine($"public const {mappedName} {value.Name} = {mappedName}.{prettyName};");
                }
            }
        }

        public static string GetEnumNamePrefix(string typeName)
        {
            if (s_knownEnumPrefixes.TryGetValue(typeName, out string knownValue))
            {
                return knownValue;
            }

            List<string> parts = new List<string>(4);
            int chunkStart = 0;
            for (int i = 0; i < typeName.Length; i++)
            {
                char typeNameChar = typeName[i];
                if (char.IsUpper(typeNameChar))
                {
                    if (chunkStart != i)
                    {
                        parts.Add(typeName.Substring(chunkStart, i - chunkStart));
                    }

                    chunkStart = i;
                    if (i == typeName.Length - 1)
                    {
                        parts.Add(typeName.Substring(i, 1));
                    }
                }
                else if (i == typeName.Length - 1)
                {
                    parts.Add(typeName.Substring(chunkStart, typeName.Length - chunkStart));
                }
            }


            for (int i = 0; i < parts.Count; i++)
            {
                if (
                        parts[i] == "Flag"
                    || parts[i] == "Flags"
                    || (parts[i] == "K" && (i + 2) < parts.Count && parts[i + 1] == "H" && parts[i + 2] == "R")
                    || (parts[i] == "A" && (i + 2) < parts.Count && parts[i + 1] == "M" && parts[i + 2] == "D")
                    || (parts[i] == "E" && (i + 2) < parts.Count && parts[i + 1] == "X" && parts[i + 2] == "T")
                    || (parts[i] == "Type" && (i + 3) < parts.Count && parts[i + 1] == "N" && parts[i + 2] == "V" && parts[i + 3] == "X")
                    )
                {
                    parts = new List<string>(parts.Take(i));
                    break;
                }
                if (parts[i] == "Flags2") {
                    parts = new List<string>(parts.Take(i));
                    parts.Add("2");
                    break;
                }
            }

            return string.Join("_", parts.Select(s => s.ToUpper()));
        }

        public static string TrimIgnoredParts(string ret)
        {
            foreach (string ignored in s_ignoredParts)
            {
                int index;
                if ((index = ret.IndexOf(ignored, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    ret = ret.Remove(index, ignored.Length);
                }
            }

            return ret;
        }

        public static string GetPrettyEnumName(string value, string enumPrefix)
        {
            if (s_knownEnumValueNames.TryGetValue(value, out string knownName))
            {
                return knownName;
            }

            return CodegenUtil.GetPrettyName(value, enumPrefix, s_ignoredParts);
        }
    }
}
