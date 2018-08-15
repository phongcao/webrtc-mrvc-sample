using Org.WebRtc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebRTCUWP.Utilities
{
    /// <summary>
    /// Utility class to organize SDP negotiation.
    /// </summary>
    class SdpUtils
    {
        /// <summary>
        /// Forces the SDP to use the selected audio and video codecs.
        /// </summary>
        /// <param name="sdp">Session description.</param>
        /// <param name="audioCodec">Audio codec.</param>
        /// <param name="videoCodec">Video codec.</param>
        /// <returns>True if succeeds to force to use the selected audio/video codecs.</returns>
        public static bool SelectCodecs(ref string sdp, CodecInfo audioCodec, CodecInfo videoCodec)
        {
            Regex mfdRegex = new Regex("\r\nm=audio.*RTP.*?( .\\d*)+\r\n");
            Match mfdMatch = mfdRegex.Match(sdp);
            List<string> mfdListToErase = new List<string>(); //mdf = media format descriptor
            bool audioMediaDescFound = mfdMatch.Groups.Count > 1; //Group 0 is whole match
#if ORTCLIB
            byte audioCodecId=audioCodec?.PreferredPayloadType ?? 0;
            byte videoCodecId=videoCodec?.PreferredPayloadType ?? 0;
#else
            int audioCodecId = audioCodec?.Id ?? 0;
            int videoCodecId = videoCodec?.Id ?? 0;
#endif
            if (audioMediaDescFound)
            {
                if (audioCodec == null)
                {
                    return false;
                }

                for (int groupCtr = 1/*Group 0 is whole match*/; groupCtr < mfdMatch.Groups.Count; groupCtr++)
                {
                    for (int captureCtr = 0; captureCtr < mfdMatch.Groups[groupCtr].Captures.Count; captureCtr++)
                    {
                        mfdListToErase.Add(mfdMatch.Groups[groupCtr].Captures[captureCtr].Value.TrimStart());
                    }
                }

                if (!mfdListToErase.Remove(audioCodecId.ToString()))
                {
                    return false;
                }
            }

            mfdRegex = new Regex("\r\nm=video.*RTP.*?( .\\d*)+\r\n");
            mfdMatch = mfdRegex.Match(sdp);
            bool videoMediaDescFound = mfdMatch.Groups.Count > 1; //Group 0 is whole match
            if (videoMediaDescFound)
            {
                if (videoCodec == null)
                {
                    return false;
                }

                for (int groupCtr = 1/*Group 0 is whole match*/; groupCtr < mfdMatch.Groups.Count; groupCtr++)
                {
                    for (int captureCtr = 0; captureCtr < mfdMatch.Groups[groupCtr].Captures.Count; captureCtr++)
                    {
                        mfdListToErase.Add(mfdMatch.Groups[groupCtr].Captures[captureCtr].Value.TrimStart());
                    }
                }

                if (!mfdListToErase.Remove(videoCodecId.ToString()))
                {
                    return false;
                }
            }

            if (audioMediaDescFound)
            {
                // Alter audio entry
                Regex audioRegex = new Regex("\r\n(m=audio.*RTP.*?)( .\\d*)+");
                sdp = audioRegex.Replace(sdp, "\r\n$1 " + audioCodecId);
            }

            if (videoMediaDescFound)
            {
                // Alter video entry
                Regex videoRegex = new Regex("\r\n(m=video.*RTP.*?)( .\\d*)+");
                sdp = videoRegex.Replace(sdp, "\r\n$1 " + videoCodecId);
            }

            // Remove associated rtp mapping, format parameters, feedback parameters
            Regex removeOtherMdfs = new Regex("a=(rtpmap|fmtp|rtcp-fb):(" + String.Join("|", mfdListToErase) + ") .*\r\n");
            sdp = removeOtherMdfs.Replace(sdp, "");

            return true;
        }

        public static bool SetMediaBitrate(ref string sdp, string media, int bitrate)
        {
            var lines = sdp.Split('\n');
            var line = -1;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("m=" + media))
                {
                    line = i;
                    break;
                }
            }

            if (line == -1)
            {
                Debug.WriteLine("Could not find the m line for", media);
                return false;
            }

            // Pass the m line
            line++;

            // Skip i and c lines
            while (lines[line].StartsWith("i=") || lines[line].StartsWith("c="))
            {
                line++;
            }

            // If we're on a b line, replace it
            if (lines[line].StartsWith("b"))
            {
                //Debug.WriteLine("Replaced b line at line: " + line);
                lines[line] = "b=AS:" + bitrate;
                sdp = string.Join("\n", lines);

                return true;
            }

            // Add a new b line
            var newLines = new List<string>(lines.Take(line));
            newLines.Add("b=AS:" + bitrate + "\r");
            var newLinesArr = newLines.Concat(lines.Skip(line).Take(lines.Length));

            sdp = string.Join("\n", newLinesArr);

            return true;
        }
    }
}
