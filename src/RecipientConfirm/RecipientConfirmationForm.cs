using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RecipientConfirm
{
    internal sealed class RecipientConfirmationForm : Form
    {
        public RecipientConfirmationForm(IReadOnlyList<RecipientInfo> recipients)
        {
            Text = "送信先の確認";
            Font = new Font("Yu Gothic UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(650, 450);
            Size = new Size(920, 620);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 12),
                Text = $"次の宛先に送信します。全 {recipients.Count} 件 " +
                    $"（To: {recipients.Count(r => r.Kind == "To")} / " +
                    $"Cc: {recipients.Count(r => r.Kind == "Cc")} / " +
                    $"Bcc: {recipients.Count(r => r.Kind == "Bcc")}）"
            }, 0, 0);

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = SystemColors.Window,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                AccessibleName = "全送信先一覧"
            };
            AddColumn(grid, "種別", 65);
            AddColumn(grid, "表示名", 200);
            AddColumn(grid, "メールアドレス", 370);
            AddColumn(grid, "配布リスト", 120);
            foreach (var recipient in recipients)
                grid.Rows.Add(recipient.Kind, recipient.Name, recipient.Address,
                    recipient.IsGroup ? "グループ" : "");
            layout.Controls.Add(grid, 0, 1);

            var detail = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AccessibleName = "選択した送信先の全文",
                Margin = new Padding(0, 8, 0, 8)
            };
            Action showDetails = () =>
            {
                if (grid.CurrentRow == null) return;
                var recipient = recipients[grid.CurrentRow.Index];
                detail.Text = recipient.Kind + "\r\n" + recipient.Name + "\r\n" + recipient.Address;
            };
            grid.SelectionChanged += (sender, args) => showDetails();
            showDetails();
            layout.Controls.Add(detail, 0, 2);

            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 12),
                Text = recipients.Any(r => r.IsGroup)
                    ? "グループは宛先アドレスを表示しています。内部メンバーは展開していません。"
                    : "キャンセルすると、メールを送信せず編集画面に戻ります。"
            }, 0, 3);

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var cancel = new Button { Text = "キャンセル", AutoSize = true, DialogResult = DialogResult.Cancel };
            var send = new Button { Text = "送信する", AutoSize = true, DialogResult = DialogResult.OK };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(send);
            layout.Controls.Add(buttons, 0, 4);
            CancelButton = cancel;
            // Initial Enter/Esc cancels. Sending requires deliberately selecting Send.
            AcceptButton = cancel;
            Shown += (sender, args) => cancel.Focus();
        }

        private static void AddColumn(DataGridView grid, string name, int width)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = name,
                Width = width,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }
    }
}
