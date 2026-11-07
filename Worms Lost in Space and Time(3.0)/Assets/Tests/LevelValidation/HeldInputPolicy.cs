using System;
using System.Collections.Generic;
using System.Linq;

namespace Worms.LevelValidation
{
    [Serializable]
    public sealed class ButtonHold
    {
        public int FirstJump;
        public int LastJump;
        public int StartFrame;
        public int EndFrame;
        public bool Preheld;
    }

    [Serializable]
    public sealed class HeldInputAnalysis
    {
        public bool Complete;
        public bool Verified;
        public string Reason = "Not evaluated.";
        public List<ButtonHold> Groups = new List<ButtonHold>();
        public List<ProfileResult> Profiles = new List<ProfileResult>();
    }

    /// <summary>Proposes holds only across consecutive, earliest-legal attachments; real replay must verify them.</summary>
    public static class HeldInputPolicy
    {
        public static List<ButtonHold> Build(InputReplay replay)
        {
            var groups = new List<ButtonHold>();
            for (int index = 0; index < replay.Jumps.Count; index++)
            {
                var jump = replay.Jumps[index];
                var attachment = replay.Expected.FirstOrDefault(item => item.Kind == "Attached"
                    && item.AttachmentOrdinal == jump.AttachmentOrdinal && item.SurfaceId == jump.SurfaceId);
                bool earliest = attachment != null && jump.Frame == attachment.Frame + 1;
                var previous = groups.LastOrDefault();
                if (earliest && previous != null
                    && jump.AttachmentOrdinal == replay.Jumps[index - 1].AttachmentOrdinal + 1)
                {
                    previous.LastJump = index;
                    previous.EndFrame = jump.Frame;
                }
                else
                {
                    var departure = attachment == null ? null : replay.Expected.LastOrDefault(item => item.Kind == "ForcedDetach"
                        && item.AttachmentOrdinal == jump.AttachmentOrdinal - 1 && item.Frame < attachment.Frame);
                    int preparedStart = jump.AttachmentOrdinal == 1 ? 0 : departure == null ? jump.Frame : departure.Frame + 1;
                    bool preheld = earliest && preparedStart < jump.Frame;
                    groups.Add(new ButtonHold { FirstJump = index, LastJump = index,
                        StartFrame = preheld ? preparedStart : jump.Frame, EndFrame = jump.Frame, Preheld = preheld });
                }
            }
            return groups;
        }
    }
}
