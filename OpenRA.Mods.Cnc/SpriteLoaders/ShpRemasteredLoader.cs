#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.SpriteLoaders
{
	public class ShpRemasteredLoader : ISpriteLoader
	{
		public static bool IsShpRemastered(Stream s)
		{
			var start = s.Position;
			var isZipFile = s.ReadUInt32() == 0x04034B50;
			s.Position = start;

			return isZipFile;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsShpRemastered(s))
			{
				frames = null;
				return false;
			}

			frames = new ShpRemasteredSprite(s).Frames.ToArray();
			return true;
		}
	}

	public class ShpRemasteredSprite
	{
		static readonly Regex MetaRegex = new(
			@"^\{""size"":\s*\[\s*(?<width>\d+)\s*,\s*(?<height>\d+)\s*\]," +
			@"\s*""crop"":\s*\[\s*(?<left>\d+)\s*,\s*(?<top>\d+)\s*,\s*(?<right>\d+)\s*,\s*(?<bottom>\d+)\s*\]\s*\}$");

		static int ParseGroup(Match match, string group)
		{
			return Exts.ParseInt32Invariant(match.Groups[group].ValueSpan);
		}

		public IReadOnlyList<ISpriteFrame> Frames { get; }

		public ShpRemasteredSprite(Stream stream)
		{
			var container = new ZipFile(stream);

			var tgaEntries = container.OfType<ZipEntry>()
				.Where(entry => entry.Name.EndsWith(".tga", StringComparison.OrdinalIgnoreCase))
				.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			var frames = new ISpriteFrame[tgaEntries.Length];
			for (var i = 0; i < frames.Length; i++)
			{
				var tgaEntry = tgaEntries[i];
				var metaName = tgaEntry.Name[..^4] + ".meta";
				var metaEntry = container.GetEntry(metaName);
				using (var tgaStream = container.GetInputStream(tgaEntry))
				{
					var metaStream = metaEntry != null ? container.GetInputStream(metaEntry) : null;
					if (metaStream != null)
					{
						string metaText;
						using (metaStream)
						using (var metaReader = new StreamReader(metaStream, bufferSize: 64))
							metaText = metaReader.ReadToEnd();

						var meta = MetaRegex.Match(metaText);
						if (meta.Success)
						{
							var crop = Rectangle.FromLTRB(
								ParseGroup(meta, "left"), ParseGroup(meta, "top"),
								ParseGroup(meta, "right"), ParseGroup(meta, "bottom"));

							var frameSize = new Size(ParseGroup(meta, "width"), ParseGroup(meta, "height"));
							frames[i] = new TgaSprite.TgaFrame(tgaStream, frameSize, crop, 0.125f);
						}
						else
							frames[i] = new TgaSprite.TgaFrame(tgaStream);
					}
					else
						frames[i] = new TgaSprite.TgaFrame(tgaStream);
				}
			}

			Frames = frames;
		}
	}
}
