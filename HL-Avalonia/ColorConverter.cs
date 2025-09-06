#if Avalonia
using System;
using System.Globalization;
using Avalonia.Media;

namespace HL
{
	internal static class ColorConverter
	{
		enum ColorTextForm : byte
		{
			Unknown = 0,
			Numeric,
			ScRgb,
			KnownCssX11,
			Context
		}

		private const string ParsersIllegalToken = "Token is not valid.";

		public static Color ConvertFromInvariantString(string color)
		{
			ColorTextForm colorKind;
			string        text = MatchColor(color, out colorKind);

			if (colorKind == ColorTextForm.Numeric) {
				return ParseHexColor(text);
			} else if (colorKind == ColorTextForm.Context) {
				throw new NotSupportedException(
					"So-called context color are not supported by this external implementation.");
			} else if (colorKind == ColorTextForm.ScRgb) {
				throw new NotSupportedException("scRGB colors are not supported by Avalonia.");
			} else {
				var brush = ColorStringToKnownBrush(text);
				return brush?.Color ?? throw new FormatException(ParsersIllegalToken);
			}
		}

		private static Color ParseHexColor(string trimmedColor)
		{
			int a = 255;
			int r, g, b;
			var c   = trimmedColor.Length;
			var hc1 = ParseHexChar(trimmedColor[1]);
			var hc2 = ParseHexChar(trimmedColor[2]);
			var hc3 = ParseHexChar(trimmedColor[3]);
			if (c > 7) {
				a = hc1 * 16 + hc2;
				r = hc3 * 16 + ParseHexChar(trimmedColor[4]);
				g = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
				b = ParseHexChar(trimmedColor[7]) * 16 + ParseHexChar(trimmedColor[8]);
			} else if (c > 5) {
				r = hc1 * 16 + hc2;
				g = hc3 * 16 + ParseHexChar(trimmedColor[4]);
				b = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
			} else if (c > 4) {
				a = 17 * hc1;
				r = 17 * hc2;
				g = 17 * hc3;
				b = 17 * ParseHexChar(trimmedColor[4]);
			} else {
				r = 17 * hc1;
				g = 17 * hc2;
				b = 17 * hc3;
			}

			return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
		}

		private static int ParseHexChar(char c)
		{
			if (c >= '0' && c <= '9') {
				return c - 48;
			} else if (c >= 'a' && c <= 'f') {
				return c + (10 - 97);
			} else if (c >= 'A' && c <= 'F') {
				return c + (10 - 65);
			}
			throw new FormatException(ParsersIllegalToken);
		}

		private static string MatchColor(string colorString, out ColorTextForm kind)
		{
			var text = colorString.Trim();
			var l    = text.Length;
			if ((l == 4 || l == 5 || l == 7 || l == 9) && text[0] == '#') {
				kind = ColorTextForm.Numeric;
			} else if (text.StartsWith("sc#", StringComparison.Ordinal)) {
				kind = ColorTextForm.ScRgb;
			} else if (text.StartsWith("ContextColor ", StringComparison.OrdinalIgnoreCase)) {
				kind = ColorTextForm.Context;
			} else {
				kind = ColorTextForm.KnownCssX11;
			}
			return text;
		}

		private static IImmutableSolidColorBrush ColorStringToKnownBrush(string colorString)
		{
			if (colorString != null) {
				var text  = colorString.ToUpper(CultureInfo.InvariantCulture);
				var len   = text.Length;
				var first = text[0];
				if (len == 3) {
					if (text.Equals("RED")) {
						return Brushes.Red;
					} else if (text.Equals("TAN")) {
						return Brushes.Tan;
					}
				} else if (len == 4) {
					if ((first == 'A') && text.Equals("AQUA")) {
						return Brushes.Aqua;
					} else if ((first == 'B') && text.Equals("BLUE")) {
						return Brushes.Blue;
					} else if ((first == 'C') && text.Equals("CYAN")) {
						return Brushes.Aqua;
					} else if (first == 'G') {
						if (text.Equals("GOLD")) {
							return Brushes.Gold;
						} else if (text.Equals("GRAY")) {
							return Brushes.Gray;
						}
					} else if ((first == 'L') && text.Equals("LIME")) {
						return Brushes.Lime;
					} else if ((first == 'N') && text.Equals("NAVY")) {
						return Brushes.Navy;
					} else if (first == 'P') {
						if (text.Equals("PERU")) {
							return Brushes.Peru;
						} else if (text.Equals("PINK")) {
							return Brushes.Pink;
						} else if (text.Equals("PLUM")) {
							return Brushes.Plum;
						}
					} else if ((first == 'S') && text.Equals("SNOW")) {
						return Brushes.Snow;
					} else if ((first == 'T') && text.Equals("TEAL")) {
						return Brushes.Teal;
					}
				} else if (len == 5) {
					if ((first == 'A') && text.Equals("AZURE")) {
						return Brushes.Azure;
					} else if (first == 'B') {
						if (text.Equals("BEIGE")) {
							return Brushes.Beige;
						} else if (text.Equals("BLACK")) {
							return Brushes.Black;
						} else if (text.Equals("BROWN")) {
							return Brushes.Brown;
						}
					} else if ((first == 'C') && text.Equals("CORAL")) {
						return Brushes.Coral;
					} else if ((first == 'G') && text.Equals("GREEN")) {
						return Brushes.Green;
					} else if ((first == 'I') && text.Equals("IVORY")) {
						return Brushes.Ivory;
					} else if ((first == 'K') && text.Equals("KHAKI")) {
						return Brushes.Khaki;
					} else if ((first == 'L') && text.Equals("LINEN")) {
						return Brushes.Linen;
					} else if ((first == 'O') && text.Equals("OLIVE")) {
						return Brushes.Olive;
					} else if (first == 'W') {
						if (text.Equals("WHEAT")) {
							return Brushes.Wheat;
						} else if (text.Equals("WHITE")) {
							return Brushes.White;
						}
					}
				} else if (len == 6) {
					if ((first == 'B') && text.Equals("BISQUE")) {
						return Brushes.Bisque;
					} else if ((first == 'I') && text.Equals("INDIGO")) {
						return Brushes.Indigo;
					} else if ((first == 'M') && text.Equals("MAROON")) {
						return Brushes.Maroon;
					} else if (first == 'O') {
						if (text.Equals("ORANGE")) {
							return Brushes.Orange;
						} else if (text.Equals("ORCHID")) {
							return Brushes.Orchid;
						}
					} else if ((first == 'P') && text.Equals("PURPLE")) {
						return Brushes.Purple;
					} else if (first == 'S') {
						if (text.Equals("SALMON")) {
							return Brushes.Salmon;
						} else if (text.Equals("SIENNA")) {
							return Brushes.Sienna;
						} else if (text.Equals("SILVER")) {
							return Brushes.Silver;
						}
					} else if ((first == 'T') && text.Equals("TOMATO")) {
						return Brushes.Tomato;
					} else if ((first == 'V') && text.Equals("VIOLET")) {
						return Brushes.Violet;
					} else if ((first == 'Y') && text.Equals("YELLOW")) {
						return Brushes.Yellow;
					}
				} else if (len == 7) {
					if ((first == 'C') && text.Equals("CRIMSON")) {
						return Brushes.Crimson;
					} else if (first == 'D') {
						if (text.Equals("DARKRED")) {
							return Brushes.DarkRed;
						} else if (text.Equals("DIMGRAY")) {
							return Brushes.DimGray;
						}
					} else if ((first == 'F') && text.Equals("FUCHSIA")) {
						return Brushes.Fuchsia;
					} else if ((first == 'H') && text.Equals("HOTPINK")) {
						return Brushes.HotPink;
					} else if ((first == 'M') && text.Equals("MAGENTA")) {
						return Brushes.Fuchsia;
					} else if ((first == 'O') && text.Equals("OLDLACE")) {
						return Brushes.OldLace;
					} else if ((first == 'S') && text.Equals("SKYBLUE")) {
						return Brushes.SkyBlue;
					} else if ((first == 'T') && text.Equals("THISTLE")) {
						return Brushes.Thistle;
					}
				} else if (len == 8) {
					if ((first == 'C') && text.Equals("CORNSILK")) {
						return Brushes.Cornsilk;
					} else if (first == 'D') {
						if (text.Equals("DARKBLUE")) {
							return Brushes.DarkBlue;
						} else if (text.Equals("DARKCYAN")) {
							return Brushes.DarkCyan;
						} else if (text.Equals("DARKGRAY")) {
							return Brushes.DarkGray;
						} else if (text.Equals("DEEPPINK")) {
							return Brushes.DeepPink;
						}
					} else if ((first == 'H') && text.Equals("HONEYDEW")) {
						return Brushes.Honeydew;
					} else if ((first == 'L') && text.Equals("LAVENDER")) {
						return Brushes.Lavender;
					} else if ((first == 'M') && text.Equals("MOCCASIN")) {
						return Brushes.Moccasin;
					} else if (first == 'S') {
						if (text.Equals("SEAGREEN")) {
							return Brushes.SeaGreen;
						} else if (text.Equals("SEASHELL")) {
							return Brushes.SeaShell;
						}
					}
				} else if (len == 9) {
					if ((first == 'A') && text.Equals("ALICEBLUE")) {
						return Brushes.AliceBlue;
					} else if ((first == 'B') && text.Equals("BURLYWOOD")) {
						return Brushes.BurlyWood;
					} else if (first == 'C') {
						if (text.Equals("CADETBLUE")) {
							return Brushes.CadetBlue;
						} else if (text.Equals("CHOCOLATE")) {
							return Brushes.Chocolate;
						}
					} else if (first == 'D') {
						if (text.Equals("DARKGREEN")) {
							return Brushes.DarkGreen;
						} else if (text.Equals("DARKKHAKI")) {
							return Brushes.DarkKhaki;
						}
					} else if ((first == 'F') && text.Equals("FIREBRICK")) {
						return Brushes.Firebrick;
					} else if (first == 'G') {
						if (text.Equals("GAINSBORO")) {
							return Brushes.Gainsboro;
						} else if (text.Equals("GOLDENROD")) {
							return Brushes.Goldenrod;
						}
					} else if ((first == 'I') && text.Equals("INDIANRED")) {
						return Brushes.IndianRed;
					} else if (first == 'L') {
						if (text.Equals("LAWNGREEN")) {
							return Brushes.LawnGreen;
						} else if (text.Equals("LIGHTBLUE")) {
							return Brushes.LightBlue;
						} else if (text.Equals("LIGHTCYAN")) {
							return Brushes.LightCyan;
						} else if (text.Equals("LIGHTGRAY")) {
							return Brushes.LightGray;
						} else if (text.Equals("LIGHTPINK")) {
							return Brushes.LightPink;
						} else if (text.Equals("LIMEGREEN")) {
							return Brushes.LimeGreen;
						}
					} else if (first == 'M') {
						if (text.Equals("MINTCREAM")) {
							return Brushes.MintCream;
						} else if (text.Equals("MISTYROSE")) {
							return Brushes.MistyRose;
						}
					} else if (first == 'O') {
						if (text.Equals("OLIVEDRAB")) {
							return Brushes.OliveDrab;
						} else if (text.Equals("ORANGERED")) {
							return Brushes.OrangeRed;
						}
					} else if (first == 'P') {
						if (text.Equals("PALEGREEN")) {
							return Brushes.PaleGreen;
						} else if (text.Equals("PEACHPUFF")) {
							return Brushes.PeachPuff;
						}
					} else if (first == 'R') {
						if (text.Equals("ROSYBROWN")) {
							return Brushes.RosyBrown;
						} else if (text.Equals("ROYALBLUE")) {
							return Brushes.RoyalBlue;
						}
					} else if (first == 'S') {
						if (text.Equals("SLATEBLUE")) {
							return Brushes.SlateBlue;
						} else if (text.Equals("SLATEGRAY")) {
							return Brushes.SlateGray;
						} else if (text.Equals("STEELBLUE")) {
							return Brushes.SteelBlue;
						}
					} else if ((first == 'T') && text.Equals("TURQUOISE")) {
						return Brushes.Turquoise;
					}
				} else if (len == 10) {
					if ((first == 'A') && text.Equals("AQUAMARINE")) {
						return Brushes.Aquamarine;
					} else if ((first == 'B') && text.Equals("BLUEVIOLET")) {
						return Brushes.BlueViolet;
					} else if ((first == 'C') && text.Equals("CHARTREUSE")) {
						return Brushes.Chartreuse;
					} else if (first == 'D') {
						if (text.Equals("DARKORANGE")) {
							return Brushes.DarkOrange;
						} else if (text.Equals("DARKORCHID")) {
							return Brushes.DarkOrchid;
						} else if (text.Equals("DARKSALMON")) {
							return Brushes.DarkSalmon;
						} else if (text.Equals("DARKVIOLET")) {
							return Brushes.DarkViolet;
						} else if (text.Equals("DODGERBLUE")) {
							return Brushes.DodgerBlue;
						}
					} else if ((first == 'G') && text.Equals("GHOSTWHITE")) {
						return Brushes.GhostWhite;
					} else if (first == 'L') {
						if (text.Equals("LIGHTCORAL")) {
							return Brushes.LightCoral;
						} else if (text.Equals("LIGHTGREEN")) {
							return Brushes.LightGreen;
						}
					} else if ((first == 'M') && text.Equals("MEDIUMBLUE")) {
						return Brushes.MediumBlue;
					} else if (first == 'P') {
						if (text.Equals("PAPAYAWHIP")) {
							return Brushes.PapayaWhip;
						} else if (text.Equals("POWDERBLUE")) {
							return Brushes.PowderBlue;
						}
					} else if ((first == 'S') && text.Equals("SANDYBROWN")) {
						return Brushes.SandyBrown;
					} else if ((first == 'W') && text.Equals("WHITESMOKE")) {
						return Brushes.WhiteSmoke;
					}
				} else if (len == 11) {
					if (first == 'D') {
						if (text.Equals("DARKMAGENTA")) {
							return Brushes.DarkMagenta;
						} else if (text.Equals("DEEPSKYBLUE")) {
							return Brushes.DeepSkyBlue;
						}
					} else if (first == 'F') {
						if (text.Equals("FLORALWHITE")) {
							return Brushes.FloralWhite;
						} else if (text.Equals("FORESTGREEN")) {
							return Brushes.ForestGreen;
						}
					} else if ((first == 'G') && text.Equals("GREENYELLOW")) {
						return Brushes.GreenYellow;
					} else if (first == 'L') {
						if (text.Equals("LIGHTSALMON")) {
							return Brushes.LightSalmon;
						} else if (text.Equals("LIGHTYELLOW")) {
							return Brushes.LightYellow;
						}
					} else if ((first == 'N') && text.Equals("NAVAJOWHITE")) {
						return Brushes.NavajoWhite;
					} else if (first == 'S') {
						if (text.Equals("SADDLEBROWN")) {
							return Brushes.SaddleBrown;
						} else if (text.Equals("SPRINGGREEN")) {
							return Brushes.SpringGreen;
						}
					} else if ((first == 'T') && text.Equals("TRANSPARENT")) {
						return Brushes.Transparent;
					} else if ((first == 'Y') && text.Equals("YELLOWGREEN")) {
						return Brushes.YellowGreen;
					}
				} else if (len == 12) {
					if ((first == 'A') && text.Equals("ANTIQUEWHITE")) {
						return Brushes.AntiqueWhite;
					} else if ((first == 'D') && text.Equals("DARKSEAGREEN")) {
						return Brushes.DarkSeaGreen;
					} else if (first == 'L') {
						if (text.Equals("LIGHTSKYBLUE")) {
							return Brushes.LightSkyBlue;
						} else if (text.Equals("LEMONCHIFFON")) {
							return Brushes.LemonChiffon;
						}
					} else if (first == 'M') {
						if (text.Equals("MEDIUMORCHID")) {
							return Brushes.MediumOrchid;
						} else if (text.Equals("MEDIUMPURPLE")) {
							return Brushes.MediumPurple;
						} else if (text.Equals("MIDNIGHTBLUE")) {
							return Brushes.MidnightBlue;
						}
					}
				} else if (len == 13) {
					if (first == 'D') {
						if (text.Equals("DARKSLATEBLUE")) {
							return Brushes.DarkSlateBlue;
						} else if (text.Equals("DARKSLATEGRAY")) {
							return Brushes.DarkSlateGray;
						} else if (text.Equals("DARKGOLDENROD")) {
							return Brushes.DarkGoldenrod;
						} else if (text.Equals("DARKTURQUOISE")) {
							return Brushes.DarkTurquoise;
						}
					} else if (first == 'L') {
						if (text.Equals("LIGHTSEAGREEN")) {
							return Brushes.LightSeaGreen;
						} else if (text.Equals("LAVENDERBLUSH")) {
							return Brushes.LavenderBlush;
						}
					} else if (first == 'P') {
						if (text.Equals("PALEGOLDENROD")) {
							return Brushes.PaleGoldenrod;
						} else if (text.Equals("PALETURQUOISE")) {
							return Brushes.PaleTurquoise;
						} else if (text.Equals("PALEVIOLETRED")) {
							return Brushes.PaleVioletRed;
						}
					}
				} else if (len == 14) {
					if ((first == 'B') && text.Equals("BLANCHEDALMOND")) {
						return Brushes.BlanchedAlmond;
					} else if ((first == 'C') && text.Equals("CORNFLOWERBLUE")) {
						return Brushes.CornflowerBlue;
					} else if ((first == 'D') && text.Equals("DARKOLIVEGREEN")) {
						return Brushes.DarkOliveGreen;
					} else if (first == 'L') {
						if (text.Equals("LIGHTSLATEGRAY")) {
							return Brushes.LightSlateGray;
						} else if (text.Equals("LIGHTSTEELBLUE")) {
							return Brushes.LightSteelBlue;
						}
					} else if ((first == 'M') && text.Equals("MEDIUMSEAGREEN")) {
						return Brushes.MediumSeaGreen;
					}
				} else if (len == 15) {
					if (text.Equals("MEDIUMSLATEBLUE")) {
						return Brushes.MediumSlateBlue;
					} else if (text.Equals("MEDIUMTURQUOISE")) {
						return Brushes.MediumTurquoise;
					} else if (text.Equals("MEDIUMVIOLETRED")) {
						return Brushes.MediumVioletRed;
					}
				} else if ((len == 16) && text.Equals("MEDIUMAQUAMARINE")) {
					return Brushes.MediumAquamarine;
				} else if ((len == 17) && text.Equals("MEDIUMSPRINGGREEN")) {
					return Brushes.MediumSpringGreen;
				} else if ((len == 20) && text.Equals("LIGHTGOLDENRODYELLOW")) {
					return Brushes.LightGoldenrodYellow;
				}
			}
			return null;
		}
	}
}
#endif
