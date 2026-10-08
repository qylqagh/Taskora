using System;
using System.Drawing;
using System.Windows.Forms;
using Taskora.БазаДанных;
using Taskora.Логика;

namespace Taskora.Формы
{
    public class AdminUsersForm : Form
    {
        private readonly DataGridView grid = new DataGridView();

        public AdminUsersForm()
        {
            InitializeUi();
            LoadUsers();
        }

        private void InitializeUi()
        {
            Icon = Ui.LoadIcon();
            Text = "Taskora — Пользователи";
            ClientSize = new Size(950, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.Background;
            Font = Ui.GetFont(10);

            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White
            };

            Label title = Ui.CreateLabel("Пользователи", 16, FontStyle.Bold, Ui.Text);
            title.Dock = DockStyle.Left;
            title.Padding = new Padding(20, 22, 10, 0);

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(0, 18, 20, 0)
            };

            Button addButton = Ui.CreateButton("+ Добавить", Ui.Primary, new Size(130, 36));
            addButton.Margin = new Padding(0, 0, 8, 0);
            addButton.Click += AddButton_Click;

            Button editButton = Ui.CreateButton("Изменить", ColorTranslator.FromHtml("#3B82F6"), new Size(110, 36));
            editButton.Margin = new Padding(0, 0, 8, 0);
            editButton.Click += EditButton_Click;

            Button deleteButton = Ui.CreateButton("Удалить", Ui.Danger, new Size(100, 36));
            deleteButton.Margin = new Padding(0, 0, 8, 0);
            deleteButton.Click += DeleteButton_Click;

            Button refreshButton = Ui.CreateOutlineButton("Обновить", Ui.Primary, new Size(110, 36));
            refreshButton.Click += (s, e) => LoadUsers();

            flow.Controls.Add(addButton);
            flow.Controls.Add(editButton);
            flow.Controls.Add(deleteButton);
            flow.Controls.Add(refreshButton);

            topPanel.Controls.Add(flow);
            topPanel.Controls.Add(title);

            RoundedPanel gridPanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };

            Ui.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;

            gridPanel.Controls.Add(grid);

            Controls.Add(gridPanel);
            Controls.Add(topPanel);
        }

        private void LoadUsers()
        {
            try
            {
                grid.DataSource = UserService.GetAll();
                ConfigureGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureGrid()
        {
            SetColumn("id", false);
            SetColumn("имя", true, "Имя", 35);
            SetColumn("логин", true, "Логин", 30);
            SetColumn("роль", true, "Роль", 35);
        }

        private void SetColumn(string name, bool visible, string header = null, int fillWeight = 0)
        {
            if (!grid.Columns.Contains(name))
                return;

            DataGridViewColumn column = grid.Columns[name];

            column.Visible = visible;

            if (!string.IsNullOrWhiteSpace(header))
                column.HeaderText = header;

            if (visible && fillWeight > 0)
                column.FillWeight = fillWeight;
        }

        private int GetSelectedUserId()
        {
            if (grid.CurrentRow == null)
                return -1;

            if (!grid.Columns.Contains("id"))
                return -1;

            object value = grid.CurrentRow.Cells["id"].Value;

            if (value == null || value == DBNull.Value)
                return -1;

            return Convert.ToInt32(value);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            using UserForm form = new UserForm(0);

            if (form.ShowDialog(this) == DialogResult.OK)
                LoadUsers();
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            int id = GetSelectedUserId();

            if (id < 0)
            {
                MessageBox.Show(this, "Выберите пользователя.", "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using UserForm form = new UserForm(id);

                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            int id = GetSelectedUserId();

            if (id < 0)
            {
                MessageBox.Show(this, "Выберите пользователя.", "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                "Вы действительно хотите удалить выбранного пользователя?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            try
            {
                UserService.Delete(id, Session.CurrentUser.Id);
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}