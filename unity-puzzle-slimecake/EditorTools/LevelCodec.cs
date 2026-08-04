#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using SlimeCake.Core;
using UnityEngine;

namespace SlimeCake.LevelEditor
{
    /// <summary>
    /// Encodes and decodes level data to zipped base64 strings.
    /// Format: "SC1:" + base64(gzip(LevelData.ToJson()))
    /// </summary>
    public static class LevelCodec
    {
        private const string Prefix = "SC1:";

        // Limit output size to prevent zip bombs.
        private const int MaxDecodedBytes = 4 * 1024 * 1024;

        public static string? Encode(LevelData data)
        {
            if (data == null) return null;
            try
            {
                var json = data.ToJson();
                var bytes = Encoding.UTF8.GetBytes(json);
                using var compressed = new MemoryStream();
                using (var gz = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                {
                    gz.Write(bytes, 0, bytes.Length);
                }
                return Prefix + Convert.ToBase64String(compressed.ToArray());
            }
            catch (Exception e)
            {
                Debug.LogError($"LevelCodec.Encode failed: {e}");
                return null;
            }
        }

        public static LevelData? Decode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            try
            {
                var trimmed = code.Trim();
                if (!trimmed.StartsWith(Prefix)) return null;
                var b64 = trimmed.Substring(Prefix.Length).Trim();
                var compressed = Convert.FromBase64String(b64);
                using var input = new MemoryStream(compressed);
                using var gz = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                var buffer = new byte[8192];
                int read;
                int total = 0;
                while ((read = gz.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += read;
                    if (total > MaxDecodedBytes)
                    {
                        Debug.LogWarning("LevelCodec.Decode failed: decompressed payload exceeds max size.");
                        return null;
                    }
                    output.Write(buffer, 0, read);
                }
                var json = Encoding.UTF8.GetString(output.ToArray());
                return LevelData.FromJson(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LevelCodec.Decode failed: {e.Message}");
                return null;
            }
        }
    }
}
