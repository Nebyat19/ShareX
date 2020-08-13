﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2020 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShareX.ImageEffectsLib
{
    public static class ImageEffectPackager
    {
        private const string ConfigFileName = "Config.json";

        public static string Package(string outputFilePath, string configJson, string assetsFolderPath)
        {
            if (!string.IsNullOrEmpty(outputFilePath))
            {
                string outputFolder = Path.GetDirectoryName(outputFilePath);
                Helpers.CreateDirectory(outputFolder);

                string configFilePath = Path.Combine(outputFolder, ConfigFileName);
                File.WriteAllText(configFilePath, configJson, Encoding.UTF8);

                List<ZipEntryInfo> entries = new List<ZipEntryInfo>();
                entries.Add(new ZipEntryInfo(configFilePath, ConfigFileName));

                if (!string.IsNullOrEmpty(assetsFolderPath) && Directory.Exists(assetsFolderPath))
                {
                    string parentFolderPath = Directory.GetParent(assetsFolderPath).FullName;
                    int entryNamePosition = parentFolderPath.Length + 1;

                    foreach (string assetPath in Directory.EnumerateFiles(assetsFolderPath, "*.*", SearchOption.AllDirectories).Where(x => Helpers.IsImageFile(x)))
                    {
                        string entryName = assetPath.Substring(entryNamePosition);
                        entries.Add(new ZipEntryInfo(assetPath, entryName));
                    }
                }

                try
                {
                    ZipManager.Compress(outputFilePath, entries);
                }
                finally
                {
                    File.Delete(configFilePath);
                }

                return outputFilePath;
            }

            return null;
        }

        public static string ExtractPackage(string packageFilePath, string destination)
        {
            string configJson = null;

            if (!string.IsNullOrEmpty(packageFilePath) && File.Exists(packageFilePath) && !string.IsNullOrEmpty(destination))
            {
                ZipManager.Extract(packageFilePath, destination, true, entry =>
                {
                    if (Helpers.IsImageFile(entry.Name))
                    {
                        return true;
                    }

                    if (configJson == null && entry.FullName.Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream stream = entry.Open())
                        using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
                        {
                            configJson = streamReader.ReadToEnd();
                        }
                    }

                    return false;
                }, 20_000_000);
            }

            return configJson;
        }
    }
}