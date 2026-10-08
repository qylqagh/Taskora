using System;
using System.Drawing;
using System.Windows.Forms;
using Taskora.БазаДанных;
using Taskora.Логика;

namespace Taskora.Формы
{
    public class MainForm : Form
    {
        private readonly DataGridView grid = new DataGridView();

        private TextBox searchBox;
        private ComboBox statusBox;
        private ComboBox categoryBox;

        private Panel topPanel;
        private Button logoutButton;
        private Button usersButton;

        private bool suppressCategoryChanged = false;

        public MainForm()
        {
            InitializeUi();
            LoadStatuses();
            LoadCategories();
            LoadTasks();
        }

        private void InitializeUi()
        {
            Icon = Ui.LoadIcon();
            Text = "Taskora — Менеджер задач";
            ClientSize = new Size(1150, 700);
            MinimumSize = new Size(980, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ui.Background;
            Font = Ui.GetFont(10);

            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.White
            };

            topPanel.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Ui.Border), 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);

            Label appName = Ui.CreateLabel("Taskora", 18, FontStyle.Bold, Ui.Primary, false);
            Size appNameSize = TextRenderer.MeasureText(appName.Text, appName.Font);
            appName.Size = appNameSize;
            appName.Location = new Point(20, 12);

            Label userLabel = Ui.CreateLabel(
                $"Привет, {Session.CurrentUser.Имя} · {(Session.IsAdmin ? "Администратор" : "Пользователь")}",
                9,
                FontStyle.Regular,
                Ui.Muted,
                false
            );
            Size userLabelSize = TextRenderer.MeasureText(userLabel.Text, userLabel.Font);
            userLabel.Size = userLabelSize;
            userLabel.Location = new Point(22, appName.Bottom + 6);

            topPanel.Height = userLabel.Bottom + 12;

            FlowLayoutPanel topButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, (topPanel.Height - 38) / 2, 20, 0)
            };

            logoutButton = Ui.CreateButton("Выйти", Ui.Danger, new Size(90, 38));
            logoutButton.Margin = new Padding(10, 0, 0, 0);
            logoutButton.Click += (s, e) => Application.Restart();

            if (Session.IsAdmin)
            {
                usersButton = Ui.CreateButton("Пользователи", Ui.Primary, new Size(140, 38));
                usersButton.Margin = new Padding(0, 0, 0, 0);
                usersButton.Click += UsersButton_Click;
                topButtons.Controls.Add(usersButton);
            }

            topButtons.Controls.Add(logoutButton);

            topPanel.Controls.Add(topButtons);
            topPanel.Controls.Add(appName);
            topPanel.Controls.Add(userLabel);

            FlowLayoutPanel toolPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(20, 14, 20, 12),
                BackColor = Ui.Background
            };

            searchBox = Ui.CreateTextBox(240);
            searchBox.PlaceholderText = "Поиск задачи...";
            searchBox.Margin = new Padding(0, 2, 10, 2);
            searchBox.TextChanged += SearchBox_TextChanged;

            statusBox = Ui.CreateCombo(170);
            statusBox.Margin = new Padding(0, 0, 10, 0);
            statusBox.SelectedIndexChanged += StatusBox_SelectedIndexChanged;

            categoryBox = Ui.CreateCombo(190);
            categoryBox.Margin = new Padding(0, 0, 10, 0);
            categoryBox.SelectedIndexChanged += CategoryBox_SelectedIndexChanged;

            Button addButton = Ui.CreateButton("+ Новая задача", Ui.Primary, new Size(150, 36));
            addButton.Margin = new Padding(0, 0, 8, 0);
            addButton.Click += AddButton_Click;

            Button editButton = Ui.CreateButton("Изменить", ColorTranslator.FromHtml("#3B82F6"), new Size(110, 36));
            editButton.Margin = new Padding(0, 0, 8, 0);
            editButton.Click += EditButton_Click;

            Button completeButton = Ui.CreateButton("Выполнено", Ui.Success, new Size(120, 36));
            completeButton.Margin = new Padding(0, 0, 8, 0);
            completeButton.Click += CompleteButton_Click;

            Button deleteButton = Ui.CreateButton("Удалить", Ui.Danger, new Size(100, 36));
            deleteButton.Margin = new Padding(0, 0, 0, 0);
            deleteButton.Click += DeleteButton_Click;

            toolPanel.Controls.Add(searchBox);
            toolPanel.Controls.Add(statusBox);
            toolPanel.Controls.Add(categoryBox);
            toolPanel.Controls.Add(addButton);
            toolPanel.Controls.Add(editButton);
            toolPanel.Controls.Add(completeButton);
            toolPanel.Controls.Add(deleteButton);

            RoundedPanel gridPanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };

            Ui.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;

            gridPanel.Controls.Add(grid);

            Controls.Add(gridPanel);
            Controls.Add(toolPanel);
            Controls.Add(topPanel);
        }

        private void LoadStatuses()
        {
            statusBox.Items.Clear();
            statusBox.Items.Add("Все статусы");
            statusBox.Items.AddRange(TaskService.Statuses);
            statusBox.SelectedIndex = 0;
        }

        private void LoadCategories()
        {
            suppressCategoryChanged = true;

            string selected = categoryBox.SelectedItem?.ToString();

            categoryBox.Items.Clear();
            categoryBox.Items.Add("Все категории");

            try
            {
                foreach (string category in TaskService.GetCategories(Session.CurrentUser.Id, Session.IsAdmin))
                {
                    categoryBox.Items.Add(category);
                }
            }
            catch
            {

            }

            if (selected != null && categoryBox.Items.Contains(selected))
                categoryBox.SelectedItem = selected;
            else
                categoryBox.SelectedIndex = 0;

            suppressCategoryChanged = false;
        }

        private void LoadTasks()
        {
            try
            {
                string search = searchBox?.Text.Trim() ?? "";
                string status = statusBox?.SelectedItem?.ToString() ?? "";
                string category = categoryBox?.SelectedItem?.ToString() ?? "";

                var table = TaskService.GetTasks(
                    Session.CurrentUser.Id,
                    Session.IsAdmin,
                    search,
                    status,
                    category
                );

                grid.DataSource = table;

                ConfigureGrid();
                ColorizeGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureGrid()
        {
            SetColumn("id", false);
            SetColumn("описание", false);
            SetColumn("дата_создания", false);

            SetColumn("название", true, "Название", 28);
            SetColumn("категория", true, "Категория", 15);
            SetColumn("приоритет", true, "Приоритет", 12);
            SetColumn("срок", true, "Срок", 12);
            SetColumn("статус", true, "Статус", 14);

            if (Session.IsAdmin)
                SetColumn("автор", true, "Автор", 19);
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

        private void ColorizeGrid()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string status = grid.Columns.Contains("статус")
                    ? row.Cells["статус"].Value?.ToString() ?? ""
                    : "";

                string due = grid.Columns.Contains("срок")
                    ? row.Cells["срок"].Value?.ToString() ?? ""
                    : "";

                string priority = grid.Columns.Contains("приоритет")
                    ? row.Cells["приоритет"].Value?.ToString() ?? ""
                    : "";

                if (status == "Выполнена")
                    row.DefaultCellStyle.ForeColor = Ui.Success;
                else if (status == "Отменена")
                    row.DefaultCellStyle.ForeColor = Ui.Muted;

                if (grid.Columns.Contains("срок") && DateTime.TryParse(due, out DateTime date))
                {
                    if (date.Date < DateTime.Today && status != "Выполнена" && status != "Отменена")
                        row.Cells["срок"].Style.ForeColor = Ui.Danger;
                }

                if (grid.Columns.Contains("приоритет"))
                {
                    if (priority == "Высокий")
                        row.Cells["приоритет"].Style.ForeColor = Ui.Danger;
                    else if (priority == "Средний")
                        row.Cells["приоритет"].Style.ForeColor = Ui.Warning;
                    else
                        row.Cells["приоритет"].Style.ForeColor = Ui.Muted;
                }
            }
        }

        private int GetSelectedTaskId()
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
            using TaskForm form = new TaskForm(0);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadCategories();
                LoadTasks();
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            int id = GetSelectedTaskId();

            if (id < 0)
            {
                ShowInfo("Выберите задачу.");
                return;
            }

            try
            {
                using TaskForm form = new TaskForm(id);

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    LoadCategories();
                    LoadTasks();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void CompleteButton_Click(object sender, EventArgs e)
        {
            int id = GetSelectedTaskId();

            if (id < 0)
            {
                ShowInfo("Выберите задачу.");
                return;
            }

            try
            {
                TaskService.SetStatus(id, Session.CurrentUser.Id, Session.IsAdmin, "Выполнена");
                LoadTasks();
                ShowSuccess("Задача отмечена как выполненная.");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            int id = GetSelectedTaskId();

            if (id < 0)
            {
                ShowInfo("Выберите задачу.");
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                "Вы действительно хотите удалить эту задачу?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            try
            {
                TaskService.Delete(id, Session.CurrentUser.Id, Session.IsAdmin);
                LoadCategories();
                LoadTasks();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void UsersButton_Click(object sender, EventArgs e)
        {
            using AdminUsersForm form = new AdminUsersForm();
            form.ShowDialog(this);
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            LoadTasks();
        }

        private void StatusBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTasks();
        }

        private void CategoryBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!suppressCategoryChanged)
                LoadTasks();
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(this, message, "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowSuccess(string message)
        {
            MessageBox.Show(this, message, "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "Taskora", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}