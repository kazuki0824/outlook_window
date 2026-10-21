using System;
using System.Collections.Generic;

namespace RecipientConfirm
{
    internal static class SendGate
    {
        public static bool Approve(Func<IReadOnlyList<RecipientInfo>> read,
            Func<IReadOnlyList<RecipientInfo>, bool> confirm, out string reason)
        {
            reason = null;
            var before = read();
            if (before == null || before.Count == 0)
            {
                reason = "宛先がないため送信を中止しました。";
                return false;
            }
            if (!confirm(before))
                return false;

            // ShowDialog pumps messages. Other automation could edit recipients meanwhile.
            var after = read();
            if (after == null || !RecipientInfo.SameRecipients(before, after))
            {
                reason = "確認中に宛先が変わったため送信を中止しました。もう一度送信してください。";
                return false;
            }
            return true;
        }
    }
}
