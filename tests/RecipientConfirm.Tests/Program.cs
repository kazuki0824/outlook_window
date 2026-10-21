using System;
using System.Collections.Generic;
using RecipientConfirm;

internal static class Program
{
    private static int failures;
    private static int total;
    private static RecipientInfo R(string kind = "To", string address = "alice@example.test",
        string name = "Alice", bool group = false) => new RecipientInfo(kind, name, address, group);

    private static int Main()
    {
        Test("explicit approval with unchanged To/Cc/Bcc permits send", () =>
        {
            var rows = new[] { R(), R("Cc"), R("Bcc") };
            string reason;
            Assert(SendGate.Approve(() => rows, shown =>
            {
                Assert(shown.Count == 3 && shown[2].Kind == "Bcc");
                return true;
            }, out reason));
            Assert(reason == null);
        });
        Test("cancel does not perform a second read", () =>
        {
            int reads = 0;
            string reason;
            Assert(!SendGate.Approve(() => { reads++; return new[] { R() }; }, rows => false, out reason));
            Assert(reads == 1 && reason == null);
        });
        Test("empty recipients never invoke confirmation", () =>
        {
            string reason;
            Assert(!SendGate.Approve(() => new RecipientInfo[0], rows => throw new Exception(), out reason));
            Assert(reason != null);
        });
        Test("null snapshot blocks", () =>
        {
            string reason;
            Assert(!SendGate.Approve(() => null, rows => true, out reason));
        });
        Test("recipient added during confirmation blocks", () => Changed(new[] { R(), R("Bcc") }));
        Test("recipient removed during confirmation blocks", () => Changed(new RecipientInfo[0]));
        Test("Bcc changed to To blocks", () => Changed(new[] { R("Bcc") }));
        Test("address changed blocks", () => Changed(new[] { R(address: "bob@example.test") }));
        Test("display name changed blocks", () => Changed(new[] { R(name: "Bob") }));
        Test("group classification changed blocks", () => Changed(new[] { R(group: true) }));
        Test("null re-read blocks", () => Changed(null));
        Test("order alone does not change recipient multiset", () =>
            Assert(RecipientInfo.SameRecipients(new[] { R(), R("Cc") }, new[] { R("Cc"), R() })));
        Test("duplicates are counted", () =>
            Assert(!RecipientInfo.SameRecipients(new[] { R(), R() }, new[] { R(), R("Bcc") })));
        Test("first read exception never approves", () => Throws<InvalidOperationException>(() =>
        {
            string reason;
            SendGate.Approve(() => throw new InvalidOperationException(), rows => true, out reason);
        }));
        Test("second read exception never approves", () => Throws<InvalidOperationException>(() =>
        {
            int reads = 0;
            string reason;
            SendGate.Approve(() => ++reads == 1 ? new[] { R() } : throw new InvalidOperationException(),
                rows => true, out reason);
        }));
        Test("dialog exception never approves", () => Throws<InvalidOperationException>(() =>
        {
            string reason;
            SendGate.Approve(() => new[] { R() }, rows => throw new InvalidOperationException(), out reason);
        }));
        Test("blank address rejected", () => Throws<ArgumentException>(() => R(address: " ")));
        Test("unknown recipient kind rejected", () => Throws<ArgumentException>(() => R("ReplyTo")));
        Console.WriteLine($"{total - failures}/{total} passed");
        return failures == 0 ? 0 : 1;
    }

    private static void Changed(IReadOnlyList<RecipientInfo> after)
    {
        int reads = 0;
        string reason;
        Assert(!SendGate.Approve(() => ++reads == 1 ? new[] { R() } : after, rows => true, out reason));
        Assert(reads == 2 && reason != null);
    }

    private static void Assert(bool condition)
    {
        if (!condition) throw new Exception("Assertion failed.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }

    private static void Test(string name, Action action)
    {
        total++;
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
}
