using System;

namespace Asv.IO;

/// <summary>
/// ULog files have the following three sections:
/// 
/// ----------------------
/// |       Header       |
/// ----------------------
/// |    Definitions     |
/// ----------------------
/// |        Data        |
/// ----------------------
/// </summary>
[Flags]
public enum TokenPlaceFlags
{
    None = 0,
    /// <summary>
    /// Token can be placed only in header section
    /// </summary>
    Header = 0b0000_0001,
    /// <summary>
    /// Token can be placed only in definition section
    /// </summary>
    Definition = 0b0000_0010,
    /// <summary>
    /// Token can be placed only in data section
    /// </summary>
    Data = 0b0000_0100,
    /// <summary>
    /// Token can be placed in header and definition sections
    /// </summary>
    DefinitionAndData = Definition | Data
}