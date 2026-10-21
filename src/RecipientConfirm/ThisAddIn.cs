using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace RecipientConfirm
{
    // VSTO host entry point, written explicitly so no IDE code generation is required.
    [Microsoft.VisualStudio.Tools.Applications.Runtime.StartupObject(0)]
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public sealed class ThisAddIn : Microsoft.Office.Tools.Outlook.OutlookAddInBase
    {
        private Outlook.Application outlook;
        private bool checking;

        public ThisAddIn(Microsoft.Office.Tools.Outlook.Factory factory, IServiceProvider services)
            : base(factory, services, "AddIn", "ThisAddIn") { }

        protected override void Initialize()
        {
            base.Initialize();
            outlook = GetHostItem<Outlook.Application>(typeof(Outlook.Application), "Application");
            System.Windows.Forms.Application.EnableVisualStyles();
        }

        protected override void InitializeDataBindings()
        {
            BeginInit();
            EndInit();
        }

        protected override void FinishInitialization()
        {
            outlook.ItemSend += OnItemSend;
            OnStartup();
        }

        protected override void OnShutdown()
        {
            if (outlook != null)
                outlook.ItemSend -= OnItemSend;
            base.OnShutdown();
        }

        private void OnItemSend(object item, ref bool cancel)
        {
            // Respect cancellation already requested by another add-in.
            if (cancel || !(item is Outlook.MailItem mail))
                return;
            cancel = true;
            if (checking)
                return;

            checking = true;
            try
            {
                var owner = new OutlookWindow(GetActiveWindow());
                string reason;
                bool approved = SendGate.Approve(() => RecipientReader.Read(mail), recipients =>
                {
                    using (var dialog = new RecipientConfirmationForm(recipients))
                        return dialog.ShowDialog(owner) == DialogResult.OK;
                }, out reason);
                if (approved)
                    cancel = false;
                else if (reason != null)
                    MessageBox.Show(owner, reason, "送信を中止しました",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                // Do not let an exception cross Outlook's COM event boundary or allow send.
                cancel = true;
                MessageBox.Show("宛先を確認できなかったため、送信を中止しました。\r\n" +
                    "宛先の名前解決を確認し、個人用の連絡先グループは展開してください。\r\n" +
                    "Exchangeへの接続が必要な場合もあります。",
                    "送信先の確認", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { checking = false; }
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetActiveWindow();

        private sealed class OutlookWindow : IWin32Window
        {
            public IntPtr Handle { get; }
            public OutlookWindow(IntPtr handle) { Handle = handle; }
        }
    }
}
