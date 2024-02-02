// Originally based on from https://stackoverflow.com/a/29464157

using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public class BmFont
    {
        private readonly Dictionary<char, FontChar> characterMap;
        private readonly Texture2D texture;
        public readonly int lineHeight;

        public BmFont(string basePath)
        {
            string fntPath = Path.Combine(Constants.rootPath, basePath + ".fnt");

            XmlSerializer deserializer = new XmlSerializer(typeof(FontDescription));
            using TextReader textReader = new StreamReader(fntPath);
            var fontDescription = (FontDescription) deserializer.Deserialize(textReader);

            Debug.Assert(fontDescription.Pages.Count == 1);
            texture = Texture2D.FromFile(Game1.game.GraphicsDevice, Path.Combine(Directory.GetParent(fntPath).FullName, fontDescription.Pages[0].File));

            characterMap = new Dictionary<char, FontChar>();

            foreach (FontChar fontCharacter in fontDescription.Chars)
                characterMap.Add((char)fontCharacter.ID, fontCharacter);

            lineHeight = fontDescription.Common.LineHeight;
        }

        public int measureText(string text, int overrideCharacterWidth = 0)
        {
            if (overrideCharacterWidth > 0)
                return text.Length * overrideCharacterWidth;

            int x = 0;
            foreach (char c in text)
            {
                FontChar fontChar;
                if (!characterMap.TryGetValue(c, out fontChar))
                    characterMap.TryGetValue('?', out fontChar);

                if (fontChar != null)
                    x += fontChar.XAdvance;
            }

            return x;
        }

        public Vec2i draw(string text, Vec2i pos, MySpriteBatch spriteBatch, int overrideCharacterWidth = 0, Color? underlineColor = null, Color? tintColor = null)
        {
            if (tintColor == null)
                tintColor = Color.White;

            Vec2i originalPos = pos;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                FontChar fontChar;
                if (!characterMap.TryGetValue(c, out fontChar))
                    characterMap.TryGetValue('?', out fontChar);

                if (fontChar != null)
                {
                    var sourceRectangle = new Rect(fontChar.X, fontChar.Y, fontChar.Width, fontChar.Height);
                    var position = new Vec2f(pos.x + fontChar.XOffset, pos.y + fontChar.YOffset);

                    // float color = ((float)i)/((float)text.Length-1);
                    // spriteBatch.r(Textures.white).pos(position).size(sourceRectangle.size).color(new Color(0f,color,0f,1f)).draw();
                    spriteBatch.r(texture).pos(position).size(sourceRectangle.size).uv(sourceRectangle).color(tintColor.Value).draw();
                    pos.x += overrideCharacterWidth != 0 ? overrideCharacterWidth : fontChar.XAdvance;
                }
            }

            if (underlineColor.HasValue)
            {
                spriteBatch.r(Textures.white).color(underlineColor.Value)
                                             .pos(originalPos + new Vec2i(0, lineHeight))
                                             .size(new Vec2f(pos.x - originalPos.x, 1))
                                             .draw();
            }

            return pos;
        }

        [Serializable]
        [XmlRoot("font")]
        public class FontDescription
        {
            [XmlElement("info")] public FontInfo Info { get; set; }

            [XmlElement("common")] public FontCommon Common { get; set; }

            [XmlArray("pages")]
            [XmlArrayItem("page")]
            public List<FontPage> Pages { get; set; }

            [XmlArray("chars")]
            [XmlArrayItem("char")]
            public List<FontChar> Chars { get; set; }

            [XmlArray("kernings")]
            [XmlArrayItem("kerning")]
            public List<FontKerning> Kernings { get; set; }
        }

        [Serializable]
        public class FontInfo
        {
            [XmlAttribute("face")] public string Face { get; set; }

            [XmlAttribute("size")] public Int32 Size { get; set; }

            [XmlAttribute("bold")] public Int32 Bold { get; set; }

            [XmlAttribute("italic")] public Int32 Italic { get; set; }

            [XmlAttribute("charset")] public string CharSet { get; set; }

            [XmlAttribute("unicode")] public Int32 Unicode { get; set; }

            [XmlAttribute("stretchH")] public Int32 StretchHeight { get; set; }

            [XmlAttribute("smooth")] public Int32 Smooth { get; set; }

            [XmlAttribute("aa")] public Int32 SuperSampling { get; set; }

            private Rectangle _Padding;

            [XmlAttribute("padding")]
            public string Padding
            {
                get => _Padding.X + "," + _Padding.Y + "," + _Padding.Width + "," + _Padding.Height;
                set
                {
                    string[] padding = value.Split(',');
                    _Padding = new Rectangle(Convert.ToInt32(padding[0]), Convert.ToInt32(padding[1]), Convert.ToInt32(padding[2]), Convert.ToInt32(padding[3]));
                }
            }

            private Point _Spacing;

            [XmlAttribute("spacing")]
            public string Spacing
            {
                get => _Spacing.X + "," + _Spacing.Y;
                set
                {
                    string[] spacing = value.Split(',');
                    _Spacing = new Point(Convert.ToInt32(spacing[0]), Convert.ToInt32(spacing[1]));
                }
            }

            [XmlAttribute("outline")] public Int32 OutLine { get; set; }
        }

        [Serializable]
        public class FontCommon
        {
            [XmlAttribute("lineHeight")] public Int32 LineHeight { get; set; }

            [XmlAttribute("base")] public Int32 Base { get; set; }

            [XmlAttribute("scaleW")] public Int32 ScaleW { get; set; }

            [XmlAttribute("scaleH")] public Int32 ScaleH { get; set; }

            [XmlAttribute("pages")] public Int32 Pages { get; set; }

            [XmlAttribute("packed")] public Int32 Packed { get; set; }

            [XmlAttribute("alphaChnl")] public Int32 AlphaChannel { get; set; }

            [XmlAttribute("redChnl")] public Int32 RedChannel { get; set; }

            [XmlAttribute("greenChnl")] public Int32 GreenChannel { get; set; }

            [XmlAttribute("blueChnl")] public Int32 BlueChannel { get; set; }
        }

        [Serializable]
        public class FontPage
        {
            [XmlAttribute("id")] public Int32 ID { get; set; }

            [XmlAttribute("file")] public string File { get; set; }
        }

        [Serializable]
        public class FontChar
        {
            [XmlAttribute("id")] public Int32 ID { get; set; }

            [XmlAttribute("x")] public Int32 X { get; set; }

            [XmlAttribute("y")] public Int32 Y { get; set; }

            [XmlAttribute("width")] public Int32 Width { get; set; }

            [XmlAttribute("height")] public Int32 Height { get; set; }

            [XmlAttribute("xoffset")] public Int32 XOffset { get; set; }

            [XmlAttribute("yoffset")] public Int32 YOffset { get; set; }

            [XmlAttribute("xadvance")] public Int32 XAdvance { get; set; }

            [XmlAttribute("page")] public Int32 Page { get; set; }

            [XmlAttribute("chnl")] public Int32 Channel { get; set; }
        }

        [Serializable]
        public class FontKerning
        {
            [XmlAttribute("first")] public Int32 First { get; set; }

            [XmlAttribute("second")] public Int32 Second { get; set; }

            [XmlAttribute("amount")] public Int32 Amount { get; set; }
        }
    }
}