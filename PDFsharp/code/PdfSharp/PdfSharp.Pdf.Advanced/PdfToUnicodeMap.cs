#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   Stefan Lange (mailto:Stefan.Lange@pdfsharp.com)
//
// Copyright (c) 2005-2009 empira Software GmbH, Cologne (Germany)
//
// http://www.pdfsharp.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using PdfSharp1_32.Drawing;
using PdfSharp1_32.Internal;
using PdfSharp1_32.Fonts.OpenType;
using PdfSharp1_32.Pdf.Advanced;
using PdfSharp1_32.Fonts;
using PdfSharp1_32.Pdf.Filters;

namespace PdfSharp1_32.Pdf.Advanced
{
  /// <summary>
  /// Represents a ToUnicode map for composite font.
  /// </summary>
  internal sealed class PdfToUnicodeMap : PdfDictionary
  {
    public PdfToUnicodeMap(PdfDocument document)
      : base(document)
    {
    }

    public PdfToUnicodeMap(PdfDocument document, CMapInfo cmapInfo)
      : base(document)
    {
      this.cmapInfo = cmapInfo;
    }

    /// <summary>
    /// Gets or sets the CMap info.
    /// </summary>
    public CMapInfo CMapInfo
    {
      get { return this.cmapInfo; }
      set { this.cmapInfo = value; }
    }
    CMapInfo cmapInfo;

    /// <summary>
    /// Creates the ToUnicode map from the CMapInfo.
    /// </summary>
    internal override void PrepareForSave()
    {
      base.PrepareForSave();

      // This code comes literaly from PDF Reference
      string prefix =
        "/CIDInit /ProcSet findresource begin\n" +
        "12 dict begin\n" +
        "begincmap\n" +
        "/CIDSystemInfo << /Registry (Adobe)/Ordering (UCS)/Supplement 0>> def\n" +
        "/CMapName /Adobe-Identity-UCS def /CMapType 2 def\n";
      string suffix = "endcmap CMapName currentdict /CMap defineresource pop end end";

      Dictionary<int, char> glyphIndexToCharacter = new Dictionary<int, char>();
      int lowIndex = 65536, hiIndex = -1;
      foreach (KeyValuePair<char, int> entry in this.cmapInfo.CharacterToGlyphIndex)
      {
        int index = (int)entry.Value;
        lowIndex = Math.Min(lowIndex, index);
        hiIndex = Math.Max(hiIndex, index);
        //glyphIndexToCharacter.Add(index, entry.Key);
        glyphIndexToCharacter[index] = entry.Key;
      }

      MemoryStream ms = new MemoryStream();
#if !SILVERLIGHT
      StreamWriter wrt = new StreamWriter(ms, Encoding.ASCII);
#else
      StreamWriter wrt = new StreamWriter(ms, Encoding.UTF8);
#endif

      wrt.Write(prefix);

      wrt.WriteLine("1 begincodespacerange");
      wrt.WriteLine(String.Format("<{0:X4}><{1:X4}>", lowIndex, hiIndex));
      wrt.WriteLine("endcodespacerange");

      // Sorting seems not necessary. The limit is 100 entries, we will see.
      wrt.WriteLine(String.Format("{0} beginbfrange", glyphIndexToCharacter.Count));
      foreach (KeyValuePair<int, char> entry in glyphIndexToCharacter)
        wrt.WriteLine(String.Format("<{0:X4}><{0:X4}><{1:X4}>", entry.Key, (int)entry.Value));
      wrt.WriteLine("endbfrange");

      wrt.Write(suffix);
      wrt.Close();

      // Compress like content streams
      byte[] bytes = ms.ToArray();
      ms.Close();
      if (Owner.Options.CompressContentStreams)
      {
        Elements.SetName("/Filter", "/FlateDecode");
        bytes = Filtering.FlateDecode.Encode(bytes);
      }
      PdfStream stream = CreateStream(bytes);
    }

    public sealed class Keys : PdfDictionary.PdfStream.Keys
    {
    }
  }
}
