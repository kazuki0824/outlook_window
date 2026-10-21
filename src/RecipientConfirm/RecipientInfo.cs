using System;
using System.Collections.Generic;
using System.Linq;

namespace RecipientConfirm
{
    internal sealed class RecipientInfo
    {
        public string Kind { get; }
        public string Name { get; }
        public string Address { get; }
        public bool IsGroup { get; }

        public RecipientInfo(string kind, string name, string address, bool isGroup)
        {
            if (kind != "To" && kind != "Cc" && kind != "Bcc")
                throw new ArgumentException("Unknown recipient kind.", nameof(kind));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Recipient has no SMTP address.", nameof(address));
            Kind = kind;
            Name = name ?? string.Empty;
            Address = address;
            IsGroup = isGroup;
        }

        // Preserve duplicates and recipient kinds. A Bcc -> To change requires review.
        // Compare exactly, including display name; even an ambiguous change cancels send.
        public static bool SameRecipients(IReadOnlyList<RecipientInfo> first,
            IReadOnlyList<RecipientInfo> second)
        {
            if (first.Count != second.Count)
                return false;
            var remaining = second.ToList();
            foreach (var recipient in first)
            {
                int index = remaining.FindIndex(other =>
                    recipient.Kind == other.Kind && recipient.Name == other.Name &&
                    recipient.Address == other.Address && recipient.IsGroup == other.IsGroup);
                if (index < 0)
                    return false;
                remaining.RemoveAt(index);
            }
            return true;
        }
    }
}
