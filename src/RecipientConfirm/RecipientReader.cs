using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace RecipientConfirm
{
    internal static class RecipientReader
    {
        private const string SmtpUnicode = "http://schemas.microsoft.com/mapi/proptag/0x39FE001F";
        private const string SmtpAnsi = "http://schemas.microsoft.com/mapi/proptag/0x39FE001E";
        private const int MapiNotFound = unchecked((int)0x8004010F);

        public static IReadOnlyList<RecipientInfo> Read(Outlook.MailItem mail)
        {
            var result = new List<RecipientInfo>();
            Outlook.Recipients recipients = null;
            try
            {
                recipients = mail.Recipients;
                int count = recipients.Count;
                for (int i = 1; i <= count; i++)
                {
                    Outlook.Recipient recipient = null;
                    Outlook.AddressEntry entry = null;
                    try
                    {
                        recipient = recipients[i];
                        if (!recipient.Resolved)
                            throw new InvalidOperationException("Unresolved recipient.");
                        entry = recipient.AddressEntry;
                        if (entry == null)
                            throw new InvalidOperationException("Missing address entry.");
                        var userType = entry.AddressEntryUserType;
                        bool isGroup = userType == Outlook.OlAddressEntryUserType.olExchangeDistributionListAddressEntry ||
                            userType == Outlook.OlAddressEntryUserType.olOutlookDistributionListAddressEntry;
                        string kind;
                        switch ((Outlook.OlMailRecipientType)recipient.Type)
                        {
                            case Outlook.OlMailRecipientType.olTo: kind = "To"; break;
                            case Outlook.OlMailRecipientType.olCC: kind = "Cc"; break;
                            case Outlook.OlMailRecipientType.olBCC: kind = "Bcc"; break;
                            default: throw new InvalidOperationException("Unsupported recipient type.");
                        }
                        result.Add(new RecipientInfo(kind, recipient.Name,
                            ReadSmtpAddress(recipient, entry), isGroup));
                    }
                    finally
                    {
                        Release(entry);
                        Release(recipient);
                    }
                }
                if (recipients.Count != count)
                    throw new InvalidOperationException("Recipients changed during enumeration.");
                return result.AsReadOnly();
            }
            finally { Release(recipients); }
        }

        private static string ReadSmtpAddress(Outlook.Recipient recipient, Outlook.AddressEntry entry)
        {
            // Prefer the recipient row over the directory primary address: aliases may differ.
            Outlook.PropertyAccessor accessor = null;
            try
            {
                accessor = recipient.PropertyAccessor;
                var address = ReadOptionalProperty(accessor, SmtpUnicode) ??
                    ReadOptionalProperty(accessor, SmtpAnsi);
                if (!string.IsNullOrWhiteSpace(address))
                    return address;
            }
            finally { Release(accessor); accessor = null; }

            if (string.Equals(entry.Type, "SMTP", StringComparison.OrdinalIgnoreCase))
                return entry.Address;

            var kind = entry.AddressEntryUserType;
            if (kind == Outlook.OlAddressEntryUserType.olExchangeUserAddressEntry ||
                kind == Outlook.OlAddressEntryUserType.olExchangeRemoteUserAddressEntry)
            {
                Outlook.ExchangeUser user = null;
                try
                {
                    user = entry.GetExchangeUser();
                    if (user != null && !string.IsNullOrWhiteSpace(user.PrimarySmtpAddress))
                        return user.PrimarySmtpAddress;
                }
                finally { Release(user); }
            }
            if (kind == Outlook.OlAddressEntryUserType.olExchangeDistributionListAddressEntry)
            {
                Outlook.ExchangeDistributionList group = null;
                try
                {
                    group = entry.GetExchangeDistributionList();
                    if (group != null && !string.IsNullOrWhiteSpace(group.PrimarySmtpAddress))
                        return group.PrimarySmtpAddress;
                }
                finally { Release(group); }
            }
            try
            {
                accessor = entry.PropertyAccessor;
                var address = ReadOptionalProperty(accessor, SmtpUnicode) ??
                    ReadOptionalProperty(accessor, SmtpAnsi);
                if (!string.IsNullOrWhiteSpace(address))
                    return address;
            }
            finally { Release(accessor); }
            // Personal contact groups usually have no SMTP address: expand them in Outlook.
            throw new InvalidOperationException("Cannot resolve SMTP address. Expand contact groups first.");
        }

        private static string ReadOptionalProperty(Outlook.PropertyAccessor accessor, string property)
        {
            try
            {
                var value = accessor.GetProperty(property) as string;
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            catch (COMException error) when (error.ErrorCode == MapiNotFound)
            {
                return null;
            }
        }

        private static void Release(object value)
        {
            // Only release temporary references obtained here, never the event's MailItem
            // or the host Application. Never use FinalReleaseComObject on shared RCWs.
            if (value != null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }
    }
}
